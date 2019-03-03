using System.Net.Http.Headers;
using Microsoft.SharePoint.Client;

namespace SharePoint.Authentication.ACS
{
    public class ACSTokenHelper : TokenHelper
    {
        private readonly ACSAuthenticationParameters _authenticationParameters;

        public ACSTokenHelper(ACSAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }
    }
}