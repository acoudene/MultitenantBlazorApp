// Changelogs Date  | Author                | Description
// 2022-11-22       | Anthony Coudène (ACE) | Creation

using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace MultitenantBlazorApp.Server
{
  public class MyClaimsTransformation : IClaimsTransformation
  {
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      if (principal == null) throw new ArgumentNullException(nameof(principal));
      if (principal.Identity is not ClaimsIdentity) throw new InvalidOperationException("ClaimsPrincipal is not a ClaimsIdentity");

      // To fill

      return Task.FromResult(principal);
    }
  }
}
