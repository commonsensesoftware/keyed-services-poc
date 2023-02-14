namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Defines the behavior of a dependency.
/// </summary>
public interface IDependency
{
    /// <summary>
    /// Gets the value of the resolved dependency.
    /// </summary>
    /// <value>The resolved dependency value.</value>
    object Value { get; }
}