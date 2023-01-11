// Changelogs Date  | Author                | Description
// 2022-12-14       | Anthony Coudène (ACE) | MN-1198 Creation

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MultitenantBlazorApp.Server
{
  public class ByTenantJwtBearerHandler : JwtBearerHandler
  {
    private readonly IConfiguration _configuration;

    public ByTenantJwtBearerHandler(IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      var identity = Context?.User?.Identity as ClaimsIdentity;
      if (identity == null)
        return base.HandleAuthenticateAsync();

      var tenantId = "default";

      var tenantClaim = identity.FindFirst("Tenant");
      if (tenantClaim != null)
        tenantId = tenantClaim.Value;

      var options = OptionsMonitor.CurrentValue;
      if (options == null)
        return base.HandleAuthenticateAsync();

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
      options.Authority = _configuration[authorityConfigKey];
      options.Audience = _configuration[audienceConfigKey];
      options.RequireHttpsMetadata = false;
      var clientId = _configuration[clientIdConfigKey];

      string? roleClaimTemplate = _configuration[roleClaimTemplateConfigKey];
      if (!string.IsNullOrWhiteSpace(roleClaimTemplate))
      {
        options.TokenValidationParameters.RoleClaimType = roleClaimTemplate.Replace($"${{{clientIdKey}}}", clientId);
      }
      options.TokenValidationParameters.NameClaimType = _configuration[nameClaimTypeConfigKey]; // Fait sens ici car côté serveur, on utiliserait le nom pour la traçabilité
      options.TokenValidationParameters.ValidateIssuer = true;


      return base.HandleAuthenticateAsync();
    }
  }
}
