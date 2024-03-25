// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using System.Security.Claims;
using System.Text.Json;

namespace Multitenant.Security.WebAssembly;

public static class JWTParser
{
  public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
  {
    if (string.IsNullOrWhiteSpace(jwt))
      return Enumerable.Empty<Claim>();

    //Format d'aun token JWT : Header.Payload(en base 64).SigningKey
    var claims = new List<Claim>();
    var items = jwt.Split('.');
    if (!items.Any())
      return Enumerable.Empty<Claim>();

    var payload = items[1];

    var jsonBytes = ParseBase64WithoutPadding(payload);
    if (jsonBytes == null)
      return Enumerable.Empty<Claim>();

    var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
    if (keyValuePairs == null)
      return Enumerable.Empty<Claim>();

    claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() == null ? string.Empty : kvp.Value.ToString()!)));

    return claims;
  }

  private static byte[] ParseBase64WithoutPadding(string base64)
  {
    // TODO ACE - A mettre quand on gèrera proprement les exceptions
    //if (string.IsNullOrWhiteSpace(base64)) throw new ArgumentNullException(nameof(base64));

    if (string.IsNullOrWhiteSpace(base64))
      return Array.Empty<byte>();

    switch (base64.Length % 4)
    {
      case 2: base64 += "=="; break;
      case 3: base64 += "="; break;
    }
    //Replace non base64 caractere
    base64 = base64.Replace("_", "/");
    return Convert.FromBase64String(base64);
  }
}
