using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml;
using Microsoft.Online.SharePoint.TenantAdministration;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.Exceptions;
using SharePoint.Authentication.Owin.Exceptions;
using SharePoint.Authentication.Owin.Extensions;
using SharePoint.Authentication.Owin.Helpers;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.Controllers
{
    public abstract class SharePointLoginController : ApiController
    {
        public virtual string LowTrustLandingPageUrl { get; } = "/";
        public virtual string HighTrustLandingPageUrl { get; } = "/";
        public virtual string HighTrustAppPackageName { get; } = "highTrust.app";
        public virtual string HighTrustLoginPageUrl { get; } = null;

        private readonly LowTrustTokenHelper _lowTrustTokenHelper;
        private readonly HighTrustTokenHelper _highTrustTokenHelper;
        private readonly ISharePointSessionProvider _sharePointSessionProvider;
        private readonly HighTrustAuthenticationParameters _highTrustAuthenticationParameters;

        protected SharePointLoginController(ISharePointSessionProvider sharePointSessionProvider, LowTrustTokenHelper lowTrustTokenHelper, HighTrustTokenHelper highTrustTokenHelper, HighTrustAuthenticationParameters highTrustAuthenticationParameters)
        {
            _sharePointSessionProvider = sharePointSessionProvider;
            _highTrustTokenHelper = highTrustTokenHelper;
            _highTrustAuthenticationParameters = highTrustAuthenticationParameters;
            _lowTrustTokenHelper = lowTrustTokenHelper;
        }

        public virtual async Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            var queryString = Request.GetQueryNameValuePairs().ToList();
            var spHostUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPHostUrl", StringComparison.CurrentCultureIgnoreCase)).Value;
            
            if (spHostUrl == null)
                throw new SharePointHostUrlNotAvailableException();

            var contextToken = _lowTrustTokenHelper.GetContextTokenFromRequest(HttpContext.Current.Request);

            try
            {
                var contextTokenAuthority = HttpContext.Current.Request.Url.Authority;
                var contextTokenObj = _lowTrustTokenHelper.ReadAndValidateContextToken(contextToken, contextTokenAuthority);

                var sessionId = Guid.NewGuid();
                var spAppUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPAppWebUrl", StringComparison.CurrentCultureIgnoreCase)).Value;
                var sharePointSession = new SharePointSession()
                {
                    SessionId = sessionId,
                    SharePointHostWebUrl = spHostUrl,
                    SharePointAppWebUrl = spAppUrl,
                    ContextToken = contextToken,
                    ContextTokenAuthority = contextTokenAuthority,
                };

                await _sharePointSessionProvider.SaveSharePointSession(sessionId, sharePointSession);


                var accessTokenResponse = _lowTrustTokenHelper.GetAccessToken(contextTokenObj, new Uri(spHostUrl).Authority);
                using (var clientContext = _lowTrustTokenHelper.GetClientContextWithAccessToken(spHostUrl, accessTokenResponse.AccessToken))
                {
                    await LowTrustPostAuthenticationAsync(clientContext);
                }

                var callbackResponse = EmbeddedData.Get<string, ISharePointSessionProvider>("SharePoint.Authentication.Owin.Templates.UserLogin.Response.html").Replace("[CallbackUrl]", LowTrustLandingPageUrl);
                var sessionCookie = GetCookieHeader("session-id", sessionId.ToString("N"), Request.RequestUri.Host, contextTokenObj.ValidTo, true, true);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(callbackResponse, Encoding.UTF8, "text/html");
                response.Headers.AddCookies(new[] { sessionCookie, });
                return response;
            }
            catch (Exception )
            {
                var response = Request.CreateResponse(HttpStatusCode.Redirect);
                var redirectUrl = _lowTrustTokenHelper.GetRedirectUrl(HttpContext.Current.Request);
                response.Headers.Location = redirectUrl ?? throw new CanNotRedirectException();
                return response;
            }
        }

        public virtual Task LowTrustPostAuthenticationAsync(ClientContext clientContext)
        {
            return Task.FromResult(false);
        }

        public virtual async Task<HttpResponseMessage> HighTrustLoginAsync()
        {
            var queryString = Request.GetQueryNameValuePairs().ToList();
            var spHostUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPHostUrl", StringComparison.CurrentCultureIgnoreCase)).Value;

            if (spHostUrl == null)
                throw new SharePointHostUrlNotAvailableException();

            HighTrustTokenHelper highTrustTokenHelper;
            if (User.Identity.IsAuthenticated && _highTrustAuthenticationParameters.ClientId != null && _highTrustAuthenticationParameters.ClientSecret != null)
            {
                highTrustTokenHelper = _highTrustTokenHelper;
            }
            else
            {
                var credentials = await _sharePointSessionProvider.GetHighTrustCredentials(spHostUrl);
                highTrustTokenHelper = new HighTrustTokenHelper(new HighTrustAuthenticationParameters()
                {
                    ClientId = credentials.ClientId,
                    ClientSecret = credentials.ClientSecret,
                });
            }

            try
            {
                using var context = await highTrustTokenHelper.GetAppOnlyAuthenticatedContext(spHostUrl);
                context.Load(context.Web);
                await context.ExecuteQueryAsync();

                await HighTrustPostAuthenticationAsync(context);

                var callbackResponse = EmbeddedData.Get<string, ISharePointSessionProvider>("SharePoint.Authentication.Owin.Templates.UserLogin.Response.html").Replace("[CallbackUrl]", HighTrustLandingPageUrl);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(callbackResponse, Encoding.UTF8, "text/html");
                return response;
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized);
            }
        }


        public virtual Task HighTrustPostAuthenticationAsync(ClientContext clientContext)
        {
            return Task.FromResult(false);
        }

        [SuppressMessage("ReSharper", "UseUsingVarLocalVariable")]
        public virtual async Task<HttpResponseMessage> DownloadHighTrustAddInAsync()
        {
            if (this.User.Identity.IsAuthenticated == false)
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            var sharePointHostWebUrl = this.Request.GetSharePointHostWebUrl() ?? throw new SharePointAuthenticationException("SharePoint host url not found.");
            var highTrustCredentials = await _sharePointSessionProvider.GetHighTrustCredentials(sharePointHostWebUrl);

            if (highTrustCredentials == null)
                return Request.CreateResponse(HttpStatusCode.Unauthorized);

            using (var appStream = await GetHighTrustAddInPackage())
            {
                using (var tempStream = new MemoryStream())
                {
                    appStream.CopyTo(tempStream);
                    tempStream.Seek(0, SeekOrigin.Begin);

                    using (var archive = new ZipArchive(tempStream, ZipArchiveMode.Update, true))
                    {
                        var entry = archive.GetEntry("AppManifest.xml");
                        var appManifestXmlDocument = new XmlDocument();
                        if (entry != null)
                        {
                            using (var entryStream = entry.Open())
                            {
                                using (var appManifest = new StreamReader(entryStream))
                                {
                                    appManifestXmlDocument.Load(appManifest);
                                }
                            }
                            entry.Delete();
                        }

                        var newEntry = archive.CreateEntry("AppManifest.xml");
                        using (var newEntryStream = newEntry.Open())
                        {
                            using (var stringWriter = new StreamWriter(newEntryStream))
                            {
                                using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                                {
                                    var clientIdNode = appManifestXmlDocument.DocumentElement?["AppPrincipal"]?["RemoteWebApplication"];
                                    if (clientIdNode?.Attributes != null)
                                    {
                                        clientIdNode.Attributes["ClientId"].Value = highTrustCredentials.ClientId;
                                    }
                                    var startPageNode = appManifestXmlDocument.DocumentElement?["Properties"]?["StartPage"];
                                    if (startPageNode != null)
                                    {
                                        startPageNode.InnerText = $"{HighTrustLoginPageUrl}?{{StandardTokens}}";
                                    }

                                    appManifestXmlDocument.WriteTo(xmlTextWriter);
                                    xmlTextWriter.Flush();
                                }
                            }
                        }

                    }

                    tempStream.Seek(0, SeekOrigin.Begin);
                    var httpResponseMessage = Request.CreateResponse(HttpStatusCode.OK);

                    var memoryStream = new MemoryStream();
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        var demoFile = archive.CreateEntry(HighTrustAppPackageName);

                        using (var entryStream = demoFile.Open())
                        {
                            tempStream.CopyTo(entryStream);
                        }
                    }
                    memoryStream.Seek(0, SeekOrigin.Begin);

                    httpResponseMessage.Content = new StreamContent(memoryStream);
                    httpResponseMessage.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = $"{HighTrustAppPackageName}.zip"
                    };
                    httpResponseMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    return httpResponseMessage;
                }
            }
        }

        public virtual Task<Stream> GetHighTrustAddInPackage()
        {
            return Task.FromResult<Stream>(null);
        }

        public virtual CookieHeaderValue GetCookieHeader(string cookieName, string cookieValue, string domain, DateTimeOffset expires, bool secure, bool httpOnly)
        {
            var cookie = new CookieHeaderValue(cookieName, cookieValue)
            {
                Expires = expires,
                Domain = domain,
                HttpOnly = httpOnly,
                Secure = secure,
                Path = "/",
            };

            return cookie;
        }
    }
}
