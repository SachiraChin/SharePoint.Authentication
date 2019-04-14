using Microsoft.IdentityModel.SecurityTokenService;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleLowTrustAuthenticationParameters : LowTrustAuthenticationParameters
    {
        public sealed override string ClientId { get; set; }

        public sealed override string ClientSecret { get; set; }

        public SampleLowTrustAuthenticationParameters()
        {
            ClientId = ConfigurationManager.AppSettings["sampleMvc:LowTrustClientId"];
            ClientSecret = ConfigurationManager.AppSettings["sampleMvc:LowTrustClientSecret"];
        }
    }
}