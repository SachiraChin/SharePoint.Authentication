using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;

namespace SharePoint.Authentication
{
    public abstract class AcsAuthenticationParameters : IAuthenticationParameters
    {
        public abstract string ClientId { get; }
        public abstract string IssuerId { get; }
        public abstract string HostedAppHostNameOverride { get; }
        public abstract string HostedAppHostName { get; }
        public abstract string ClientSecret { get; }
        public abstract string SecondaryClientSecret { get; }
        public abstract string Realm { get; }
        public abstract string ServiceNamespace { get; }
        public abstract string SigningCertificatePath { get; }
        public abstract string SigningCertificatePassword { get; }
        public abstract X509Certificate2 Certificate { get; }
        public abstract X509SigningCredentials SigningCredentials { get; }
    }
}
