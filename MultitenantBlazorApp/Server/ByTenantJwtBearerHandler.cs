// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace MultitenantBlazorApp.Server
{
  /// <summary>
  /// Handler for JWT configuration by Tenant
  /// </summary>
  public class ByTenantJwtBearerHandler : JwtBearerHandler
  {
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly IStatelessTenantIdProvider _tenantIdProvider;
    private readonly TimeSpan _cacheDelay = TimeSpan.FromHours(1);

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
        IMemoryCache memoryCache,
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration,
        IStatelessTenantIdProvider tenantIdProvider)
        : base(options, logger, encoder, clock)
    {
      Guard.IsNotNull(memoryCache);
      Guard.IsNotNull(configuration);
      Guard.IsNotNull(tenantIdProvider);

      _memoryCache = memoryCache;
      _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _tenantIdProvider = tenantIdProvider ?? throw new ArgumentNullException(nameof(tenantIdProvider));
    }

    protected async Task<OpenIdConnectConfiguration?> ResolveCurrentOpenIdConfigurationAsync()
    {
      if (Context == null)
        return null;

      // Try several method to get tenant id, first from request, then from claims
      var tenantId = _tenantIdProvider.GetTenantId(Request);
      if (string.IsNullOrEmpty(tenantId) && Context?.User != null)
        tenantId = _tenantIdProvider.GetTenantId(Context.User);
      if (string.IsNullOrEmpty(tenantId))
        tenantId = "default";

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

      Options.Authority = authority;
      Options.Audience = audience;
      Options.RequireHttpsMetadata = false;

      Options.TokenValidationParameters.RoleClaimType = roleClaimTypeRaw.Replace($"${{{clientIdKey}}}", clientId);
      Options.TokenValidationParameters.NameClaimType = nameClaimType; // Fait sens ici car côté serveur, on utiliserait le nom pour la traçabilité
      Options.TokenValidationParameters.ValidAudience = audience;
      Options.TokenValidationParameters.ValidateIssuer = true;

      var cacheKey = $"DynamicAuthorityJwtBearerHandlerConfigurationResolver__{authority}";
      var ret = await _memoryCache.GetOrCreateAsync(cacheKey, async cacheEntry =>
      {
        cacheEntry.AbsoluteExpirationRelativeToNow = _cacheDelay;
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>($"{authority}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
        var authorityConfiguration = await configurationManager.GetConfigurationAsync(Context!.RequestAborted);
        return authorityConfiguration;
      });

      return ret;
    }


    /// <inheritdoc />
    protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new JwtBearerEvents());

    /// <summary>
    /// Searches the 'Authorization' header for a 'Bearer' token. If the 'Bearer' token is found, it is validated using <see cref="TokenValidationParameters"/> set in the options.
    /// </summary>
    /// <returns></returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      string? token;
      try
      {
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

        var currentConfiguration = await ResolveCurrentOpenIdConfigurationAsync();
        var validationParameters = Options.TokenValidationParameters.Clone();
        if (currentConfiguration != null)
        {
          var issuers = new[] { currentConfiguration.Issuer };
          validationParameters.ValidIssuers = validationParameters.ValidIssuers?.Concat(issuers) ?? issuers;

          validationParameters.IssuerSigningKeys = validationParameters.IssuerSigningKeys?.Concat(currentConfiguration.SigningKeys)
              ?? currentConfiguration.SigningKeys;
        }


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
              //Logger.TokenValidationFailed(ex);

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

            //Logger.TokenValidationSucceeded();

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

        return AuthenticateResult.Fail("No SecurityTokenValidator available for token: " + token ?? "[null]");
      }
      catch (Exception ex)
      {
        //Logger.ErrorProcessingMessage(ex);

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

    /// <inheritdoc />
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
      var authResult = await HandleAuthenticateOnceSafeAsync();
      var eventContext = new JwtBearerChallengeContext(Context, Scheme, Options, properties)
      {
        AuthenticateFailure = authResult?.Failure
      };

      // Avoid returning error=invalid_token if the error is not caused by an authentication failure (e.g missing token).
      if (Options.IncludeErrorDetails && eventContext.AuthenticateFailure != null)
      {
        eventContext.Error = "invalid_token";
        eventContext.ErrorDescription = CreateErrorDescription(eventContext.AuthenticateFailure);
      }

      await Events.Challenge(eventContext);
      if (eventContext.Handled)
      {
        return;
      }

      Response.StatusCode = 401;

      if (string.IsNullOrEmpty(eventContext.Error) &&
          string.IsNullOrEmpty(eventContext.ErrorDescription) &&
          string.IsNullOrEmpty(eventContext.ErrorUri))
      {
        Response.Headers.Append(HeaderNames.WWWAuthenticate, Options.Challenge);
      }
      else
      {
        // https://tools.ietf.org/html/rfc6750#section-3.1
        // WWW-Authenticate: Bearer realm="example", error="invalid_token", error_description="The access token expired"
        var builder = new StringBuilder(Options.Challenge);
        if (Options.Challenge.IndexOf(' ') > 0)
        {
          // Only add a comma after the first param, if any
          builder.Append(',');
        }
        if (!string.IsNullOrEmpty(eventContext.Error))
        {
          builder.Append(" error=\"");
          builder.Append(eventContext.Error);
          builder.Append('\"');
        }
        if (!string.IsNullOrEmpty(eventContext.ErrorDescription))
        {
          if (!string.IsNullOrEmpty(eventContext.Error))
          {
            builder.Append(',');
          }

          builder.Append(" error_description=\"");
          builder.Append(eventContext.ErrorDescription);
          builder.Append('\"');
        }
        if (!string.IsNullOrEmpty(eventContext.ErrorUri))
        {
          if (!string.IsNullOrEmpty(eventContext.Error) ||
              !string.IsNullOrEmpty(eventContext.ErrorDescription))
          {
            builder.Append(',');
          }

          builder.Append(" error_uri=\"");
          builder.Append(eventContext.ErrorUri);
          builder.Append('\"');
        }

        Response.Headers.Append(HeaderNames.WWWAuthenticate, builder.ToString());
      }
    }

    /// <inheritdoc />
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
      var forbiddenContext = new ForbiddenContext(Context, Scheme, Options);
      Response.StatusCode = 403;
      return Events.Forbidden(forbiddenContext);
    }

    private static string CreateErrorDescription(Exception authFailure)
    {
      IReadOnlyCollection<Exception> exceptions;
      if (authFailure is AggregateException agEx)
      {
        exceptions = agEx.InnerExceptions;
      }
      else
      {
        exceptions = new[] { authFailure };
      }

      var messages = new List<string>(exceptions.Count);

      foreach (var ex in exceptions)
      {
        // Order sensitive, some of these exceptions derive from others
        // and we want to display the most specific message possible.
        switch (ex)
        {
          case SecurityTokenInvalidAudienceException stia:
            messages.Add($"The audience '{stia.InvalidAudience ?? "(null)"}' is invalid");
            break;
          case SecurityTokenInvalidIssuerException stii:
            messages.Add($"The issuer '{stii.InvalidIssuer ?? "(null)"}' is invalid");
            break;
          case SecurityTokenNoExpirationException _:
            messages.Add("The token has no expiration");
            break;
          case SecurityTokenInvalidLifetimeException stil:
            messages.Add("The token lifetime is invalid; NotBefore: "
                + $"'{stil.NotBefore?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'"
                + $", Expires: '{stil.Expires?.ToString(CultureInfo.InvariantCulture) ?? "(null)"}'");
            break;
          case SecurityTokenNotYetValidException stnyv:
            messages.Add($"The token is not valid before '{stnyv.NotBefore.ToString(CultureInfo.InvariantCulture)}'");
            break;
          case SecurityTokenExpiredException ste:
            messages.Add($"The token expired at '{ste.Expires.ToString(CultureInfo.InvariantCulture)}'");
            break;
          case SecurityTokenSignatureKeyNotFoundException _:
            messages.Add("The signature key was not found");
            break;
          case SecurityTokenInvalidSignatureException _:
            messages.Add("The signature is invalid");
            break;
        }
      }

      return string.Join("; ", messages);
    }
  }
}
