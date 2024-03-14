﻿// MIT License

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Alyio.AspNetCore.ApiMessages;

/// <summary>
///  Equivalent to HTTP status 404. <see cref="NotFoundMessage"/> indicates that the requested resource does not exist on the server.
/// </summary>
#pragma warning disable CA1710 // Identifiers should have correct suffix
public class NotFoundMessage : Exception, IApiMessage
#pragma warning restore CA1710 // Identifiers should have correct suffix
{
    /// <summary>
    /// Initialize a new instance of <see cref="NotFoundMessage"/> class.
    /// </summary>
    public NotFoundMessage()
    {
        this.ProblemDetails = new ProblemDetails
        {
            Title = XMessage.NotFound,
            Status = StatusCodes.Status404NotFound,
            Type = StatusCodeTypes.Status404NotFound,
        };
    }

    /// <summary>
    /// Initialize a new instance of <see cref="NotFoundMessage"/> class.
    /// </summary>
    public NotFoundMessage(string detail) : this()
    {
        this.ProblemDetails.Detail = detail;
    }

    /// <inheritdoc />
    public ProblemDetails ProblemDetails { get; }
}
