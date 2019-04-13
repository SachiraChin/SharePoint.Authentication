using System.Web;
using System.Web.Mvc;

namespace SharePoint.Authentication.Sample.AppWeb
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
