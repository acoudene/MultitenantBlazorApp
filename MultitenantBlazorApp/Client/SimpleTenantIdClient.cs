// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | MN-1198-Creation

using MultitenantBlazorApp.Client.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;

namespace MultitenantBlazorApp.Client.Tenant
{
  /// <summary>
  /// Get a tenant id by query string
  /// </summary>
  public class SimpleTenantIdClient
  {
    public const string TenantIdKey = "Tenant";

    private readonly NavigationManager _navigationManager;

    public SimpleTenantIdClient(NavigationManager navigationManager)
    {
      _navigationManager = navigationManager;
    }

    public string? GetCurrentTenantId()
    {
      var uri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
      bool bIsOk = uri.TryGetQueryString<string?>(TenantIdKey, out string? tenantId);
      return tenantId;
    }

  }
}
