using NUnit.Framework;

namespace AsuMech.Tests;

public class ConfigurationLayoutTests
{
    [Test]
    public void ConfigSavedWithTheOldDefaultMovesToTheNewLayoutOnce()
    {
        var config = new Configuration { UseNewLayout = false, LayoutVersion = 0 };
        Assert.That(config.MigrateLayout(), Is.True);
        Assert.That(config.UseNewLayout, Is.True);

        config.UseNewLayout = false;
        Assert.That(config.MigrateLayout(), Is.False);
        Assert.That(config.UseNewLayout, Is.False);
    }
}
