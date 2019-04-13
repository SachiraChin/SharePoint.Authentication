using System.Web.Http.Dependencies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;

namespace SharePoint.Authentication.Owin
{
    public class SharePointAuthenticationOptions : AuthenticationOptions
    {
        public TokenValidationParameters JwtValidationParameters { get; set; }
        public bool AllowNonBrowserRequests { get; set; }
        public int TokenCacheDurationInMinutes { get; set; } = 10;
        public IDependencyResolver DependencyResolver { get; set; }

        public SharePointAuthenticationOptions() : base("SharePointAuthentication")
        {
        }
    }
}