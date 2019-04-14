using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharePoint.Authentication.Owin.AuthenticationParameters
{
    public class OwinHighTrustAuthenticationParameters : HighTrustAuthenticationParameters
    {
        public override string ClientId { get; set; }

        public override string ClientSecret { get; set; }
    }
}
