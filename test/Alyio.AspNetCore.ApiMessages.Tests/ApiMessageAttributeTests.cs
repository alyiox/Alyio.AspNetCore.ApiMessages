// MIT License

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Alyio.AspNetCore.ApiMessages.Tests;

#pragma warning disable CA1822 // Mark members as static
[ApiMessage]
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("400")]
    public IActionResult Get400() => throw new BadRequestException(XMessage.ValidationFailed);

    [HttpGet("401")]
    public IActionResult Get401() => throw new UnauthorizedException();

    [HttpGet("403")]
    public IActionResult Get403() => throw new ForbiddenException();

    [HttpGet("404")]
    public IActionResult Get404() => throw new NotFoundException();

    [HttpGet("500")]
    public IActionResult Get500() => throw new InternalServerErrorException();

    [HttpGet("ok")]
    public IActionResult GetOk() => new OkObjectResult("OK");
}
#pragma warning restore CA1822 // Mark members as static

public class ApiMessageAttributeTests
{
    [Theory]
    [InlineData("/api/test/400", HttpStatusCode.BadRequest, StatusCodeTypes.Status400BadRequest)]
    [InlineData("/api/test/401", HttpStatusCode.Unauthorized, StatusCodeTypes.Status401Unauthorized)]
    [InlineData("/api/test/403", HttpStatusCode.Forbidden, StatusCodeTypes.Status403Forbidden)]
    [InlineData("/api/test/404", HttpStatusCode.NotFound, StatusCodeTypes.Status404NotFound)]
    [InlineData("/api/test/500", HttpStatusCode.InternalServerError, StatusCodeTypes.Status500InternalServerError)]
    public async Task Attribute_WhenApiMessageExceptionIsThrown_ReturnsCorrectProblemDetails(string path, HttpStatusCode expectedStatusCode, string expectedType)
    {
        // Arrange
        using var host = CreateWebHost();

        await host.StartAsync();

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync(path);

        // Assert
        Assert.Equal(expectedStatusCode, response.StatusCode);
        var details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(details);
        Assert.Equal((int)expectedStatusCode, details.Status);
        Assert.Equal(expectedType, details.Type);
    }

    [Fact]
    public async Task Attribute_WhenNoExceptionIsThrown_ReturnsSuccess()
    {
        // Arrange
        using var host = CreateWebHost();

        await host.StartAsync();

        using var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/api/test/ok");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("OK", body);
    }

    private static IHost CreateWebHost()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseTestServer();
                builder.Configure((ctx, app) =>
                {
                    app.UseRouting();
                    app.UseEndpoints(p => p.MapControllers());
                });
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddApiMessages();
                services.AddControllers().AddApplicationPart(typeof(TestController).Assembly);
            })
            .Build();

        return host;
    }
}
