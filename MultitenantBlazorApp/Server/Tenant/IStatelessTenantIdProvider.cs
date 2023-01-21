// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using System.Security.Claims;

namespace MultitenantBlazorApp.Server
{
  /// <summary>
  /// Stateless provider to manager tenant id
  /// </summary>
  public interface IStatelessTenantIdProvider
  {
    /// <summary>
    /// Tenant id from request
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    string? GetTenantId(HttpRequest request);

    /// <summary>
    /// Tenant id from claims
    /// </summary>
    /// <param name="claimsPrincipal"></param>
    /// <returns></returns>
    string? GetTenantId(ClaimsPrincipal claimsPrincipal);
  }
}