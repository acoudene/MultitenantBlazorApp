// Changelogs Date  | Author                | Description
// 2022-12-14       | Anthony Coudène (ACE) | MN-1198 Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using MultitenantBlazorApp.Server;
using System.Text.Encodings.Web;

namespace MultitenantBlazorApp.Server
{
  /// <summary>
  /// Handler for JWT configuration by Tenant
  /// </summary>
  public class ByTenantJwtBearerHandler : JwtBearerHandler
  {
    private readonly IConfiguration _configuration;
    private readonly IStatelessTenantIdProvider _tenantIdProvider;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    /// <param name="encoder"></param>
    /// <param name="clock"></param>
    /// <param name="configuration"></param>
    /// <param name="tenantIdProvider"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ByTenantJwtBearerHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration,
        IStatelessTenantIdProvider tenantIdProvider)
        : base(options, logger, encoder, clock)
    {
      Guard.IsNotNull(configuration);
      Guard.IsNotNull(tenantIdProvider);

      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _tenantIdProvider = tenantIdProvider ?? throw new ArgumentNullException(nameof(tenantIdProvider));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      // Try several method to get tenant id, first from request, then from claims
      var tenantId = _tenantIdProvider.GetTenantId(Request);
      if (string.IsNullOrEmpty(tenantId) && Context?.User != null)
        tenantId = _tenantIdProvider.GetTenantId(Context.User);
      if (string.IsNullOrEmpty(tenantId))
        tenantId = "default";

      var options = OptionsMonitor.CurrentValue;
      if (options == null)
        return await base.HandleAuthenticateAsync();

      // Get OIDC configuration from a givent tenant id
      const string oidcKey = "Oidc";
      const string authorityKey = "Authority";
      const string clientIdKey = "ClientId";
      const string audienceKey = "Audience";
      const string nameClaimTypeKey = "NameClaimType";
      const string roleClaimTemplateKey = "RoleClaimTemplate";

      var tenantConfigKey = $"{oidcKey}:{tenantId}";
      var authorityConfigKey = $"{tenantConfigKey}:{authorityKey}";
      var clientIdConfigKey = $"{tenantConfigKey}:{clientIdKey}";
      var audienceConfigKey = $"{tenantConfigKey}:{audienceKey}";
      var nameClaimTypeConfigKey = $"{tenantConfigKey}:{nameClaimTypeKey}";
      var roleClaimTemplateConfigKey = $"{tenantConfigKey}:{roleClaimTemplateKey}";

      // Useful to trigger user validation on legacy server
      //options.EventsType = typeof(UserValidationJwtBearerEvents);

      string? authority = _configuration[authorityConfigKey];
      if (string.IsNullOrWhiteSpace(authority)) throw new InvalidOperationException($"Missing {authorityConfigKey} configuration for tenant: {tenantId}");

      string? audience = _configuration[audienceConfigKey];
      if (string.IsNullOrWhiteSpace(audience)) throw new InvalidOperationException($"Missing {audienceConfigKey} configuration for tenant: {tenantId}");

      string? clientId = _configuration[clientIdConfigKey];
      if (string.IsNullOrWhiteSpace(clientId)) throw new InvalidOperationException($"Missing {clientIdConfigKey} configuration for tenant: {tenantId}");

      string? roleClaimTypeRaw = _configuration[roleClaimTemplateConfigKey];
      if (string.IsNullOrWhiteSpace(roleClaimTypeRaw)) throw new InvalidOperationException($"Missing {roleClaimTemplateConfigKey} configuration for tenant: {tenantId}");

      string? nameClaimType = _configuration[nameClaimTypeConfigKey];
      if (string.IsNullOrWhiteSpace(nameClaimType)) throw new InvalidOperationException($"Missing {nameClaimTypeConfigKey} configuration for tenant: {tenantId}");

      options.Authority = authority;
      options.Audience = audience;
      options.RequireHttpsMetadata = false;

      options.TokenValidationParameters.RoleClaimType = roleClaimTypeRaw.Replace($"${{{clientIdKey}}}", clientId);
      options.TokenValidationParameters.NameClaimType = nameClaimType; // Fait sens ici car côté serveur, on utiliserait le nom pour la traçabilité

      options.TokenValidationParameters.ValidateIssuer = true;

      return await base.HandleAuthenticateAsync();
    }
  }
}
