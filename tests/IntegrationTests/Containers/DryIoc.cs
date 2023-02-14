namespace IntegrationTests.Containers;

using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using IntegrationTests;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Container", nameof(DryIoc))]
public class DryIocTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        var keyedServices = services.RemoveKeyedServices();
        var container = new Container();

        container.Populate(services);

        if (keyedServices.Count > 0)
        {
            var visitor = new DryIocKeyedServiceVisitor(container);
            visitor.Visit(keyedServices);
        }

        return container.BuildServiceProvider();
    }
}

internal sealed class DryIocKeyedServiceVisitor : KeyedServiceDescriptorVisitor
{
    private readonly IContainer container;

    public DryIocKeyedServiceVisitor(IContainer container)
        : base(typeof(DryIocDependency<,>), typeof(DryIocDependency<,,>)) => this.container = container;

    protected override void VisitDependency(ServiceDescriptor serviceDescriptor)
    {
        var reuse = ToReuse(serviceDescriptor.Lifetime);

        container.Register(
            serviceDescriptor.ServiceType,
            serviceDescriptor.ImplementationType,
            reuse);
    }

    protected override void VisitService(Type key, ServiceDescriptor serviceDescriptor)
    {
        var serviceKey = key.GetHashCode();
        var reuse = ToReuse(serviceDescriptor.Lifetime);

        // equivalent to RegisterDescriptor, but with a key
        if (serviceDescriptor.ImplementationType != null)
        {
            container.Register(
                serviceDescriptor.ServiceType,
                serviceDescriptor.ImplementationType,
                reuse,
                serviceKey: serviceKey);
        }
        else if (serviceDescriptor.ImplementationFactory != null)
        {
            container.RegisterDelegate(
                serviceDescriptor.ServiceType,
                serviceDescriptor.ImplementationFactory,
                reuse,
                serviceKey: serviceKey);
        }
        else
        {
            container.RegisterInstance(
                true,
                serviceDescriptor.ServiceType,
                serviceDescriptor.ImplementationInstance,
                serviceKey: serviceKey);
        }
    }

    private static IReuse ToReuse(ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Scoped => Reuse.ScopedOrSingleton,
            ServiceLifetime.Singleton => Reuse.Singleton,
            _ => Reuse.Transient,
        };
}

internal sealed class DryIocDependency<TKey, TService> :
    TypeNameDependency<TKey, TService>
    where TService : notnull
{
    public DryIocDependency(IContainer container)
        : base(name => container.Resolve<TService>(name.GetHashCode())) { }
}

internal sealed class DryIocDependency<TKey, TService, TImplementation> :
    TypeNameDependency<TKey, TService, TImplementation>
    where TService : notnull
    where TImplementation : notnull, TService
{
    public DryIocDependency(IContainer container)
        : base(name => container.Resolve<TImplementation>(name.GetHashCode())) { }
}