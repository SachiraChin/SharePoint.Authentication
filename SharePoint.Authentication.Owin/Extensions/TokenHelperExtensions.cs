using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.Extensions
{
    public static class TokenHelperExtensions
    {
        public static ClientContext CreateClientContext(this LowTrustTokenHelper lowTrustTokenHelper, string webUrl = null)
        {
            var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");

            return lowTrustTokenHelper.GetClientContextWithAccessToken(webUrl ?? cachedSession.SharePointHostWebUrl, cachedSession.AccessToken);
        }
    }
}
