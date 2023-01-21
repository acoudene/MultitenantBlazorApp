using MultitenantBlazorApp.Client.Tenant;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MultitenantBlazorApp.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

var host = default(WebAssemblyHost);
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IStatefulTenantIdProvider, ByNavSubdomainTenantIdProvider>();

//builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthenticationStateProvider>());

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
    });
    //.AddAccountClaimsPrincipalFactory<ApplicationAuthenticationState, LegacyClaimsPrincipalFactory>();

builder.Services.AddApiAuthorization();

host = builder.Build();
await host.RunAsync();
