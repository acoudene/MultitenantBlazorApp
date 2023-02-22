// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using MultitenantBlazorApp.Shared;

namespace MultitenantBlazorApp.Client.Configurations;

public class ByTenantOidcProviderOptionsProvider : IOidcProviderOptionsProvider
{
  public const string OidcConfigKey = "Oidc";
  public const string TemplateConfigKey = "${Template}";

  public string GetTenantConfigKey(string tenantId)
  {
    return $"{OidcConfigKey}:{tenantId}";
  }

  private readonly IConfiguration _configuration;
  private readonly IStatefulTenantIdProvider _tenantIdProvider;

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="configuration"></param>
  /// <param name="tenantIdProvider"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public ByTenantOidcProviderOptionsProvider(
    IConfiguration configuration,
    IStatefulTenantIdProvider tenantIdProvider)
  {
    Guard.IsNotNull(configuration);
    Guard.IsNotNull(tenantIdProvider);

    _configuration = configuration;
    _tenantIdProvider = tenantIdProvider;
  }

  /// <summary>
  /// Add options from tenant
  /// </summary>
  /// <param name="providerOptions"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public void ConfigureOptions(OidcProviderOptions providerOptions, RemoteAuthenticationUserOptions userOptions)
  {
    if (providerOptions == null)
      return;

    // Get tenant id or default value
    string? tenantId = _tenantIdProvider.GetCurrentTenantId();
    if (tenantId == null || string.IsNullOrWhiteSpace(tenantId))
      throw new InvalidOperationException("Missing tenant id");

    string tenantConfigKey = GetTenantConfigKey(tenantId);
    var tenantConfigSection = _configuration.GetSection(tenantConfigKey);
    if (tenantConfigSection == null)
    {
      tenantConfigSection = _configuration.GetSection(TemplateConfigKey);
      if (tenantConfigSection == null)
        throw new InvalidOperationException("Missing template config for all tenants");
    }

    var OidcProviderOptionsConfiguration = new OidcProviderOptionsConfiguration();
    tenantConfigSection.Bind(OidcProviderOptionsConfiguration);

    var templateValueReplacer = new ByTenantTemplateValueReplacer();
    templateValueReplacer.StoreTenantId(tenantId);

    string? clientId = OidcProviderOptionsConfiguration.ClientId;
    if (string.IsNullOrWhiteSpace(clientId)) throw new InvalidOperationException($"Missing {nameof(OidcProviderOptionsConfiguration.ClientId)} configuration for tenant: {tenantId}");
    clientId = templateValueReplacer.Replace(clientId);
    templateValueReplacer.StoreClientId(clientId);

    string? authority = OidcProviderOptionsConfiguration.Authority;
    if (string.IsNullOrWhiteSpace(authority)) throw new InvalidOperationException($"Missing {nameof(OidcProviderOptionsConfiguration.Authority)} configuration for tenant: {tenantId}");
    authority = templateValueReplacer.Replace(authority);

    string? responseType = OidcProviderOptionsConfiguration.ResponseType;
    if (string.IsNullOrWhiteSpace(responseType)) throw new InvalidOperationException($"Missing {nameof(OidcProviderOptionsConfiguration.ResponseType)} configuration for tenant: {tenantId}");
    responseType = templateValueReplacer.Replace(responseType);

    string? roleClaimTemplate = OidcProviderOptionsConfiguration.RoleClaimTemplate;
    if (string.IsNullOrWhiteSpace(roleClaimTemplate)) throw new InvalidOperationException($"Missing {nameof(OidcProviderOptionsConfiguration.RoleClaimTemplate)} configuration for tenant: {tenantId}");
    roleClaimTemplate = templateValueReplacer.Replace(roleClaimTemplate);

    providerOptions.Authority = authority;
    providerOptions.ClientId = clientId;
    providerOptions.ResponseType= responseType;
    userOptions.RoleClaim = roleClaimTemplate;
  }
}
