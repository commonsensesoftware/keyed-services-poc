namespace IntegrationTests.Containers;

using IntegrationTests;
using IntegrationTests.Services;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

[Trait("Container", nameof(Lamar))]
public class LamarTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services) =>
        Container.BuildAsync(registry => Configure(registry, services)).GetAwaiter().GetResult();

    private static void Configure(ServiceRegistry registry, IServiceCollection services)
    {
        var keyedServices = services.RemoveKeyedServices();

        if (keyedServices.Count > 0)
        {
            var visitor = new LamarKeyedServiceVisitor(registry);
            visitor.Visit(keyedServices);
        }

        registry.AddRange(services);
    }
}

internal sealed class LamarKeyedServiceVisitor : KeyedServiceDescriptorVisitor
{
    private readonly ServiceRegistry registry;

    public LamarKeyedServiceVisitor(ServiceRegistry registry)
        : base(typeof(LamarDependency<,>), typeof(LamarDependency<,,>)) => this.registry = registry;

    protected override void VisitDependency(ServiceDescriptor serviceDescriptor)
    {
        var instance = registry.For(serviceDescriptor.ServiceType)
                               .Use(serviceDescriptor.ImplementationType);

        instance.Lifetime = serviceDescriptor.Lifetime;
    }

    protected override void VisitService(Type key, ServiceDescriptor serviceDescriptor)
    {
        // TODO: yuck! how do we use ServiceDescriptor.ImplementationFactory or 
        // ServiceDescriptor.ImplementationInstance with the non-generic API? there
        // has to be a better way, but this at least shows that it will work
        var name = key.GetHashCode().ToString();
        var serviceType = serviceDescriptor.ServiceType;
        var lifetime = serviceDescriptor.Lifetime;

        if (serviceDescriptor.ImplementationType is Type implementationType)
        {
            registry.For(serviceType).Use(implementationType).Named(name).Lifetime = lifetime;
        }
        else if (serviceDescriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
        {
            var methodOfT = GetType().GetRuntimeMethods().First(m => m.Name == nameof(RegisterFactory))!;
            var method = methodOfT.MakeGenericMethod(serviceType);
            method.Invoke(this, new object[] { name, factory, lifetime });
        }
        else
        {
            var instance = serviceDescriptor.ImplementationInstance!;
            var methodOfT = GetType().GetRuntimeMethods().First(m => m.Name == nameof(RegisterInstance))!;
            var method = methodOfT.MakeGenericMethod(serviceType, instance.GetType());
            method.Invoke(this, new object[] { name, instance, lifetime });
        }
    }

    private void RegisterFactory<TSvc>(string name, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
        where TSvc : class
    {
        var instance = registry.For<TSvc>().Add(c => (TSvc)factory(c)).Named(name);
        instance.Lifetime = lifetime;
    }

    private void RegisterInstance<TSvc, TImpl>(string name, TImpl existing, ServiceLifetime lifetime)
        where TSvc : class
        where TImpl : class, TSvc
    {
        var instance = registry.For<TSvc>().Add(existing).Named(name);
        instance.Lifetime = lifetime;
    }
}

internal sealed class LamarDependency<TKey, TService> :
    StringNameDependency<TKey, TService>
    where TService : notnull
{
    public LamarDependency(IServiceContext resolver)
        : base(resolver.GetInstance<TService>) { }
}

internal sealed class LamarDependency<TKey, TService, TImplementation> :
    StringNameDependency<TKey, TService, TImplementation>
    where TService : notnull
    where TImplementation : notnull, TService
{
    public LamarDependency(IServiceContext resolver)
        : base(resolver.GetInstance<TImplementation>) { }
}