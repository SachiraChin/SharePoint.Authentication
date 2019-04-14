using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Owin.Models;

namespace SharePoint.Authentication.Owin.AuthenticationParameters
{
    public class OwinHighTrustAuthenticationParameters : HighTrustAuthenticationParameters
    {
        public override string ClientId
        {
            get
            {
                var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");
                return cachedSession.HighTrustClientId;
            }
            set => throw new NotImplementedException();
        }

        public override string ClientSecret
        {
            get
            {
                var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");
                return cachedSession.HighTrustClientSecret;
            }
            set => throw new NotImplementedException();
        }
    }
}
