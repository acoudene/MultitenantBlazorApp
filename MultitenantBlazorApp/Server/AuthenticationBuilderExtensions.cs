// Changelogs Date  | Author                | Description
// 2022-12-14       | Anthony Coudène (ACE) | MN-1198 Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MultitenantBlazorApp.Server
{
  public static class AuthenticationBuilderExtensions
  {
    public static AuthenticationBuilder AddByTenantJwtBearer(this AuthenticationBuilder builder, string authenticationScheme, Action<JwtBearerOptions> action = null)
    {
      builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<JwtBearerOptions>, JwtBearerPostConfigureOptions>());

      if (action != null)
        return builder.AddScheme<JwtBearerOptions, ByTenantJwtBearerHandler>(authenticationScheme, null, action);

      return builder.AddScheme<JwtBearerOptions, ByTenantJwtBearerHandler>(authenticationScheme, null, _ => { });
    }
  }
}
