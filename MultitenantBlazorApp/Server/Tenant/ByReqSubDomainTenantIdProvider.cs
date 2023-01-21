// Changelogs Date  | Author                | Description
// 2022-12-14       | Anthony Coudène (ACE) | MN-1198 Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using MultitenantBlazorApp.Client.Helpers;
using System.Security.Claims;

namespace MultitenantBlazorApp.Server
{
  /// <summary>
  /// Stateless provider to manager tenant id by subdomain
  /// </summary>
  public class ByReqSubDomainTenantIdProvider : IStatelessTenantIdProvider
  {
    private const string TenantIdKey = "Tenant";

    /// <summary>
    /// Tenant id from request
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public string? GetTenantId(HttpRequest request)
    {
      Guard.IsNotNull(request);

      var url = request.GetDisplayUrl();
      if (string.IsNullOrEmpty(url))
        ThrowHelper.ThrowInvalidOperationException("No display url for request");

      var uri = new Uri(url);
      return uri.GetSubdomain();
    }

    /// <summary>
    /// Tenant id from claims
    /// </summary>
    /// <param name="claimsPrincipal"></param>
    /// <returns></returns>
    public string? GetTenantId(ClaimsPrincipal claimsPrincipal)
    {
      Guard.IsNotNull(claimsPrincipal);

      var identity = claimsPrincipal.Identity as ClaimsIdentity;
      return identity?.FindFirst(TenantIdKey)?.Value;
    }
  }
}