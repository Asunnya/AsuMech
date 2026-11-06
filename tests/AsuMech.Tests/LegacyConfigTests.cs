using System.IO;

namespace AsuMech.Tests;

public class LegacyConfigTests
{
    private DirectoryInfo pluginConfigs = null!;

    [SetUp]
    public void SetUp() => pluginConfigs = Directory.CreateTempSubdirectory("legacy-config-");

    [TearDown]
    public void TearDown() => pluginConfigs.Delete(true);

    private FileInfo NewFile => new(Path.Combine(pluginConfigs.FullName, "AsuMech.json"));
    private DirectoryInfo NewDir => new(Path.Combine(pluginConfigs.FullName, "AsuMech"));

    private void WriteLegacy(string json) => File.WriteAllText(Path.Combine(pluginConfigs.FullName, LegacyConfig.Name + ".json"), json);

    private static readonly string SavedByOldAssembly = $$"""
        {
          "$type": "{{LegacyConfig.Name}}.Configuration, {{LegacyConfig.Name}}",
          "RelayServerUrl": "relay.example.com",
          "UseCustomRelay": true,
          "RelayAccessToken": "join-secret",
          "RelayTokenOrigin": "wss://relay.example.com:443",
          "PeerSecret": "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
          "BestStreaks": {
            "$type": "System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Int32, System.Private.CoreLib]], System.Private.CoreLib",
            "Dsr/P7": 4
          },
          "RecentScenarios": ["Dsr/P7", "Uwu/Garuda"],
          "ZoneDownOpcodes": [1, 2, 3]
        }
        """;

    [Test]
    public void ConfigSavedUnderTheOldNameLoadsWhole()
    {
        WriteLegacy(SavedByOldAssembly);

        var config = LegacyConfig.Import(NewFile, NewDir);

        Assert.That(config, Is.Not.Null);
        Assert.That(config!.ImportedFrom, Is.EqualTo(LegacyConfig.Name));
        Assert.That(config.RelayServerUrl, Is.EqualTo("relay.example.com"));
        Assert.That(config.UseCustomRelay, Is.True);
        Assert.That(config.TokenForRelay("relay.example.com"), Is.EqualTo("join-secret"));
        Assert.That(config.PeerSecret, Is.EqualTo("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"));
        Assert.That(config.BestStreaks, Is.EqualTo(new Dictionary<string, int> { ["Dsr/P7"] = 4 }));
        Assert.That(config.RecentScenarios, Is.EqualTo(new[] { "Dsr/P7", "Uwu/Garuda" }));
        Assert.That(config.ZoneDownOpcodes, Is.EqualTo(new uint[] { 1, 2, 3 }));
    }

    [Test]
    public void ExistingConfigIsNeverOverwritten()
    {
        WriteLegacy(SavedByOldAssembly);
        File.WriteAllText(NewFile.FullName, "{}");

        Assert.That(LegacyConfig.Import(NewFile, NewDir), Is.Null);
    }

    [Test]
    public void NothingToImportWithoutTheOldConfig()
        => Assert.That(LegacyConfig.Import(NewFile, NewDir), Is.Null);

    [Test]
    public void BeforeTheRenameTheConfigIsItsOwnLegacyAndIsLeftAlone()
    {
        var same = new FileInfo(Path.Combine(pluginConfigs.FullName, LegacyConfig.Name + ".json"));
        Assert.That(LegacyConfig.Import(same, new DirectoryInfo(Path.Combine(pluginConfigs.FullName, LegacyConfig.Name))), Is.Null);
    }

    [Test]
    public void LogsCapturesAndReplaysComeAlongWithoutReplacingNewFiles()
    {
        WriteLegacy(SavedByOldAssembly);
        var legacyDir = Directory.CreateDirectory(Path.Combine(pluginConfigs.FullName, LegacyConfig.Name));
        Directory.CreateDirectory(Path.Combine(legacyDir.FullName, "replays"));
        File.WriteAllText(Path.Combine(legacyDir.FullName, "replays", "pull.csv"), "old");
        File.WriteAllText(Path.Combine(legacyDir.FullName, "keep.txt"), "old");
        NewDir.Create();
        File.WriteAllText(Path.Combine(NewDir.FullName, "keep.txt"), "new");

        LegacyConfig.Import(NewFile, NewDir);

        Assert.That(File.ReadAllText(Path.Combine(NewDir.FullName, "replays", "pull.csv")), Is.EqualTo("old"));
        Assert.That(File.ReadAllText(Path.Combine(NewDir.FullName, "keep.txt")), Is.EqualTo("new"));
    }
}
