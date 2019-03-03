using System.Net.Http.Headers;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.ACS.AuthenticationParameters;

namespace SharePoint.Authentication.ACS.TokenHelpers
{
    public class LowTrustTokenHelper : TokenHelper
    {
        private readonly LowTrustAuthenticationParameters _authenticationParameters;

        public LowTrustTokenHelper(LowTrustAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }
    }
}