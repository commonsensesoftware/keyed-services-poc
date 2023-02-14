namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents a keyed dependency.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TService">The type of dependency.</typeparam>
/// <typeparam name="TImplementation">The type of dependency implementation.</typeparam>
public sealed class Dependency<TKey, TService, TImplementation> :
    IDependency<TKey, TService>
    where TService : notnull
    where TImplementation : notnull, TService
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dependency{TKey, TService, TImplementation}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The associated <see cref="IServiceProvider">service provider</see>.</param>
    public Dependency(IServiceProvider serviceProvider) =>
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public TService Value => (TService)serviceProvider.GetRequiredService(KeyedType.Create<TKey, TImplementation>());

    object IDependency.Value => Value;
}