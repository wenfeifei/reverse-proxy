# Cross-Origin Requests (CORS)

Introduced: preview3

## Introduction

The reverse proxy can handle cross-origin requests before they are proxied to the destination servers. This can reduce load on the destination servers and ensure consistent policies are implemented across your applications.

## Defaults
The requests won't be automatically matched for cors preflight requests unless enabled in the route or application configuration.

## Configuration
CORS policies can be specified per route via [ProxyRoute.CorsPolicy](xref:Microsoft.ReverseProxy.Abstractions.ProxyRoute.CorsPolicy) and can be bound from the `Routes` sections of the config file. As with other route properties, this can be modified and reloaded without restarting the proxy. Policy names are case insensitive.

Example:
```JSON
{
  "ReverseProxy": {
    "Routes": [
      {
        "RouteId": "route1",
        "ClusterId": "cluster1",
        "CorsPolicy": "customPolicy",
        "Match": {
          "Host": "localhost"
        },
      }
    ],
    "Clusters": {
      "cluster1": {
        "Destinations": {
          "cluster1/destination1": {
            "Address": "https://localhost:10001/"
          }
        }
      }
    }
  }
}
```

[CORS policies](https://docs.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-3.1#cors-with-named-policy-and-middleware) are an ASP.NET Core concept that the proxy utilizes. The proxy provides the above configuration to specify a policy per route and the rest is handled by existing ASP.NET Core CORS Middleware.

CORS policies can be configured in Startup.ConfigureServices as follows:
```
public void ConfigureServices(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("customPolicy", builder =>
        {
            builder.AllowAnyOrigin();
        });
    });
}
```

In Startup.Configure add the CORS middleware between Routing and Endpoints.

```
public void Configure(IApplicationBuilder app)
{
    app.UseRouting();

    app.UseCors();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapReverseProxy();
    });
}
```


### DefaultPolicy

Specifying the value `default` in a route's `CorsPolicy` parameter means that route will use the policy defined in [CorsOptions.DefaultPolicy](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.cors.infrastructure.corsoptions.defaultpolicyname).

### Disable CORS

Specifying the value `disable` in a route's `CorsPolicy` parameter means the CORS middleware will refuse the CORS requests.
