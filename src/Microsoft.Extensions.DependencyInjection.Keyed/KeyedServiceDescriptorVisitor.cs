using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection;

// NOTE: this type is not strictly necessary, but it removes a lot of ceremony for what container
// implementers need to do in order to remap service registrations in a way that they can consume.

/// <summary>
/// Represents the base implementation for a keyed <see cref="ServiceDescriptor"/> visitor.
/// </summary>
public abstract class KeyedServiceDescriptorVisitor
{
    private static readonly Type idependency = typeof(IDependency<,>);
    private readonly Type dependencyOf2;
    private readonly Type dependencyOf3;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyedServiceDescriptorVisitor"/> class.
    /// </summary>
    /// <param name="dependencyOf2">The replacement type for <see cref="Dependency(Type, Type)"/>.</param>
    /// <param name="dependencyOf3">The replacement type for <see cref="Dependency(Type, Type, Type)"/>.</param>
    protected KeyedServiceDescriptorVisitor(
        Type dependencyOf2,
        Type dependencyOf3)
    {
        this.dependencyOf2 = ValidateType(dependencyOf2, 2);
        this.dependencyOf3 = ValidateType(dependencyOf3, 3);
    }

    /// <summary>
    /// Visits the specified collection of keyed services.
    /// </summary>
    /// <param name="keyedServices">A read-only dictionary of keyed services.</param>
    public void Visit(IReadOnlyDictionary<Type, IServiceCollection> keyedServices)
    {
        foreach (var (key, services) in keyedServices)
        {
            for (var i = 0; i < services.Count; i++)
            {
                var service = services[i];

                if (IsDependency(service.ServiceType))
                {
                    VisitDependency(RemapDependency(service));
                }
                else
                {
                    VisitService(key, service);
                }
            }
        }
    }

    /// <summary>
    /// Remaps the specified <see cref="ServiceDescriptor"/> to a container-specific
    /// <see cref="ServiceDescriptor"/> for <see cref="IDependency{TKey, TService}"/>.
    /// </summary>
    /// <param name="serviceDescriptor">The service descriptor to remap.</param>
    /// <returns>A new, remapped <see cref="ServiceDescriptor"/>.</returns>
    /// <exception cref="ArgumentException"><see cref="ServiceDescriptor.ImplementationType"/>
    /// in <paramref name="serviceDescriptor"/> is <c>null</c>.</exception>
    protected virtual ServiceDescriptor RemapDependency(ServiceDescriptor serviceDescriptor)
    {
        if (serviceDescriptor.ImplementationType is not Type implementationType)
        {
            var message = $"{nameof(ServiceDescriptor)}.{nameof(ServiceDescriptor.ImplementationType)} cannot be null.";
            throw new ArgumentException(message);
        }

        var serviceType = serviceDescriptor.ServiceType;
        var args = serviceType.GenericTypeArguments;
        var keyType = args[0];
        serviceType = args[1];

        // remap: Dependency<,> or Dependency<,,>
        // to:    [Container]Dependency<,> or [Container]Dependency<,,>
        //
        // note: additional validation recommended
        args = implementationType.GenericTypeArguments;
        var dependencyType = args.Length switch
        {
            2 => Dependency(keyType, serviceType),
            3 => Dependency(keyType, serviceType, args[^1]),
            _ => throw Unexpected(implementationType),
        };

        return new(IDependency(keyType, serviceType), dependencyType, serviceDescriptor.Lifetime);

        static NotSupportedException Unexpected(Type type) =>
            new($"Type {type} was expected to be a generic type with 2 or 3 type arguments.");
    }

    /// <summary>
    /// Visits the <see cref="ServiceDescriptor"/> for <see cref="IDependency{TKey, TService}"/>.
    /// </summary>
    /// <param name="serviceDescriptor">The service descriptor to visit.</param>
    protected abstract void VisitDependency(ServiceDescriptor serviceDescriptor);

    /// <summary>
    /// Visits the <see cref="ServiceDescriptor"/> for a service.
    /// </summary>
    /// <param name="key">The type representing the key associated with the service.</param>
    /// <param name="serviceDescriptor">The service descriptor to visit.</param>
    protected abstract void VisitService(Type key, ServiceDescriptor serviceDescriptor);

    /// <summary>
    /// Creates and returns a new, closed type for <see cref="IDependency{TKey, TService}"/>.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="serviceType">The service type.</param>
    /// <returns>A new closed type for <see cref="IDependency{TKey, TService}"/>.</returns>
    protected Type IDependency(Type keyType, Type serviceType) =>
        idependency.MakeGenericType(keyType, serviceType);

    /// <summary>
    /// Creates and returns a new, closed type for a container-specific implementation of
    /// <see cref="Dependency(Type, Type, Type)"/>.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="serviceType">The service type.</param>
    /// <returns>A new closed type.</returns>
    protected Type Dependency(Type keyType, Type serviceType) =>
        dependencyOf2.MakeGenericType(keyType, serviceType);

    /// <summary>
    /// Creates and returns a new, closed type for a container-specific implementation of
    /// <see cref="Dependency(Type, Type, Type)"/>.
    /// </summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="serviceType">The service type.</param>
    /// <param name="implementationType">The service implementation type.</param>
    /// <returns>A new closed type.</returns>
    protected Type Dependency(Type keyType, Type serviceType, Type implementationType) =>
        dependencyOf3.MakeGenericType(keyType, serviceType, implementationType);

    private static Type ValidateType(Type dependencyType, int expectedTypeArgCount)
    {
        if (dependencyType == null)
        {
            throw new ArgumentNullException("dependencyOf" + expectedTypeArgCount);
        }

        if (!dependencyType.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"{dependencyType} is not a generic type definition.");
        }

        var interfaces = dependencyType.GetInterfaces();
        var found = false;

        for (var i = 0; i < interfaces.Length; i++)
        {
            var iface = interfaces[i];

            if (!iface.IsGenericType)
            {
                continue;
            }

            var typeDef = iface.IsGenericTypeDefinition ? iface : iface.GetGenericTypeDefinition();

            if (found = typeDef.Equals(idependency))
            {
                break;
            }
        }

        if (!found)
        {
            throw new ArgumentException($"{dependencyType} does not implement {idependency}.");
        }

        if (dependencyType.GetGenericArguments().Length != expectedTypeArgCount)
        {
            throw new ArgumentException($"{dependencyType} is expected to have {expectedTypeArgCount} type arguments.");
        }

        return dependencyType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDependency(Type type) =>
        type.IsGenericType &&
        !type.IsGenericTypeDefinition &&
        type.GetGenericTypeDefinition().IsAssignableFrom(idependency);
}