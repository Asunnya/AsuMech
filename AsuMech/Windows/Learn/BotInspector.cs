using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Core.SimObjects;
using AsuMech.Learn;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AsuMech.Windows.Learn;

// Target a bot during a run and this says which strat call sent it where, and the lesson's rule
// for that step and seat. Reads the orders the strat already gives; nothing is computed for it.
internal sealed class BotInspector : Window
{
    private const float WidthPx = 380f;
    private const float RightMarginPx = 20f;
    private const float TopPx = 330f;

    public BotInspector() : base("Bot inspector###AsuMechBotInspector")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing
                | ImGuiWindowFlags.NoSavedSettings;
        RespectCloseHotkey = false;
        ShowCloseButton = false;
    }

    private (ISimPartyMember Member, SimCharacter Character)? Targeted()
    {
        if (!Plugin.Config.ShowBotInspector || Plugin.Game.ActiveScenario is null) return null;
        if (Plugin.TargetManager.Target is not { } target) return null;
        var world = Plugin.Game.World;
        foreach (PartyRole role in System.Enum.GetValues<PartyRole>())
            if (world.Party.Get(role) is { } member && member.EntityId == target.EntityId && member != world.Party.Player && member is ISimPartyMember seat)
                return (seat, member);
        return null;
    }

    public override void PreOpenCheck() => IsOpen = Targeted() is not null;

    public override void PreDraw()
    {
        var display = ImGui.GetIO().DisplaySize;
        var scale = ImGuiHelpers.GlobalScale;
        Position = new Vector2(display.X - (WidthPx + RightMarginPx) * scale, TopPx * scale);
        PositionCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        if (Targeted() is not { } targeted) return;
        var world = Plugin.Game.World;
        var role = targeted.Member.Role;
        var lesson = Plugin.Game.ActiveStrat is { } strat && Plugin.Game.ActiveScenario is { } scenario ? LessonLoader.For(scenario, strat) : null;
        var order = world.LastOrders.TryGetValue(role, out var o) ? o : null;
        ImGui.PushTextWrapPos(WidthPx * ImGuiHelpers.GlobalScale);
        foreach (var (label, text) in BotInspection.Describe(role, order, targeted.Character.Position, world.Events.Elapsed, lesson))
        {
            using (AsuTheme.PushTextColor(AsuTheme.Muted)) ImGui.TextUnformatted($"{label}:");
            ImGui.SameLine();
            ImGui.TextWrapped(text);
        }
        ImGui.PopTextWrapPos();
    }
}
