using AnoMech.Core.Game;
using AnoMech.Scenarios;
using Dalamud.Bindings.ImGui;

namespace AnoMech.Windows.Asu;

// Filled in by later redesign steps.
public sealed partial class AsuMainWindow
{
    private enum FeedbackKind { Scenario, Strat, Bug }

    private void DrawFeedbackButton() { }
    private void DrawRequestLink(FeedbackKind kind) { }
    private static string BestStreakText(IScenario scenario) => "";
    private static void DrawStreak() => ImGui.TextDisabled($"Streak {Plugin.Practice.Streak}");
    private void DrawLearn(Game game, IScenario scenario, bool inSession) => Muted("Learn mode is not built yet.");
    private void DrawSelector(float width)
    {
        if (ImGui.Button($"{SelectorLabel()} ▾##selector", new System.Numerics.Vector2(width, 0f)))
            ImGui.OpenPopup("selector");
    }
}
