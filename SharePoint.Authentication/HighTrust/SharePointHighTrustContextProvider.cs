using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;

namespace SharePoint.Authentication
{
    /// <summary>
    /// Default provider for SharePointHighTrustContext.
    /// </summary>
    public class SharePointHighTrustContextProvider : SharePointContextProvider
    {
        private const string SPContextKey = "SPContext";
        private readonly ISessionProvider<SharePointHighTrustContext> _sessionProvider;

        public SharePointHighTrustContextProvider(HighTrustTokenHelper tokenHelper, ISessionProvider<SharePointHighTrustContext> sessionProvider) : base(tokenHelper)
        {
            _sessionProvider = sessionProvider;
        }

        protected override SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest)
        {
            WindowsIdentity logonUserIdentity = httpRequest.LogonUserIdentity;
            if (logonUserIdentity == null || !logonUserIdentity.IsAuthenticated || logonUserIdentity.IsGuest || logonUserIdentity.User == null)
            {
                return null;
            }

            return new SharePointHighTrustContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, logonUserIdentity, _tokenHelper);
        }

        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointHighTrustContext spHighTrustContext = spContext as SharePointHighTrustContext;

            if (spHighTrustContext != null)
            {
                Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
                WindowsIdentity logonUserIdentity = httpContext.Request.LogonUserIdentity;

                return spHostUrl == spHighTrustContext.SPHostUrl &&
                       logonUserIdentity != null &&
                       logonUserIdentity.IsAuthenticated &&
                       !logonUserIdentity.IsGuest &&
                       logonUserIdentity.User == spHighTrustContext.LogonUserIdentity.User;
            }

            return false;
        }

        protected override async Task<SharePointContext> LoadSharePointContext(HttpContextBase httpContext)
        {
            return await _sessionProvider?.Get(SPContextKey);
        }

        protected override async Task SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            await _sessionProvider?.Set(SPContextKey, spContext as SharePointHighTrustContext);
        }
    }
}