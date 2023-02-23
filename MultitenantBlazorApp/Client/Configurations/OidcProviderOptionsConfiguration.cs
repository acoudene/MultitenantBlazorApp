// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

namespace MultitenantBlazorApp.Client.Configurations;

public record OidcProviderOptionsConfiguration
{
  public string? Authority { get; set; }

  public string? ClientId { get; set; }

  public string? ResponseType { get; set; }

  public string? RoleClaimTemplate { get; set; }    
}
