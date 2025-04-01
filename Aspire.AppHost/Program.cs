/// Prerequisites if needed:
/// Having this kind of line in C:\windows\system32\drivers\etc\hosts (RunAsAdmin mode)
/// 127.0.0.1 mytenant.localhost.com

var builder = DistributedApplication.CreateBuilder(args);

/// https://learn.microsoft.com/en-us/dotnet/aspire/authentication/keycloak-integration?tabs=dotnet-cli
var keycloak = builder.AddKeycloak("keycloak", 9090)
  .WithRealmImport("./Realms");

/// https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles
builder.AddProject<Projects.MultitenantBlazorApp_Server>("multitenantblazorapp-server")
  .WithEndpoint("https", endpoint => endpoint.IsProxied = false)
  .WithReference(keycloak)
  .WaitFor(keycloak);

builder.AddProject<Projects.MyApi>("myapi")
  .WithEndpoint("https", endpoint => endpoint.IsProxied = false)
  .WithReference(keycloak)
  .WaitFor(keycloak);

builder.Build().Run();
