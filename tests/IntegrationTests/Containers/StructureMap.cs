namespace IntegrationTests.Containers;

using IntegrationTests;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using StructureMap;
using StructureMap.Pipeline;
using Xunit;

[Trait("Container", nameof(StructureMap))]
public class StructureMapTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        var container = new Container();
        container.Configure(expression => Configure(expression, services));
        return container.GetInstance<IServiceProvider>();
    }

    private static void Configure(ConfigurationExpression configure, IServiceCollection services)
    {
        var keyedServices = services.RemoveKeyedServices();

        if (keyedServices.Count == 0)
        {
            configure.Populate(services);
        }
        else
        {
            var registry = new Registry();
            var visitor = new StructureMapKeyedServiceVisitor(registry);

            visitor.Visit(keyedServices);
            registry.Populate(services);
            configure.AddRegistry(registry);
        }
    }
}

internal sealed class StructureMapKeyedServiceVisitor : KeyedServiceDescriptorVisitor
{
    private readonly Registry registry;

    public StructureMapKeyedServiceVisitor(Registry registry)
        : base(typeof(StructureMapDependency<,>), typeof(StructureMapDependency<,,>)) => this.registry = registry;

    protected override void VisitDependency(ServiceDescriptor serviceDescriptor)
    {
        registry.For(serviceDescriptor.ServiceType)
                .LifecycleIs(ToLifecycle(serviceDescriptor.Lifetime))
                .Use(serviceDescriptor.ImplementationType);
    }

    protected override void VisitService(Type key, ServiceDescriptor serviceDescriptor)
    {
        var name = key.GetHashCode().ToString();
        var lifecycle = ToLifecycle(serviceDescriptor.Lifetime);

        if (serviceDescriptor.ImplementationType != null)
        {
            registry.For(serviceDescriptor.ServiceType)
                    .LifecycleIs(lifecycle)
                    .Use(serviceDescriptor.ImplementationType)
                    .Named(name);
        }
        else if (serviceDescriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
        {
            registry.For(serviceDescriptor.ServiceType)
                    .LifecycleIs(lifecycle)
                    .Use(context => factory(context.GetInstance<IServiceProvider>()))
                    .Named(name);
        }
        else
        {
            registry.For(serviceDescriptor.ServiceType)
                    .LifecycleIs(lifecycle)
                    .Use(serviceDescriptor.ImplementationInstance)
                    .Named(name);
        }
    }

    private static ILifecycle ToLifecycle(ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Scoped => Lifecycles.Container,
            ServiceLifetime.Singleton => Lifecycles.Singleton,
            _ => Lifecycles.Unique,
        };
}

internal sealed class StructureMapDependency<TKey, TService> :
    StringNameDependency<TKey, TService>
    where TService : notnull
{
    public StructureMapDependency(IContainer resolver)
        : base(resolver.GetInstance<TService>) { }
}

internal sealed class StructureMapDependency<TKey, TService, TImplementation> :
    StringNameDependency<TKey, TService, TImplementation>
    where TService : notnull
    where TImplementation : notnull, TService
{
    public StructureMapDependency(IContainer resolver)
        : base(resolver.GetInstance<TImplementation>) { }
}