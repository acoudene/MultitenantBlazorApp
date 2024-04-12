// Changelogs Date  | Author                | Description
// 2023-02-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Multitenant.Security.AspNetCore.Configurations;

public interface IJwtBearerOptionsProvider
{
  /// <summary>
  /// Resolve configuration from authority
  /// </summary>
  /// <param name="options"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task<OpenIdConnectConfiguration?> GetOpenIdConfigurationAsync(JwtBearerOptions options, CancellationToken cancellationToken);

  /// <summary>
  /// Add options from tenant
  /// </summary>
  /// <returns></returns>
  /// <exception cref="InvalidOperationException"></exception>
  void ConfigureOptions(JwtBearerOptions options);

}