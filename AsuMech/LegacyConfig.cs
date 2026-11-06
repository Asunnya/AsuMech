using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AsuMech;

// Dalamud names a plugin's config file and folder after its InternalName, so the rename starts
// from an empty config. The one the old name left beside it is read once; its "$type" names a
// class in an assembly that no longer exists, so the type tags are dropped before reading.
public static class LegacyConfig
{
    public const string Name = "AnoMech"; // rename:keep

    public static Configuration? Import(FileInfo configFile, DirectoryInfo configDirectory)
    {
        if (configFile.Exists || configFile.DirectoryName is not { } dir) return null;
        var legacyFile = new FileInfo(Path.Combine(dir, Name + ".json"));
        if (!legacyFile.Exists || legacyFile.FullName == configFile.FullName) return null;

        var root = JToken.Parse(File.ReadAllText(legacyFile.FullName));
        StripTypeTags(root);
        var serializer = JsonSerializer.Create(new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
        if (root.ToObject<Configuration>(serializer) is not { } config) return null;
        config.ImportedFrom = Name;

        if (configDirectory.Parent is { } parent)
            CopyMissing(new DirectoryInfo(Path.Combine(parent.FullName, Name)), configDirectory);
        return config;
    }

    private static void StripTypeTags(JToken token)
    {
        if (token is JObject obj) obj.Remove("$type");
        foreach (var child in token.Children()) StripTypeTags(child);
    }

    private static void CopyMissing(DirectoryInfo from, DirectoryInfo to)
    {
        if (!from.Exists || from.FullName == to.FullName) return;
        to.Create();
        foreach (var file in from.EnumerateFiles())
        {
            var target = Path.Combine(to.FullName, file.Name);
            if (!File.Exists(target)) file.CopyTo(target);
        }
        foreach (var sub in from.EnumerateDirectories())
            CopyMissing(sub, new DirectoryInfo(Path.Combine(to.FullName, sub.Name)));
    }
}
