// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multitenant.Configuration;
using Multitenant.Configuration.AspNetCore;
using Multitenant.Security.AspNetCore;
using Multitenant.Security.AspNetCore.Configurations;
using MultitenantBlazorApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// Tenant by subdomain
builder.Services.AddTransient<IStatefulTenantIdProvider, ByReqSubDomainTenantIdProvider>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddByTenantJwtBearer(JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddAuthorization();

builder.Services.AddScoped<UserValidationJwtBearerEvents>();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IClaimsTransformation, MyClaimsTransformation>();

builder.Services.AddTransient<IJwtBearerOptionsProvider, ByTenantJwtBearerOptionsProvider>();


builder.Services.AddTransient<AccessTokenHandler>();
builder.Services
  .AddHttpClient("myapi", client => client.BaseAddress = new Uri(new Uri(@"https://mytenant.localhost.com:7269/"), "weatherforecast/"))
  .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
  {
    ServerCertificateCustomValidationCallback =
            (message, cert, chain, errors) => true
  })
  .AddHttpMessageHandler<AccessTokenHandler>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseWebAssemblyDebugging();
}
else
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery(); // Enable this line to protect against CSRF attacks
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(MultitenantBlazorApp.Client._Imports).Assembly);

app.Run();
