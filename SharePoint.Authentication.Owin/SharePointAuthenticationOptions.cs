using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http.Dependencies;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Security;

namespace SharePoint.Authentication.Owin
{
    public delegate Task AuthenticationHandlerPostAuthenticateDelegate(IOwinContext owinContext, IDependencyScope dependencyScope, ClaimsPrincipal principal);

    public class SharePointAuthenticationOptions : AuthenticationOptions
    {
        public bool AllowNonBrowserRequests { get; set; }
        public int TokenCacheDurationInMinutes { get; set; } = 10;
        public IDependencyResolver DependencyResolver { get; set; }
        public bool InjectCredentialsForHighTrust { get; set; }

        public event AuthenticationHandlerPostAuthenticateDelegate OnAuthenticationHandlerPostAuthenticate;

        public SharePointAuthenticationOptions() : base("SharePointAuthentication")
        {
        }

        internal Task InvokeOnOnAuthenticationHandlerPost(IOwinContext owinContext, IDependencyScope dependencyScope, ClaimsPrincipal principal)
        {
            return OnAuthenticationHandlerPostAuthenticate?.Invoke(owinContext, dependencyScope, principal);
        }
    }
}