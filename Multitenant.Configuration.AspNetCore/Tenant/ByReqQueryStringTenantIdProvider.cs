// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Multitenant.Configuration.Helpers;

namespace Multitenant.Configuration.AspNetCore;

/// <summary>
/// Stateless provider to manager tenant id by query string
/// </summary>
public class ByReqQueryStringTenantIdProvider : IStatefulTenantIdProvider
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
  public ByReqQueryStringTenantIdProvider(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
  }

  /// <summary>
  /// Get tenant id from current state
  /// </summary>
  /// <returns></returns>
  public string? GetCurrentTenantId()
  {
    var request = _httpContextAccessor.HttpContext?.Request;
    if (request == null)
      return default;

    var url = request.GetDisplayUrl();
    if (string.IsNullOrWhiteSpace(url))
      throw new InvalidOperationException("No display url for request");

    var uri = new Uri(url);
    if (!uri.TryGetQueryString<string>(TenantIdKey, out var tenantId))
      return default;

    return tenantId;
  }
}