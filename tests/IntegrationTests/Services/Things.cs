namespace IntegrationTests.Services;

public abstract class ThingBase : IThing
{
    protected ThingBase() { }

    public override string ToString() => GetType().Name;
}

public sealed class Thing : ThingBase { }

public sealed class KeyedThing : ThingBase { }

public sealed class Thing1 : ThingBase { }

public sealed class Thing2 : ThingBase { }

public sealed class Thing3 : ThingBase { }