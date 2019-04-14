using System.Web;
using System.Web.Mvc;

namespace SharePoint.Authentication.Sample.HighTrustAppWeb
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
