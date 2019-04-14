using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;

namespace SharePoint.Authentication
{
    public abstract class LowTrustAuthenticationParameters : IAuthenticationParameters
    {
        public virtual string ClientId { get; set; } = null;
        public virtual string IssuerId { get; set; } = null;
        public virtual string HostedAppHostNameOverride { get; set; } = null;
        public virtual string HostedAppHostName { get; set; } = null;
        public virtual string ClientSecret { get; set; } = null;
        public virtual string SecondaryClientSecret { get; set; } = null;
        public virtual string Realm { get; set; } = null;
        public virtual string ServiceNamespace { get; set; } = null;
        public virtual string SigningCertificatePath { get; set; } = null;
        public virtual string SigningCertificatePassword { get; set; } = null;
        public virtual X509Certificate2 Certificate { get; set; } = null;
        public virtual X509SigningCredentials SigningCredentials { get; set; } = null;
    }
}
