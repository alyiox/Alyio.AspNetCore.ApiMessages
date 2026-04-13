// MIT License

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alyio.AspNetCore.ApiMessages.Filters;

/// <summary>
/// Represents an endpoint filter that handles <see cref="IApiMessage"/> exceptions and writes Problem Details to the HTTP response.
/// </summary>
internal sealed class ApiMessageFilter : IEndpointFilter
{
    private readonly ILogger<ApiMessageFilter> _logger;

    public ApiMessageFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ApiMessageFilter>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            if (ex is IApiMessage message)
            {
                if (context.HttpContext.Response.HasStarted)
                {
                    Log.ResponseAlreadyStarted(_logger);
                    throw; // Re-throw if response has started
                }

                var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                context.HttpContext.Response.StatusCode = message.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError;
                await problemDetailsService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = message.ProblemDetails
                });

                return Results.Empty;
            }
            throw; // Re-throw if not IApiMessage
        }
    }
}
