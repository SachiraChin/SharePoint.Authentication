using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SharePoint.Authentication.Owin.Extensions;

namespace SharePoint.Authentication.Sample.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly LowTrustTokenHelper _lowTrustTokenHelper;

        public HomeController(LowTrustTokenHelper lowTrustTokenHelper)
        {
            _lowTrustTokenHelper = lowTrustTokenHelper;
        }

        public async Task<ActionResult> Index()
        {
            ViewBag.Title = "Home Page";

            using (var context = _lowTrustTokenHelper.CreateClientContext())
            {
                var web = context.Web;
                var user = context.Web.CurrentUser;

                context.Load(web, w => w.Title);
                context.Load(user, u => u.Title);

                await context.ExecuteQueryAsync();

                ViewBag.SiteTitle = web.Title;
                ViewBag.UserTitle = user.Title;
            }

            return View();
        }
    }
}
