// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MultitenantBlazorApp.Client;
using MultitenantBlazorApp.Client.Tenant;

var host = default(WebAssemblyHost);
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddHttpClient("Authenticated", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Authenticated"));


builder.Services.AddScoped<IStatefulTenantIdProvider, ByNavSubdomainTenantIdProvider>();

builder.Services
    .AddOidcAuthentication(options =>
    {
      if (host == null) throw new InvalidOperationException("Missing host");

      var configuration = builder.Configuration;
      if (configuration == null) throw new InvalidOperationException("Missing configuration");

      var tenantIdClient = host.Services.GetRequiredService<IStatefulTenantIdProvider>();
      if (tenantIdClient == null)
        throw new InvalidOperationException($"Missing {nameof(IStatefulTenantIdProvider)} implementation");

      string? tenantId = tenantIdClient.GetCurrentTenantId();
      if (string.IsNullOrWhiteSpace(tenantId))
        tenantId = "default";

      const string oidcKey = "Oidc";
      const string clientIdKey = "ClientId";
      const string roleClaimTemplateKey = "RoleClaimTemplate";

      string tenantConfigKey = $"{oidcKey}:{tenantId}";
      string clientIdConfigKey = $"{tenantConfigKey}:{clientIdKey}";
      string roleClaimTemplateConfigKey = $"{tenantConfigKey}:{roleClaimTemplateKey}";

      configuration.Bind(tenantConfigKey, options.ProviderOptions);

      // Si on ne surcharge pas cette option, .NET cherche le contenu de "roles" et ne passe donc rien dans l'identité
      string? clientId = configuration[clientIdConfigKey];
      if (string.IsNullOrWhiteSpace(clientId)) throw new InvalidOperationException($"Missing {clientIdConfigKey} configuration for tenant: {tenantId}");

      string? roleClaimTemplateConfig = configuration[roleClaimTemplateConfigKey];
      if (string.IsNullOrWhiteSpace(roleClaimTemplateConfig)) throw new InvalidOperationException($"Missing {roleClaimTemplateConfigKey} configuration for tenant: {tenantId}");

      options.UserOptions.RoleClaim = roleClaimTemplateConfig.Replace($"${{{clientIdKey}}}", clientId);
    })
    .AddAccountClaimsPrincipalFactory<MyClaimsPrincipalFactory>();

builder.Services.AddApiAuthorization();

host = builder.Build();
await host.RunAsync();
