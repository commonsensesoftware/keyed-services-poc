namespace IntegrationTests.Containers;

using IntegrationTests;
using LightInject.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Container", nameof(LightInject))]
public class LightInjectTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services) => services.CreateLightInjectServiceProvider();
}