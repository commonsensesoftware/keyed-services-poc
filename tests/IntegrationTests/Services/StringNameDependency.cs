namespace IntegrationTests.Services;

using Microsoft.Extensions.DependencyInjection;

internal abstract class StringNameDependency<TKey, TService> : IDependency<TKey, TService>
    where TService : notnull
{
    private readonly string key = KeyedType.Create<TKey, TService>().GetHashCode().ToString();
    private readonly Func<string, TService> resolve;

    protected StringNameDependency(Func<string, TService> resolve) => this.resolve = resolve;

    public TService Value => resolve(key);

    object IDependency.Value => Value;
}

internal abstract class StringNameDependency<TKey, TService, TImplementation> :
    IDependency<TKey, TService>
    where TService : notnull
    where TImplementation : notnull, TService
{
    private readonly string key = KeyedType.Create<TKey, TImplementation>().GetHashCode().ToString();
    private readonly Func<string, TImplementation> resolve;

    protected StringNameDependency(Func<string, TImplementation> resolve) => this.resolve = resolve;

    public TService Value => resolve(key);

    object IDependency.Value => Value;
}