using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Caching;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleSharePointContextCacheProvider<T> : ISharePointContextCacheProvider<T> where T : SharePointContext
    {
        private const string SPContextKey = "SPContext";

        public T Get(HttpContextBase httpContext)
        {
            return httpContext.Session[SPContextKey] as T;
        }

        public Task<T> GetAsync(HttpContextBase httpContext)
        {
            return Task.FromResult(httpContext.Session[SPContextKey] as T);
        }

        public void Set(HttpContextBase httpContext, T context)
        {
            httpContext.Session[SPContextKey] = context;
        }

        public Task SetAsync(HttpContextBase httpContext, T context)
        {
            httpContext.Session[SPContextKey] = context;

            return Task.FromResult(true);
        }
    }
}