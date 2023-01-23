// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using MultitenantBlazorApp.Client.Tenant;
using System.Security.Claims;

namespace MultitenantBlazorApp.Server
{
  public class MyClaimsTransformation : IClaimsTransformation
  {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IStatefulTenantIdProvider _tenantIdProvider;

    public MyClaimsTransformation(
      IHttpContextAccessor httpContextAccessor,
      IStatefulTenantIdProvider tenantIdProvider)
    {
      Guard.IsNotNull(httpContextAccessor);
      Guard.IsNotNull(tenantIdProvider);

      _httpContextAccessor = httpContextAccessor;
      _tenantIdProvider = tenantIdProvider;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      if (principal == null) throw new ArgumentNullException(nameof(principal));
      if (principal.Identity is not ClaimsIdentity) throw new InvalidOperationException("ClaimsPrincipal is not a ClaimsIdentity");

      var tenantId = _tenantIdProvider.GetCurrentTenantId();
      if (string.IsNullOrWhiteSpace(tenantId))
        tenantId = "default";

      // To fill


      return Task.FromResult(principal);
    }
  }
}
