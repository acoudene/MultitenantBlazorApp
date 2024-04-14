using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Multitenant.Configuration;
using Multitenant.Configuration.AspNetCore;
using Multitenant.Security.AspNetCore;
using Multitenant.Security.AspNetCore.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Tenant by subdomain
builder.Services.AddTransient<IStatefulTenantIdProvider, ByReqSubDomainTenantIdProvider>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddByTenantJwtBearer(JwtBearerDefaults.AuthenticationScheme);

builder.Services.AddScoped<UserValidationJwtBearerEvents>();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddTransient<IClaimsTransformation, MyClaimsTransformation>();

builder.Services.AddTransient<IJwtBearerOptionsProvider, ByTenantJwtBearerOptionsProvider>();

builder.Services.AddControllers();

builder.Services.AddSingleton<IMemoryCache, MemoryCache>();

const string allowSpecificOrigins = "frontend";
builder.Services.AddCors(options =>
{
  options.AddPolicy(name: allowSpecificOrigins,
                  policy =>
                  {
                    policy.WithOrigins("https://mylocaltenant.localhost.com:5002")
                    .AllowAnyMethod() // fix error : has been blocked by CORS policy: Method PUT is not allowed by Access-Control-Allow-Methods in preflight response.
                    .AllowAnyHeader();
                  });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseCors(allowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
