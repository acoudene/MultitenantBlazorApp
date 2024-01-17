// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using MultitenantBlazorApp.Server.Configurations;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace MultitenantBlazorApp.Server;

/// <summary>
/// Handler for JWT configuration by Tenant
/// </summary>
public class ByTenantJwtBearerHandler : JwtBearerHandler
{
  // TODO - <Specific Multitenant>
  private readonly IJwtBearerOptionsProvider _jwtBearerOptionsProvider;
  // TODO - </Specific Multitenant>

  /// <summary>
  /// Constructor
  /// </summary>
  /// <param name="options"></param>
  /// <param name="logger"></param>
  /// <param name="encoder"></param>
  /// <param name="clock"></param>
  /// <param name="jwtBearerOptionsProvider"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public ByTenantJwtBearerHandler(
      IOptionsMonitor<JwtBearerOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder,
      ISystemClock clock,
      // TODO - <Specific Multitenant>
      IJwtBearerOptionsProvider jwtBearerOptionsProvider)
      // TODO - </Specific Multitenant>
      : base(options, logger, encoder, clock)
  {
    // TODO - <Specific Multitenant>
    Guard.IsNotNull(jwtBearerOptionsProvider);

    _jwtBearerOptionsProvider = jwtBearerOptionsProvider;
    // TODO - </Specific Multitenant>
  }

  /// <summary>
  /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
  /// </summary>
  /// <returns></returns>
  protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    string? token;
    try
    {
      // TODO - <Specific Multitenant>
      // Add specific options from tenant
      _jwtBearerOptionsProvider.ConfigureOptions(Options);
      // TODO - </Specific Multitenant>

      // Give application opportunity to find from a different location, adjust, or reject token
      var messageReceivedContext = new MessageReceivedContext(Context, Scheme, Options);

      // event can set the token
      await Events.MessageReceived(messageReceivedContext);
      if (messageReceivedContext.Result != null)
      {
        return messageReceivedContext.Result;
      }

      // If application retrieved token from somewhere else, use that.
      token = messageReceivedContext.Token;

      if (string.IsNullOrEmpty(token))
      {
        string authorization = Request.Headers.Authorization.ToString();

        // If no authorization header found, nothing to process further
        if (string.IsNullOrEmpty(authorization))
        {
          return AuthenticateResult.NoResult();
        }

        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
          token = authorization.Substring("Bearer ".Length).Trim();
        }

        // If no token found, no further work possible
        if (string.IsNullOrEmpty(token))
        {
          return AuthenticateResult.NoResult();
        }
      }

      // TODO - <Specific Multitenant>
      if (Options.Authority == null)
      {
        return AuthenticateResult.NoResult();
      }

      var currentConfiguration = await _jwtBearerOptionsProvider.GetOpenIdConfigurationAsync(Options, Context.RequestAborted);
      var validationParameters = Options.TokenValidationParameters.Clone();
      if (currentConfiguration != null)
      {
        var issuers = new[] { currentConfiguration.Issuer };
        validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(issuers) ?? issuers;

        validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(currentConfiguration.SigningKeys)
            ?? currentConfiguration.SigningKeys;
      }
      // TODO - </Specific Multitenant>

      List<Exception>? validationFailures = null;
      SecurityToken? validatedToken = null;
      foreach (var validator in Options.SecurityTokenValidators)
      {
        if (validator.CanReadToken(token))
        {
          ClaimsPrincipal principal;
          try
          {
            principal = validator.ValidateToken(token, validationParameters, out validatedToken);
          }
          catch (Exception ex)
          {
            Logger.TokenValidationFailed(ex);

            // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
            if (Options.RefreshOnIssuerKeyNotFound && Options.ConfigurationManager != null
                && ex is SecurityTokenSignatureKeyNotFoundException)
            {
              Options.ConfigurationManager.RequestRefresh();
            }

            if (validationFailures == null)
            {
              validationFailures = new List<Exception>(1);
            }
            validationFailures.Add(ex);
            continue;
          }

          Logger.TokenValidationSucceeded();

          var tokenValidatedContext = new TokenValidatedContext(Context, Scheme, Options)
          {
            Principal = principal,
            SecurityToken = validatedToken
          };

          tokenValidatedContext.Properties.ExpiresUtc = GetSafeDateTime(validatedToken.ValidTo);
          tokenValidatedContext.Properties.IssuedUtc = GetSafeDateTime(validatedToken.ValidFrom);

          await Events.TokenValidated(tokenValidatedContext);
          if (tokenValidatedContext.Result != null)
          {
            return tokenValidatedContext.Result;
          }

          if (Options.SaveToken)
          {
            tokenValidatedContext.Properties.StoreTokens(new[]
            {
              new AuthenticationToken { Name = "access_token", Value = token }
            });
          }

          tokenValidatedContext.Success();
          return tokenValidatedContext.Result!;
        }
      }

      if (validationFailures != null)
      {
        var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
        {
          Exception = (validationFailures.Count == 1) ? validationFailures[0] : new AggregateException(validationFailures)
        };

        await Events.AuthenticationFailed(authenticationFailedContext);
        if (authenticationFailedContext.Result != null)
        {
          return authenticationFailedContext.Result;
        }

        return AuthenticateResult.Fail(authenticationFailedContext.Exception);
      }

      // TODO - <Specific Multitenant>
      return AuthenticateResult.Fail("No SecurityTokenValidator available for token: " + token ?? "[null]");
      // TODO - </Specific Multitenant>
    }
    catch (Exception ex)
    {
      Logger.ErrorProcessingMessage(ex);

      var authenticationFailedContext = new AuthenticationFailedContext(Context, Scheme, Options)
      {
        Exception = ex
      };

      await Events.AuthenticationFailed(authenticationFailedContext);
      if (authenticationFailedContext.Result != null)
      {
        return authenticationFailedContext.Result;
      }

      throw;
    }
  }

  private static DateTime? GetSafeDateTime(DateTime dateTime)
  {
    // Assigning DateTime.MinValue or default(DateTime) to a DateTimeOffset when in a UTC+X timezone will throw
    // Since we don't really care about DateTime.MinValue in this case let's just set the field to null
    if (dateTime == DateTime.MinValue)
    {
      return null;
    }
    return dateTime;
  }
}
