using System;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace SharePoint.Authentication
{
    public class HighTrustTokenHelper : TokenHelper
    {
        private readonly HighTrustAuthenticationParameters _authenticationParameters;

        public HighTrustTokenHelper(HighTrustAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
            _authenticationParameters = authenticationParameters;
        }

        public Task<ClientContext> GetAppOnlyAuthenticatedContext(string webUrl)
        {
            var siteUri = new Uri(webUrl);
            var at = GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, siteUri.Authority, GetRealmFromTargetUrl(siteUri));

            return Task.FromResult(GetClientContextWithAccessToken(webUrl, at.AccessToken));
        }
    }
}
