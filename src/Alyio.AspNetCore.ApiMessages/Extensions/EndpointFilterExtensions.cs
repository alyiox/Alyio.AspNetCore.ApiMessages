// MIT License

using Alyio.AspNetCore.ApiMessages.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Alyio.AspNetCore.ApiMessages.Extensions;

/// <summary>
/// Extension methods for adding API message handling to <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class EndpointFilterExtensions
{
    /// <summary>
    /// Adds an endpoint filter to handle <see cref="IApiMessage"/> exceptions and format responses as Problem Details API messages.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the endpoint convention builder.</typeparam>
    /// <param name="builder">The endpoint convention builder.</param>
    /// <returns>The <typeparamref name="TBuilder"/> so that additional calls can be chained.</returns>
    public static TBuilder AddApiMessage<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilterFactory((context, next) =>
        {
            var filter = context.ApplicationServices.GetRequiredService<ApiMessageFilter>();
            return (invocationContext) => filter.InvokeAsync(invocationContext, next);
        });

        return builder;
    }
}
