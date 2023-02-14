namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents a keyed dependency.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TService">The type of dependency.</typeparam>
public sealed class Dependency<TKey, TService> : IDependency<TKey, TService>
    where TService : notnull
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Dependency{TKey, TService}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The associated <see cref="IServiceProvider">service provider</see>.</param>
    public Dependency(IServiceProvider serviceProvider) =>
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public TService Value => (TService)serviceProvider.GetRequiredService(KeyedType.Create<TKey, TService>());

    object IDependency.Value => Value;
}