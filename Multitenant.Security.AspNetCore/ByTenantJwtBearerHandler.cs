// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation
// 2024-01-17       | Anthony Coudène (ACE) | Adaptations to .Net 8

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Multitenant.Security.AspNetCore.Configurations;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Multitenant.Security.AspNetCore;

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
  /// <param name="jwtBearerOptionsProvider"></param>
  /// <exception cref="ArgumentNullException"></exception>
  public ByTenantJwtBearerHandler(
      IOptionsMonitor<JwtBearerOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder,
      // TODO - <Specific Multitenant>
      IJwtBearerOptionsProvider jwtBearerOptionsProvider)
      // TODO - </Specific Multitenant>
      : base(options, logger, encoder)
  {
    // TODO - <Specific Multitenant>    
    _jwtBearerOptionsProvider = jwtBearerOptionsProvider ?? throw new ArgumentNullException(nameof(jwtBearerOptionsProvider));
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
      // TODO - </Specific Multitenant>

      var tvp = await SetupTokenValidationParametersAsync();
      List<Exception>? validationFailures = null;
      SecurityToken? validatedToken = null;
      ClaimsPrincipal? principal = null;

      if (!Options.UseSecurityTokenValidators)
      {
        foreach (var tokenHandler in Options.TokenHandlers)
        {
          try
          {
            var tokenValidationResult = await tokenHandler.ValidateTokenAsync(token, tvp);
            if (tokenValidationResult.IsValid)
            {
              principal = new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
              validatedToken = tokenValidationResult.SecurityToken;
              break;
            }
            else
            {
              validationFailures ??= new List<Exception>(1);
              RecordTokenValidationError(tokenValidationResult.Exception ?? new SecurityTokenValidationException($"The TokenHandler: '{tokenHandler}', was unable to validate the Token."), validationFailures);
            }
          }
          catch (Exception ex)
          {
            validationFailures ??= new List<Exception>(1);
            RecordTokenValidationError(ex, validationFailures);
          }
        }
      }
      else
      {
#pragma warning disable CS0618 // Type or member is obsolete
        foreach (var validator in Options.SecurityTokenValidators)
        {
          if (validator.CanReadToken(token))
          {
            try
            {
              principal = validator.ValidateToken(token, tvp, out validatedToken);
            }
            catch (Exception ex)
            {
              validationFailures ??= new List<Exception>(1);
              RecordTokenValidationError(ex, validationFailures);
              continue;
            }
          }
        }
#pragma warning restore CS0618 // Type or member is obsolete
      }

      if (principal != null && validatedToken != null)
      {
        Logger.TokenValidationSucceeded();

        var tokenValidatedContext = new TokenValidatedContext(Context, Scheme, Options)
        {
          Principal = principal
        };

        tokenValidatedContext.SecurityToken = validatedToken;
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

      if (!Options.UseSecurityTokenValidators)
      {
        // TODO - <Specific Multitenant>
        return AuthenticateResult.Fail("No TokenHandler was able to validate the token.");
        // TODO - </Specific Multitenant>
      }

      // TODO - <Specific Multitenant>
      return AuthenticateResult.Fail("No SecurityTokenValidator available for token.");
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

  private void RecordTokenValidationError(Exception exception, List<Exception> exceptions)
  {
    if (exception != null)
    {
      Logger.TokenValidationFailed(exception);
      exceptions.Add(exception);
    }

    // Refresh the configuration for exceptions that may be caused by key rollovers. The user can also request a refresh in the event.
    // Refreshing on SecurityTokenSignatureKeyNotFound may be redundant if Last-Known-Good is enabled, it won't do much harm, most likely will be a nop.
    if (Options.RefreshOnIssuerKeyNotFound && Options.ConfigurationManager != null
        && exception is SecurityTokenSignatureKeyNotFoundException)
    {
      Options.ConfigurationManager.RequestRefresh();
    }
  }

  private async Task<TokenValidationParameters> SetupTokenValidationParametersAsync()
  {

    // TODO - <Specific Multitenant>    
    var currentConfiguration = await _jwtBearerOptionsProvider.GetOpenIdConfigurationAsync(Options, Context.RequestAborted);
    // Clone to avoid cross request race conditions for updated configurations.
    var tokenValidationParameters = Options.TokenValidationParameters.Clone();
    if (currentConfiguration != null)
    {
      var issuers = new[] { currentConfiguration.Issuer };
      tokenValidationParameters.ValidIssuers = (tokenValidationParameters.ValidIssuers == null
        ? issuers
        : tokenValidationParameters.ValidIssuers.Concat(issuers));
      tokenValidationParameters.IssuerSigningKeys = (tokenValidationParameters.IssuerSigningKeys == null
        ? currentConfiguration.SigningKeys
        : tokenValidationParameters.IssuerSigningKeys.Concat(currentConfiguration.SigningKeys));
    }

    return tokenValidationParameters;
    // TODO - </Specific Multitenant>
  }
}
