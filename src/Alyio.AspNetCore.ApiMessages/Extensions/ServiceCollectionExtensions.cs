// MIT License

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add API message handling services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds API message handling services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddApiMessages(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails();
        services.AddSingleton<Alyio.AspNetCore.ApiMessages.Filters.ApiMessageFilter>();
        services.AddExceptionHandler<Alyio.AspNetCore.ApiMessages.ApiMessagesExceptionHandler>();
        return services;
    }
}
