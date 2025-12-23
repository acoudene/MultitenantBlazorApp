// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Multitenant.Configuration;
using Multitenant.Configuration.WebAssembly.Configurations;
using Multitenant.Configuration.WebAssembly.Tenant;
using Multitenant.Security.WebAssembly;

var host = default(WebAssemblyHost);
var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services
    .AddHttpClient("Authenticated", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Authenticated"));

//builder.Services
//    .AddHttpClient("weatherforecast", client => client.BaseAddress = new Uri($"https://mylocaltenant.localhost.com:7269/weatherforecast/"))
//    // Http Message handler to integrate access token to http headers
//    .AddHttpMessageHandler(x =>
//    {
//      var handler = x.GetRequiredService<AuthorizationMessageHandler>()
//          .ConfigureHandler(new[] { "https://mylocaltenant.localhost.com:7269" });

//      return handler;
//    });

builder.Services
    .AddHttpClient("weatherforecast", client => client.BaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), "weatherforecast/"))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped<IStatefulTenantIdProvider, ByNavSubdomainTenantIdProvider>();
builder.Services.AddScoped<IOidcProviderOptionsProvider, ByTenantOidcProviderOptionsProvider>();

builder.Services
    .AddOidcAuthentication(options =>
    {
      if (host == null) throw new InvalidOperationException("Missing host");

      var oidcProviderOptionsProvider = host.Services.GetRequiredService<IOidcProviderOptionsProvider>();
      if (oidcProviderOptionsProvider == null)
        throw new InvalidOperationException($"Missing {nameof(IOidcProviderOptionsProvider)} implementation");

      oidcProviderOptionsProvider.ConfigureOptions(options.ProviderOptions, options.UserOptions);
    })
    .AddAccountClaimsPrincipalFactory<MyClaimsPrincipalFactory>();

builder.Services.AddApiAuthorization();

host = builder.Build();
await host.RunAsync();
