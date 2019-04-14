using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.WebApi;
using Owin;
using SharePoint.Authentication.Caching;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.AuthenticationParameters;
using SharePoint.Authentication.Sample.Authentication;

[assembly: OwinStartup(typeof(SharePoint.Authentication.Sample.Startup))]

namespace SharePoint.Authentication.Sample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            var container = GetUnityContainer();
            var dependencyResolver = new UnityHierarchicalDependencyResolver(container);
            config.DependencyResolver = dependencyResolver;
            WebApiConfig.Register(config);
            ConfigureAuth(app, dependencyResolver);

            app.UseWebApi(config);

            AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        private void ConfigureAuth(IAppBuilder app, UnityHierarchicalDependencyResolver dependencyResolver)
        {
            app.Use<SharePointAuthenticationMiddleware>(new SharePointAuthenticationOptions()
            {
                DependencyResolver = dependencyResolver,
                TokenCacheDurationInMinutes = 10,
                AllowNonBrowserRequests = false,
            });

        }

        private UnityContainer GetUnityContainer()
        {
            var container = new UnityContainer();
            container.RegisterType<LowTrustAuthenticationParameters, SampleLowTrustAuthenticationParameters>(new HierarchicalLifetimeManager());
            container.RegisterType<HighTrustAuthenticationParameters, OwinHighTrustAuthenticationParameters>(new HierarchicalLifetimeManager());
            container.RegisterType<LowTrustTokenHelper>(new HierarchicalLifetimeManager());
            container.RegisterType<HighTrustTokenHelper>(new HierarchicalLifetimeManager());

            container.RegisterType<ISharePointSessionProvider, SampleSharePointSessionProvider>(new HierarchicalLifetimeManager());

            return container;
        }
    }
}
