// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MultitenantBlazorApp.Shared;

namespace MultitenantBlazorApp.Server.Configurations;

public class ByTenantJwtBearerOptionsProvider : IJwtBearerOptionsProvider
{
  public const string OidcConfigKey = "Oidc";
  public const string TemplateConfigKey = "${Template}";

  public string GetTenantConfigKey(string tenantId)
  {
    return $"{OidcConfigKey}:{tenantId}";
  }

  private readonly IMemoryCache _memoryCache;
  private readonly IConfiguration _configuration;
  private readonly IStatefulTenantIdProvider _tenantIdProvider;

  private TimeSpan _cacheDelayInSec = TimeSpan.FromSeconds(120);

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="memoryCache"></param>
  /// <param name="configuration"></param>
  /// <param name="tenantIdProvider"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public ByTenantJwtBearerOptionsProvider(
      IMemoryCache memoryCache,
      IConfiguration configuration,
      IStatefulTenantIdProvider tenantIdProvider)
  {
    Guard.IsNotNull(memoryCache);
    Guard.IsNotNull(configuration);
    Guard.IsNotNull(tenantIdProvider);

    _memoryCache = memoryCache;
    _configuration = configuration;
    _tenantIdProvider = tenantIdProvider;
  }

  /// <summary>
  /// Resolve configuration from authority
  /// </summary>
  /// <param name="options"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<OpenIdConnectConfiguration?> GetOpenIdConfigurationAsync(JwtBearerOptions options, CancellationToken cancellationToken)
  {
    Guard.IsNotNull(options);

    string? authority = options.Authority;
    Guard.IsNotNullOrWhiteSpace(authority);

    var cacheKey = $"{authority}__{GetType().FullName}";
    var ret = await _memoryCache.GetOrCreateAsync(cacheKey, async cacheEntry =>
    {
      cacheEntry.AbsoluteExpirationRelativeToNow = _cacheDelayInSec;
      string metadataAddress = options.MetadataAddress;
      if (metadataAddress == null)
        throw new InvalidOperationException($"Missing metadata address for {authority}");

      // For debug purpose only:        
      //var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever() { RequireHttps = false });                
      var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress, new OpenIdConnectConfigurationRetriever());
      var authorityConfiguration = await configurationManager.GetConfigurationAsync(cancellationToken);
      return authorityConfiguration;
    });

    return ret;
  }

  /// <summary>
  /// Add options from tenant
  /// </summary>
  /// <param name="options"></param>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  public void ConfigureOptions(JwtBearerOptions options)
  {
    if (options == null)
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

    var jwtBearerOptionsConfiguration = new JwtBearerOptionsConfiguration();
    tenantConfigSection.Bind(jwtBearerOptionsConfiguration);

    // Useful to trigger user validation on legacy server
    options.EventsType = typeof(UserValidationJwtBearerEvents);

    var templateValueReplacer = new ByTenantTemplateValueReplacer();
    templateValueReplacer.StoreTenantId(tenantId);

    string? clientId = jwtBearerOptionsConfiguration.ClientId;
    if (string.IsNullOrWhiteSpace(clientId)) throw new InvalidOperationException($"Missing {nameof(jwtBearerOptionsConfiguration.ClientId)} configuration for tenant: {tenantId}");
    clientId = templateValueReplacer.Replace(clientId);
    templateValueReplacer.StoreClientId(clientId);

    string? authority = jwtBearerOptionsConfiguration.Authority;
    if (string.IsNullOrWhiteSpace(authority)) throw new InvalidOperationException($"Missing {nameof(jwtBearerOptionsConfiguration.Authority)} configuration for tenant: {tenantId}");
    authority = templateValueReplacer.Replace(authority);

    string? audience = jwtBearerOptionsConfiguration.Audience;
    if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException($"Missing {nameof(jwtBearerOptionsConfiguration.Audience)} configuration for tenant: {tenantId}");
    audience = templateValueReplacer.Replace(audience);

    string? roleClaimTemplate = jwtBearerOptionsConfiguration.RoleClaimTemplate;
    if (string.IsNullOrWhiteSpace(roleClaimTemplate)) throw new InvalidOperationException($"Missing {nameof(jwtBearerOptionsConfiguration.RoleClaimTemplate)} configuration for tenant: {tenantId}");
    roleClaimTemplate = templateValueReplacer.Replace(roleClaimTemplate);

    string? nameClaimType = jwtBearerOptionsConfiguration.NameClaimType;
    if (string.IsNullOrWhiteSpace(nameClaimType)) throw new InvalidOperationException($"Missing {nameof(jwtBearerOptionsConfiguration.NameClaimType)} configuration for tenant: {tenantId}");
    nameClaimType = templateValueReplacer.Replace(nameClaimType);

    int? cacheDelayInSec = jwtBearerOptionsConfiguration.CacheDelayInSec;
    if (cacheDelayInSec != null)
      _cacheDelayInSec = TimeSpan.FromSeconds(cacheDelayInSec.Value);

    options.Authority = authority;
    options.Audience = audience;
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters.RoleClaimType = roleClaimTemplate;
    options.TokenValidationParameters.NameClaimType = nameClaimType;
    options.TokenValidationParameters.ValidAudience = audience;
    options.TokenValidationParameters.ValidAudiences = new List<string>() { audience };
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidateAudience = false;

    options.MetadataAddress = $"{authority}{(!authority.EndsWith("/", StringComparison.Ordinal) ? "/" : string.Empty)}.well-known/openid-configuration";
  }
}
