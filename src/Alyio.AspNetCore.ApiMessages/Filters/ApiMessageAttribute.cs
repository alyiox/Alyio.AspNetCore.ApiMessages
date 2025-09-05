﻿// MIT License

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alyio.AspNetCore.ApiMessages.Filters;

/// <summary>
/// Represents an exception filter to handler <see cref="IApiMessage"/> message and writes <see cref="IApiMessage.ProblemDetails"/> to the current http response.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ApiMessageAttribute : ExceptionFilterAttribute
{
    /// <summary>
    /// Writes the API message into <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context"><see cref="ExceptionContext"/>.</param>
    /// <returns></returns>
    public override Task OnExceptionAsync(ExceptionContext context)
    {
        if (!context.HttpContext.Response.HasStarted && context.Exception is IApiMessage message)
        {
            context.ExceptionHandled = true;
            return context.HttpContext.WriteProblemDetailsAsync(message);
        }

        return Task.CompletedTask;
    }
}
