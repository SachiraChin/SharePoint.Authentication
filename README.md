# SharePoint.Authentication
SharePoint.Authentication is an inject-able SharePoint context and token helper which can be used in multi-tenant applications. 

Reason I came up with this project is due to problems I've met while creating high trust multi-tenant application. In the project I've worked on had different client id and secret for each tenant. As you may recall, SharePoint context provider automcally added to web project currently rely on only one client id and secret which must be added to web.config file. This was not the solution I wanted because, application had different client id and secret (provided by sellar dashboard) for low trust part of the app and had different client id and secret for each teant for high trust.

To authetication layer to function properly, I wanted it to instatiated per tenant/user and wanted it to inject-able via Unity container. This is the solution I came up with to fix that issue.

## Getting started

You must implement few interfaces and abstract classes in order to use this in an application.

### IAuthenticationParameters

This is the base interface which used to define parameters needed for SharePoint authentication. You do not have to implement this interface directly, but implement two abstract classes implemented from this interface.

```csharp
namespace SharePoint.Authentication
{
    public interface IAuthenticationParameters
    {
        string ClientId { get; }
        string ClientSecret { get; }
        string IssuerId { get; }
        string HostedAppHostNameOverride { get; }
        string HostedAppHostName { get; }
        string SecondaryClientSecret { get; }
        string Realm { get; }
        string ServiceNamespace { get; }

        string SigningCertificatePath { get; }
        string SigningCertificatePassword { get; }
        X509Certificate2 Certificate { get; }
        X509SigningCredentials SigningCredentials { get; }
    }
}
```

It's not required to implement all members of this interface, you can implement members only used in your application. For example, if you are using ClientId and ClientSecret for authentication, you can add implementation only for those, you can leave others empty.

### AcsAuthenticationParameters

This abstract class is implemented from ```IAuthenticationParameters```. If you need to use ```AcsTokenHelper```, ```SharePointAcsContext``` or ```SharePointAcsContextProvider``` inside your application, you have to implement this class to provide required parameters.

### HighTrustAuthenticationParameters

This is again an abstract class implemented from ```IAuthenticationParameters```. You can use implementation of this class to use ```HighTrustTokenHelper```, ```SharePointHighTrustContext``` or ```SharePointHighTrustContextProvider``` inside your application.
