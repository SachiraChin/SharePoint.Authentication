using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;

// ReSharper disable InconsistentNaming

namespace SharePoint.Authentication
{
    public interface IAuthenticationParameters
    {
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string IssuerId { get; set; }
        string HostedAppHostNameOverride { get; set; }
        string HostedAppHostName { get; set; }
        string SecondaryClientSecret { get; set; }
        string Realm { get; set; }
        string ServiceNamespace { get; set; }

        string SigningCertificatePath { get; set; }
        string SigningCertificatePassword { get; set; }
        X509Certificate2 Certificate { get; set; }
        X509SigningCredentials SigningCredentials { get; set; }
    }
}
