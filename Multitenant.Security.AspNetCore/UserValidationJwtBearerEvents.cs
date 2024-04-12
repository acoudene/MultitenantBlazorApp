// Changelogs Date  | Author                | Description
// 2022-07-26       | Anthony Coudène (ACE) | MN-221 Integrate Oidc/OAuth2 protocol as unique authentication mode

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace Multitenant.Security.AspNetCore;

public class UserValidationJwtBearerEvents : JwtBearerEvents
{
  private readonly ILogger<UserValidationJwtBearerEvents> _logger;

  public UserValidationJwtBearerEvents(ILogger<UserValidationJwtBearerEvents> logger)
  {    
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public override Task TokenValidated(TokenValidatedContext context)
  {
    if (context is null) throw new ArgumentNullException(nameof(context));

    // Do something
    _logger.LogDebug($"Token {context.SecurityToken?.Id} is validated.");

    return base.TokenValidated(context);
  }
  public override Task AuthenticationFailed(AuthenticationFailedContext context)
  {
    if (context is null) throw new ArgumentNullException(nameof(context));

    // Do something
    _logger.LogWarning($"Token authentication has failed for {context.Principal?.Identity?.Name}.");

    return base.AuthenticationFailed(context);
  }
}
