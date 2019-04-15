using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.Exceptions;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.Extensions
{
    public static class TokenHelperExtensions
    {
        public static ClientContext CreateClientContext(this LowTrustTokenHelper lowTrustTokenHelper, string webUrl = null)
        {
            var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");

            if (cachedSession == null)
                throw new SharePointAuthenticationException("Cached credentials not found.");

            return lowTrustTokenHelper.GetClientContextWithAccessToken(webUrl ?? cachedSession.SharePointHostWebUrl, cachedSession.AccessToken);
        }
    }
}
