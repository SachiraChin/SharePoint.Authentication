using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SharePoint.Authentication.Exceptions;
using SharePoint.Authentication.Helpers;

namespace SharePoint.Authentication.Controllers
{
    public abstract class SharePointLoginController : ApiController
    {
        private readonly SharePointLowTrustContextProvider _lowTrustContextProvider;
        private readonly LowTrustTokenHelper _lowTrustTokenHelper;
        private readonly ISharePointSessionProvider _sharePointSessionProvider;

        protected SharePointLoginController(LowTrustTokenHelper lowTrustTokenHelper, SharePointLowTrustContextProvider lowTrustContextProvider, ISharePointSessionProvider sharePointSessionProvider)
        {
            _lowTrustContextProvider = lowTrustContextProvider;
            _sharePointSessionProvider = sharePointSessionProvider;
            _lowTrustTokenHelper = lowTrustTokenHelper;
        }

        public virtual async Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            var response = Request.CreateResponse(HttpStatusCode.Redirect);
            var queryString = this.Request.GetQueryNameValuePairs().ToList();
            var spHostUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPHostUrl", StringComparison.CurrentCultureIgnoreCase)).Value;
            
            if (spHostUrl == null)
                throw new SharePointHostUrlNotAvailableException();

            var redirectionStatus = _lowTrustContextProvider.CheckRedirectionStatus(HttpContext.Current, out var redirectUrl);

            switch (redirectionStatus)
            {
                case RedirectionStatus.ShouldRedirect:
                    response.Headers.Location = redirectUrl;
                    return response;
                case RedirectionStatus.CanNotRedirect:
                    throw new CanNotRedirectException();
                case RedirectionStatus.Ok:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(redirectionStatus), redirectionStatus, $"{nameof(redirectionStatus)} value is out of range.");
            }

            var sessionId = Guid.NewGuid();
            var spAppUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPAppWebUrl", StringComparison.CurrentCultureIgnoreCase)).Value;
            var sharePointSession = new SharePointSession()
            {
                SessionId = sessionId,
                SharePointHostWebUrl = spHostUrl,
                SharePointAppWebUrl = spAppUrl,
            };

            await _sharePointSessionProvider.SaveSharePointSession(sessionId, sharePointSession);
            var callbackUrl = await GetCallbackUrlAsync(sessionId.ToString("N"));

            var contextTokenUrl = _lowTrustTokenHelper.GetAppContextTokenRequestUrl(spHostUrl, callbackUrl);
            response.Headers.Location = new Uri(contextTokenUrl);
            return response;
        }

        public virtual async Task<HttpResponseMessage> LowTrustLoginCallbackAsync(string sessionId)
        {
            var sharePointSession = await _sharePointSessionProvider.GetSharePointSession(Guid.Parse(sessionId));
            var contextTokenAuthority = HttpContext.Current.Request.Url.Authority;
            var contextToken = _lowTrustTokenHelper.GetContextTokenFromRequest(HttpContext.Current.Request);
            var contextTokenObj = _lowTrustTokenHelper.ReadAndValidateContextToken(contextToken, contextTokenAuthority);

            sharePointSession.ContextToken = contextToken;
            sharePointSession.ContextTokenAuthority = contextTokenAuthority;

            await _sharePointSessionProvider.UpdateSharePointSession(Guid.Parse(sessionId), sharePointSession);

            var callbackResponse = EmbeddedData.Get<string, TokenHelper>("SharePoint.Authentication.Templates.UserLogin.Response.html").Replace("[CallbackUrl]", "/");
            var sessionCookie = GetCookieHeader("session-id", sessionId, this.Request.RequestUri.Host, contextTokenObj.ValidTo, true, true);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(callbackResponse, Encoding.UTF8, "text/html");
            response.Headers.AddCookies(new[] { sessionCookie, });
            return response;
        }
        
        private CookieHeaderValue GetCookieHeader(string cookieName, string cookieValue, string domain, DateTimeOffset expires, bool secure, bool httpOnly)
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

        public abstract Task<string> GetCallbackUrlAsync(string sessionId);
    }
}
