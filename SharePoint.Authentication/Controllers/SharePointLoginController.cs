using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SharePoint.Authentication.Exceptions;

namespace SharePoint.Authentication.Controllers
{
    public abstract class SharePointLoginController : ApiController
    {
        private readonly SharePointAcsContextProvider _acsContextProvider;
        private readonly AcsTokenHelper _acsTokenHelper;

        protected SharePointLoginController(AcsTokenHelper acsTokenHelper, SharePointAcsContextProvider acsContextProvider)
        {
            _acsContextProvider = acsContextProvider;
            _acsTokenHelper = acsTokenHelper;
        }

        public virtual async Task<HttpResponseMessage> Login()
        {
            var response = Request.CreateResponse(HttpStatusCode.Redirect);
            var queryString = this.Request.GetQueryNameValuePairs().ToList();
            var spHostUrl = queryString.FirstOrDefault(k => string.Equals(k.Key, "SPHostUrl", StringComparison.CurrentCultureIgnoreCase)).Value;
            
            if (spHostUrl == null)
                throw new SharePointHostUrlNotAvailableException();

            var redirectionStatus = _acsContextProvider.CheckRedirectionStatus(HttpContext.Current, out var redirectUrl);

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
            var callbackUrl = await SaveStateAndReturnCallbackUrl(stateId);

            var contextTokenUrl = _acsTokenHelper.GetAppContextTokenRequestUrl(spHostUrl, callbackUrl);
            response.Headers.Location = new Uri(contextTokenUrl);
            return response;
        }

        public virtual async Task<HttpResponseMessage> LoginCallback(string stateId)
        {
            var response = Request.CreateResponse(HttpStatusCode.Redirect);
            var contextToken = _acsTokenHelper.GetContextTokenFromRequest(HttpContext.Current.Request);


        }

        public abstract Task<string> SaveStateAndReturnCallbackUrl(string stateId);
    }
}
