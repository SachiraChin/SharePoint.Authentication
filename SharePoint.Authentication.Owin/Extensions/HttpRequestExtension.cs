using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.Extensions
{
    public static class HttpRequestExtension
    {
        public static string GetSharePointHostWebUrl(this HttpRequestBase request)
        {
            var owin = request.GetOwinContext();
            var cachedSession = owin.Get<CachedSession>("CachedSession");
            return cachedSession?.SharePointHostWebUrl;
        }
        public static string GetSharePointHostWebUrl(this HttpRequestMessage request)
        {
            var owin = request.GetOwinContext();
            var cachedSession = owin.Get<CachedSession>("CachedSession");
            return cachedSession?.SharePointHostWebUrl;
        }
    }
}
