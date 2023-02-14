# Overview
A proof-of-concept for keyed services with `Microsoft.Extensions.DependencyInjection` using a `Type` as universal key as opposed other approaches such as _magic_ strings.

## Background and Motivation

The main reason this has not been supported is that `IServiceProvider.GetService(Type type)`  does not afford a way to retrieve a service by key. `IServiceProvider` has been the staple interface for service location since .NET 1.0 and changing or ignoring its well-established place in history is a nonstarter.  However... what if we could have our cake _and_ eat it to? 🤔 

A keyed service is a concept that comes up often in the IoC world. All, if not almost all, DI frameworks support registering and retrieving one or more services by a combination of type and key. There _are_ ways to make keyed services work in the existing design, but they are clunky to use (ex: via `Func<string, T>`). The following proposal would add support for keyed services to the existing `Microsoft.Extensions.DependencyInjection.*` libraries **without** breaking the `IServiceProvider` contract nor requiring any container framework changes.

## API Proposal

The first requirement is to define a _key_ for a service. `Type` is already a key. This proposal will use the novel idea of also using `Type` as a _composite key_. This design provides the following advantages:

- No _magic strings_
- No attributes or other required metadata
- No hidden service location lookups (e.g. a la _magic string_)
- No name collisions (types are unique)
- No additional interfaces required for resolution (ex: `ISupportRequiredService`)
- No implementation changes to the existing containers
- No additional library references (from the FCL or otherwise)
- Resolution intuitively fails if a key and service combination does not exist in the container

>The type names that follow are for illustration and _might_ change if the proposal is accepted.

**Resolving Services**

To resolve a keyed dependency we'll define the following contracts:

```c#
// required to 'access' a keyed service via typeof(T)
public interface IDependency
{
    object Value { get; }
}

public interface IDependency<in TKey, out TService> : IDependency
    where TService : notnull
{
    new TService Value { get; }
}
```

The following extension methods will be added to `ServiceProviderServiceExtensions`:

```c#
public static class ServiceProviderServiceExtensions
{
    public static object? GetService(
        this IServiceProvider serviceProvider,
        Type serviceType,
        Type key);

    public static object GetRequiredService(
        this IServiceProvider serviceProvider,
        Type serviceType,
        Type key);

    public static IEnumerable<object> GetServices(
        this IServiceProvider serviceProvider,
        Type serviceType,
        Type key);

    public static T? GetService<T>(
        this IServiceProvider serviceProvider,
        Type key) where T : notnull;

    public static T GetRequiredService<T>(
        this IServiceProvider serviceProvider,
        string key) where T : notnull;

    public static IEnumerable<T> GetServices<T>(
        this IServiceProvider serviceProvider,
        string key) where T : notnull;
}
```

Here is a partial example of how it would be implemented:

```c#
public static class ServiceProviderExtensions
{
    public static object? GetService(
        this IServiceProvider serviceProvider,
        Type serviceType,
        Type key)
    {
        var keyedType = typeof(IDependency<,>).MakeGenericType(key, serviceType);
        var dependency = (IDependency?)serviceProvider.GetService(keyedType);
        return dependency?.Value;
    }

    public static TService? GetService<TKey, TService>(this IServiceProvider serviceProvider)
        where TService : notnull
    {
        var dependency = serviceProvider.GetService<IDependency<TKey, TService>>();
        return dependency is null ? default : dependency.Value;
    }

    public static IEnumerable<TService> GetServices<TKey, TService>(this IServiceProvider serviceProvider)
        where TService : notnull
    {
        foreach (var dependency in serviceProvider.GetServices<IDependency<TKey, TService>>())
        {
            yield return dependency.Value;
        }
    }
}
```

**Registering Services**

Now that we have a way to _resolve_ a keyed service, how do we register one? `Type` is already used as a key, but we need a way to create an arbitrary composite key. To achieve this, we'll perform a little trickery on the `Type` which **only** affects how it is mapped in a container; thus making it a _composite key_. It does **not** change the runtime behavior nor require special Reflection _magic_. We are effectively taking advantage of the knowledge that `Type` will be used as the gatekeeper for a key used in service resolution for all container implementations. A specific container implementation does not need to actually use `Type` as
many containers already use `String` or some other type.

```c#
public static class KeyedType
{
    public static Type Create(Type key, Type type) =>
        new KeyedTypeInfo(key,type);
    
    public static Type Create<TKey, TType>() where TType : notnull =>
        new KeyedTypeInfo(typeof(TKey), typeof(TType));

    public static bool IsKey(Type type) => type is KeyedTypeInfo;

    private sealed class KeyedTypeInfo :
        TypeInfo,
        IReflectableType,
        ICloneable,
        IEquatable<Type>,
        IEquatable<TypeInfo>,
        IEquatable<KeyedTypeInfo>
    {
        private readonly Type key;
        private readonly Type type;

        public KeyedTypeInfo(Type key, Type type)
        {
            this.key = key;
            this.type = type;
        }

        public override int GetHashCode() => HashCode.Combine(key, type);

        // remainder omitted for brevity
    }
}
```

This might look _magical_, but it's not. `Type` is already being used as a key when it's mapped in a container. `KeyedTypeInfo` has all the appearance of the original type, but produces a different hash code when combined with another type. This affords for determinate, discrete unions of type registrations, which allows mapping the intended service multiple times.

Container implementers are free to perform the registration however they like, but the generic, out-of-the-box implementation would look like:

```c#
public sealed class Dependency<TKey, TService> : IDependency<TKey, TService>
    where TService : notnull
{
    private readonly IServiceProvider serviceProvider;

    public Dependency(IServiceProvider serviceProvider) =>
        this.serviceProvider = serviceProvider;

    public TService Value =>
        (TService)serviceProvider.GetRequiredService(KeyedType.Create<TKey, TService>());

    object IDependency.Value => Value;
}
```

Container implementers _might_ provide their own extension methods to make registration more succinct, but it is not required. The following registrations would work today without any container implementation changes:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddTransient(KeyedType.Create<Key.Thing1, IThing>(), typeof(Thing1));
    services.AddTransient<IDependency<Key.Thing1, IThing>, Dependency<Key.Thing1, IThing>>();
}

public void ConfigureUnity(IUnityContainer container)
{
    container.RegisterType(KeyedType.Create<Key.Thing1, IThing>(), typeof(Thing1));
    container.RegisterType<IDependency<Key.Thing1, IThing>, Dependency<Key.Thing1, IThing>>();
}

public void ConfigureAutofac(ContainerBuilder builder)
{
    builder.RegisterType(typeof(Thing1)).As(KeyedType.Create<Key.Thing1, IThing>());
    builder.RegisterType<Dependency<Key.Thing1, IThing>>().As<IDependency<Key.Thing1, IThing>>();
}
```

There is a minor drawback of requiring two registrations per keyed service in the container, but resolution for consumers is succinctly:

```c#
var longForm = serviceProvider.GetRequiredService<IDependency<Key.Thing1, IThing>>().Value;
var shortForm = serviceProvider.GetRequiredService<Key.Thing1, IThing>();
```

The following extension methods will be added to provide common registration through `IServiceCollection`
for all container frameworks:

```c#
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSingleton<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection AddSingleton(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection TryAddSingleton<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection TryAddSingleton(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection AddTransient<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection AddTransient(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection TryAddTransient<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection TryAddTransient(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection AddScoped<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection AddScoped(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection TryAddScoped<TKey, TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection TryAddScoped(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType);

    public static IServiceCollection TryAddEnumerable<TKey, TService, TImplementation>(
        this IServiceCollection services,
        ServiceLifetime lifetime)
        where TService : class
        where TImplementation : class, TService;

    public static IServiceCollection TryAddEnumerable(
        this IServiceCollection services,
        Type keyType,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime);
}
```

## API Usage

Putting it all together, here's how the API can be leveraged for any container framework that supports registration through `IServiceCollection`.

```c#
public interface IThing
{
    string ToString();
}

public abstract class ThingBase : IThing
{
    protected ThingBase() { }
    public override string ToString() => GetType().Name;
}

public sealed class Thing : ThingBase { }

public sealed class KeyedThing : ThingBase { }

public sealed class Thing1 : ThingBase { }

public sealed class Thing2 : ThingBase { }

public sealed class Thing3 : ThingBase { }

public static class Key
{
    public sealed class Thingies { }
    public sealed class Thing1 { }
    public sealed class Thing2 { }
}

public class CatInTheHat
{
    private readonly IDependency<Key.Thing1, IThing> thing1;
    private readonly IDependency<Key.Thing2, IThing> thing2;

    public CatInTheHat(
        IDependency<Key.Thing1, IThing> thing1,
        IDependency<Key.Thing2, IThing> thing2)
    {
        this.thing1 = thing1;
        this.thing2 = thing2;
    }

    public IThing Thing1 => thing1.Value;
    public IThing Thing2 => thing2.Value;
}

public void ConfigureServices(IServiceCollection collection)
{
    // keyed types
    services.AddSingleton<Key.Thing1, IThing, Thing1>();
    services.AddTransient<Key.Thing2, IThing, Thing2>();

    // non-keyed type with keyed type dependencies
    services.AddSingleton<CatInTheHat>();

    // keyed open generics
    services.AddTransient(typeof(IGeneric<>), typeof(Generic<>));
    services.AddSingleton(typeof(IDependency<,>), typeof(GenericDependency<,>));

    // keyed IEnumerable<T>
    services.TryAddEnumerable<Key.Thingies, IThing, Thing1>(ServiceLifetime.Transient);
    services.TryAddEnumerable<Key.Thingies, IThing, Thing2>(ServiceLifetime.Transient);
    services.TryAddEnumerable<Key.Thingies, IThing, Thing3>(ServiceLifetime.Transient);

    var provider = services.BuildServiceProvider();

    // resolve non-keyed type with keyed type dependencies
    var catInTheHat = provider.GetRequiredService<CatInTheHat>();

    // resolve keyed, open generic
    var openGeneric = provider.GetRequiredService<Key.Thingy, IGeneric<object>>();

    // resolve keyed IEnumerable<T>
    var thingies = provider.GetServices<Key.Thingies, IThing>();

    // related services such as IServiceProviderIsService
    // new extension methods could be added to make this more succinct
    var query = provider.GetRequiredService<IServiceProviderIsService>();
    var thing1Registered = query.IsService(typeof(IDependency<Key.Thing1, IThing>));
    var thing2Registered = query.IsService(typeof(IDependency<Key.Thing2, IThing>));
}
```

## Container Integration

| Container    | By Key             | By Key<br/>(Generic) | Many<br/>By Key    | Many By<br/>Key (Generic) | Open<br/>Generics  | Existing<br/>Instance |  Implementation<br/>Factory |
| ------------ | ------------------ | -------------------- | ------------------ | ------------------------- | ------------------ | --------------------- |  -------------------------- |
| Default      | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| Autofac      | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| DryIoc       | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| Grace        | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| Lamar        | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| LightInject  | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| Stashbox     | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| StructureMap | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |
| Unity        | :white_check_mark: | :white_check_mark:   | :white_check_mark: | :white_check_mark:        | :white_check_mark: | :white_check_mark:    |  :white_check_mark:         |

| Container    | Just<br/>Works     | No Container<br/>Changes | No Adapter<br/>Changes |
| ------------ | ------------------ | ------------------------ | ---------------------- |
| Default      | :white_check_mark: | :white_check_mark:       | :white_check_mark:     |
| Autofac      | :white_check_mark: | :white_check_mark:       | :white_check_mark:     |
| DryIoc       | :x:                | :white_check_mark:       | :x:                    |
| Grace        | :x:<sup>1</sup>    | :white_check_mark:       | :x:<sup>1</sup>        |
| Lamar        | :x:                | :white_check_mark:       | :x:                    |
| LightInject  | :white_check_mark: | :white_check_mark:       | :white_check_mark:     |
| Stashbox     | :x:                | :white_check_mark:       | :x:                    |
| StructureMap | :x:                | :white_check_mark:       | :x:                    |
| Unity        | :white_check_mark: | :white_check_mark:       | :white_check_mark:     |

<sub>[1]: Only _Implementation Factory_ doesn't work out-of-the-box</sub>

- **Just Works**: Works without any changes
- **No Container Changes**: Works without requiring fundamental container changes
- **No Adapter Changes**: Works without changing the way a container adapts to `IServiceCollection`

The proposed API usage and test cases demonstrate one approach that a container implementer _could_ use to register keyed services in
`IServiceCollection` through their adapter for scenarios where it doesn't work directly out-of-the-box. The provided implementation is
naive and in no way is meant to demonstrate how an implementer _must_ or _should_ adapt their container. The API proposal design review
may surface additional or better solutions.