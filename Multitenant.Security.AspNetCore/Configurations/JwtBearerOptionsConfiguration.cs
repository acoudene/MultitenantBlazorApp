// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

namespace Multitenant.Security.AspNetCore.Configurations;

public record JwtBearerOptionsConfiguration
{
  public string? Authority { get; set; }

  public string? ClientId { get; set; }

  public string? Audience { get; set; }

  public string? TargetUserRolesClaimName { get; set; }

  public string? NameClaimType { get; set; }

  public string? RoleClaimTemplate { get; set; }

  public int? CacheDelayInSec { get; set; }
}
