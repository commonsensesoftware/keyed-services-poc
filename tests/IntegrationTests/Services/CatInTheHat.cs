namespace IntegrationTests.Services;

using Microsoft.Extensions.DependencyInjection;

public class CatInTheHat
{
    private readonly IDependency<Key.Thing1, IThing> thing1;
    private readonly IDependency<Key.Thing2, IThing> thing2;

    public CatInTheHat(
        IDependency<Key.Thing1, IThing> thing1,
        IDependency<Key.Thing2, IThing> thing2)
    {
        this.thing1 = thing1;
        this.thing2 = thing2;
    }

    public IThing Thing1 => thing1.Value;

    public IThing Thing2 => thing2.Value;
}