# SharePoint authentication for modern web apps

```SharePoint.Authentication``` is an inject-able SharePoint context and token helper which can be used in multi-tenant applications. Abilities of this library extended by its sister library ```SharePoint.Authentication.Owin```.

Reason I came up with this project is due to problems I've met while creating high trust multi-tenant application. In the project I've worked on had different client id and secret for each tenant. As you may recall, SharePoint context provider automatically added to web project currently rely on only one client id and secret which must be added to web.config file. This was not the solution I wanted because, application had different client id and secret (provided by seller dashboard) for low trust part of the app and had different client id and secret for each tenant for high trust. To authentication layer to function properly, I wanted it to instantiated per tenant/user and wanted it to inject-able via Unity container. This is the solution I came up with to fix that issue.

Solution I came up consists of two separate libraries, ```SharePoint.Authentication``` and ```SharePoint.Authentication.Owin```. First library is the base which I made from token helpers and providers made by Microsoft. Second library made with one specific purpose in mind which is to use SharePoint authentication in MVC or Web API based applications. ```SharePoint.Authentication``` contains all the functions you need to build an authentication layer, but if you want don't want to build one yourself, you can use ```SharePoint.Authentication.Owin```.

## DISCLAIMER

This library is just an extension for existing code provided by Microsoft. Almost all code here is copied from Microsoft provided authentication and context provider code, I made few changes to work with scenarios described below, but all props goes to engineers who wrote it from scratch.

## How to install

This package is available to download via nuget package manager.

```bash
Install-Package SharePoint.Authentication
Install-Package SharePoint.Authentication.Owin
```

## Getting started

You must implement few interfaces and abstract classes in order to use this in an application. Please follow steps provided in next few paragraphs to get it up and running.

### IAuthenticationParameters

This is the base interface which used to define parameters needed for SharePoint authentication. You do not have to implement this interface directly, but implement two abstract classes implemented from this interface.

```csharp
namespace SharePoint.Authentication
{
    public interface IAuthenticationParameters
    {
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string IssuerId { get; set; }
        string HostedAppHostNameOverride { get; set; }
        string HostedAppHostName { get; set; }
        string SecondaryClientSecret { get; set; }
        string Realm { get; set; }
        string ServiceNamespace { get; set; }

        string SigningCertificatePath { get; set; }
        string SigningCertificatePassword { get; set; }
        X509Certificate2 Certificate { get; set; }
        X509SigningCredentials SigningCredentials { get; set; }
    }
}
```

It's not required to implement all members of this interface, you can implement members only used in your application. For example, if you are using ClientId and ClientSecret for authentication, you can add implementation only for those, you can leave others empty.

### LowTrustAuthenticationParameters

This abstract class is implemented from ```IAuthenticationParameters```. If you need to use ```LowTrustTokenHelper```, ```SharePointLowTrustContext``` or ```SharePointLowTrustContextProvider``` inside your application, you have to implement this class to provide required parameters.

```csharp
public class SampleLowTrustAuthenticationParameters :LowTrustAuthenticationParameters
{
    public sealed override string ClientId { get; set; }
    public sealed override string ClientSecret { get; set; }
    public SampleLowTrustAuthenticationParameters()
    {
        ClientId = ConfigurationManager.AppSettings["sampleMvc:LowTrustClientId"];
        ClientSecret = ConfigurationManager.AppSettings["sampleMvc:LowTrustClientSecret"];
    }
}
```

As you can see in sample above, here I make sure that client id and client secret is loaded once from constructor of parameters class. When you use dependency injection to inject instance of this class, you can make it a singleton to improve performance.

### HighTrustAuthenticationParameters

This is again an abstract class implemented from ```IAuthenticationParameters```. You can use implementation of this class to use ```HighTrustTokenHelper```, ```SharePointHighTrustContext``` or ```SharePointHighTrustContextProvider``` inside your application.

```csharp
public class OwinHighTrustAuthenticationParameters :HighTrustAuthenticationParameters
{
    public override string ClientId
    {
        get
        {
            var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");
            return cachedSession.HighTrustClientId;
        }
        set => throw new NotImplementedException();
    }
    public override string ClientSecret
    {
        get
        {
            var cachedSession = HttpContext.Current.GetOwinContext().Get<CachedSession>("CachedSession");
            return cachedSession.HighTrustClientSecret;
        }
        set => throw new NotImplementedException();
    }
}
```

Above sample is taken from ```SharePoint.Authentication.Owin``` which I have provided to design Owin based Web APIs. As you can see, here I get high trust client id and secret from a instance saved inside Owin context. (This is just an example usage, if you use fixed client id and secret for both high trust and low trust connections, you can use same sample as previous.)

## Inject and use

After having parameter classes implemented, you can your favorite dependency resolver to inject dependencies. I have used Unity in below samples. Since the sample project has both MVC 5 and Web API 2, I have used ```Unity.Mvc``` and ```Unity.AspNet.WebApi``` respectively to register resolvers.

Now when you use this library, you have two ways to use its classes. If you are fond of ```SharePointContextProvider``` and ```SharePointContext```, you can use it's implementation. But in my personal view, using these implementations gives you less control over how things done. But nevertheless, its really easy to use implementation and works without any issue whatsoever.

But if you want more control, you can completely avoid use of ```SharePointContextProvider``` and ```SharePointContext``` and work only with ```TokenProvider```. Since it's bit advanced usage compared to ```SharePointContextProvider```, I have came up with ```SharePoint.Authentication.Owin``` which simplifies the usage of it.

### Use ```SharePointContextProvider```

In order to use ```SharePointContextProvider```, you first have to implement its ```ISharePointContextCacheProvider```, this allows you to implement your own caching mechanism for ```SharePointContext```. By default, Microsoft provided implementation uses sessions storage to cache the context. To use sessions for caching, you can use simple implementation like this.

```csharp
namespace SharePoint.Authentication.Sample.Authentication
{
    public class SampleSharePointContextCacheProvider<T> : ISharePointContextCacheProvider<T> where T : SharePointContext
    {
        private const string SPContextKey = "SPContext";

        public T Get(HttpContextBase httpContext)
        {
            return httpContext.Session[SPContextKey] as T;
        }

        public Task<T> GetAsync(HttpContextBase httpContext)
        {
            return Task.FromResult(httpContext.Session[SPContextKey] as T);
        }

        public void Set(HttpContextBase httpContext, T context)
        {
            httpContext.Session[SPContextKey] = context;
        }

        public Task SetAsync(HttpContextBase httpContext, T context)
        {
            httpContext.Session[SPContextKey] = context;

            return Task.FromResult(true);
        }
    }
}

```

After having this interface implemented, you can use your dependency resolver to register classes as below.

```csharp
container.RegisterSingleton<LowTrustAuthenticationParameters, SampleLowTrustAuthenticationParameters>();
container.RegisterType<HighTrustAuthenticationParameters, OwinHighTrustAuthenticationParameters>();
container.RegisterType<LowTrustTokenHelper>();
container.RegisterType<HighTrustTokenHelper>();

container.RegisterType<ISharePointContextCacheProvider<SharePointLowTrustContext>, SampleSharePointContextCacheProvider<SharePointLowTrustContext>>();
container.RegisterType<ISharePointContextCacheProvider<SharePointHighTrustContext>, SampleSharePointContextCacheProvider<SharePointHighTrustContext>>();

container.RegisterType<SharePointLowTrustContextProvider>();
container.RegisterType<SharePointHighTrustContextProvider>();
```

After registering your dependencies, you can get any of registered classes injected to your controllers.

```csharp
public class HomeController : Controller
{
    private readonly SharePointLowTrustContextProvider _lowTrustContextProvider;

    public HomeController(SharePointLowTrustContextProvider lowTrustContextProvider)
    {
        _lowTrustContextProvider = lowTrustContextProvider;
    }

    public async Task<ActionResult> Index()
    {
        ViewBag.Title = "Home Page";
        var sharePointContext = _lowTrustContextProvider.GetSharePointContext(HttpContext);
        using (var clientContext = sharePointContext.CreateUserClientContextForSPHost())
        {
            var web = clientContext.Web;
            var user = clientContext.Web.CurrentUser;
            clientContext.Load(web, w => w.Title);
            clientContext.Load(user, u => u.Title);
            await clientContext.ExecuteQueryAsync();
        }
        return View();
    }
}
```

### Using ```SharePoint.Authentication.Owin```

Implemented this library to use to use in modern API based applications. Though I said modern, it doesn't work with .NET Core APIs, but it works with good old ASP.NET Web API 2 (which is almost good as .NET Core API).

I tried to simplify the usage most as I can, but I'm not sure I went too far with it. It's up to you to give me feedback on how it needs to be changed.

This library primarily consists of 4 major components. Those are,

1. Easy to use SharePoint login controller
2. Owin authentication middleware
3. Authenticated client side API access
4. Multi-tenant high trust package provider

In order to use above components, you have implement ```ISharePointSessionProvider``` within your application. If you do not wish to use multi-tenant high trust package manager, you can ignore ```HighTrustCredentials``` related methods. Sample I have provided in the solution looks like below.

```csharp
public class SampleSharePointSessionProvider : ISharePointSessionProvider
{
    private const string VerySecurePassword = "[Very Secure Password]";

    public async Task SaveSharePointSession(Guid sessionId, SharePointSession sharePointSession)
    {
        using var context = new SampleDataContext();
        var model = new SampleSharePointSession()
        {
            SessionId = sessionId,
            ContextToken = string.IsNullOrWhiteSpace(sharePointSession.ContextToken) ? null : StringCipher.Encrypt(sharePointSession.ContextToken, VerySecurePassword),
            ContextTokenAuthority = sharePointSession.ContextTokenAuthority,
            SharePointAppWebUrl = sharePointSession.SharePointAppWebUrl,
            SharePointHostWebUrl = sharePointSession.SharePointHostWebUrl,
        };
        context.SampleSharePointSessions.Add(model);
        await context.SaveChangesAsync();
    }

    public async Task<SharePointSession> GetSharePointSession(Guid sessionId)
    {
        using var context = new SampleDataContext();
        var dbModel = await context.SampleSharePointSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
        if (dbModel == null) return null;
        var model = new SharePointSession()
        {
            SessionId = sessionId,
            ContextToken = dbModel.ContextToken == null ? null : StringCipher.Decrypt(dbModel.ContextToken, VerySecurePassword),
            ContextTokenAuthority = dbModel.ContextTokenAuthority,
            SharePointAppWebUrl = dbModel.SharePointAppWebUrl,
            SharePointHostWebUrl = dbModel.SharePointHostWebUrl,
        };
        return model;
    }

    public async Task SaveHighTrustCredentials(HighTrustCredentials highTrustCredentials)
    {
        using var context = new SampleDataContext();
        var model = new SampleHighTrustCredentials()
        {
            ClientId = string.IsNullOrWhiteSpace(highTrustCredentials.ClientId) ? null : StringCipher.Encrypt(highTrustCredentials.ClientId, VerySecurePassword),
            ClientSecret = string.IsNullOrWhiteSpace(highTrustCredentials.ClientSecret) ? null : StringCipher.Encrypt(highTrustCredentials.ClientSecret, VerySecurePassword),
            SharePointHostWebUrl = highTrustCredentials.SharePointHostWebUrl,
            SharePointHostWebUrlHash = GetSha256(highTrustCredentials.SharePointHostWebUrl),
        };
        context.SampleHighTrustCredentials.Add(model);
        await context.SaveChangesAsync();
    }

    public async Task<HighTrustCredentials> GetHighTrustCredentials(string spHostWebUrl)
    {
        using var context = new SampleDataContext();
        var spHostWebUrlHash = GetSha256(spHostWebUrl);
        var dbModel = await context.SampleHighTrustCredentials.FirstOrDefaultAsync(c => c.SharePointHostWebUrlHash == spHostWebUrlHash);
        if (dbModel == null) return null;
        return new HighTrustCredentials()
        {
            ClientId = dbModel.ClientId == null ? null : StringCipher.Decrypt(dbModel.ClientId, VerySecurePassword),
            ClientSecret = dbModel.ClientSecret == null ? null : StringCipher.Decrypt(dbModel.ClientSecret, VerySecurePassword),
            SharePointHostWebUrl = dbModel.SharePointHostWebUrl,
        };
    }
}
```

#### SharePoint login controller

This library comes with an API controller which can be use as entry point for the application. You can have to implement this in order to use authentication middleware inside the application.

```csharp
[RoutePrefix("login")]
public class LoginController : SharePointLoginController
{
    public override string LowTrustLandingPageUrl { get; } = "/";

    public LoginController(ISharePointSessionProvider sharePointSessionProvider, LowTrustTokenHelper lowTrustTokenHelper, HighTrustTokenHelper highTrustTokenHelper, HighTrustAuthenticationParameters highTrustAuthenticationParameters) : base(sharePointSessionProvider, lowTrustTokenHelper, highTrustTokenHelper, highTrustAuthenticationParameters)
    {
    }

    [HttpPost]
    [Route]
    public override Task<HttpResponseMessage> LowTrustLoginAsync()
    {
        return base.LowTrustLoginAsync();
    }

    public override Task LowTrustPostAuthenticationAsync(ClientContext clientContext)
    {
        return base.LowTrustPostAuthenticationAsync(clientContext);
    }

    public override CookieHeaderValue GetCookieHeader(string cookieName, string cookieValue, string domain, DateTimeOffset expires, bool secure, bool httpOnly)
    {
        return base.GetCookieHeader(cookieName, cookieValue, domain, expires, secure, httpOnly);
    }
}
```

Just like that, you have login entry point for your application. As you can see from the route config, entry point for above implementation is ```{app-root}\login```. You can set this login url for SharePoint add-in package. This endpoint invokes the ```ISharePointSessionProvider.SaveSharePointSession``` to save session details and adds HTTPOnly, Secure cookie to store session id. Optionally you can override ```GetCookieHeader``` make adjustments to cookie itself.

#### Owin authentication middleware

This middleware is there for you to enable full SharePoint based authentication for your API/web application. It has inbuilt caching enabled and contains locking mechanisms to stop sessions to be generated in case of parallel API calls. You can use middleware as below.

```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        var dependencyResolver = new UnityDependencyResolver(UnityConfig.Container);
        config.DependencyResolver = dependencyResolver;
        WebApiConfig.Register(config);
        ConfigureAuth(app, dependencyResolver);
        app.UseWebApi(config);
    }

    private void ConfigureAuth(IAppBuilder app, System.Web.Http.Dependencies.IDependencyResolver dependencyResolver)
    {
        var sharePointAuthenticationOptions = new SharePointAuthenticationOptions()
        {
            DependencyResolver = dependencyResolver,
            TokenCacheDurationInMinutes = 10,
            AllowNonBrowserRequests = false,
            InjectCredentialsForHighTrust = true,
        };
        sharePointAuthenticationOptions.OnAuthenticationHandlerPostAuthenticate += OnAuthenticationHandlerPostAuthenticate;
        app.Use<SharePointAuthenticationMiddleware>(sharePointAuthenticationOptions);
    }

    private Task OnAuthenticationHandlerPostAuthenticate(IOwinContext owinContext, IDependencyScope dependencyScope, ClaimsPrincipal principal)
    {
        return Task.FromResult(false);
    }
}
```

Given that ```SharePointLoginController``` is implemented and login works without any issue, this middleware make sure that all controllers honors the ```[Authorize]``` attribute. This middleware has caching implemented by default so database won't get throttled by session validation queries. It also has locking mechanism in-place to manage cache coherency.

Default cache provider has ```MemoryCache``` to store session data, lock provider uses ```ConcurrentDictionary``` with ```SemaphoreSlim``` to lock concurrent session invalidations. If you want to use more advanced caching and locking mechanisms, you have to implement ```ICacheProvider``` and ```ILockProvider```. You can inject new implementations via your dependency resolver and middleware will automatically will pick it up for you.

#### Authenticated client side API access

Well, if you have implemented above two components properly, this comes with them without any hassle. You don't need any authentication validations on client side, what you have to do is just send API requests from which ever the client side library you prefer, it will work just like that.

```html
@section Scripts{
    <script type="text/javascript">
        $(document).ready(function () {
            $.getJSON("/api/values", function (response) {
                var responseJson = JSON.stringify(response);
                $("#apiResponse").html("Endpoint: /api/values<br />Response: " + responseJson);
            });
        });
    </script>
}
<h2>Client Side API Access</h2>
<div id="apiResponse">

</div>
```

TRUST ME, IT WORKS. ;)

#### Multi-tenant high trust package provider

This is the final component of the application and it has very specific purpose. Reason I came up with it is the all the troubles I went through to make SharePoint store provider hosted app which has high trust add-in packed with it. This component made so that developers and follow good security practices easily for their multi-tenant application.

Usually when multi-tenant application is developed, they have fixed client id and secret. But when app is distributed via app store and admin is installing the high trust add-in manually, keeping fixed client id and secret is not very secure thing to do. Solution I came up with is, component where it generates unique client id and secret per tenant and update your high trust app package with new client id.

In this way, you never have to worry about leaking a fixed client id and secret in any way, and it's very easy to secure client id and secret when you have control over it. As you may have noticed in my ```ISharePointSessionProvider``` implementation, it stores client id and secret encrypted in the database itself. You can use more secure ways than I provided above to make sure it's stored securely.

To enabled this, you have to set ```InjectCredentialsForHighTrust``` as ```true``` in ```SharePointAuthenticationOptions``` as you can see in above example. Then, update your login controller as below.

```csharp
[RoutePrefix("login")]
public class LoginController : SharePointLoginController
{
    public override string HighTrustLandingPageUrl { get; } = "/";
    public override string HighTrustAppPackageName { get; } = "HighTrustApp.app";
    public override string HighTrustLoginPageUrl => "https://spauthtest.com:44388/login/high-trust";

    public LoginController(ISharePointSessionProvider sharePointSessionProvider, LowTrustTokenHelper lowTrustTokenHelper, HighTrustTokenHelper highTrustTokenHelper, HighTrustAuthenticationParameters highTrustAuthenticationParameters) : base(sharePointSessionProvider, lowTrustTokenHelper, highTrustTokenHelper, highTrustAuthenticationParameters)
    {
    }

    [HttpPost]
    [Route("high-trust")]
    [Authorize]
    public override Task<HttpResponseMessage> HighTrustLoginAsync()
    {
        return base.HighTrustLoginAsync();
    }

    public override Task HighTrustPostAuthenticationAsync(ClientContext clientContext)
    {
        return base.HighTrustPostAuthenticationAsync(clientContext);
    }

    [HttpGet]
    [Route("high-trust-package")]
    [Authorize]
    public override Task<HttpResponseMessage> DownloadHighTrustAddInAsync()
    {
        return base.DownloadHighTrustAddInAsync();
    }

    public override Task<Stream> GetHighTrustAddInPackage()
    {
        var packageStream = EmbeddedData.Get<Startup>("SharePoint.Authentication.Sample.Templates.HighTrustAppPackage.app");
        return Task.FromResult(packageStream);
    }
}
```

Overrides you must make in order to this to work are, ```HighTrustLoginPageUrl``` and ```GetHighTrustAddInPackage```. ```HighTrustLoginPageUrl``` is the login URL which you can set from ```HighTrustLoginAsync``` method. ```GetHighTrustAddInPackage``` is there to get your high trust app package.

After updating your login page for this, simple implementation as below to allow user to download generated app package.

```csharp
public class HighTrustController : Controller
{
    private readonly ISharePointSessionProvider _sharePointSessionProvider;
    private readonly HighTrustTokenHelper _highTrustTokenHelper;

    public HighTrustController(ISharePointSessionProvider sharePointSessionProvider, HighTrustTokenHelper highTrustTokenHelper)
    {
        _sharePointSessionProvider = sharePointSessionProvider;
        _highTrustTokenHelper = highTrustTokenHelper;
    }

    // GET: HighTrust
    public async Task<ActionResult> Index()
    {
        var spHostUrl = this.Request.GetSharePointHostWebUrl();
        var credentials = await _sharePointSessionProvider.GetHighTrustCredentials(spHostUrl);
        if (credentials == null)
        {
            credentials = HighTrustCredentials.GenerateUniqueHighTrustCredentials(spHostUrl, "90601b8750f84a699771379aa944fca1", "74adaa2afa4946ce981d8fe212543752");
            await _sharePointSessionProvider.SaveHighTrustCredentials(credentials);
            return View(credentials);
        }

        try
        {
            using (var context = await _highTrustTokenHelper.GetAppOnlyAuthenticatedContext(spHostUrl))
            {
                var web = context.Web;
                var user = context.Web.CurrentUser;
                context.Load(web, w => w.Title);
                context.Load(user, u => u.Title, u => u.IsSiteAdmin);
                await context.ExecuteQueryAsync();
                ViewBag.SiteTitle = web.Title;
                ViewBag.UserTitle = user.Title;
                ViewBag.UserIsSiteAdmin = user.IsSiteAdmin;
            }
        }
        catch (Exception e)
        {
            ViewBag.HighTrustValidationMessage = $"High trust validation failed. {e.Message}";
        }
        return View(credentials);
    }
}
```

```html
<h1>SharePoint high trust connection</h1>

<p><b>Client id: </b> @Model.ClientId</p>
<p><b>Client secret: </b> @Model.ClientSecret</p>

<div class="row">
    <div class="col-md-12">
        <h2>SharePoint High Trust Connection</h2>
        <p><b>Host web title:</b> @ViewBag.SiteTitle</p>
        <p><b>User name:</b> @ViewBag.UserTitle</p>
        <p><b>User is site admin:</b> @ViewBag.UserIsSiteAdmin</p>
        <p style="color: red">@ViewBag.HighTrustValidationMessage</p>
    </div>
</div>
<div class="row">
    <div class="col-md-12">
        <h2>Setup SharePoint High App</h2>
        <ol>
            <li>Go to app registration page -> <a href="@Model.SharePointHostWebUrl/_layouts/15/AppRegNew.aspx" target="_blank">@Model.SharePointHostWebUrl/_layouts/15/AppRegNew.aspx</a></li>
            <li>
                Register new app with below details.
                <ul>
                    <li>Client id: @Model.ClientId</li>
                    <li>Client secret: @Model.ClientSecret</li>
                    <li>Title: Sample high trust app</li>
                    <li>App domain: @(new Uri("https://spauthtest.com:44388/login/high-trust").Authority)</li>
                    <li>Redirect uri: https://spauthtest.com:44388/login/high-trust</li>
                </ul>
            </li>
            <li>Download app package (zip) -> <a href="https://spauthtest.com:44388/login/high-trust-package" target="_blank">https://spauthtest.com:44388/login/high-trust-package</a></li>
            <li>Go to app catalog and install downloaded app package (unzip first)</li>
            <li>Go to Site contents and install high trust app</li>
            <li>Refresh page to validate credentials</li>
        </ol>
    </div>
</div>
```

Please look into the sample project I have provided here to more details.

I hope this help someone to make a great application and looking forward for your feedback.

Happy coding!