namespace Microsoft.Extensions.DependencyInjection;

using System.Runtime.CompilerServices;

/// <summary>
/// Provides extensions methods for <see cref="IServiceCollection"/>.
/// </summary>
public static partial class KeyedServiceCollectionExtensions
{
    /// <summary>
    /// Removes keyed services from the specified service collection into a dictionary of keyed services, if any.
    /// </summary>
    /// <param name="services">The service collection to remove keyed services from.</param>
    /// <returns>A read-only dictionary containing of services mapped by key.</returns>
    public static IReadOnlyDictionary<Type, IServiceCollection> RemoveKeyedServices(this IServiceCollection services) =>
        services.RemoveKeyedServices(() => new ServiceCollection());

    /// <summary>
    /// Removes keyed services from the specified service collection into a dictionary of keyed services, if any.
    /// </summary>
    /// <param name="services">The service collection to remove keyed services from.</param>
    /// <param name="newServiceCollection">The factory function used to create new service collections.</param>
    /// <returns>A read-only dictionary containing of services mapped by key.</returns>
    public static IReadOnlyDictionary<Type, IServiceCollection> RemoveKeyedServices(
        this IServiceCollection services,
        Func<IServiceCollection> newServiceCollection)
    {
        var removed = default(Dictionary<Type, IServiceCollection>);
        var dependency = typeof(IDependency<,>);

        // remove and bucketize all services where ServiceDescriptor.ServicType is from:
        // 1. KeyedType.Create
        // 2. IDependency<TKey,TService>
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var service = services[i];
            var key = service.ServiceType;

            if (KeyedType.TryDeconstruct(key, out _, out var serviceType))
            {
                // clone ServiceDescriptor without the key applied
                service = service switch
                {
                    { ImplementationFactory: var f, Lifetime: var l } when f != null => new(serviceType, f, l),
                    { ImplementationInstance: var o } when o != null => new(serviceType, o),
                    { ImplementationType: var t, Lifetime: var l } when t != null => new(serviceType, t, l),
                    _ => throw new NotSupportedException("ServiceDescriptor could not be cloned."),
                };
            }
            else if (IsType(key, dependency))
            {
                var args = key.GenericTypeArguments;
                key = KeyedType.Create(args[0], args[0]);
            }
            else
            {
                continue;
            }

            removed ??= new();

            if (!removed.TryGetValue(key, out var keyedServices))
            {
                removed.Add(key, keyedServices = newServiceCollection());
            }

            keyedServices.Add(service);
            services.RemoveAt(i);
        }

        removed ??= new Dictionary<Type, IServiceCollection>(capacity: 0);
        return removed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsType(Type type, Type expected) =>
        type.IsGenericType &&
        !type.IsGenericTypeDefinition &&
        type.GetGenericTypeDefinition().IsAssignableFrom(expected);
}