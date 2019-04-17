using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Dependencies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;
using Newtonsoft.Json;
using SharePoint.Authentication.Caching;
using SharePoint.Authentication.Exceptions;
using SharePoint.Authentication.Owin.Extensions;
using SharePoint.Authentication.Owin.Models;
// ReSharper disable UseUsingVarLocalVariable

namespace SharePoint.Authentication.Owin
{
    public class SharePointAuthenticationHandler : AuthenticationHandler<SharePointAuthenticationOptions>
    {
        public const string SessionCacheMemoryGroupName = "SharePoint.Authentication.SharePointSession";
        private readonly SharePointAuthenticationOptions _sharePointAuthenticationOptions;

        public SharePointAuthenticationHandler(SharePointAuthenticationOptions sharePointAuthenticationOptions)
        {
            _sharePointAuthenticationOptions = sharePointAuthenticationOptions;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var owin = HttpContext.Current.GetOwinContext();

                if (owin.Request.Method == HttpMethod.Options.Method)
                    return new AuthenticationTicket(new ClaimsIdentity(), new AuthenticationProperties());

                if (_sharePointAuthenticationOptions.AllowNonBrowserRequests == false
                    && (!owin.Request.Headers.ContainsKey("User-Agent") || owin.Request.Headers["User-Agent"].ToLower().StartsWith("curl"))
                    && !owin.Request.Headers.ContainsKey("Origin")
                    && !owin.Request.Headers.ContainsKey("Referer"))
                {
                    throw new SharePointAuthenticationException($"Request wasn't sent from a browser. " +
                                                                $"User-Agent: {(owin.Request.Headers.ContainsKey("User-Agent") ? owin.Request.Headers["User-Agent"] : null)}, " +
                                                                $"Origin: {(owin.Request.Headers.ContainsKey("Origin") ? owin.Request.Headers["Origin"] : null)}, " +
                                                                $"Referer: {(owin.Request.Headers.ContainsKey("Referer") ? owin.Request.Headers["Referer"] : null)}, ");
                }

                using (var dependencyScope = _sharePointAuthenticationOptions.DependencyResolver.BeginScope())
                {
                    var cachedSession = await GetCachedSession(dependencyScope, owin);

                    if (cachedSession == null)
                    {
                        throw new SharePointAuthenticationException("Context token is null");
                    }

                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        // Validate the token expiry
                        ValidateLifetime = true,

                        // If you want to allow a certain amount of clock drift, set that here:
                        ClockSkew = _sharePointAuthenticationOptions.ClockSkew,

                        RequireSignedTokens = true,
                    };

                    if (_sharePointAuthenticationOptions.ValidateIssuerSigningKeys)
                    {
                        tokenValidationParameters.ValidateIssuerSigningKey = true;
                        tokenValidationParameters.IssuerSigningKeys = await GetPublicKeysCached(dependencyScope);
                    }

                    if (_sharePointAuthenticationOptions.ValidateIssuer)
                    {
                        tokenValidationParameters.ValidateIssuer = true;
                        tokenValidationParameters.ValidIssuer = cachedSession.Issuer;
                    }

                    if (_sharePointAuthenticationOptions.ValidateAudience)
                    {
                        tokenValidationParameters.ValidateAudience = true;
                        tokenValidationParameters.ValidAudience = cachedSession.Audience;
                    }

                    var principal = handler.ValidateToken(cachedSession.AccessToken, tokenValidationParameters, out var validToken);

                    if (!(validToken is JwtSecurityToken))
                    {
                        throw new SharePointAuthenticationException("Invalid JWT");
                    }

                    if (!(principal.Identity is ClaimsIdentity identity))
                        return new AuthenticationTicket(null, new AuthenticationProperties());

                    await _sharePointAuthenticationOptions.InvokeOnOnAuthenticationHandlerPost(owin, dependencyScope, principal);

                    return new AuthenticationTicket(identity, new AuthenticationProperties());
                }
            }
            catch (Exception ex)
            {
                var msg = JsonConvert.SerializeObject(ex, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                Trace.TraceError($"{msg}");
                return null;
            }
        }

        public static Task<List<SecurityKey>> GetPublicKeysCached(IDependencyScope dependencyScope)
        {
            const string key = "SharePoint.Authentication.AADAccess.PublicKeys";

            var cacheProvider = dependencyScope.Resolve<ICacheProvider>() ?? new MemoryCacheProvider();
            var lockProvider = dependencyScope.Resolve<ILockProvider>() ?? new LockProvider();

            return lockProvider.PerformActionLockedAsync(key, () => cacheProvider.GetAsync(key, GetPublicKeys, 12 * 60, true));
        }

        public static Task<List<SecurityKey>> GetPublicKeys()
        {
            using (var client = new WebClient())
            {
                var openIdConfigStr = client.DownloadString("https://login.microsoftonline.com/common/.well-known/openid-configuration");
                var openIdConfig = JsonConvert.DeserializeObject<AADOpenIdConfig>(openIdConfigStr);
                var jwKeys = client.DownloadString(openIdConfig.jwks_uri);
                var response = JsonConvert.DeserializeObject<AADPublicKeys>(jwKeys);

                var keys = new List<SecurityKey>();
                foreach (var webKey in response.keys)
                {
                    var e = Decode(webKey.e);
                    var n = Decode(webKey.n);

                    var key = new RsaSecurityKey(new RSAParameters { Exponent = e, Modulus = n })
                    {
                        KeyId = webKey.kid
                    };

                    keys.Add(key);
                }

                return Task.FromResult(keys);
            }
        }

        private async Task<CachedSession> GetCachedSession(IDependencyScope dependencyScope, IOwinContext owin)
        {
            var cacheProvider = dependencyScope.Resolve<ICacheProvider>() ?? new MemoryCacheProvider();
            var lockProvider = dependencyScope.Resolve<ILockProvider>() ?? new LockProvider();

            async Task<CachedSession> GetNewAccessToken(Guid sessionId)
            {
                var sharePointSessionProvider = dependencyScope.Resolve<ISharePointSessionProvider>();
                var sharePointSession = await sharePointSessionProvider.GetSharePointSession(sessionId);

                if (sharePointSession == null)
                    return null;

                var lowTrustTokenHelper = dependencyScope.Resolve<LowTrustTokenHelper>();
                var contextTokenObj = lowTrustTokenHelper.ReadAndValidateContextToken(sharePointSession.ContextToken, sharePointSession.ContextTokenAuthority);

                if (contextTokenObj.ValidTo < DateTimeOffset.Now)
                    return null;

                var spHostUrl = new Uri(sharePointSession.SharePointHostWebUrl);
                var accessTokenResponse = lowTrustTokenHelper.GetAccessToken(contextTokenObj, spHostUrl.Authority);

                var cachedSession = new CachedSession()
                {
                    SessionId = sharePointSession.SessionId,
                    AccessToken = accessTokenResponse.AccessToken,
                    SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
                };

                if (_sharePointAuthenticationOptions.ValidateAudience || _sharePointAuthenticationOptions.ValidateIssuer)
                {
                    var realm = lowTrustTokenHelper.GetRealmFromTargetUrl(spHostUrl);
                    if (_sharePointAuthenticationOptions.ValidateAudience)
                    {
                        var audience = lowTrustTokenHelper.GetFormattedPrincipal(TokenHelper.SharePointPrincipal, spHostUrl.Authority, realm);
                        cachedSession.Audience = audience;
                    }

                    if (_sharePointAuthenticationOptions.ValidateIssuer)
                    {
                        var issuer = lowTrustTokenHelper.GetFormattedPrincipal(TokenHelper.AcsPrincipalName, null, realm);
                        cachedSession.Issuer = issuer;
                    }
                }


                if (!_sharePointAuthenticationOptions.InjectCredentialsForHighTrust) return cachedSession;

                var highTrustCredentials = await sharePointSessionProvider.GetHighTrustCredentials(sharePointSession.SharePointHostWebUrl);

                if (highTrustCredentials == null) return cachedSession;

                cachedSession.HighTrustClientId = highTrustCredentials.ClientId;
                cachedSession.HighTrustClientSecret = highTrustCredentials.ClientSecret;

                return cachedSession;
            }

            var cookieValues = GetCookieValues(owin, "session-id");
            foreach (var cookieValue in cookieValues)
            {
                if (!Guid.TryParse(cookieValue, out var sessionId))
                {
                    continue;
                }

                var cacheKey = sessionId.ToString("N");
                var cacheSessionKey = $"{SessionCacheMemoryGroupName}.{cacheKey}";
                var session = await lockProvider.PerformActionLockedAsync(cacheSessionKey,
                    () => cacheProvider.GetAsync(cacheSessionKey, () => GetNewAccessToken(sessionId), _sharePointAuthenticationOptions.TokenCacheDurationInMinutes, false));

                if (string.IsNullOrWhiteSpace(session?.AccessToken)) continue;

                owin.Set("CachedSession", session);

                return session;
            }

            return null;
        }

        private static IEnumerable<string> GetCookieValues(IOwinContext owin, string cookieName)
        {
            var cookieHeaders = owin.Request.Headers.Where(c => c.Key == "Cookie").ToList();
            foreach (var cookieHeader in cookieHeaders)
            {
                var cookieHeaderValues = cookieHeader.Value;
                var val = "";
                foreach (var cookieHeaderValue in cookieHeaderValues)
                {
                    var cookieParts = cookieHeaderValue.Split(';');
                    foreach (var cookie in cookieParts)
                    {
                        if (cookie.Trim().StartsWith($"{cookieName}="))
                        {
                            val = cookie.Trim().Replace($"{cookieName}=", "");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(val))
                    continue;

                yield return val;
            }
        }

        public static byte[] Decode(string arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = arg
                .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                .Replace("_", "/")
                .Replace("-", "+");


            return Convert.FromBase64String(s);
        }
    }
}