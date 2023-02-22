// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using MultitenantBlazorApp.Client.Tenant;
using MultitenantBlazorApp.Shared;
using System.Security.Claims;

namespace MultitenantBlazorApp.Server
{
  /// <summary>
  /// Stateless provider to manager tenant id by subdomain
  /// </summary>
  public class ByReqClaimsTenantIdProvider : IStatefulTenantIdProvider
  {
    private const string TenantIdKey = "Tenant";
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Get tenant key
    /// </summary>
    /// <returns></returns>
    public string GetTenantIdKey() => TenantIdKey;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authenticationState"></param>
    /// <param name="claimsProvider"></param>
    public ByReqClaimsTenantIdProvider(IHttpContextAccessor httpContextAccessor)
    {
      Guard.IsNotNull(httpContextAccessor);

      _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Get tenant id from current state
    /// </summary>
    /// <returns></returns>
    public string? GetCurrentTenantId()
    {
      var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
      if (claimsPrincipal == null)
        return default;

      var identity = claimsPrincipal.Identity as ClaimsIdentity;
      return identity?.FindFirst(TenantIdKey)?.Value;
    }
  }
}