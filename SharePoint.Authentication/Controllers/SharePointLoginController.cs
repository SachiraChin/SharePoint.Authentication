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

        protected SharePointLoginController(LowTrustTokenHelper lowTrustTokenHelper, SharePointLowTrustContextProvider lowTrustContextProvider)
        {
            _lowTrustContextProvider = lowTrustContextProvider;
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

            var stateId = Guid.NewGuid().ToString("N");
            var callbackUrl = await SaveStateAndReturnCallbackUrlAsync(stateId);

            var contextTokenUrl = _lowTrustTokenHelper.GetAppContextTokenRequestUrl(spHostUrl, callbackUrl);
            response.Headers.Location = new Uri(contextTokenUrl);
            return response;
        }

        public virtual async Task<HttpResponseMessage> LowTrustLoginCallbackAsync(string stateId)
        {
            await CleanStateDataAsync(stateId);
            var contextToken = _lowTrustTokenHelper.GetContextTokenFromRequest(HttpContext.Current.Request);
            var contextTokenObj = _lowTrustTokenHelper.ReadAndValidateContextToken(contextToken, HttpContext.Current.Request.Url.Authority);
            
            var sessionId = Guid.NewGuid().ToString("N");

            await SaveContextTokenAsync(sessionId, contextTokenObj.ValidTo, contextToken);
            
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

        public abstract Task<string> SaveStateAndReturnCallbackUrlAsync(string stateId);
        
        public abstract Task CleanStateDataAsync(string stateId);
        
        public abstract Task SaveContextTokenAsync(string sessionId, DateTimeOffset expireDate, string contextToken);
    }
}
