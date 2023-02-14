namespace IntegrationTests.Containers;

using Grace.DependencyInjection;
using Grace.DependencyInjection.Extensions;
using IntegrationTests;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

[Trait("Container", nameof(Grace))]
public class GraceTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        // note: we have to detect and remap ServiceDescriptor.ImplementationFactory;
        // otherwise we could have just used:
        //
        // return new DependencyInjectionContainer().Populate(services);

        var container = new DependencyInjectionContainer();
        var original = new ServiceCollection();

        for (var i = 0; i < services.Count; i++)
        {
            original.Insert(i, services[i]);
        }

        var keyedServices = services.RemoveKeyedServices();

        if (keyedServices.Count == 0 || RemapNotRequired(keyedServices))
        {
            return container.Populate(original);
        }

        var visitor = new GraceKeyedServiceVisitor(container);

        visitor.Visit(keyedServices);

        return container.Populate(services);
    }

    private static bool RemapNotRequired(IReadOnlyDictionary<Type, IServiceCollection> services) =>
        !services.Values.SelectMany(v => v).Any(sd => sd.ImplementationFactory != null);
}

internal sealed class GraceKeyedServiceVisitor : KeyedServiceDescriptorVisitor
{
    private readonly IInjectionScope container;

    public GraceKeyedServiceVisitor(IInjectionScope container)
        : base(typeof(GraceDependency<,>), typeof(GraceDependency<,,>)) => this.container = container;

    protected override void VisitDependency(ServiceDescriptor serviceDescriptor)
    {
        var serviceType = serviceDescriptor.ServiceType;
        var implementationType = serviceDescriptor.ImplementationType!;
        var lifetime = serviceDescriptor.Lifetime;

        container.Configure(c => c.Export(implementationType).As(serviceType).WithLifetime(lifetime));
    }

    protected override void VisitService(Type key, ServiceDescriptor serviceDescriptor)
    {
        var name = key.GetHashCode();
        var serviceType = serviceDescriptor.ServiceType;
        var lifetime = serviceDescriptor.Lifetime;

        if (serviceDescriptor.ImplementationType is Type implementationType)
        {
            container.Configure(c => c.Export(implementationType).AsKeyed(serviceType, name).WithLifetime(lifetime));
        }
        else if (serviceDescriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
        {
            container.Configure(c => c.ExportFactory(factory).AsKeyed(serviceType, name).WithLifetime(lifetime));
        }
        else
        {
            var instance = serviceDescriptor.ImplementationInstance;
            container.Configure(c => c.ExportInstance(instance).AsKeyed(serviceType, name).WithLifetime(lifetime));
        }
    }
}

// HACK: forked for proposal purposes as these are internal to Grace
internal static class GraceExtensions
{
    internal static IFluentExportStrategyConfiguration WithLifetime(this IFluentExportStrategyConfiguration configuration, ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Scoped => configuration.Lifestyle.SingletonPerScope(),
            ServiceLifetime.Singleton => configuration.Lifestyle.Singleton(),
            _ => configuration,
        };

    internal static IFluentExportInstanceConfiguration<T> WithLifetime<T>(this IFluentExportInstanceConfiguration<T> configuration, ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Scoped => configuration.Lifestyle.SingletonPerScope(),
            ServiceLifetime.Singleton => configuration.Lifestyle.Singleton(),
            _ => configuration,
        };
}

internal sealed class GraceDependency<TKey, TService> :
    TypeNameDependency<TKey, TService>
    where TService : notnull
{
    public GraceDependency(ILocatorService locator)
        : base(name => locator.Locate<TService>(withKey: name.GetHashCode())) { }
}

internal sealed class GraceDependency<TKey, TService, TImplementation> :
    TypeNameDependency<TKey, TService, TImplementation>
    where TService : notnull
    where TImplementation : notnull, TService
{
    public GraceDependency(ILocatorService locator)
        : base(name => locator.Locate<TImplementation>(withKey: name.GetHashCode())) { }
}