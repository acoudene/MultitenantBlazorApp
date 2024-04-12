// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Configuration;

/// <summary>
/// Dedicated service to get tenant id information
/// </summary>
public interface IStatefulTenantIdProvider
{
    /// <summary>
    /// Get tenant id from a dedicated approach (i.e. query string, storage, ...)
    /// </summary>
    /// <returns></returns>
    string? GetCurrentTenantId();

    /// <summary>
    /// Get tenant key
    /// </summary>
    /// <returns></returns>
    string GetTenantIdKey();
}