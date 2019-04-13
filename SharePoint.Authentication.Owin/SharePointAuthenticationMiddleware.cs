using Microsoft.Owin;
using Microsoft.Owin.Security.Infrastructure;

namespace SharePoint.Authentication.Owin
{
    public class SharePointAuthenticationMiddleware : AuthenticationMiddleware<SharePointAuthenticationOptions>
    {
        public SharePointAuthenticationMiddleware(OwinMiddleware next, SharePointAuthenticationOptions options) : base(next, options)
        {
        }

        protected override AuthenticationHandler<SharePointAuthenticationOptions> CreateHandler()
        {
            return new SharePointAuthenticationHandler(this.Options);
        }
    }
}