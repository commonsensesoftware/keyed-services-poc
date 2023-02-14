namespace IntegrationTests.Containers;

using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Unity = Unity.Microsoft.DependencyInjection.ServiceProviderExtensions;

[Trait("Container", nameof(Unity))]
public class UnityTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services) => Unity.BuildServiceProvider(services);
}