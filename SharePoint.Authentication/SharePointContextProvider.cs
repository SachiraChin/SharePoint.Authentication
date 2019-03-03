using System;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Tokens;

namespace SharePoint.Authentication
{
    /// <summary>
    /// Provides SharePointContext instances.
    /// </summary>
    public abstract class SharePointContextProvider
    {
        protected readonly TokenHelper _tokenHelper;

        protected SharePointContextProvider(TokenHelper tokenHelper)
        {
            _tokenHelper = tokenHelper;
        }
        
        /// <summary>
        /// Checks if it is necessary to redirect to SharePoint for user to authenticate.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="redirectUrl">The redirect url to SharePoint if the status is ShouldRedirect. <c>Null</c> if the status is Ok or CanNotRedirect.</param>
        /// <returns>Redirection status.</returns>
        public RedirectionStatus CheckRedirectionStatus(HttpContextBase httpContext, out Uri redirectUrl)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            redirectUrl = null;
            bool contextTokenExpired = false;

            try
            {
                if (GetSharePointContext(httpContext) != null)
                {
                    return RedirectionStatus.Ok;
                }
            }
            catch (SecurityTokenExpiredException)
            {
                contextTokenExpired = true;
            }

            const string SPHasRedirectedToSharePointKey = "SPHasRedirectedToSharePoint";

            if (!string.IsNullOrEmpty(httpContext.Request.QueryString[SPHasRedirectedToSharePointKey]) && !contextTokenExpired)
            {
                return RedirectionStatus.CanNotRedirect;
            }

            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);

            if (spHostUrl == null)
            {
                return RedirectionStatus.CanNotRedirect;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(httpContext.Request.HttpMethod, "POST"))
            {
                return RedirectionStatus.CanNotRedirect;
            }

            Uri requestUrl = httpContext.Request.Url;

            var queryNameValueCollection = HttpUtility.ParseQueryString(requestUrl.Query);

            // Removes the values that are included in {StandardTokens}, as {StandardTokens} will be inserted at the beginning of the query string.
            queryNameValueCollection.Remove(SharePointContext.SPHostUrlKey);
            queryNameValueCollection.Remove(SharePointContext.SPAppWebUrlKey);
            queryNameValueCollection.Remove(SharePointContext.SPLanguageKey);
            queryNameValueCollection.Remove(SharePointContext.SPClientTagKey);
            queryNameValueCollection.Remove(SharePointContext.SPProductNumberKey);

            // Adds SPHasRedirectedToSharePoint=1.
            queryNameValueCollection.Add(SPHasRedirectedToSharePointKey, "1");

            UriBuilder returnUrlBuilder = new UriBuilder(requestUrl);
            returnUrlBuilder.Query = queryNameValueCollection.ToString();

            // Inserts StandardTokens.
            const string StandardTokens = "{StandardTokens}";
            string returnUrlString = returnUrlBuilder.Uri.AbsoluteUri;
            returnUrlString = returnUrlString.Insert(returnUrlString.IndexOf("?") + 1, StandardTokens + "&");

            // Constructs redirect url.
            string redirectUrlString = _tokenHelper.GetAppContextTokenRequestUrl(spHostUrl.AbsoluteUri, Uri.EscapeDataString(returnUrlString));

            redirectUrl = new Uri(redirectUrlString, UriKind.Absolute);

            return RedirectionStatus.ShouldRedirect;
        }

        /// <summary>
        /// Checks if it is necessary to redirect to SharePoint for user to authenticate.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <param name="redirectUrl">The redirect url to SharePoint if the status is ShouldRedirect. <c>Null</c> if the status is Ok or CanNotRedirect.</param>
        /// <returns>Redirection status.</returns>
        public RedirectionStatus CheckRedirectionStatus(HttpContext httpContext, out Uri redirectUrl)
        {
            return CheckRedirectionStatus(new HttpContextWrapper(httpContext), out redirectUrl);
        }

        /// <summary>
        /// Creates a SharePointContext instance with the specified HTTP request.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
        public SharePointContext CreateSharePointContext(HttpRequestBase httpRequest)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException("httpRequest");
            }

            // SPHostUrl
            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpRequest);
            if (spHostUrl == null)
            {
                return null;
            }

            // SPAppWebUrl
            string spAppWebUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.QueryString[SharePointContext.SPAppWebUrlKey]);
            Uri spAppWebUrl;
            if (!Uri.TryCreate(spAppWebUrlString, UriKind.Absolute, out spAppWebUrl) ||
                !(spAppWebUrl.Scheme == Uri.UriSchemeHttp || spAppWebUrl.Scheme == Uri.UriSchemeHttps))
            {
                spAppWebUrl = null;
            }

            // SPLanguage
            string spLanguage = httpRequest.QueryString[SharePointContext.SPLanguageKey];
            if (string.IsNullOrEmpty(spLanguage))
            {
                return null;
            }

            // SPClientTag
            string spClientTag = httpRequest.QueryString[SharePointContext.SPClientTagKey];
            if (string.IsNullOrEmpty(spClientTag))
            {
                return null;
            }

            // SPProductNumber
            string spProductNumber = httpRequest.QueryString[SharePointContext.SPProductNumberKey];
            if (string.IsNullOrEmpty(spProductNumber))
            {
                return null;
            }

            return CreateSharePointContext(spHostUrl, spAppWebUrl, spLanguage, spClientTag, spProductNumber, httpRequest);
        }

        /// <summary>
        /// Creates a SharePointContext instance with the specified HTTP request.
        /// </summary>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
        public SharePointContext CreateSharePointContext(HttpRequest httpRequest)
        {
            return CreateSharePointContext(new HttpRequestWrapper(httpRequest));
        }

        /// <summary>
        /// Gets a SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
        public async Task<SharePointContext> GetSharePointContextAsync(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
            if (spHostUrl == null)
            {
                return null;
            }

            SharePointContext spContext = await LoadSharePointContextAsync(httpContext);

            if (spContext == null || !ValidateSharePointContext(spContext, httpContext))
            {
                spContext = CreateSharePointContext(httpContext.Request);

                if (spContext != null)
                {
                    await SaveSharePointContextAsync(spContext, httpContext);
                }
            }

            return spContext;
        }
        
        /// <summary>
        /// Gets a SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
        public SharePointContext GetSharePointContext(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException("httpContext");
            }

            Uri spHostUrl = SharePointContext.GetSPHostUrl(httpContext.Request);
            if (spHostUrl == null)
            {
                return null;
            }

            SharePointContext spContext = LoadSharePointContext(httpContext);

            if (spContext == null || !ValidateSharePointContext(spContext, httpContext))
            {
                spContext = CreateSharePointContext(httpContext.Request);

                if (spContext != null)
                {
                    SaveSharePointContext(spContext, httpContext);
                }
            }

            return spContext;
        }

        /// <summary>
        /// Gets a SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
        public async Task<SharePointContext> GetSharePointContextAsync(HttpContext httpContext)
        {
            return await GetSharePointContextAsync(new HttpContextWrapper(httpContext));
        }
        
        /// <summary>
        /// Gets a SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found and a new instance can't be created.</returns>
        public SharePointContext GetSharePointContext(HttpContext httpContext)
        {
            return GetSharePointContext(new HttpContextWrapper(httpContext));
        }

        /// <summary>
        /// Creates a SharePointContext instance.
        /// </summary>
        /// <param name="spHostUrl">The SharePoint host url.</param>
        /// <param name="spAppWebUrl">The SharePoint app web url.</param>
        /// <param name="spLanguage">The SharePoint language.</param>
        /// <param name="spClientTag">The SharePoint client tag.</param>
        /// <param name="spProductNumber">The SharePoint product number.</param>
        /// <param name="httpRequest">The HTTP request.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if errors occur.</returns>
        protected abstract SharePointContext CreateSharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, HttpRequestBase httpRequest);

        /// <summary>
        /// Validates if the given SharePointContext can be used with the specified HTTP context.
        /// </summary>
        /// <param name="spContext">The SharePointContext.</param>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>True if the given SharePointContext can be used with the specified HTTP context.</returns>
        protected abstract bool ValidateSharePointContext(SharePointContext spContext, HttpContextBase httpContext);

        /// <summary>
        /// Loads the SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found.</returns>
        protected abstract Task<SharePointContext> LoadSharePointContextAsync(HttpContextBase httpContext);

        /// <summary>
        /// Saves the specified SharePointContext instance associated with the specified HTTP context.
        /// <c>null</c> is accepted for clearing the SharePointContext instance associated with the HTTP context.
        /// </summary>
        /// <param name="spContext">The SharePointContext instance to be saved, or <c>null</c>.</param>
        /// <param name="httpContext">The HTTP context.</param>
        protected abstract Task SaveSharePointContextAsync(SharePointContext spContext, HttpContextBase httpContext);

        /// <summary>
        /// Loads the SharePointContext instance associated with the specified HTTP context.
        /// </summary>
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>The SharePointContext instance. Returns <c>null</c> if not found.</returns>
        protected abstract SharePointContext LoadSharePointContext(HttpContextBase httpContext);

        /// <summary>
        /// Saves the specified SharePointContext instance associated with the specified HTTP context.
        /// <c>null</c> is accepted for clearing the SharePointContext instance associated with the HTTP context.
        /// </summary>
        /// <param name="spContext">The SharePointContext instance to be saved, or <c>null</c>.</param>
        /// <param name="httpContext">The HTTP context.</param>
        protected abstract void SaveSharePointContext(SharePointContext spContext, HttpContextBase httpContext);
    }
}