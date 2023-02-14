namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Defines the behavior of a keyed dependency.
/// </summary>
/// <typeparam name="TKey">The type of key.</typeparam>
/// <typeparam name="TService">The type of dependency.</typeparam>
public interface IDependency<in TKey, out TService> :
    IDependency
    where TService : notnull
{
    /// <summary>
    /// Gets the value of the resolved dependency.
    /// </summary>
    /// <value>The resolved dependency value.</value>
    new TService Value { get; }
}