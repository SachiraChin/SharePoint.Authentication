using Microsoft.IdentityModel.SecurityTokenService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleAcsAuthenticationParameters : AcsAuthenticationParameters
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

        public SampleAcsAuthenticationParameters()
        {
            ClientId = ConfigurationManager.AppSettings["sampleMvc:AcsClientId"];
            ClientSecret = ConfigurationManager.AppSettings["sampleMvc:AcsClientSecret"];
        }
    }
}