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
        private readonly LowTrustTokenHelper _lowTrustTokenHelper;
        private readonly ISharePointSessionProvider _sharePointSessionProvider;

        protected SharePointLoginController(LowTrustTokenHelper lowTrustTokenHelper, ISharePointSessionProvider sharePointSessionProvider)
        {
            _sharePointSessionProvider = sharePointSessionProvider;
            _lowTrustTokenHelper = lowTrustTokenHelper;
        }

        public virtual async Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            var queryString = this.Request.GetQueryNameValuePairs().ToList();
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

                var callbackResponse = EmbeddedData.Get<string, TokenHelper>("SharePoint.Authentication.Templates.UserLogin.Response.html").Replace("[CallbackUrl]", "/");
                var sessionCookie = GetCookieHeader("session-id", sessionId.ToString("N"), this.Request.RequestUri.Host, contextTokenObj.ValidTo, true, true);
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
    }
}
