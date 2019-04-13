using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using SharePoint.Authentication.Caching;
using SharePoint.Authentication.Tokens;

namespace SharePoint.Authentication
{
    /// <summary>
    /// Default provider for SharePointLowTrustContext.
    /// </summary>
    public class SharePointLowTrustContextProvider : SharePointContextProvider
    {
        private const string SPContextKey = "SPContext";
        private const string SPCacheKeyKey = "SPCacheKey";
        private readonly ICacheProvider<SharePointLowTrustContext> _sessionProvider;

        public SharePointLowTrustContextProvider(LowTrustTokenHelper tokenHelper) : base(tokenHelper)
        {
            _sessionProvider = null;
        }

        public SharePointLowTrustContextProvider(LowTrustTokenHelper tokenHelper, ICacheProvider<SharePointLowTrustContext> sessionProvider) : base(tokenHelper)
        {
            _sessionProvider = sessionProvider;
        }

        protected override SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest)
        {
            string contextTokenString = _tokenHelper.GetContextTokenFromRequest(httpRequest);
            if (string.IsNullOrEmpty(contextTokenString))
            {
                return null;
            }

            SharePointContextToken contextToken = null;
            try
            {
                contextToken = _tokenHelper.ReadAndValidateContextToken(contextTokenString, httpRequest.Url.Authority);
            }
            catch (WebException)
            {
                return null;
            }
            catch (AudienceUriValidationFailedException)
            {
                return null;
            }

            return new SharePointLowTrustContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, contextTokenString, contextToken, _tokenHelper);
        }

        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointLowTrustContext spLowTrustContext = spContext as SharePointLowTrustContext;

            if (spLowTrustContext != null)
            {
                Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
                string contextToken = _tokenHelper.GetContextTokenFromRequest(httpContext.Request);
                HttpCookie spCacheKeyCookie = httpContext.Request.Cookies[SPCacheKeyKey];
                string spCacheKey = spCacheKeyCookie != null ? spCacheKeyCookie.Value : null;

                return spHostUrl == spLowTrustContext.SPHostUrl &&
                       !string.IsNullOrEmpty(spLowTrustContext.CacheKey) &&
                       spCacheKey == spLowTrustContext.CacheKey &&
                       !string.IsNullOrEmpty(spLowTrustContext.ContextToken) &&
                       (string.IsNullOrEmpty(contextToken) || contextToken == spLowTrustContext.ContextToken);
            }

            return false;
        }

        protected override async Task<SharePointContext> LoadSharePointContextAsync(HttpContextBase httpContext)
        {
            if (_sessionProvider == null) return null;
            
            return await _sessionProvider.GetAsync(SPContextKey, null);
        }

        protected override async Task SaveSharePointContextAsync(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointLowTrustContext spLowTrustContext = spContext as SharePointLowTrustContext;

            if (spLowTrustContext != null)
            {
                HttpCookie spCacheKeyCookie = new HttpCookie(SPCacheKeyKey)
                {
                    Value = spLowTrustContext.CacheKey,
                    Secure = true,
                    HttpOnly = true
                };

                httpContext.Response.AppendCookie(spCacheKeyCookie);
            }
            
            if (_sessionProvider == null) return;

            await _sessionProvider.SetAsync(SPContextKey, spLowTrustContext);
        }

        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
        {
            return _sessionProvider?.Get(SPContextKey);
        }

        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointLowTrustContext spLowTrustContext = spContext as SharePointLowTrustContext;

            if (spLowTrustContext != null)
            {
                HttpCookie spCacheKeyCookie = new HttpCookie(SPCacheKeyKey)
                {
                    Value = spLowTrustContext.CacheKey,
                    Secure = true,
                    HttpOnly = true
                };

                httpContext.Response.AppendCookie(spCacheKeyCookie);
            }

            _sessionProvider.Set(SPContextKey, spLowTrustContext);
        }
    }
}