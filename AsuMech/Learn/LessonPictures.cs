using System.Collections.Generic;
using System.Linq;

namespace AsuMech.Learn;

// Image is a path under Learn/Pictures; Steps maps MasteryStore.StepId to a page index.
public sealed record LessonPicture(string Title, string Image, string? Text = null, Dictionary<string, string>? Roles = null);

public sealed record LessonPictures(string Credit, List<LessonPicture> Pages, Dictionary<string, int> Steps)
{
    public const string ResourcePrefix = "AsuMech.Learn.Pictures.";

    public static string ResourceName(string image) => ResourcePrefix + image.Replace('/', '.').Replace('\\', '.');

    public static bool Embedded(string image) => typeof(LessonPictures).Assembly.GetManifestResourceInfo(ResourceName(image)) != null;

    public IEnumerable<int> StepPages => Steps.Values.Where(i => i >= 0 && i < Pages.Count).Distinct().Order();
}
