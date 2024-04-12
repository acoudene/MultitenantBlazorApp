// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Multitenant.Configuration;
using System.Security.Claims;

namespace Multitenant.Security.AspNetCore;

public class MyClaimsTransformation : IClaimsTransformation
{
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly IStatefulTenantIdProvider _tenantIdProvider;

  public MyClaimsTransformation(
    IHttpContextAccessor httpContextAccessor,
    IStatefulTenantIdProvider tenantIdProvider)
  {
    _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    _tenantIdProvider = tenantIdProvider ?? throw new ArgumentNullException(nameof(tenantIdProvider));
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
