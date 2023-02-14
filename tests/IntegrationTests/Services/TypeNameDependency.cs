namespace IntegrationTests.Services;

using Microsoft.Extensions.DependencyInjection;

internal abstract class TypeNameDependency<TKey, TService> : IDependency<TKey, TService>
    where TService : notnull
{
    private readonly Type key = KeyedType.Create<TKey, TService>();
    private readonly Func<Type, TService> resolve;

    protected TypeNameDependency(Func<Type, TService> resolve) => this.resolve = resolve;

    public TService Value => resolve(key);

    object IDependency.Value => Value;
}

internal abstract class TypeNameDependency<TKey, TService, TImplementation> :
    IDependency<TKey, TService>
    where TService : notnull
    where TImplementation : notnull, TService
{
    private readonly Type key = KeyedType.Create<TKey, TImplementation>();
    private readonly Func<Type, TImplementation> resolve;

    protected TypeNameDependency(Func<Type, TImplementation> resolve) => this.resolve = resolve;

    public TService Value => resolve(key);

    object IDependency.Value => Value;
}