// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Newtonsoft.Json;
using System.Security.Claims;

namespace MultitenantBlazorApp.Client
{
  public class MyClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
  {
    private readonly IHttpClientFactory _httpClientFactory;

    public MyClaimsPrincipalFactory(
        IAccessTokenProviderAccessor accessor,
        IHttpClientFactory httpClientFactory)
        : base(accessor)
    {
      _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public override async ValueTask<ClaimsPrincipal> CreateUserAsync(RemoteUserAccount account, RemoteAuthenticationUserOptions options)
    {
      var user = await base.CreateUserAsync(account, options);
      if (user == null) throw new InvalidOperationException("No given user");
      if (user.Identity is not ClaimsIdentity) throw new InvalidOperationException("ClaimsPrincipal is not a ClaimsIdentity");
      var identity = user.Identity as ClaimsIdentity;
      if (identity == null || !identity.IsAuthenticated)
        return user;

      // Call server to get sso API and claims to affect to user.
      string? legacyToken = await CreateAndUpdateUserAsync(default(CancellationToken));
      if (string.IsNullOrWhiteSpace(legacyToken))
        throw new InvalidOperationException("Missing legacy token");

      var claims = JWTParser.ParseClaimsFromJwt(legacyToken);
      if (claims == null)
        throw new InvalidOperationException("Can't convert token to claims");

      foreach (var claim in claims)
      {
        var foundClaim = identity.FindFirst(claim.Type);
        if (foundClaim != null)
          identity.RemoveClaim(foundClaim);

        identity.AddClaim(claim);
      }

      return user;
    }

    protected async Task<string?> CreateAndUpdateUserAsync(CancellationToken cancellationToken)
    {
      // Call API (don't use using var to let factory manage http pool
      //var httpClient = _httpClientFactory.CreateClient("Authenticated");

      //var request = new HttpRequestMessage(HttpMethod.Post, "SsoLegacy/CreateUser");

      //HttpResponseMessage response = await httpClient.SendObjectAsync(request, cancellationToken);

      //string? jsonResponse = await response?.Content?.ReadAsStringAsync(cancellationToken);
      //if (string.IsNullOrWhiteSpace(jsonResponse)) throw new InvalidOperationException($"No content in the response for {CreateAndUpdateUserAsync}");

      //dynamic? data = JsonConvert.DeserializeObject(jsonResponse);
      //return data?.token;
      return null;
    }

    //private void MapArrayClaimsToMultipleSeparateClaims(RemoteUserAccount account, ClaimsIdentity claimsIdentity)
    //{
    //    foreach (var keyValuePair in account.AdditionalProperties)
    //    {
    //        var key = keyValuePair.Key;
    //        var value = keyValuePair.Value;
    //        if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
    //        {
    //            claimsIdentity.RemoveClaim(claimsIdentity.FindFirst(key));
    //            var claims = element
    //                .EnumerateArray()
    //                .Select(x => new Claim(key, x.ToString()));
    //            claimsIdentity.AddClaims(claims);
    //        }
    //    }
    //}
  }
}
