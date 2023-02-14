namespace System;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Gets a service of the specific type and key.
    /// </summary>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <param name="key">The service key.</param>
    /// <returns>The matching service instance or <c>null</c>.</returns>
    public static object? GetService(this IServiceProvider serviceProvider, Type serviceType, Type key)
    {
        var keyedType = typeof(IDependency<,>).MakeGenericType(key, serviceType);
        var dependency = (IDependency?)serviceProvider.GetService(keyedType);
        return dependency?.Value;
    }

    /// <summary>
    /// Gets a required service of the specific type and key.
    /// </summary>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <param name="key">The service key.</param>
    /// <returns>The matching service instance.</returns>
    public static object GetRequiredService(this IServiceProvider serviceProvider, Type serviceType, Type key) =>
        serviceProvider.GetService(serviceType, key) ?? throw NoSuchService(key, serviceType);

    /// <summary>
    /// Gets a sequence of services of the specific type and key.
    /// </summary>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <param name="serviceType">The type of service to retrieve.</param>
    /// <param name="key">The service key.</param>
    /// <returns>A <see cref="IEnumerable{T}">sequence</see> of matching services.</returns>
    public static IEnumerable<object?> GetServices(this IServiceProvider serviceProvider, Type serviceType, Type key)
    {
        var keyedType = typeof(IDependency<,>).MakeGenericType(key, serviceType);

        foreach (IDependency? dependency in serviceProvider.GetServices(keyedType))
        {
            yield return dependency?.Value;
        }
    }

    /// <summary>
    /// Gets a service of the specific type and key.
    /// </summary>
    /// <typeparam name="TKey">The service key.</typeparam>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <returns>The matching service instance of type <typeparamref name="TService"/> or <c>null</c>.</returns>
    public static TService? GetService<TKey, TService>(this IServiceProvider serviceProvider) where TService : notnull
    {
        var dependency = serviceProvider.GetService<IDependency<TKey, TService>>();
        return dependency is null ? default : dependency.Value;
    }

    /// <summary>
    /// Gets a required service of the specific type and key.
    /// </summary>
    /// <typeparam name="TKey">The service key.</typeparam>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <returns>The matching service instance of type <typeparamref name="TService"/>.</returns>
    public static TService GetRequiredService<TKey, TService>(this IServiceProvider serviceProvider) where TService : notnull =>
        (serviceProvider.GetService<IDependency<TKey, TService>>() ?? throw NoSuchService(typeof(TKey), typeof(TService))).Value;

    /// <summary>
    /// Gets a sequence of services of the specific type and key.
    /// </summary>
    /// <typeparam name="TKey">The service key.</typeparam>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="serviceProvider">The extended <see cref="IServiceProvider">service provider</see>.</param>
    /// <returns>A <see cref="IEnumerable{T}">sequence</see> of matching services of type <typeparamref name="TService"/>.</returns>
    public static IEnumerable<TService> GetServices<TKey, TService>(this IServiceProvider serviceProvider) where TService : notnull
    {
        foreach (var dependency in serviceProvider.GetServices<IDependency<TKey, TService>>())
        {
            yield return dependency.Value;
        }
    }

    private static NotSupportedException NoSuchService(Type key, Type serviceType) =>
        new($"No service of type {serviceType.Name} with key {key.Name} could be found.");
}