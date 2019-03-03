using System;
using System.Web;
using Microsoft.SharePoint.Client;

namespace SharePoint.Authentication.ACS
{
    /// <summary>
    /// Encapsulates all the information from SharePoint.
    /// </summary>
    public abstract class SharePointContext
    {
        public const string SPHostUrlKey = "SPHostUrl";
        public const string SPAppWebUrlKey = "SPAppWebUrl";
        public const string SPLanguageKey = "SPLanguage";
        public const string SPClientTagKey = "SPClientTag";
        public const string SPProductNumberKey = "SPProductNumber";

        protected static readonly TimeSpan AccessTokenLifetimeTolerance = TimeSpan.FromMinutes(5.0);

        private readonly Uri spHostUrl;
        private readonly Uri spAppWebUrl;
        private readonly string spLanguage;
        private readonly string spClientTag;
        private readonly string spProductNumber;

        // <AccessTokenString, UtcExpiresOn>
        protected Tuple<string, DateTime> userAccessTokenForSPHost;
        protected Tuple<string, DateTime> userAccessTokenForSPAppWeb;
        protected Tuple<string, DateTime> appOnlyAccessTokenForSPHost;
        protected Tuple<string, DateTime> appOnlyAccessTokenForSPAppWeb;
        protected readonly TokenHelper _tokenHelper;

        /// <summary>
        /// Gets the SharePoint host url from QueryString of the specified HTTP request.
        /// </summary>
        /// <param name="httpRequest">The specified HTTP request.</param>
        /// <returns>The SharePoint host url. Returns <c>null</c> if the HTTP request doesn't contain the SharePoint host url.</returns>
        public static Uri GetSPHostUrl(HttpRequestBase httpRequest)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException("httpRequest");
            }

            string spHostUrlString = TokenHelper.EnsureTrailingSlash(httpRequest.QueryString[SPHostUrlKey]);
            Uri spHostUrl;
            if (Uri.TryCreate(spHostUrlString, UriKind.Absolute, out spHostUrl) &&
                (spHostUrl.Scheme == Uri.UriSchemeHttp || spHostUrl.Scheme == Uri.UriSchemeHttps))
            {
                return spHostUrl;
            }

            return null;
        }

        /// <summary>
        /// Gets the SharePoint host url from QueryString of the specified HTTP request.
        /// </summary>
        /// <param name="httpRequest">The specified HTTP request.</param>
        /// <returns>The SharePoint host url. Returns <c>null</c> if the HTTP request doesn't contain the SharePoint host url.</returns>
        public Uri GetSPHostUrl(HttpRequest httpRequest)
        {
            return GetSPHostUrl(new HttpRequestWrapper(httpRequest));
        }

        /// <summary>
        /// The SharePoint host url.
        /// </summary>
        public Uri SPHostUrl
        {
            get { return this.spHostUrl; }
        }

        /// <summary>
        /// The SharePoint app web url.
        /// </summary>
        public Uri SPAppWebUrl
        {
            get { return this.spAppWebUrl; }
        }

        /// <summary>
        /// The SharePoint language.
        /// </summary>
        public string SPLanguage
        {
            get { return this.spLanguage; }
        }

        /// <summary>
        /// The SharePoint client tag.
        /// </summary>
        public string SPClientTag
        {
            get { return this.spClientTag; }
        }

        /// <summary>
        /// The SharePoint product number.
        /// </summary>
        public string SPProductNumber
        {
            get { return this.spProductNumber; }
        }

        /// <summary>
        /// The user access token for the SharePoint host.
        /// </summary>
        public abstract string UserAccessTokenForSPHost
        {
            get;
        }

        /// <summary>
        /// The user access token for the SharePoint app web.
        /// </summary>
        public abstract string UserAccessTokenForSPAppWeb
        {
            get;
        }

        /// <summary>
        /// The app only access token for the SharePoint host.
        /// </summary>
        public abstract string AppOnlyAccessTokenForSPHost
        {
            get;
        }

        /// <summary>
        /// The app only access token for the SharePoint app web.
        /// </summary>
        public abstract string AppOnlyAccessTokenForSPAppWeb
        {
            get;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="spHostUrl">The SharePoint host url.</param>
        /// <param name="spAppWebUrl">The SharePoint app web url.</param>
        /// <param name="spLanguage">The SharePoint language.</param>
        /// <param name="spClientTag">The SharePoint client tag.</param>
        /// <param name="spProductNumber">The SharePoint product number.</param>
        /// <param name="tokenHelper">Token helper instance</param>
        protected SharePointContext(Uri spHostUrl, Uri spAppWebUrl, string spLanguage, string spClientTag, string spProductNumber, TokenHelper tokenHelper)
        {
            if (spHostUrl == null)
            {
                throw new ArgumentNullException("spHostUrl");
            }

            if (string.IsNullOrEmpty(spLanguage))
            {
                throw new ArgumentNullException("spLanguage");
            }

            if (string.IsNullOrEmpty(spClientTag))
            {
                throw new ArgumentNullException("spClientTag");
            }

            if (string.IsNullOrEmpty(spProductNumber))
            {
                throw new ArgumentNullException("spProductNumber");
            }

            this.spHostUrl = spHostUrl;
            this.spAppWebUrl = spAppWebUrl;
            this.spLanguage = spLanguage;
            this.spClientTag = spClientTag;
            this.spProductNumber = spProductNumber;
            _tokenHelper = tokenHelper;
        }

        /// <summary>
        /// Creates a user ClientContext for the SharePoint host.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public ClientContext CreateUserClientContextForSPHost()
        {
            return CreateClientContext(this.SPHostUrl, this.UserAccessTokenForSPHost);
        }

        /// <summary>
        /// Creates a user ClientContext for the SharePoint app web.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public ClientContext CreateUserClientContextForSPAppWeb()
        {
            return CreateClientContext(this.SPAppWebUrl, this.UserAccessTokenForSPAppWeb);
        }

        /// <summary>
        /// Creates app only ClientContext for the SharePoint host.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public ClientContext CreateAppOnlyClientContextForSPHost()
        {
            return CreateClientContext(this.SPHostUrl, this.AppOnlyAccessTokenForSPHost);
        }

        /// <summary>
        /// Creates an app only ClientContext for the SharePoint app web.
        /// </summary>
        /// <returns>A ClientContext instance.</returns>
        public ClientContext CreateAppOnlyClientContextForSPAppWeb()
        {
            return CreateClientContext(this.SPAppWebUrl, this.AppOnlyAccessTokenForSPAppWeb);
        }

        /// <summary>
        /// Gets the database connection string from SharePoint for autohosted add-in.
        /// This method is deprecated because the autohosted option is no longer available.
        /// </summary>
        [Obsolete("This method is deprecated because the autohosted option is no longer available.", true)]
        public string GetDatabaseConnectionString()
        {
            throw new NotSupportedException("This method is deprecated because the autohosted option is no longer available.");
        }

        /// <summary>
        /// Determines if the specified access token is valid.
        /// It considers an access token as not valid if it is null, or it has expired.
        /// </summary>
        /// <param name="accessToken">The access token to verify.</param>
        /// <returns>True if the access token is valid.</returns>
        protected static bool IsAccessTokenValid(Tuple<string, DateTime> accessToken)
        {
            return accessToken != null &&
                   !string.IsNullOrEmpty(accessToken.Item1) &&
                   accessToken.Item2 > DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a ClientContext with the specified SharePoint site url and the access token.
        /// </summary>
        /// <param name="spSiteUrl">The site url.</param>
        /// <param name="accessToken">The access token.</param>
        /// <returns>A ClientContext instance.</returns>
        private ClientContext CreateClientContext(Uri spSiteUrl, string accessToken)
        {
            if (spSiteUrl != null && !string.IsNullOrEmpty(accessToken))
            {
                return _tokenHelper.GetClientContextWithAccessToken(spSiteUrl.AbsoluteUri, accessToken);
            }

            return null;
        }
    }
}
