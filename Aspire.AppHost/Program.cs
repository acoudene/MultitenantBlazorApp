/// Create a builder for the distributed application
/// 
/// Prerequisites if needed:
/// Having this kind of line in C:\windows\system32\drivers\etc\hosts (RunAsAdmin mode)
/// 127.0.0.1 mytenant.localhost.com

// Create a builder for the distributed application
var builder = DistributedApplication.CreateBuilder(args);

/// Use Jaeger
var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithEndpoint("http", endpoint => { endpoint.Port = 16686; endpoint.TargetPort = 16686; endpoint.UriScheme = "http"; })     // Interface web Jaeger
    .WithEndpoint("gRPC", endpoint => { endpoint.Port = 4317; endpoint.TargetPort = 4317; endpoint.UriScheme = "http"; })       // OTLP gRPC
    .WithEndpoint("http-oltp", endpoint => { endpoint.Port = 4318; endpoint.TargetPort = 4318; endpoint.UriScheme = "http"; })  // OTLP HTTP
    .WithLifetime(ContainerLifetime.Persistent)
    ;

// Add parameters for username and password
var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "admin", secret: true);

/// Integrate Keycloak for authentication
/// Documentation: https://learn.microsoft.com/en-us/dotnet/aspire/authentication/keycloak-integration?tabs=dotnet-cli
var keycloak = builder.AddKeycloak("keycloak", 9090, username, password)
  .WithArgs(
  "--tracing-enabled=true", // Enable tracing for monitoring, export by default to http://localhost:4317 in gRPC
  "--tracing-endpoint=http://jaeger:4317", // 👈 Ajout de l'endpoint correct
  "--metrics-enabled=true", // Enable metrics for monitoring
  "--event-metrics-user-enabled=true",
  "--event-metrics-user-events=login,logout",
  "--event-metrics-user-tags=realm,idp,clientId"
  //,"--log-level=INFO,org.keycloak:debug,org.keycloak.events:trace" // Enable detailed logging for debugging
  //,"--log-level=debug" // Enable detailed logging for debugging
  )
  .WithRealmImport("./Realms")
  .WithLifetime(ContainerLifetime.Persistent)
  .WaitFor(jaeger); // Configure Keycloak with a persistent lifetime, useful to avoid long startup times on each run

/// Add and configure the Multitenant Blazor App server project
/// Documentation: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/launch-profiles
builder.AddProject<Projects.MultitenantBlazorApp>("multitenantblazorapp")
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
