// MIT License

using System.Net;
using System.Net.Http.Json;
using Alyio.AspNetCore.ApiMessages.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Alyio.AspNetCore.ApiMessages.Tests;

public class ApiMessageFilterTests
{
    [Theory]
    [InlineData("/400", HttpStatusCode.BadRequest, StatusCodeTypes.Status400BadRequest)]
    [InlineData("/401", HttpStatusCode.Unauthorized, StatusCodeTypes.Status401Unauthorized)]
    [InlineData("/403", HttpStatusCode.Forbidden, StatusCodeTypes.Status403Forbidden)]
    [InlineData("/404", HttpStatusCode.NotFound, StatusCodeTypes.Status404NotFound)]
    [InlineData("/500", HttpStatusCode.InternalServerError, StatusCodeTypes.Status500InternalServerError)]
    public async Task Filter_WhenApiMessageExceptionIsThrown_ReturnsCorrectProblemDetails(string path, HttpStatusCode expectedStatusCode, string expectedType)
    {
        // Arrange
        using var host = CreateWebHost(endpoints =>
        {
            endpoints.MapGet(path, () => { throw GetExceptionForPath(path); }).AddApiMessage();
        });

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

    private static IHost CreateWebHost(Action<IEndpointRouteBuilder> configureEndpoints)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseTestServer();

                builder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddApiMessages();
                });

                builder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        configureEndpoints(endpoints);
                    });
                });
            })
            .Build();

        return host;
    }

    private static Exception GetExceptionForPath(string path) => path switch
    {
        "/400" => new BadRequestException(XMessage.ValidationFailed),
        "/401" => new UnauthorizedException(),
        "/403" => new ForbiddenException(),
        "/404" => new NotFoundException(),
        "/500" => new InternalServerErrorException(),
        _ => new InvalidOperationException("Should not happen in this test.")
    };
}
