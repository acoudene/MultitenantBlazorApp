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
    public static AuthenticationBuilder AddByTenantJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions>? action = null)
    {
      builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());

      if (action != null)
        return builder.AddScheme<JwtBearerOptions, ByTenantJwtBearerHandler>(authenticationScheme, null, action);

      return builder.AddScheme<JwtBearerOptions, ByTenantJwtBearerHandler>(authenticationScheme, null, _ => { });
    }
  }
}
