using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Dependencies;
using Microsoft.Owin;
using SharePoint.Authentication.Caching;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.Extensions
{
    public static class HttpRequestExtension
    {
        public static string GetSharePointHostWebUrl(this HttpRequestBase request)
        {
            var owin = request.GetOwinContext();
            return owin.GetCurrentCacheSession()?.SharePointHostWebUrl;
        }

        public static string GetSharePointHostWebUrl(this HttpRequestMessage request)
        {
            var owin = request.GetOwinContext();
            return owin.GetCurrentCacheSession()?.SharePointHostWebUrl;
        }
        public static async Task ResetCacheForCurrentSession(this HttpRequestBase request, IDependencyResolver resolver)
        {
            var owin = request.GetOwinContext();
            await ResetCacheForCurrentSession(owin, resolver);
        }

        public static async Task ResetCacheForCurrentSession(this HttpRequestMessage request, IDependencyResolver resolver)
        {
            var owin = request.GetOwinContext();
            await ResetCacheForCurrentSession(owin, resolver);
        }

        private static async Task ResetCacheForCurrentSession(IOwinContext owin, IDependencyResolver resolver)
        {
            var cacheSession = owin.GetCurrentCacheSession();
            if (cacheSession == null) return;

            using (var dependencyScope = resolver.BeginScope())
            {
                var cacheProvider = dependencyScope.Resolve<ICacheProvider>() ?? new MemoryCacheProvider();
                var cacheSessionKey = $"{SharePointAuthenticationHandler.SessionCacheMemoryGroupName}.{cacheSession.SessionId:N}";
                await cacheProvider.RemoveAsync(cacheSessionKey);
            }
        }

        internal static CachedSession GetCurrentCacheSession(this IOwinContext owin)
        {
            var cachedSession = owin.Get<CachedSession>("CachedSession");
            return cachedSession;
        }
    }
}
