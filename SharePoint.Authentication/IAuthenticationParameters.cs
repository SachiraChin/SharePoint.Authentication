using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.SecurityTokenService;

// ReSharper disable InconsistentNaming

namespace SharePoint.Authentication.ACS
{
    public interface IAuthenticationParameters
    {
        string ClientId { get; }
        string IssuerId { get; }
        string HostedAppHostNameOverride { get; }
        string HostedAppHostName { get; }
        string ClientSecret { get; }
        string SecondaryClientSecret { get; }
        string Realm { get; }
        string ServiceNamespace { get; }

        string SigningCertificatePath { get; }
        string SigningCertificatePassword { get; }
        X509Certificate2 Certificate { get; }
        X509SigningCredentials SigningCredentials { get; }
    }
}
