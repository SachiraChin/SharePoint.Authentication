using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.Extensions;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Sample.Controllers
{
    public class HighTrustController : Controller
    {
        private readonly ISharePointSessionProvider _sharePointSessionProvider;
        private readonly HighTrustTokenHelper _highTrustTokenHelper;

        public HighTrustController(ISharePointSessionProvider sharePointSessionProvider, HighTrustTokenHelper highTrustTokenHelper)
        {
            _sharePointSessionProvider = sharePointSessionProvider;
            _highTrustTokenHelper = highTrustTokenHelper;
        }

        // GET: HighTrust
        public async Task<ActionResult> Index()
        {
            var spHostUrl = this.Request.GetSharePointHostWebUrl();
            var credentials = await _sharePointSessionProvider.GetHighTrustCredentials(spHostUrl);
            if (credentials == null)
            {
                credentials = HighTrustCredentials.GenerateUniqueHighTrustCredentials(spHostUrl, "90601b8750f84a699771379aa944fca1", "74adaa2afa4946ce981d8fe212543752");
                await _sharePointSessionProvider.SaveHighTrustCredentials(credentials);
                return View(credentials);
            }

            try
            {
                using (var context = await _highTrustTokenHelper.GetAppOnlyAuthenticatedContext(spHostUrl))
                {
                    var web = context.Web;
                    var user = context.Web.CurrentUser;

                    context.Load(web, w => w.Title);
                    context.Load(user, u => u.Title, u => u.IsSiteAdmin);

                    await context.ExecuteQueryAsync();

                    ViewBag.SiteTitle = web.Title;
                    ViewBag.UserTitle = user.Title;
                    ViewBag.UserIsSiteAdmin = user.IsSiteAdmin;
                }
            }
            catch (Exception e)
            {
                ViewBag.HighTrustValidationMessage = $"High trust validation failed. {e.Message}";
            }

            return View(credentials);
        }
    }
}