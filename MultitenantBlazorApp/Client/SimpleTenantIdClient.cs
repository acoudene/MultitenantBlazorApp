// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | MN-1198-Creation

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System;

namespace Alteva.MissionOne.Proxies.Tenant
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

    public string GetCurrentTenantId()
    {
      var uri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
      bool bIsOk = TryGetQueryString<string?>(uri, TenantIdKey, out string? tenantId);
      if (!bIsOk || string.IsNullOrWhiteSpace(tenantId)) throw new InvalidOperationException("Missing tenant!");
      return tenantId;
    }

    public static bool TryGetQueryString<T>(Uri uri, string key, out T? value)
    {
      string query = uri.IsAbsoluteUri ? uri.Query : uri.ToString();

      if (string.IsNullOrWhiteSpace(query))
      {
        value = default;
        return false;
      }
      return TryGetQueryStringInternal<T>(query, key, out value);
    }

    private static bool TryGetQueryStringInternal<T>(string queryString, string key, out T? value)
    {
      if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

      if (QueryHelpers.ParseQuery(queryString).TryGetValue(key, out var valueFromQueryString))
      {
        if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
        {
          value = (T)(object)valueAsInt;
          return true;
        }

        if (typeof(T) == typeof(string))
        {
          value = (T)(object)valueFromQueryString.ToString();
          return true;
        }

        if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
        {
          value = (T)(object)valueAsDecimal;
          return true;
        }
      }

      value = default;
      return false;
    }
  }
}
