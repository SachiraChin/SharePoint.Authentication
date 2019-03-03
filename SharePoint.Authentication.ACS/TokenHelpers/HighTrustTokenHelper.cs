using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;
using SharePoint.Authentication.ACS.AuthenticationParameters;

namespace SharePoint.Authentication.ACS.TokenHelpers
{
    public class HighTrustTokenHelper : TokenHelper
    {
        private readonly HighTrustAuthenticationParameters _authenticationParameters;

        public HighTrustTokenHelper(HighTrustAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }

        public Task<ClientContext> GetAzureADAppOnlyAuthenticatedContext(string webUrl)
        {
            var siteUri = new Uri(webUrl);
            var at = GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, siteUri.Authority, GetRealmFromTargetUrl(siteUri));

            return Task.FromResult(GetClientContextWithAccessToken(webUrl, at.AccessToken));
        }
    }
}
