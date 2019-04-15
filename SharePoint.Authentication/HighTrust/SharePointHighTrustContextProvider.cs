using System;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using SharePoint.Authentication.Caching;

namespace SharePoint.Authentication
{
    /// <summary>
    /// Default provider for SharePointHighTrustContext.
    /// </summary>
    public class SharePointHighTrustContextProvider : SharePointContextProvider
    {
        private const string SPContextKey = "SPContext";
        private readonly ISharePointContextCacheProvider<SharePointHighTrustContext> _sessionProvider;

        public SharePointHighTrustContextProvider(HighTrustTokenHelper tokenHelper) : base(tokenHelper)
        {
            _sessionProvider = null;
        }

        public SharePointHighTrustContextProvider(HighTrustTokenHelper tokenHelper, ISharePointContextCacheProvider<SharePointHighTrustContext> sessionProvider) : base(tokenHelper)
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

        protected override async Task<SharePointContext> LoadSharePointContextAsync(HttpContextBase httpContext)
        {
            if (_sessionProvider == null) return null;

            return await _sessionProvider.GetAsync(httpContext);
        }

        protected override async Task SaveSharePointContextAsync(SharePointContext spContext, HttpContextBase httpContext)
        {
            if (_sessionProvider == null) return;

            await _sessionProvider?.SetAsync(httpContext, spContext as SharePointHighTrustContext);
        }

        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
        {
            return _sessionProvider?.Get(httpContext);
        }

        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            _sessionProvider?.SetAsync(httpContext, spContext as SharePointHighTrustContext);
        }
    }
}