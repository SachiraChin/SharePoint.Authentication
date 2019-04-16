using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;

namespace SharePoint.Authentication.Owin.Extensions
{
    internal static class DependencyScopeExtensions
    {
        internal static T Resolve<T>(this IDependencyScope dependencyScope)
        {
            try
            {
                return (T) dependencyScope.GetService(typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
