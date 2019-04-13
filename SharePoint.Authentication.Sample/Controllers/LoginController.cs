using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SharePoint.Authentication.Controllers;

namespace SharePoint.Authentication.Sample.Controllers
{
    [RoutePrefix("login")]
    public class LoginController : SharePointLoginController
    {
        public LoginController(LowTrustTokenHelper lowTrustTokenHelper, SharePointLowTrustContextProvider lowTrustContextProvider, ISharePointSessionProvider sharePointSessionProvider) : base(lowTrustTokenHelper, lowTrustContextProvider, sharePointSessionProvider)
        {
        }

        [HttpPost]
        [Route]
        public override Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            return base.LowTrustLoginAsync();
        }

        [HttpPost]
        [Route("callback/{sessionId}")]
        public override Task<HttpResponseMessage> LowTrustLoginCallbackAsync(string sessionId)
        {
            return base.LowTrustLoginCallbackAsync(sessionId);
        }

        public override Task<string> GetCallbackUrlAsync(string sessionId)
        {
            return Task.FromResult($"https://spauthtest.com:44388/login/callback/{sessionId}");
        }
    }
}