using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharePoint.Authentication.Caching
{
    public interface ISharePointContextCacheProvider<T> where T : SharePointContext
    {
        Task<T> GetAsync(HttpContextBase httpContext);
        T Get(HttpContextBase httpContext);
        Task SetAsync(HttpContextBase httpContext, T context);
        void Set(HttpContextBase httpContext, T context);
    }
}
