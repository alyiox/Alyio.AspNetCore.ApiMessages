// MIT License

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alyio.AspNetCore.ApiMessages;

/// <summary>
/// A modern exception handler for .NET 8+ that integrates with the standard ASP.NET Core exception handling pipeline.
/// </summary>
public sealed class ApiMessagesExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiMessagesExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiMessagesExceptionHandler"/> class.
    /// </summary>
    public ApiMessagesExceptionHandler(ILogger<ApiMessagesExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to handle the <see cref="IApiMessage"/> exception.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <param name="exception">The <see cref="Exception"/> to handle.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
    /// <returns><c>true</c> if the exception was handled; otherwise, <c>false</c>.</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is IApiMessage message)
        {
            if (httpContext.Response.HasStarted)
            {
                Log.ResponseAlreadyStarted(_logger);
                return false;
            }

            var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            httpContext.Response.StatusCode = message.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = message.ProblemDetails
            });

            return true;
        }

        return false;
    }
}
