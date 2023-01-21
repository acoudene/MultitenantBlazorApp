// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using MultitenantBlazorApp.Client.Helpers;
using Microsoft.AspNetCore.Components;

namespace MultitenantBlazorApp.Client.Tenant
{
    /// <summary>
    /// Get a tenant id by claims
    /// </summary>
    public class ByNavSubdomainTenantIdProvider : IStatefulTenantIdProvider
    {
        const string TenantIdKey = "Tenant";
        private readonly NavigationManager _navigationManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="navigationManager"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ByNavSubdomainTenantIdProvider(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        }

        /// <summary>
        /// Get tenant key
        /// </summary>
        /// <returns></returns>
        public string GetTenantIdKey() => TenantIdKey;

        /// <summary>
        /// Get tenant id from current state
        /// </summary>
        /// <returns></returns>
        public string? GetCurrentTenantId()
        {
            var uri = _navigationManager.ToAbsoluteUri(_navigationManager.Uri);
            string? tenantId = uri.GetSubdomain();
            return tenantId;
        }
    }
}
