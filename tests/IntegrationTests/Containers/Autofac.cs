namespace IntegrationTests.Containers;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Container", nameof(Autofac))]
public class AutofacTests : Scenarios
{
    protected override IServiceProvider BuildServiceProvider(IServiceCollection services)
    {
        var builder = new ContainerBuilder();
        builder.Populate(services);
        return builder.Build().Resolve<IServiceProvider>();
    }
}