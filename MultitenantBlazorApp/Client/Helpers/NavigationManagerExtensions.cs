// Changelogs Date  | Author                | Description
// 2022-06-08       | Anthony Coudène (ACE) | MN-915 Change client architecture to better manage 
// 2022-07-26       | Anthony Coudène (ACE) | MN-221 Integrate Oidc/OAuth2 protocol as unique authentication mode
// 2022-11-22       | Anthony Coudène (ACE) | MN-1198 Full OIDC/Teams Integration

using Microsoft.AspNetCore.Components;

namespace MultitenantBlazorApp.Client.Helpers
{
  /// <summary>
  /// Helper to enrich NavigationManager
  /// </summary>
  public static class NavigationManagerExtensions
  {
    /// <summary>
    /// Try to get a value from a given key in query string
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="navManager"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool TryGetQueryString<T>(this NavigationManager navManager, string key, out T? value)
    {
      var uri = navManager.ToAbsoluteUri(navManager.Uri);
      return uri.TryGetQueryString<T>(key, out value);
    }
  }
}
