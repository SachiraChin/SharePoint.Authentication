using System;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;
using SharePoint.Authentication.Tokens;

namespace SharePoint.Authentication
{
    /// <summary>
    /// Default provider for SharePointAcsContext.
    /// </summary>
    public class SharePointAcsContextProvider : SharePointContextProvider
    {
        private const string SPContextKey = "SPContext";
        private const string SPCacheKeyKey = "SPCacheKey";
        private readonly ISessionProvider<SharePointAcsContext> _sessionProvider;

        public SharePointAcsContextProvider(ACSTokenHelper tokenHelper, ISessionProvider<SharePointAcsContext> sessionProvider) : base(tokenHelper)
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

            return new SharePointAcsContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, contextTokenString, contextToken, _tokenHelper);
        }

        protected override bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointAcsContext spAcsContext = spContext as SharePointAcsContext;

            if (spAcsContext != null)
            {
                Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
                string contextToken = _tokenHelper.GetContextTokenFromRequest(httpContext.Request);
                HttpCookie spCacheKeyCookie = httpContext.Request.Cookies[SPCacheKeyKey];
                string spCacheKey = spCacheKeyCookie != null ? spCacheKeyCookie.Value : null;

                return spHostUrl == spAcsContext.SPHostUrl &&
                       !string.IsNullOrEmpty(spAcsContext.CacheKey) &&
                       spCacheKey == spAcsContext.CacheKey &&
                       !string.IsNullOrEmpty(spAcsContext.ContextToken) &&
                       (string.IsNullOrEmpty(contextToken) || contextToken == spAcsContext.ContextToken);
            }

            return false;
        }

        protected override async Task<SharePointContext> LoadSharePointContextAsync(HttpContextBase httpContext)
        {
            if (_sessionProvider == null) return null;
            
            return await _sessionProvider.GetAsync(SPContextKey);
        }

        protected override async Task SaveSharePointContextAsync(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointAcsContext spAcsContext = spContext as SharePointAcsContext;

            if (spAcsContext != null)
            {
                HttpCookie spCacheKeyCookie = new HttpCookie(SPCacheKeyKey)
                {
                    Value = spAcsContext.CacheKey,
                    Secure = true,
                    HttpOnly = true
                };

                httpContext.Response.AppendCookie(spCacheKeyCookie);
            }
            
            if (_sessionProvider == null) return;

            await _sessionProvider.SetAsync(SPContextKey, spAcsContext);
        }

        protected override SharePointContext LoadSharePointContext(HttpContextBase httpContext)
        {
            return _sessionProvider?.Get(SPContextKey);
        }

        protected override void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext)
        {
            SharePointAcsContext spAcsContext = spContext as SharePointAcsContext;

            if (spAcsContext != null)
            {
                HttpCookie spCacheKeyCookie = new HttpCookie(SPCacheKeyKey)
                {
                    Value = spAcsContext.CacheKey,
                    Secure = true,
                    HttpOnly = true
                };

                httpContext.Response.AppendCookie(spCacheKeyCookie);
            }

            _sessionProvider.Set(SPContextKey, spAcsContext);
        }
    }
}