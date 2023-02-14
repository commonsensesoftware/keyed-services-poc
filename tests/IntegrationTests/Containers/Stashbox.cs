namespace IntegrationTests.Containers;

using FluentAssertions;
using IntegrationTests;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Stashbox;
using Stashbox.Lifetime;
using Xunit;

[Trait("Container", nameof(Stashbox))]
public class StashboxTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        var keyedServices = services.RemoveKeyedServices();

        if (keyedServices.Count == 0)
        {
            return services.UseStashbox();
        }

        return services.UseStashbox(
            container =>
            {
                var visitor = new StashboxKeyedServiceVisitor(container);
                visitor.Visit(keyedServices);
            });
    }

    [Fact]
    public void resolve_IEnumerableX3CTX3E_without_key_should_return_expected_services()
    {
        // arrange
        var container = new StashboxContainer();
        var expected = new[] { typeof(Thing1), typeof(Thing2), typeof(Thing3) };

        container.Register<IThing, Thing1>();
        container.Register<IThing, Thing2>();
        container.Register<IThing, Thing3>();

        // act
        var thingies = container.Resolve<IEnumerable<IThing>>();

        // assert
        thingies.Select(t => t.GetType()).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void resolve_IEnumerableX3CTX3E_with_key_should_return_expected_services()
    {
        // arrange
        var container = new StashboxContainer();
        var name = KeyedType.Create<Key.Thingies, IThing>().GetHashCode().ToString();
        var expected = new[] { typeof(Thing1), typeof(Thing2), typeof(Thing3) };

        container.Register<IThing, Thing1>(name);
        container.Register<IThing, Thing2>(name);
        container.Register<IThing, Thing3>(name);

        // act
        var thingies = container.Resolve<IEnumerable<IThing>>(name);

        // assert
        thingies.Select(t => t.GetType()).Should().BeEquivalentTo(expected);
    }
}

internal sealed class StashboxKeyedServiceVisitor : KeyedServiceDescriptorVisitor
{
    private readonly IStashboxContainer container;

    public StashboxKeyedServiceVisitor(IStashboxContainer container)
        : base(typeof(StashboxDependency<,>), typeof(StashboxDependency<,,>)) => this.container = container;

    protected override void VisitDependency(ServiceDescriptor serviceDescriptor)
    {
        var lifetime = ToLifetime(serviceDescriptor.Lifetime);

        container.Register(
            serviceDescriptor.ServiceType,
            serviceDescriptor.ImplementationType,
            configure => configure.WithLifetime(lifetime));
    }

    protected override void VisitService(Type key, ServiceDescriptor serviceDescriptor)
    {
        var name = key.GetHashCode();
        var lifetime = ToLifetime(serviceDescriptor.Lifetime);

        if (serviceDescriptor.ImplementationType != null)
        {
            container.Register(
                serviceDescriptor.ServiceType,
                serviceDescriptor.ImplementationType,
                configure => configure.WithLifetime(lifetime)
                                      .WithName(name));
        }
        else if (serviceDescriptor.ImplementationFactory is Func<IServiceProvider, object> factory)
        {
            container.Register(
                serviceDescriptor.ServiceType,
                configure => configure.WithLifetime(lifetime)
                                      .WithFactory(factory)
                                      .WithName(name));
        }
        else
        {
            var instance = serviceDescriptor.ImplementationInstance;

            container.Register(
                serviceDescriptor.ServiceType,
                configure => configure.WithLifetime(lifetime)
                                      .WithInstance(instance)
                                      .WithName(name));
        }
    }

    private static LifetimeDescriptor ToLifetime(ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Scoped => Lifetimes.Scoped,
            ServiceLifetime.Singleton => Lifetimes.Singleton,
            _ => Lifetimes.Transient,
        };
}

internal sealed class StashboxDependency<TKey, TService> :
    TypeNameDependency<TKey, TService>
    where TService : notnull
{
    public StashboxDependency(IDependencyResolver resolver)
        : base(name => resolver.Resolve<TService>(name.GetHashCode())) { }
}

internal sealed class StashboxDependency<TKey, TService, TImplementation> :
    TypeNameDependency<TKey, TService, TImplementation>
    where TService : notnull
    where TImplementation : notnull, TService
{
    public StashboxDependency(IDependencyResolver resolver)
        : base(name => resolver.Resolve<TImplementation>(name.GetHashCode())) { }
}