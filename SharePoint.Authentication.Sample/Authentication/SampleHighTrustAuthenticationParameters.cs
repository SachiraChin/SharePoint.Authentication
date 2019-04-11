using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleHighTrustAuthenticationParameters : HighTrustAuthenticationParameters
    {
        public override string ClientId { get; }

        public override string IssuerId => null;

        public override string HostedAppHostNameOverride => null;

        public override string HostedAppHostName => null;

        public override string ClientSecret { get; }

        public override string SecondaryClientSecret => null;

        public override string Realm => null;

        public override string ServiceNamespace => null;

        public override string SigningCertificatePath => null;

        public override string SigningCertificatePassword => null;

        public override X509Certificate2 Certificate => null;

        public override X509SigningCredentials SigningCredentials => null;
    }
}