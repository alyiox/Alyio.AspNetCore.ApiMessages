// MIT License

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Alyio.AspNetCore.ApiMessages;

/// <summary>
/// Represents an exception filter to handler <see cref="IApiMessage"/> message and writes <see cref="IApiMessage.ProblemDetails"/> to the current http response.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class ApiMessageAttribute : ExceptionFilterAttribute
{
    /// <summary>
    /// Handles <see cref="IApiMessage"/> exceptions from action methods and writes Problem Details to the HTTP response.
    /// </summary>
    /// <param name="context">The <see cref="ExceptionContext"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    public override async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!context.HttpContext.Response.HasStarted && context.Exception is IApiMessage message)
        {
            var problemDetailsService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
            context.HttpContext.Response.StatusCode = message.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context.HttpContext,
                ProblemDetails = message.ProblemDetails
            });

            context.ExceptionHandled = true;
        }
    }
}
