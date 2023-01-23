// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MultitenantBlazorApp.Server
{
  public static class AuthenticationBuilderExtensions
  {
    public static AuthenticationBuilder AddByTenantJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions>? configureOptions = null)
    {
      builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());
      return builder.AddScheme<JwtBearerOptions, ByTenantJwtBearerHandler>(authenticationScheme, displayName: null, configureOptions);
    }
  }
}
