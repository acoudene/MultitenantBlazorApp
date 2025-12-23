using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Multitenant.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace MultitenantBlazorApp.Controllers
{
  [Authorize]
  [ApiController]
  [Route("[controller]")]
  public class UserController : ControllerBase
  {

    private readonly ILogger<UserController> _logger;
    private readonly IClaimsTransformation _claimsTransformation;
    private readonly IConfiguration _configuration;
    private readonly IStatefulTenantIdProvider _tenantIdProvider;

    public UserController(
      ILogger<UserController> logger,
      IClaimsTransformation claimsTransformation,
      IConfiguration configuration,
      IStatefulTenantIdProvider tenantIdProvider)
    {
      Guard.IsNotNull(logger);
      Guard.IsNotNull(claimsTransformation);
      Guard.IsNotNull(configuration);
      Guard.IsNotNull(tenantIdProvider);

      _logger = logger;
      _claimsTransformation = claimsTransformation;
      _configuration = configuration;
      _tenantIdProvider = tenantIdProvider;
    }

    [HttpPost]
    [Route("CreateUser")]
    public async Task<ActionResult> CreateUserAsync()
    {
      if (User == null) throw new InvalidOperationException("Missing principal");

      var newPrincipal = await _claimsTransformation.TransformAsync(User);
      if (newPrincipal == null) throw new InvalidOperationException("No principal after transformation");

      var authClaims = newPrincipal.Claims;

      #region Création du token Jwt
      string? secret = _configuration["JWT:Secret"];
      if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("Missing JWT secret");

      var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

      var tokenDescriptor = new JwtSecurityToken(
          issuer: _configuration["JWT:ValidIssuer"],
          audience: _configuration["JWT:ValidAudience"],
          expires: DateTime.Now.AddHours(4),
          claims: authClaims,
          signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
          );
      #endregion

      // TODO ne renvoyer que la partie token
      return Ok(new
      {
        token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor),
        expiration = tokenDescriptor.ValidTo
      });
    }
  }
}