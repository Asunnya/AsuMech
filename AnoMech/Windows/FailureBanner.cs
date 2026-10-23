using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Windowing;

namespace AnoMech.Windows;

// The failure mark's stand-in when marks are turned off: without either, a failed mechanic nobody
// died to is easy to miss mid-fight.
internal sealed class FailureBanner : Window, IDisposable
{
    private const float HoldSeconds = 3f;
    private const float FadeSeconds = 1f;
    private static readonly Vector4 TitleColor = new(1f, 0.35f, 0.3f, 1f);

    private readonly IFontHandle titleFont;
    private string reason = "";
    private DateTime shownAt;

    public FailureBanner()
        : base("##AnoMechFailureBanner",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize)
    {
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
        titleFont = Plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68));
        Plugin.Game.MechanicFailed += OnMechanicFailed;
    }

    public void Dispose()
    {
        Plugin.Game.MechanicFailed -= OnMechanicFailed;
        titleFont.Dispose();
    }

    private void OnMechanicFailed(string failure)
    {
        if (Plugin.Config.EnableMechanicResultMarks) return;
        reason = failure;
        shownAt = DateTime.UtcNow;
        IsOpen = true;
    }

    public override void PreDraw()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + new Vector2(viewport.Size.X * 0.5f, viewport.Size.Y * 0.3f), ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        var age = (float)(DateTime.UtcNow - shownAt).TotalSeconds;
        if (age > HoldSeconds + FadeSeconds) IsOpen = false;
        ImGui.SetNextWindowBgAlpha(0f);
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, Math.Clamp(1f - (age - HoldSeconds) / FadeSeconds, 0f, 1f));
    }

    public override void PostDraw() => ImGui.PopStyleVar();

    public override void Draw()
    {
        using (titleFont.Push())
            CenteredText("MECHANIC FAILED", TitleColor);
        CenteredText(reason, new Vector4(1f, 1f, 1f, 1f));
    }

    private static void CenteredText(string text, Vector4 color)
    {
        var width = ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X;
        var offset = (width - ImGui.CalcTextSize(text).X) * 0.5f;
        if (offset > 0f) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextColored(color, text);
    }
}
