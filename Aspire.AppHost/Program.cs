/// Prerequisites if needed:
/// Having this kind of line in C:\windows\system32\drivers\etc\hosts (RunAsAdmin mode)
/// 127.0.0.1 mytenant.localhost.com

// Create a builder for the distributed application
var builder = DistributedApplication.CreateBuilder(args);

// Add parameters for username and password
var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "admin", secret: true);

/// Integrate Keycloak for authentication
/// Documentation: https://learn.microsoft.com/en-us/dotnet/aspire/authentication/keycloak-integration?tabs=dotnet-cli
var keycloak = builder.AddKeycloak("keycloak", 9090, username, password)
  .WithArgs("--metrics-enabled=true", "--event-metrics-user-enabled=true", "--event-metrics-user-events=login,logout", "--event-metrics-user-tags=realm,idp,clientId") // Enable metrics for monitoring
  .WithRealmImport("./Realms")
  .WithLifetime(ContainerLifetime.Persistent); // Configure Keycloak with a persistent lifetime, useful to avoid long startup times on each run

/// Add and configure the Multitenant Blazor App server project
/// Documentation: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles
builder.AddProject<Projects.MultitenantBlazorApp_Server>("multitenantblazorapp-server")
  .WithEndpoint("https", endpoint => endpoint.IsProxied = false) // Configure HTTPS endpoint
  .WithReference(keycloak) // Reference Keycloak for authentication
  .WaitFor(keycloak); // Ensure Keycloak is ready before starting

// Add and configure the MyApi project
builder.AddProject<Projects.MyApi>("myapi")
  .WithEndpoint("https", endpoint => endpoint.IsProxied = false) // Configure HTTPS endpoint
  .WithReference(keycloak) // Reference Keycloak for authentication
  .WaitFor(keycloak); // Ensure Keycloak is ready before starting

// Build and run the application
builder.Build().Run();
