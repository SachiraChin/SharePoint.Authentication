using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
using Microsoft.IdentityModel.S2S.Tokens;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.EventReceivers;
using SharePoint.Authentication.ACS.Tokens;
using AudienceRestriction = Microsoft.IdentityModel.Tokens.AudienceRestriction;
using AudienceUriValidationFailedException = Microsoft.IdentityModel.Tokens.AudienceUriValidationFailedException;
using SecurityTokenHandlerConfiguration = Microsoft.IdentityModel.Tokens.SecurityTokenHandlerConfiguration;

namespace SharePoint.Authentication.ACS
{
    public class TokenHelper
    {
        #region public fields

        /// <summary>
        /// SharePoint principal.
        /// </summary>
        public const string SharePointPrincipal = "00000003-0000-0ff1-ce00-000000000000";

        /// <summary>
        /// Lifetime of HighTrust access token, 12 hours.
        /// </summary>
        public readonly TimeSpan HighTrustAccessTokenLifetime = TimeSpan.FromHours(12.0);

        #endregion public fields

        #region public methods

        /// <summary>
        /// Retrieves the context token string from the specified request by looking for well-known parameter names in the 
        /// POSTed form parameters and the querystring. Returns null if no context token is found.
        /// </summary>
        /// <param name="request">HttpRequest in which to look for a context token</param>
        /// <returns>The context token string</returns>
        public string GetContextTokenFromRequest(HttpRequest request)
        {
            return GetContextTokenFromRequest(new HttpRequestWrapper(request));
        }

        /// <summary>
        /// Retrieves the context token string from the specified request by looking for well-known parameter names in the 
        /// POSTed form parameters and the querystring. Returns null if no context token is found.
        /// </summary>
        /// <param name="request">HttpRequest in which to look for a context token</param>
        /// <returns>The context token string</returns>
        public string GetContextTokenFromRequest(HttpRequestBase request)
        {
            string[] paramNames = { "AppContext", "AppContextToken", "AccessToken", "SPAppToken" };
            foreach (string paramName in paramNames)
            {
                if (!string.IsNullOrEmpty(request.Form[paramName]))
                {
                    return request.Form[paramName];
                }
                if (!string.IsNullOrEmpty(request.QueryString[paramName]))
                {
                    return request.QueryString[paramName];
                }
            }
            return null;
        }

        /// <summary>
        /// Validate that a specified context token string is intended for this application based on the parameters 
        /// specified in web.config. Parameters used from web.config used for validation include ClientId, 
        /// HostedAppHostNameOverride, HostedAppHostName, ClientSecret, and Realm (if it is specified). If HostedAppHostNameOverride is present,
        /// it will be used for validation. Otherwise, if the <paramref name="appHostName"/> is not 
        /// null, it is used for validation instead of the web.config's HostedAppHostName. If the token is invalid, an 
        /// exception is thrown. If the token is valid, TokenHelper's  STS metadata url is updated based on the token contents
        /// and a JsonWebSecurityToken based on the context token is returned.
        /// </summary>
        /// <param name="contextTokenString">The context token to validate</param>
        /// <param name="appHostName">The URL authority, consisting of  Domain Name System (DNS) host name or IP address and the port number, to use for token audience validation.
        /// If null, HostedAppHostName web.config setting is used instead. HostedAppHostNameOverride web.config setting, if present, will be used 
        /// for validation instead of <paramref name="appHostName"/> .</param>
        /// <returns>A JsonWebSecurityToken based on the context token.</returns>
        public SharePointContextToken ReadAndValidateContextToken(string contextTokenString, string appHostName = null)
        {
            JsonWebSecurityTokenHandler tokenHandler = CreateJsonWebSecurityTokenHandler();
            SecurityToken securityToken = tokenHandler.ReadToken(contextTokenString);
            JsonWebSecurityToken jsonToken = securityToken as JsonWebSecurityToken;
            SharePointContextToken token = SharePointContextToken.Create(jsonToken);

            string stsAuthority = (new Uri(token.SecurityTokenServiceUri)).Authority;
            int firstDot = stsAuthority.IndexOf('.');

            GlobalEndPointPrefix = stsAuthority.Substring(0, firstDot);
            AcsHostUrl = stsAuthority.Substring(firstDot + 1);

            tokenHandler.ValidateToken(jsonToken);

            string[] acceptableAudiences;
            if (!String.IsNullOrEmpty(_authenticationParameters.HostedAppHostNameOverride))
            {
                acceptableAudiences = _authenticationParameters.HostedAppHostNameOverride.Split(';');
            }
            else if (appHostName == null)
            {
                acceptableAudiences = new[] { _authenticationParameters.HostedAppHostName };
            }
            else
            {
                acceptableAudiences = new[] { appHostName };
            }

            bool validationSuccessful = false;
            string realm = _authenticationParameters.Realm ?? token.Realm;
            foreach (var audience in acceptableAudiences)
            {
                string principal = GetFormattedPrincipal(_authenticationParameters.ClientId, audience, realm);
                if (StringComparer.OrdinalIgnoreCase.Equals(token.Audience, principal))
                {
                    validationSuccessful = true;
                    break;
                }
            }

            if (!validationSuccessful)
            {
                throw new AudienceUriValidationFailedException(
                    String.Format(CultureInfo.CurrentCulture,
                    "\"{0}\" is not the intended audience \"{1}\"", String.Join(";", acceptableAudiences), token.Audience));
            }

            return token;
        }

        /// <summary>
        /// Retrieves an access token from ACS to call the source of the specified context token at the specified 
        /// targetHost. The targetHost must be registered for the principal that sent the context token.
        /// </summary>
        /// <param name="contextToken">Context token issued by the intended access token audience</param>
        /// <param name="targetHost">Url authority of the target principal</param>
        /// <returns>An access token with an audience matching the context token's source</returns>
        public OAuth2AccessTokenResponse GetAccessToken(SharePointContextToken contextToken, string targetHost)
        {
            string targetPrincipalName = contextToken.TargetPrincipalName;

            // Extract the refreshToken from the context token
            string refreshToken = contextToken.RefreshToken;

            if (String.IsNullOrEmpty(refreshToken))
            {
                return null;
            }

            string targetRealm = _authenticationParameters.Realm ?? contextToken.Realm;

            return GetAccessToken(refreshToken,
                                  targetPrincipalName,
                                  targetHost,
                                  targetRealm);
        }

        /// <summary>
        /// Uses the specified authorization code to retrieve an access token from ACS to call the specified principal 
        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
        /// null, the "Realm" setting in web.config will be used instead.
        /// </summary>
        /// <param name="authorizationCode">Authorization code to exchange for access token</param>
        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
        /// <param name="targetHost">Url authority of the target principal</param>
        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
        /// <param name="redirectUri">Redirect URI registered for this add-in</param>
        /// <returns>An access token with an audience of the target principal</returns>
        public OAuth2AccessTokenResponse GetAccessToken(
            string authorizationCode,
            string targetPrincipalName,
            string targetHost,
            string targetRealm,
            Uri redirectUri)
        {
            if (targetRealm == null)
            {
                targetRealm = _authenticationParameters.Realm;
            }

            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
            string clientId = GetFormattedPrincipal(_authenticationParameters.ClientId, null, targetRealm);

            // Create request for token. The RedirectUri is null here.  This will fail if redirect uri is registered
            OAuth2AccessTokenRequest oauth2Request =
                OAuth2MessageFactory.CreateAccessTokenRequestWithAuthorizationCode(
                    clientId,
                    _authenticationParameters.ClientSecret,
                    authorizationCode,
                    redirectUri,
                    resource);

            // Get token
            OAuth2S2SClient client = new OAuth2S2SClient();
            OAuth2AccessTokenResponse oauth2Response;
            try
            {
                oauth2Response =
                    client.Issue(GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
            }
            catch (RequestFailedException)
            {
                if (!string.IsNullOrEmpty(_authenticationParameters.SecondaryClientSecret))
                {
                    oauth2Request =
                    OAuth2MessageFactory.CreateAccessTokenRequestWithAuthorizationCode(
                        clientId,
                        _authenticationParameters.SecondaryClientSecret,
                        authorizationCode,
                        redirectUri,
                        resource);

                    oauth2Response =
                        client.Issue(GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
                }
                else
                {
                    throw;
                }
            }
            catch (WebException wex)
            {
                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
                {
                    string responseText = sr.ReadToEnd();
                    throw new WebException(wex.Message + " - " + responseText, wex);
                }
            }

            return oauth2Response;
        }

        /// <summary>
        /// Uses the specified refresh token to retrieve an access token from ACS to call the specified principal 
        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
        /// null, the "Realm" setting in web.config will be used instead.
        /// </summary>
        /// <param name="refreshToken">Refresh token to exchange for access token</param>
        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
        /// <param name="targetHost">Url authority of the target principal</param>
        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
        /// <returns>An access token with an audience of the target principal</returns>
        public OAuth2AccessTokenResponse GetAccessToken(
            string refreshToken,
            string targetPrincipalName,
            string targetHost,
            string targetRealm)
        {
            if (targetRealm == null)
            {
                targetRealm = _authenticationParameters.Realm;
            }

            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
            string clientId = GetFormattedPrincipal(_authenticationParameters.ClientId, null, targetRealm);

            OAuth2AccessTokenRequest oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithRefreshToken(clientId, _authenticationParameters.ClientSecret, refreshToken, resource);

            // Get token
            OAuth2S2SClient client = new OAuth2S2SClient();
            OAuth2AccessTokenResponse oauth2Response;
            try
            {
                oauth2Response =
                    client.Issue(GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
            }
            catch (RequestFailedException)
            {
                if (!string.IsNullOrEmpty(_authenticationParameters.SecondaryClientSecret))
                {
                    oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithRefreshToken(clientId, _authenticationParameters.SecondaryClientSecret, refreshToken, resource);
                    oauth2Response =
                        client.Issue(GetStsUrl(targetRealm), oauth2Request) as OAuth2AccessTokenResponse;
                }
                else
                {
                    throw;
                }
            }
            catch (WebException wex)
            {
                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
                {
                    string responseText = sr.ReadToEnd();
                    throw new WebException(wex.Message + " - " + responseText, wex);
                }
            }

            return oauth2Response;
        }

        /// <summary>
        /// Retrieves an app-only access token from ACS to call the specified principal 
        /// at the specified targetHost. The targetHost must be registered for target principal.  If specified realm is 
        /// null, the "Realm" setting in web.config will be used instead.
        /// </summary>
        /// <param name="targetPrincipalName">Name of the target principal to retrieve an access token for</param>
        /// <param name="targetHost">Url authority of the target principal</param>
        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
        /// <returns>An access token with an audience of the target principal</returns>
        public OAuth2AccessTokenResponse GetAppOnlyAccessToken(
            string targetPrincipalName,
            string targetHost,
            string targetRealm)
        {

            if (targetRealm == null)
            {
                targetRealm = _authenticationParameters.Realm;
            }

            string resource = GetFormattedPrincipal(targetPrincipalName, targetHost, targetRealm);
            string clientId = GetFormattedPrincipal(_authenticationParameters.ClientId, _authenticationParameters.HostedAppHostName, targetRealm);

            OAuth2AccessTokenRequest oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithClientCredentials(clientId, _authenticationParameters.ClientSecret, resource);
            oauth2Request.Resource = resource;

            // Get token
            OAuth2S2SClient client = new OAuth2S2SClient();

            OAuth2AccessTokenResponse oauth2Response;
            var stsUrl = GetStsUrl(targetRealm);
            try
            {
                oauth2Response = client.Issue(stsUrl, oauth2Request) as OAuth2AccessTokenResponse;
            }
            catch (RequestFailedException)
            {
                if (string.IsNullOrEmpty(_authenticationParameters.SecondaryClientSecret))
                {
                    // throw new Exception($"targetPrincipalName={targetPrincipalName},targetHost={targetHost},targetRealm={targetRealm},stsUrl={stsUrl},clientId={clientId.GetLogSafeString()},resource={resource},clientSecret={_authenticationParameters.ClientSecret.GetLogSafeString()}", e);
                    throw new Exception("Secondary client secret does not exists");
                }

                try
                {
                    oauth2Request = OAuth2MessageFactory.CreateAccessTokenRequestWithClientCredentials(clientId, _authenticationParameters.SecondaryClientSecret, resource);
                    oauth2Request.Resource = resource;

                    oauth2Response = client.Issue(stsUrl, oauth2Request) as OAuth2AccessTokenResponse;
                }
                catch (Exception e2)
                {
                    // throw new Exception($"targetPrincipalName={targetPrincipalName},targetHost={targetHost},targetRealm={targetRealm},stsUrl={stsUrl},clientId={clientId.GetLogSafeString()},resource={resource},secondaryClientSecret={_authenticationParameters.SecondaryClientSecret.GetLogSafeString()}", e2);
                    throw e2;
                }
            }
            catch (WebException wex)
            {
                using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream()))
                {
                    string responseText = sr.ReadToEnd();
                    throw new WebException(wex.Message + " - " + responseText, wex);
                }
            }

            return oauth2Response;
        }

        /// <summary>
        /// Creates a client context based on the properties of a remote event receiver
        /// </summary>
        /// <param name="properties">Properties of a remote event receiver</param>
        /// <returns>A ClientContext ready to call the web where the event originated</returns>
        public ClientContext CreateRemoteEventReceiverClientContext(SPRemoteEventProperties properties)
        {
            Uri sharepointUrl;
            if (properties.ListEventProperties != null)
            {
                sharepointUrl = new Uri(properties.ListEventProperties.WebUrl);
            }
            else if (properties.ItemEventProperties != null)
            {
                sharepointUrl = new Uri(properties.ItemEventProperties.WebUrl);
            }
            else if (properties.WebEventProperties != null)
            {
                sharepointUrl = new Uri(properties.WebEventProperties.FullUrl);
            }
            else
            {
                return null;
            }

            if (IsHighTrustApp())
            {
                return GetS2SClientContextWithWindowsIdentity(sharepointUrl, null);
            }

            return CreateAcsClientContextForUrl(properties, sharepointUrl);
        }

        /// <summary>
        /// Creates a client context based on the properties of an add-in event
        /// </summary>
        /// <param name="properties">Properties of an add-in event</param>
        /// <param name="useAppWeb">True to target the app web, false to target the host web</param>
        /// <returns>A ClientContext ready to call the app web or the parent web</returns>
        public ClientContext CreateAppEventClientContext(SPRemoteEventProperties properties, bool useAppWeb)
        {
            if (properties.AppEventProperties == null)
            {
                return null;
            }

            Uri sharepointUrl = useAppWeb ? properties.AppEventProperties.AppWebFullUrl : properties.AppEventProperties.HostWebFullUrl;
            if (IsHighTrustApp())
            {
                return GetS2SClientContextWithWindowsIdentity(sharepointUrl, null);
            }

            return CreateAcsClientContextForUrl(properties, sharepointUrl);
        }

        /// <summary>
        /// Retrieves an access token from ACS using the specified authorization code, and uses that access token to 
        /// create a client context
        /// </summary>
        /// <param name="targetUrl">Url of the target SharePoint site</param>
        /// <param name="authorizationCode">Authorization code to use when retrieving the access token from ACS</param>
        /// <param name="redirectUri">Redirect URI registered for this add-in</param>
        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
        public ClientContext GetClientContextWithAuthorizationCode(
            string targetUrl,
            string authorizationCode,
            Uri redirectUri)
        {
            return GetClientContextWithAuthorizationCode(targetUrl, SharePointPrincipal, authorizationCode, GetRealmFromTargetUrl(new Uri(targetUrl)), redirectUri);
        }

        /// <summary>
        /// Retrieves an access token from ACS using the specified authorization code, and uses that access token to 
        /// create a client context
        /// </summary>
        /// <param name="targetUrl">Url of the target SharePoint site</param>
        /// <param name="targetPrincipalName">Name of the target SharePoint principal</param>
        /// <param name="authorizationCode">Authorization code to use when retrieving the access token from ACS</param>
        /// <param name="targetRealm">Realm to use for the access token's nameid and audience</param>
        /// <param name="redirectUri">Redirect URI registered for this add-in</param>
        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
        public ClientContext GetClientContextWithAuthorizationCode(
            string targetUrl,
            string targetPrincipalName,
            string authorizationCode,
            string targetRealm,
            Uri redirectUri)
        {
            Uri targetUri = new Uri(targetUrl);

            string accessToken =
                GetAccessToken(authorizationCode, targetPrincipalName, targetUri.Authority, targetRealm, redirectUri).AccessToken;

            return GetClientContextWithAccessToken(targetUrl, accessToken);
        }

        /// <summary>
        /// Uses the specified access token to create a client context
        /// </summary>
        /// <param name="targetUrl">Url of the target SharePoint site</param>
        /// <param name="accessToken">Access token to be used when calling the specified targetUrl</param>
        /// <returns>A ClientContext ready to call targetUrl with the specified access token</returns>
        public ClientContext GetClientContextWithAccessToken(string targetUrl, string accessToken)
        {
            ClientContext clientContext = new ClientContext(targetUrl);

            clientContext.AuthenticationMode = ClientAuthenticationMode.Anonymous;
            clientContext.FormDigestHandlingEnabled = false;
            clientContext.ExecutingWebRequest +=
                delegate (object oSender, WebRequestEventArgs webRequestEventArgs)
                {
                    webRequestEventArgs.WebRequestExecutor.RequestHeaders["Authorization"] =
                        "Bearer " + accessToken;
                };

            return clientContext;
        }

        /// <summary>
        /// Retrieves an access token from ACS using the specified context token, and uses that access token to create
        /// a client context
        /// </summary>
        /// <param name="targetUrl">Url of the target SharePoint site</param>
        /// <param name="contextTokenString">Context token received from the target SharePoint site</param>
        /// <param name="appHostUrl">Url authority of the hosted add-in.  If this is null, the value in the HostedAppHostName
        /// of web.config will be used instead</param>
        /// <returns>A ClientContext ready to call targetUrl with a valid access token</returns>
        public ClientContext GetClientContextWithContextToken(
            string targetUrl,
            string contextTokenString,
            string appHostUrl)
        {
            SharePointContextToken contextToken = ReadAndValidateContextToken(contextTokenString, appHostUrl);

            Uri targetUri = new Uri(targetUrl);

            string accessToken = GetAccessToken(contextToken, targetUri.Authority).AccessToken;

            return GetClientContextWithAccessToken(targetUrl, accessToken);
        }

        /// <summary>
        /// Returns the SharePoint url to which the add-in should redirect the browser to request consent and get back
        /// an authorization code.
        /// </summary>
        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
        /// <param name="scope">Space-delimited permissions to request from the SharePoint site in "shorthand" format 
        /// (e.g. "Web.Read Site.Write")</param>
        /// <returns>Url of the SharePoint site's OAuth authorization page</returns>
        public string GetAuthorizationUrl(string contextUrl, string scope)
        {
            return string.Format(
                "{0}{1}?IsDlg=1&client_id={2}&scope={3}&response_type=code",
                EnsureTrailingSlash(contextUrl),
                AuthorizationPage,
                _authenticationParameters.ClientId,
                scope);
        }

        /// <summary>
        /// Returns the SharePoint url to which the add-in should redirect the browser to request consent and get back
        /// an authorization code.
        /// </summary>
        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
        /// <param name="scope">Space-delimited permissions to request from the SharePoint site in "shorthand" format
        /// (e.g. "Web.Read Site.Write")</param>
        /// <param name="redirectUri">Uri to which SharePoint should redirect the browser to after consent is 
        /// granted</param>
        /// <returns>Url of the SharePoint site's OAuth authorization page</returns>
        public string GetAuthorizationUrl(string contextUrl, string scope, string redirectUri)
        {
            return string.Format(
                "{0}{1}?IsDlg=1&client_id={2}&scope={3}&response_type=code&redirect_uri={4}",
                EnsureTrailingSlash(contextUrl),
                AuthorizationPage,
                _authenticationParameters.ClientId,
                scope,
                redirectUri);
        }

        /// <summary>
        /// Returns the SharePoint url to which the add-in should redirect the browser to request a new context token.
        /// </summary>
        /// <param name="contextUrl">Absolute Url of the SharePoint site</param>
        /// <param name="redirectUri">Uri to which SharePoint should redirect the browser to with a context token</param>
        /// <returns>Url of the SharePoint site's context token redirect page</returns>
        public string GetAppContextTokenRequestUrl(string contextUrl, string redirectUri)
        {
            return string.Format(
                "{0}{1}?client_id={2}&redirect_uri={3}",
                EnsureTrailingSlash(contextUrl),
                RedirectPage,
                _authenticationParameters.ClientId,
                redirectUri);
        }

        /// <summary>
        /// Retrieves an S2S access token signed by the application's private certificate on behalf of the specified 
        /// WindowsIdentity and intended for the SharePoint at the targetApplicationUri. If no Realm is specified in 
        /// web.config, an auth challenge will be issued to the targetApplicationUri to discover it.
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <param name="identity">Windows identity of the user on whose behalf to create the access token</param>
        /// <returns>An access token with an audience of the target principal</returns>
        public string GetS2SAccessTokenWithWindowsIdentity(
            Uri targetApplicationUri,
            WindowsIdentity identity)
        {
            string realm = string.IsNullOrEmpty(_authenticationParameters.Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : _authenticationParameters.Realm;

            JsonWebTokenClaim[] claims = identity != null ? GetClaimsWithWindowsIdentity(identity) : null;

            return GetS2SAccessTokenWithClaims(targetApplicationUri.Authority, realm, claims);
        }

        /// <summary>
        /// Retrieves an S2S client context with an access token signed by the application's private certificate on 
        /// behalf of the specified WindowsIdentity and intended for application at the targetApplicationUri using the 
        /// targetRealm. If no Realm is specified in web.config, an auth challenge will be issued to the 
        /// targetApplicationUri to discover it.
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <param name="identity">Windows identity of the user on whose behalf to create the access token</param>
        /// <returns>A ClientContext using an access token with an audience of the target application</returns>
        public ClientContext GetS2SClientContextWithWindowsIdentity(
            Uri targetApplicationUri,
            WindowsIdentity identity)
        {
            string realm = string.IsNullOrEmpty(_authenticationParameters.Realm) ? GetRealmFromTargetUrl(targetApplicationUri) : _authenticationParameters.Realm;

            JsonWebTokenClaim[] claims = identity != null ? GetClaimsWithWindowsIdentity(identity) : null;

            string accessToken = GetS2SAccessTokenWithClaims(targetApplicationUri.Authority, realm, claims);

            return GetClientContextWithAccessToken(targetApplicationUri.ToString(), accessToken);
        }

        /// <summary>
        /// Get authentication realm from SharePoint
        /// </summary>
        /// <param name="targetApplicationUri">Url of the target SharePoint site</param>
        /// <returns>String representation of the realm GUID</returns>
        public string GetRealmFromTargetUrl(Uri targetApplicationUri)
        {
            WebRequest request = WebRequest.Create(targetApplicationUri + "/_vti_bin/client.svc");
            request.Headers.Add("Authorization: Bearer ");

            try
            {
                using (request.GetResponse())
                {
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    return null;
                }

                string bearerResponseHeader = e.Response.Headers["WWW-Authenticate"];
                if (string.IsNullOrEmpty(bearerResponseHeader))
                {
                    return null;
                }

                const string bearer = "Bearer realm=\"";
                int bearerIndex = bearerResponseHeader.IndexOf(bearer, StringComparison.Ordinal);
                if (bearerIndex < 0)
                {
                    return null;
                }

                int realmIndex = bearerIndex + bearer.Length;

                if (bearerResponseHeader.Length >= realmIndex + 36)
                {
                    string targetRealm = bearerResponseHeader.Substring(realmIndex, 36);

                    Guid realmGuid;

                    if (Guid.TryParse(targetRealm, out realmGuid))
                    {
                        return targetRealm;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Determines if this is a high trust add-in.
        /// </summary>
        /// <returns>True if this is a high trust add-in.</returns>
        public bool IsHighTrustApp()
        {
            return _authenticationParameters.SigningCredentials != null;
        }

        /// <summary>
        /// Ensures that the specified URL ends with '/' if it is not null or empty.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <returns>The url ending with '/' if it is not null or empty.</returns>
        public static string EnsureTrailingSlash(string url)
        {
            if (!string.IsNullOrEmpty(url) && url[url.Length - 1] != '/')
            {
                return url + "/";
            }

            return url;
        }

        #endregion

        #region private fields

        //
        // Configuration Constants
        //        

        private const string AuthorizationPage = "_layouts/15/OAuthAuthorize.aspx";
        private const string RedirectPage = "_layouts/15/AppRedirect.aspx";
        private const string AcsPrincipalName = "00000001-0000-0000-c000-000000000000";
        private const string AcsMetadataEndPointRelativeUrl = "metadata/json/1";
        private const string S2SProtocol = "OAuth2";
        private const string DelegationIssuance = "DelegationIssuance1.0";
        private const string NameIdentifierClaimType = JsonWebTokenConstants.ReservedClaims.NameIdentifier;
        private const string TrustedForImpersonationClaimType = "trustedfordelegation";
        private const string ActorTokenClaimType = JsonWebTokenConstants.ReservedClaims.ActorToken;

        //
        // Environment Constants
        //

        private string GlobalEndPointPrefix = "accounts";
        private string AcsHostUrl = "accesscontrol.windows.net";

        //
        // Hosted add-in configuration
        //
        private readonly IAuthenticationParameters _authenticationParameters;
        //private readonly string ClientId;
        //private readonly string IssuerId;
        //private readonly string HostedAppHostNameOverride;
        //private readonly string HostedAppHostName;
        //private readonly string ClientSecret;
        //private readonly string SecondaryClientSecret;
        //private readonly string Realm;
        //private readonly string ServiceNamespace;

        //private readonly string ClientSigningCertificatePath;
        //private readonly string ClientSigningCertificatePassword;
        //private readonly X509Certificate2 ClientCertificate;
        //private readonly X509SigningCredentials SigningCredentials;

        public TokenHelper(IAuthenticationParameters authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }

        //private TokenHelper(string clientId, string clientSecret, string secondaryClientSecret, string issuerId, string hostedAppHostNameOverride, string hostedAppHostName, string realm, string serviceNamespace,
        //    string clientSigningCertificatePath, string clientSigningCertificatePassword, X509Certificate2 clientCertificate, X509SigningCredentials signingCredentials)
        //{
        //    ClientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientId")) ? WebConfigurationManager.AppSettings.Get("HostedAppName") : WebConfigurationManager.AppSettings.Get("ClientId");
        //    IssuerId = !string.IsNullOrWhiteSpace(issuerId) ? issuerId : string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("IssuerId")) ? ClientId : WebConfigurationManager.AppSettings.Get("IssuerId");
        //    HostedAppHostNameOverride = !string.IsNullOrWhiteSpace(hostedAppHostNameOverride) ? hostedAppHostNameOverride : WebConfigurationManager.AppSettings.Get("HostedAppHostNameOverride");
        //    HostedAppHostName = !string.IsNullOrWhiteSpace(hostedAppHostName) ? hostedAppHostName : WebConfigurationManager.AppSettings.Get("HostedAppHostName");
        //    ClientSecret = !string.IsNullOrWhiteSpace(clientSecret) ? clientSecret : string.IsNullOrEmpty(WebConfigurationManager.AppSettings.Get("ClientSecret")) ? WebConfigurationManager.AppSettings.Get("HostedAppSigningKey") : WebConfigurationManager.AppSettings.Get("ClientSecret");
        //    SecondaryClientSecret = !string.IsNullOrWhiteSpace(secondaryClientSecret) ? secondaryClientSecret : WebConfigurationManager.AppSettings.Get("SecondaryClientSecret");
        //    Realm = !string.IsNullOrWhiteSpace(realm) ? realm : WebConfigurationManager.AppSettings.Get("Realm");
        //    ServiceNamespace = !string.IsNullOrWhiteSpace(serviceNamespace) ? serviceNamespace : WebConfigurationManager.AppSettings.Get("Realm");

        //    ClientSigningCertificatePath = !string.IsNullOrWhiteSpace(clientSigningCertificatePath) ? clientSigningCertificatePath : WebConfigurationManager.AppSettings.Get("ClientSigningCertificatePath");
        //    ClientSigningCertificatePassword = !string.IsNullOrWhiteSpace(clientSigningCertificatePassword) ? clientSigningCertificatePassword : WebConfigurationManager.AppSettings.Get("ClientSigningCertificatePassword");

        //    ClientCertificate = clientCertificate ?? ((string.IsNullOrEmpty(ClientSigningCertificatePath) || string.IsNullOrEmpty(ClientSigningCertificatePassword)) ? null : new X509Certificate2(ClientSigningCertificatePath, ClientSigningCertificatePassword));
        //    SigningCredentials = signingCredentials ?? ((ClientCertificate == null) ? null : new X509SigningCredentials(ClientCertificate, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest));
        //}


        #endregion

        #region private methods

        private ClientContext CreateAcsClientContextForUrl(SPRemoteEventProperties properties, Uri sharepointUrl)
        {
            string contextTokenString = properties.ContextToken;

            if (String.IsNullOrEmpty(contextTokenString))
            {
                return null;
            }

            SharePointContextToken contextToken = ReadAndValidateContextToken(contextTokenString, OperationContext.Current.IncomingMessageHeaders.To.Host);
            string accessToken = GetAccessToken(contextToken, sharepointUrl.Authority).AccessToken;

            return GetClientContextWithAccessToken(sharepointUrl.ToString(), accessToken);
        }

        private string GetAcsMetadataEndpointUrl()
        {
            return Path.Combine(GetAcsGlobalEndpointUrl(), AcsMetadataEndPointRelativeUrl);
        }

        private string GetFormattedPrincipal(string principalName, string hostName, string realm)
        {
            if (!String.IsNullOrEmpty(hostName))
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}/{1}@{2}", principalName, hostName, realm);
            }

            return String.Format(CultureInfo.InvariantCulture, "{0}@{1}", principalName, realm);
        }

        private string GetAcsPrincipalName(string realm)
        {
            return GetFormattedPrincipal(AcsPrincipalName, new Uri(GetAcsGlobalEndpointUrl()).Host, realm);
        }

        private string GetAcsGlobalEndpointUrl()
        {
            return String.Format(CultureInfo.InvariantCulture, "https://{0}.{1}/", GlobalEndPointPrefix, AcsHostUrl);
        }

        private JsonWebSecurityTokenHandler CreateJsonWebSecurityTokenHandler()
        {
            JsonWebSecurityTokenHandler handler = new JsonWebSecurityTokenHandler();
            handler.Configuration = new SecurityTokenHandlerConfiguration();
            handler.Configuration.AudienceRestriction = new AudienceRestriction(AudienceUriMode.Never);
            handler.Configuration.CertificateValidator = X509CertificateValidator.None;

            List<byte[]> securityKeys = new List<byte[]>();
            securityKeys.Add(Convert.FromBase64String(_authenticationParameters.ClientSecret));
            if (!string.IsNullOrEmpty(_authenticationParameters.SecondaryClientSecret))
            {
                securityKeys.Add(Convert.FromBase64String(_authenticationParameters.SecondaryClientSecret));
            }

            List<SecurityToken> securityTokens = new List<SecurityToken>();
            securityTokens.Add(new MultipleSymmetricKeySecurityToken(securityKeys));

            handler.Configuration.IssuerTokenResolver =
                SecurityTokenResolver.CreateDefaultSecurityTokenResolver(
                new ReadOnlyCollection<SecurityToken>(securityTokens),
                false);
            SymmetricKeyIssuerNameRegistry issuerNameRegistry = new SymmetricKeyIssuerNameRegistry();
            foreach (byte[] securitykey in securityKeys)
            {
                issuerNameRegistry.AddTrustedIssuer(securitykey, GetAcsPrincipalName(_authenticationParameters.ServiceNamespace));
            }
            handler.Configuration.IssuerNameRegistry = issuerNameRegistry;
            return handler;
        }

        private string GetS2SAccessTokenWithClaims(
            string targetApplicationHostName,
            string targetRealm,
            IEnumerable<JsonWebTokenClaim> claims)
        {
            return IssueToken(
                _authenticationParameters.ClientId,
                _authenticationParameters.IssuerId,
                targetRealm,
                SharePointPrincipal,
                targetRealm,
                targetApplicationHostName,
                true,
                claims,
                claims == null);
        }

        private JsonWebTokenClaim[] GetClaimsWithWindowsIdentity(WindowsIdentity identity)
        {
            JsonWebTokenClaim[] claims = new JsonWebTokenClaim[]
            {
                new JsonWebTokenClaim(NameIdentifierClaimType, identity.User.Value.ToLower()),
                new JsonWebTokenClaim("nii", "urn:office:idp:activedirectory")
            };
            return claims;
        }

        private string IssueToken(
            string sourceApplication,
            string issuerApplication,
            string sourceRealm,
            string targetApplication,
            string targetRealm,
            string targetApplicationHostName,
            bool trustedForDelegation,
            IEnumerable<JsonWebTokenClaim> claims,
            bool appOnly = false)
        {
            if (null == _authenticationParameters.SigningCredentials)
            {
                throw new InvalidOperationException("SigningCredentials was not initialized");
            }

            #region Actor token

            string issuer = string.IsNullOrEmpty(sourceRealm) ? issuerApplication : string.Format("{0}@{1}", issuerApplication, sourceRealm);
            string nameid = string.IsNullOrEmpty(sourceRealm) ? sourceApplication : string.Format("{0}@{1}", sourceApplication, sourceRealm);
            string audience = string.Format("{0}/{1}@{2}", targetApplication, targetApplicationHostName, targetRealm);

            List<JsonWebTokenClaim> actorClaims = new List<JsonWebTokenClaim>();
            actorClaims.Add(new JsonWebTokenClaim(JsonWebTokenConstants.ReservedClaims.NameIdentifier, nameid));
            if (trustedForDelegation && !appOnly)
            {
                actorClaims.Add(new JsonWebTokenClaim(TrustedForImpersonationClaimType, "true"));
            }

            // Create token
            JsonWebSecurityToken actorToken = new JsonWebSecurityToken(
                issuer: issuer,
                audience: audience,
                validFrom: DateTime.UtcNow,
                validTo: DateTime.UtcNow.Add(HighTrustAccessTokenLifetime),
                signingCredentials: _authenticationParameters.SigningCredentials,
                claims: actorClaims);

            string actorTokenString = new JsonWebSecurityTokenHandler().WriteTokenAsString(actorToken);

            if (appOnly)
            {
                // App-only token is the same as actor token for delegated case
                return actorTokenString;
            }

            #endregion Actor token

            #region Outer token

            List<JsonWebTokenClaim> outerClaims = null == claims ? new List<JsonWebTokenClaim>() : new List<JsonWebTokenClaim>(claims);
            outerClaims.Add(new JsonWebTokenClaim(ActorTokenClaimType, actorTokenString));

            JsonWebSecurityToken jsonToken = new JsonWebSecurityToken(
                nameid, // outer token issuer should match actor token nameid
                audience,
                DateTime.UtcNow,
                DateTime.UtcNow.Add(HighTrustAccessTokenLifetime),
                outerClaims);

            string accessToken = new JsonWebSecurityTokenHandler().WriteTokenAsString(jsonToken);

            #endregion Outer token

            return accessToken;
        }

        #endregion

        #region AcsMetadataParser

        public X509Certificate2 GetAcsSigningCert(string realm)
        {
            JsonMetadataDocument document = GetMetadataDocument(realm);

            if (null != document.keys && document.keys.Count > 0)
            {
                JsonKey signingKey = document.keys[0];

                if (null != signingKey && null != signingKey.keyValue)
                {
                    return new X509Certificate2(Encoding.UTF8.GetBytes(signingKey.keyValue.value));
                }
            }

            throw new Exception("Metadata document does not contain ACS signing certificate.");
        }

        public string GetDelegationServiceUrl(string realm)
        {
            JsonMetadataDocument document = GetMetadataDocument(realm);

            JsonEndpoint delegationEndpoint = document.endpoints.SingleOrDefault(e => e.protocol == DelegationIssuance);

            if (null != delegationEndpoint)
            {
                return delegationEndpoint.location;
            }
            throw new Exception("Metadata document does not contain Delegation Service endpoint Url");
        }

        private JsonMetadataDocument GetMetadataDocument(string realm)
        {
            string acsMetadataEndpointUrlWithRealm = String.Format(CultureInfo.InvariantCulture, "{0}?realm={1}",
                                                                   GetAcsMetadataEndpointUrl(),
                                                                   realm);
            byte[] acsMetadata;
            using (WebClient webClient = new WebClient())
            {

                acsMetadata = webClient.DownloadData(acsMetadataEndpointUrlWithRealm);
            }
            string jsonResponseString = Encoding.UTF8.GetString(acsMetadata);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            JsonMetadataDocument document = serializer.Deserialize<JsonMetadataDocument>(jsonResponseString);

            if (null == document)
            {
                throw new Exception("No metadata document found at the global endpoint " + acsMetadataEndpointUrlWithRealm);
            }

            return document;
        }

        public string GetStsUrl(string realm)
        {
            JsonMetadataDocument document = GetMetadataDocument(realm);

            JsonEndpoint s2sEndpoint = document.endpoints.SingleOrDefault(e => e.protocol == S2SProtocol);

            if (null != s2sEndpoint)
            {
                return s2sEndpoint.location;
            }

            throw new Exception("Metadata document does not contain STS endpoint url");
        }

        private class JsonMetadataDocument
        {
            public string serviceName { get; set; }
            public List<JsonEndpoint> endpoints { get; set; }
            public List<JsonKey> keys { get; set; }
        }

        private class JsonEndpoint
        {
            public string location { get; set; }
            public string protocol { get; set; }
            public string usage { get; set; }
        }

        private class JsonKeyValue
        {
            public string type { get; set; }
            public string value { get; set; }
        }

        private class JsonKey
        {
            public string usage { get; set; }
            public JsonKeyValue keyValue { get; set; }
        }

        #endregion
    }
}
