using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.Controllers;
using SharePoint.Authentication.Owin.Helpers;

namespace SharePoint.Authentication.Sample.Controllers
{
    [RoutePrefix("login")]
    public class LoginController : SharePointLoginController
    {
        public override string HighTrustLoginPageUrl => "https://spauthtest.com:44388/login/high-trust";

        public LoginController(ISharePointSessionProvider sharePointSessionProvider, LowTrustTokenHelper lowTrustTokenHelper, HighTrustTokenHelper highTrustTokenHelper, HighTrustAuthenticationParameters highTrustAuthenticationParameters) : base(sharePointSessionProvider, lowTrustTokenHelper, highTrustTokenHelper, highTrustAuthenticationParameters)
        {
        }

        [HttpPost]
        [Route]
        public override Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            return base.LowTrustLoginAsync();
        }

        [HttpPost]
        [Route("high-trust")]
        [Authorize]
        public override Task<HttpResponseMessage> HighTrustLoginAsync()
        {
            return base.HighTrustLoginAsync();
        }

        [HttpPost]
        [Route("high-trust-package")]
        [Authorize]
        public override Task<HttpResponseMessage> DownloadHighTrustAddInAsync()
        {
            return base.DownloadHighTrustAddInAsync();
        }

        public override Task<Stream> GetHighTrustAddInPackage()
        {
            var packageStream = EmbeddedData.Get<Startup>("SharePoint.Authentication.Sample.Templates.HighTrustAppPackage.app");
            return Task.FromResult(packageStream);
        }
    }
}