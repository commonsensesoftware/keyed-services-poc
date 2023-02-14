namespace IntegrationTests;

using FluentAssertions;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public abstract class Scenarios
{
    [Fact]
    public void get_service_should_return_service_without_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton<IThing, Thing>();
        services.AddSingleton<Key.Thingy, IThing, KeyedThing>();

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService<IThing>();

        // assert
        thing.Should().BeOfType<Thing>();
    }

    [Fact]
    public void get_service_should_return_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton(typeof(Key.Thingy), typeof(IThing), typeof(Thing1));
        services.AddSingleton(typeof(IThing), typeof(Thing));

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService(typeof(IThing), typeof(Key.Thingy));

        // assert
        thing.Should().BeOfType<Thing1>();
    }

    [Fact]
    public void get_service_generic_should_return_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton<Key.Thingy, IThing, Thing2>();
        services.AddSingleton<IThing, Thing>();

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService<Key.Thingy, IThing>();

        // assert
        thing.Should().BeOfType<Thing2>();
    }

    [Fact]
    public void get_required_service_should_return_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton(typeof(Key.Thingy), typeof(IThing), typeof(Thing3));
        services.AddSingleton(typeof(IThing), typeof(Thing));

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetRequiredService(typeof(IThing), typeof(Key.Thingy));

        // assert
        thing.Should().BeOfType<Thing3>();
    }

    [Fact]
    public void get_required_service_generic_should_return_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton<Key.Thingy, IThing, Thing3>();
        services.AddSingleton<IThing, Thing>();

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetRequiredService<Key.Thingy, IThing>();

        // assert
        thing.Should().BeOfType<Thing3>();
    }

    [Fact]
    public virtual void get_services_should_return_services_by_key()
    {
        // arrange
        var services = new ServiceCollection();
        var expected = new[] { typeof(Thing1), typeof(Thing2), typeof(Thing3) };

        services.TryAddEnumerable(typeof(Key.Thingies), typeof(IThing), typeof(Thing1), ServiceLifetime.Transient);
        services.TryAddEnumerable(typeof(Key.Thingies), typeof(IThing), typeof(Thing2), ServiceLifetime.Transient);
        services.TryAddEnumerable(typeof(Key.Thingies), typeof(IThing), typeof(Thing3), ServiceLifetime.Transient);

        var provider = BuildServiceProvider(services);

        // act
        var thingies = provider.GetServices(typeof(IThing), typeof(Key.Thingies));

        // assert
        thingies.Select(t => t!.GetType()).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public virtual void get_services_generic_should_return_services_by_key()
    {
        // arrange
        var services = new ServiceCollection();
        var expected = new[] { typeof(Thing1), typeof(Thing2), typeof(Thing3) };

        services.TryAddEnumerable<Key.Thingies, IThing, Thing1>(ServiceLifetime.Transient);
        services.TryAddEnumerable<Key.Thingies, IThing, Thing2>(ServiceLifetime.Transient);
        services.TryAddEnumerable<Key.Thingies, IThing, Thing3>(ServiceLifetime.Transient);

        var provider = BuildServiceProvider(services);

        // act
        var thingies = provider.GetServices<Key.Thingies, IThing>();

        // assert
        thingies.Select(t => t.GetType()).Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void get_required_service_should_inject_keyed_dependencies()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton<Key.Thing1, IThing, Thing1>();
        services.AddTransient<Key.Thing2, IThing, Thing2>();
        services.AddSingleton<CatInTheHat>();

        var provider = BuildServiceProvider(services);

        // act
        var catInTheHat = provider.GetRequiredService<CatInTheHat>();

        // assert
        catInTheHat.Thing1.Should().BeOfType<Thing1>();
        catInTheHat.Thing2.Should().BeOfType<Thing2>();
    }

    [Fact]
    public void get_services_should_return_open_generics_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddTransient(typeof(IGeneric<>), typeof(Generic<>));
        services.AddSingleton(typeof(IDependency<,>), typeof(GenericDependency<,>));

        var provider = BuildServiceProvider(services);

        // act
        var genericThing1 = provider.GetRequiredService<Key.Thing1, IGeneric<object>>();
        var anotherGenericThing1 = provider.GetRequiredService<Key.Thing1, IGeneric<object>>();
        var genericThing2 = provider.GetRequiredService<Key.Thing2, IGeneric<object>>();

        // assert
        genericThing1.Should().BeSameAs(anotherGenericThing1);
        genericThing2.Should().NotBeSameAs(genericThing1);
        genericThing2.Should().NotBeSameAs(anotherGenericThing1);
    }

    [Fact]
    public void get_service_should_use_existing_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();
        var existing = new Thing1();

        services.AddSingleton(typeof(Key.Thingy), typeof(IThing), existing);
        services.AddSingleton(typeof(IThing), new Thing());

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService(typeof(IThing), typeof(Key.Thingy));

        // assert
        thing.Should().BeSameAs(existing);
    }

    [Fact]
    public void get_service_generic_should_use_existing_service_by_key()
    {
        // arrange
        var services = new ServiceCollection();
        var existing = new Thing2();

        services.AddSingleton<Key.Thingy, IThing, Thing2>(existing);
        services.AddSingleton<IThing>(new Thing());

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService<Key.Thingy, IThing>();

        // assert
        thing.Should().BeSameAs(existing);
    }

    [Fact]
    public void get_service_should_use_factory_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddTransient(typeof(Key.Thingy), typeof(IThing), _ => new Thing1());
        services.AddTransient(typeof(IThing), _ => new Thing());

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService(typeof(IThing), typeof(Key.Thingy));

        // assert
        thing.Should().BeOfType<Thing1>();
    }

    [Fact]
    public void get_service_generic_should_use_factory_by_key()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddTransient<Key.Thingy, IThing, Thing2>(_ => new Thing2());
        services.AddTransient<IThing>(_ => new Thing());

        var provider = BuildServiceProvider(services);

        // act
        var thing = provider.GetService<Key.Thingy, IThing>();

        // assert
        thing.Should().BeOfType<Thing2>();
    }

    protected Scenarios() { }

    protected virtual IServiceProvider BuildServiceProvider(IServiceCollection services) => services.BuildServiceProvider();
}