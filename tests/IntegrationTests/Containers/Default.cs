namespace IntegrationTests.Containers;

using FluentAssertions;
using IntegrationTests.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Container", nameof(Default))]
public class Default : Scenarios
{
    [Theory]
    [InlineData(typeof(IDependency<Key.Thing1, IThing>), true)]
    [InlineData(typeof(IDependency<Key.Thing2, IThing>), true)]
    [InlineData(typeof(IDependency<Key.Thingy, IThing>), false)]
    public void is_service_should_return_expected_result(Type serviceType, bool expected)
    {
        // arrange
        var services = new ServiceCollection();

        services.AddSingleton<Key.Thing1, IThing, Thing1>();
        services.AddTransient<Key.Thing2, IThing, Thing2>();

        var provider = BuildServiceProvider(services);

        // act
        var query = provider.GetRequiredService<IServiceProviderIsService>();

        // assert
        query.IsService(serviceType).Should().Be(expected);
    }
}