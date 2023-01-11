using Alteva.MissionOne.Proxies.Tenant;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MultitenantBlazorApp.Client;

var host = default(WebAssemblyHost);
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<SimpleTenantIdClient>();

builder.Services
    .AddOidcAuthentication(options =>
    {

      var configuration = builder.Configuration;
      if (configuration == null) throw new InvalidOperationException("Missing configuration");

      // TODO ACE: to set as dynamic key from tenant
      //string tenantId = "tenant01";

      var tenantIdClient = host.Services.GetRequiredService<SimpleTenantIdClient>();
      string tenantId = tenantIdClient.GetCurrentTenantId();

      const string oidcKey = "Oidc";
      const string clientIdKey = "ClientId";
      const string roleClaimTemplateKey = "RoleClaimTemplate";

      string tenantConfigKey = $"{oidcKey}:{tenantId}";
      string clientIdConfigKey = $"{tenantConfigKey}:{clientIdKey}";
      string roleClaimTemplateConfigKey = $"{tenantConfigKey}:{roleClaimTemplateKey}";

      configuration.Bind(tenantConfigKey, options.ProviderOptions);

      // Si on ne surcharge pas cette option, .NET cherche le contenu de "roles" et ne passe donc rien dans l'identité
      string clientId = configuration[clientIdConfigKey];
      string roleClaimTemplateConfig = configuration[roleClaimTemplateConfigKey];
      options.UserOptions.RoleClaim = roleClaimTemplateConfig.Replace($"${{{clientIdKey}}}", clientId);

      //options.UserOptions.NameClaim = "preferred_username"; // La valeur par défaut name est bien
      //options.UserOptions.ScopeClaim= "scope";
      //options.ProviderOptions.PostLogoutRedirectUri = "/";
    });

builder.Services.AddApiAuthorization();

host = builder.Build();
await host.RunAsync();
