// Changelogs Date  | Author                | Description
// 2022-07-26       | Anthony Coudène (ACE) | MN-221 Integrate Oidc/OAuth2 protocol as unique authentication mode

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MultitenantBlazorApp.Server
{
    public class UserValidationJwtBearerEvents : JwtBearerEvents
    {
        private readonly ILogger<UserValidationJwtBearerEvents> _logger;

        public UserValidationJwtBearerEvents(ILogger<UserValidationJwtBearerEvents> logger)
        {
            Guard.IsNotNull(logger);            

            _logger= logger;
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
            Guard.IsNotNull(context);

            // Do something
            _logger.LogDebug($"Token {context.SecurityToken?.Id} is validated.");

            return base.TokenValidated(context);
        }
        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            Guard.IsNotNull(context);

            // Do something
            _logger.LogWarning($"Token authentication has failed for {context.Principal?.Identity?.Name}.");

            return base.AuthenticationFailed(context);
        }
    }
}
