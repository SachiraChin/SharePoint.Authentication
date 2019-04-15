using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.Controllers;
using SharePoint.Authentication.Owin.Helpers;

namespace SharePoint.Authentication.Sample.Controllers
{
    [RoutePrefix("login")]
    public class LoginController : SharePointLoginController
    {
        public override string LowTrustLandingPageUrl { get; } = "/";
        public override string HighTrustLandingPageUrl { get; } = "/";
        public override string HighTrustAppPackageName { get; } = "HighTrustApp.app";
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

        public override Task LowTrustPostAuthenticationAsync(ClientContext clientContext)
        {
            return base.LowTrustPostAuthenticationAsync(clientContext);
        }

        public override CookieHeaderValue GetCookieHeader(string cookieName, string cookieValue, string domain, DateTimeOffset expires, bool secure, bool httpOnly)
        {
            return base.GetCookieHeader(cookieName, cookieValue, domain, expires, secure, httpOnly);
        }

        [HttpPost]
        [Route("high-trust")]
        [Authorize]
        public override Task<HttpResponseMessage> HighTrustLoginAsync()
        {
            return base.HighTrustLoginAsync();
        }

        public override Task HighTrustPostAuthenticationAsync(ClientContext clientContext)
        {
            return base.HighTrustPostAuthenticationAsync(clientContext);
        }

        [HttpGet]
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