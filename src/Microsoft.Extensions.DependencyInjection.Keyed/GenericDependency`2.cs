namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Represents a generic, keyed dependency.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TService">The type of dependency.</typeparam>
/// <remarks>This class is meant to be used for open generic.s</remarks>
public sealed class GenericDependency<TKey, TService> : IDependency<TKey, TService>
    where TService : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenericDependency{TKey, TService}"/> class.
    /// </summary>
    /// <param name="value">The injected value for a keyed, open generic.</param>
    public GenericDependency(TService value) => Value = value;

    /// <inheritdoc />
    public TService Value { get; }

    object IDependency.Value => Value;
}