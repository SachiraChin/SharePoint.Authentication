using System;
using System.Web;

namespace SharePoint.Authentication
{
    public class LowTrustTokenHelper : TokenHelper
    {
        public LowTrustTokenHelper(LowTrustAuthenticationParameters authenticationParameters) : base(authenticationParameters)
        {
        }

        public Uri GetRedirectUrl(HttpRequest request)
        {
            const string spHasRedirectedToSharePointKey = "SPHasRedirectedToSharePoint";

            if (!string.IsNullOrEmpty(request.QueryString[spHasRedirectedToSharePointKey]))
            {
                return null;
            }


            var spHostUrl = SharePointContext.GetSPHostUrl(new HttpRequestWrapper(request));

            if (spHostUrl == null)
            {
                return null;
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(request.HttpMethod, "POST"))
            {
                return null;
            }

            var requestUrl = request.Url;

            var queryNameValueCollection = HttpUtility.ParseQueryString(requestUrl.Query);

            // Removes the values that are included in {StandardTokens}, as {StandardTokens} will be inserted at the beginning of the query string.
            queryNameValueCollection.Remove(SharePointContext.SPHostUrlKey);
            queryNameValueCollection.Remove(SharePointContext.SPAppWebUrlKey);
            queryNameValueCollection.Remove(SharePointContext.SPLanguageKey);
            queryNameValueCollection.Remove(SharePointContext.SPClientTagKey);
            queryNameValueCollection.Remove(SharePointContext.SPProductNumberKey);

            // Adds SPHasRedirectedToSharePoint=1.
            queryNameValueCollection.Add(spHasRedirectedToSharePointKey, "1");

            var returnUrlBuilder = new UriBuilder(requestUrl);
            returnUrlBuilder.Query = queryNameValueCollection.ToString();

            // Inserts StandardTokens.
            const string standardTokens = "{StandardTokens}";
            var returnUrlString = returnUrlBuilder.Uri.AbsoluteUri;
            returnUrlString = returnUrlString.Insert(returnUrlString.IndexOf("?", StringComparison.Ordinal) + 1, standardTokens + "&");

            // Constructs redirect url.
            var redirectUrlString = GetAppContextTokenRequestUrl(spHostUrl.AbsoluteUri, Uri.EscapeDataString(returnUrlString));

            return new Uri(redirectUrlString, UriKind.Absolute);
        }
    }
}