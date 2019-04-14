using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.Controllers;

namespace SharePoint.Authentication.Sample.Controllers
{
    [RoutePrefix("login")]
    public class LoginController : SharePointLoginController
    {
        public LoginController(LowTrustTokenHelper lowTrustTokenHelper, ISharePointSessionProvider sharePointSessionProvider) : base(lowTrustTokenHelper, sharePointSessionProvider)
        {
        }

        [HttpPost]
        [Route]
        public override Task<HttpResponseMessage> LowTrustLoginAsync()
        {
            return base.LowTrustLoginAsync();
        }
    }
}