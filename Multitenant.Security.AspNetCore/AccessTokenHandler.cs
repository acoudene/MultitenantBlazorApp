using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Headers;

namespace Multitenant.Security.AspNetCore;

public class AccessTokenHandler : DelegatingHandler
{
  private readonly IHttpContextAccessor _httpContextAccessor;

  public AccessTokenHandler(IHttpContextAccessor httpContextAccessor)
  {
    _httpContextAccessor = httpContextAccessor;
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext is null)
      return new HttpResponseMessage(HttpStatusCode.BadRequest);

    string? accessToken = await httpContext.GetTokenAsync("access_token");
    if (string.IsNullOrWhiteSpace(accessToken))
    {
      return new HttpResponseMessage(HttpStatusCode.BadRequest)
      {
        Content = new StringContent("Missing access token")
      };
    }

    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken); // Don't use IdentityConstants.BearerScheme here (="Identity.Bearer"), it is "Bearer" anyway
    return await base.SendAsync(request, cancellationToken);
  }
}
