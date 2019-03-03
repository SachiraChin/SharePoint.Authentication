using System;
using System.Security.Principal;

namespace SharePoint.Authentication.ACS
{
    /// <summary>
    /// Encapsulates all the information from SharePoint in HighTrust mode.
    /// </summary>
    public class SharePointHighTrustContext : SharePointContext
    {
        private readonly WindowsIdentity logonUserIdentity;

        /// <summary>
        /// The Windows identity for the current user.
        /// </summary>
        public WindowsIdentity LogonUserIdentity
        {
            get { return this.logonUserIdentity; }
        }

        public override string UserAccessTokenForSPHost
        {
            get
            {
                return GetAccessTokenString(ref this.userAccessTokenForSPHost,
                    () => _tokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPHostUrl, this.LogonUserIdentity));
            }
        }

        public override string UserAccessTokenForSPAppWeb
        {
            get
            {
                if (this.SPAppWebUrl == null)
                {
                    return null;
                }

                return GetAccessTokenString(ref this.userAccessTokenForSPAppWeb,
                    () => _tokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPAppWebUrl, this.LogonUserIdentity));
            }
        }

        public override string AppOnlyAccessTokenForSPHost
        {
            get
            {
                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPHost,
                    () => _tokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPHostUrl, null));
            }
        }

        public override string AppOnlyAccessTokenForSPAppWeb
        {
            get
            {
                if (this.SPAppWebUrl == null)
                {
                    return null;
                }

                return GetAccessTokenString(ref this.appOnlyAccessTokenForSPAppWeb,
                    () => _tokenHelper.GetS2SAccessTokenWithWindowsIdentity(this.SPAppWebUrl, null));
            }
        }

        public SharePointHighTrustContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, WindowsIdentity logonUserIdentity, TokenHelper tokenHelper)
            : base(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, tokenHelper)
        {
            if (logonUserIdentity == null)
            {
                throw new ArgumentNullException("logonUserIdentity");
            }

            this.logonUserIdentity = logonUserIdentity;
        }

        /// <summary>
        /// Ensures the access token is valid and returns it.
        /// </summary>
        /// <param name="accessToken">The access token to verify.</param>
        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
        /// <returns>The access token string.</returns>
        private string GetAccessTokenString(ref Tuple<string, DateTime> accessToken, Func<string> tokenRenewalHandler)
        {
            RenewAccessTokenIfNeeded(ref accessToken, tokenRenewalHandler);

            return IsAccessTokenValid(accessToken) ? accessToken.Item1 : null;
        }

        /// <summary>
        /// Renews the access token if it is not valid.
        /// </summary>
        /// <param name="accessToken">The access token to renew.</param>
        /// <param name="tokenRenewalHandler">The token renewal handler.</param>
        private void RenewAccessTokenIfNeeded(ref Tuple<string, DateTime> accessToken, Func<string> tokenRenewalHandler)
        {
            if (IsAccessTokenValid(accessToken))
            {
                return;
            }

            DateTime expiresOn = DateTime.UtcNow.Add(_tokenHelper.HighTrustAccessTokenLifetime);

            if (_tokenHelper.HighTrustAccessTokenLifetime > AccessTokenLifetimeTolerance)
            {
                // Make the access token get renewed a bit earlier than the time when it expires
                // so that the calls to SharePoint with it will have enough time to complete successfully.
                expiresOn -= AccessTokenLifetimeTolerance;
            }

            accessToken = Tuple.Create(tokenRenewalHandler(), expiresOn);
        }
    }
}