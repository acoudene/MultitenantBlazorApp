// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using MultitenantBlazorApp.Client.Helpers;
using MultitenantBlazorApp.Client.Tenant;
using MultitenantBlazorApp.Shared;

namespace MultitenantBlazorApp.Server
{
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
      Guard.IsNotNull(httpContextAccessor);

      _httpContextAccessor = httpContextAccessor;
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
      if (string.IsNullOrEmpty(url))
        ThrowHelper.ThrowInvalidOperationException("No display url for request");

      var uri = new Uri(url);
      if (!uri.TryGetQueryString<string>(TenantIdKey, out var tenantId))
        return default;

      return tenantId;
    }
  }
}