using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using Multitenant.Security.AspNetCore.Configurations;
using System.Text.Encodings.Web;

namespace Multitenant.Security.AspNetCore.Tests;

public class ByTenantJwtBearerHandlerTests
{
  private readonly Mock<IOptionsMonitor<JwtBearerOptions>> _optionsMonitorMock;
  private readonly Mock<ILoggerFactory> _loggerFactoryMock;
  private readonly Mock<UrlEncoder> _urlEncoderMock;
  private readonly Mock<IJwtBearerOptionsProvider> _jwtBearerOptionsProviderMock;
  private readonly JwtBearerOptions _jwtBearerOptions;

  public ByTenantJwtBearerHandlerTests()
  {
    _optionsMonitorMock = new Mock<IOptionsMonitor<JwtBearerOptions>>();
    _loggerFactoryMock = new Mock<ILoggerFactory>();
    _urlEncoderMock = new Mock<UrlEncoder>();
    _jwtBearerOptionsProviderMock = new Mock<IJwtBearerOptionsProvider>();
    _jwtBearerOptions = new JwtBearerOptions();

    _optionsMonitorMock.Setup(o => o.CurrentValue).Returns(_jwtBearerOptions);
    _optionsMonitorMock.Setup(o => o.Get(It.IsAny<string>())).Returns(_jwtBearerOptions);

    _loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>()))
        .Returns(new Mock<ILogger>().Object);
  }

  [Fact]
  public async Task ChallengeAsync_ShouldSetChallengeResponse()
  {
    // Arrange
    var handler = new ByTenantJwtBearerHandler(_optionsMonitorMock.Object, _loggerFactoryMock.Object, _urlEncoderMock.Object, _jwtBearerOptionsProviderMock.Object);
    var context = new DefaultHttpContext();
    await handler.InitializeAsync(new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(ByTenantJwtBearerHandler)), context);

    // Act
    await handler.ChallengeAsync(new AuthenticationProperties());

    // Assert
    Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    Assert.Equal(JwtBearerDefaults.AuthenticationScheme, context.Response.Headers[HeaderNames.WWWAuthenticate]);
  }

  [Fact]
  public async Task ForbidAsync_ShouldSetForbidResponse()
  {
    // Arrange
    var handler = new ByTenantJwtBearerHandler(_optionsMonitorMock.Object, _loggerFactoryMock.Object, _urlEncoderMock.Object, _jwtBearerOptionsProviderMock.Object);
    var context = new DefaultHttpContext();
    await handler.InitializeAsync(new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(ByTenantJwtBearerHandler)), context);

    // Act
    await handler.ForbidAsync(new AuthenticationProperties());

    // Assert
    Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
  }
}