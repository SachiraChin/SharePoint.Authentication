using SharePoint.Authentication.Caching;
using SharePoint.Authentication.Owin;
using SharePoint.Authentication.Owin.AuthenticationParameters;
using SharePoint.Authentication.Sample.Authentication;
using System;

using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Resolution;

namespace SharePoint.Authentication.Sample
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
    {
        #region Unity Container
        private static Lazy<IUnityContainer> container =
          new Lazy<IUnityContainer>(() =>
          {
              var container = new UnityContainer();
              RegisterTypes(container);
              return container;
          });

        /// <summary>
        /// Configured Unity Container.
        /// </summary>
        public static IUnityContainer Container => container.Value;
        #endregion

        /// <summary>
        /// Registers the type mappings with the Unity container.
        /// </summary>
        /// <param name="container">The unity container to configure.</param>
        /// <remarks>
        /// There is no need to register concrete types such as controllers or
        /// API controllers (unless you want to change the defaults), as Unity
        /// allows resolving a concrete type even if it was not previously
        /// registered.
        /// </remarks>
        public static void RegisterTypes(IUnityContainer container)
        {
            // NOTE: To load from web.config uncomment the line below.
            // Make sure to add a Unity.Configuration to the using statements.
            // container.LoadConfiguration();

            // TODO: Register your type's mappings here.
            // container.RegisterType<IProductRepository, ProductRepository>();
            //var container = new UnityContainer();
            container.RegisterSingleton<LowTrustAuthenticationParameters, SampleLowTrustAuthenticationParameters>();
            container.RegisterType<HighTrustAuthenticationParameters, OwinHighTrustAuthenticationParameters>(new HierarchicalLifetimeManager());
            container.RegisterType<LowTrustTokenHelper>(new HierarchicalLifetimeManager());
            container.RegisterType<HighTrustTokenHelper>(new HierarchicalLifetimeManager());

            //container.RegisterType<ISharePointContextCacheProvider<SharePointLowTrustContext>, SampleSharePointContextCacheProvider<SharePointLowTrustContext>>(new HierarchicalLifetimeManager());
            //container.RegisterType<ISharePointContextCacheProvider<SharePointHighTrustContext>, SampleSharePointContextCacheProvider<SharePointHighTrustContext>>(new HierarchicalLifetimeManager());

            //container.RegisterType<SharePointLowTrustContextProvider>(new HierarchicalLifetimeManager());
            //container.RegisterType<SharePointHighTrustContextProvider>(new HierarchicalLifetimeManager());

            container.RegisterType<ISharePointSessionProvider, SampleSharePointSessionProvider>(new HierarchicalLifetimeManager());

            //return container;
        }
    }
}