// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multitenant.Configuration;
using Multitenant.Configuration.AspNetCore;
using Multitenant.Security.AspNetCore;
using Multitenant.Security.AspNetCore.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Tenant by subdomain
builder.Services.AddTransient<IStatefulTenantIdProvider, ByReqSubDomainTenantIdProvider>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddByTenantJwtBearer(JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddScoped<UserValidationJwtBearerEvents>();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IClaimsTransformation, MyClaimsTransformation>();

builder.Services.AddTransient<IJwtBearerOptionsProvider, ByTenantJwtBearerOptionsProvider>();

builder.Services.AddHttpClient("myapi", client => client.BaseAddress = new Uri(@"http+https://myapi"));

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

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
