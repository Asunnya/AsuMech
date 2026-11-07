using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AsuMech.Core.Game.Party;
using AsuMech.Learn;
using AsuMech.Network;
using AsuMech.Windows.Asu;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace AsuMech.Windows.Learn;

// The lesson's guide pictures, one page per step; follows the run unless the player browses.
internal sealed class PictureViewer : Window
{
    private const float DefaultWidthPx = 520f;
    private const float MinImagePx = 120f;

    private LessonPictures? pictures;
    private LessonDirector? subscribed;
    private int shown;
    private bool follow = true;
    private PartyRole role;

    public PictureViewer() : base("Guide pictures###AsuMechPictures")
    {
        Size = new Vector2(DefaultWidthPx, DefaultWidthPx * 0.75f);
        SizeCondition = ImGuiCond.FirstUseEver;
        RespectCloseHotkey = false;
    }

    public override void PreOpenCheck()
    {
        var session = LearnSession.Current;
        if (session is { Ended: false, Lesson.Pictures: { Pages.Count: > 0 } pics } && session.Level != LearnLevel.Exam)
        {
            if (!ReferenceEquals(subscribed, session.Director))
            {
                if (subscribed != null) subscribed.StepChanged -= OnStepChanged;
                subscribed = session.Director;
                subscribed.StepChanged += OnStepChanged;
                pictures = pics;
                role = session.Role;
                shown = pics.StepPages.DefaultIfEmpty(0).First();
                follow = true;
            }
            IsOpen = true;
        }
        else
        {
            if (subscribed != null) subscribed.StepChanged -= OnStepChanged;
            subscribed = null;
            pictures = null;
            IsOpen = false;
        }
    }

    private void OnStepChanged(ActiveStep step)
    {
        if (follow) Show(step);
    }

    private void JumpToCurrent()
    {
        if (subscribed?.Current is { } step) Show(step);
    }

    private void Show(ActiveStep step)
    {
        if (pictures is null || step.Step is null) return;
        if (pictures.Steps.TryGetValue(MasteryStore.StepId(step.Step.Key), out var index) && index >= 0 && index < pictures.Pages.Count)
            shown = index;
    }

    private IDisposable? theme;

    public override void PreDraw() => theme = AsuTheme.Push();

    public override void PostDraw()
    {
        theme?.Dispose();
        theme = null;
    }

    public override void Draw()
    {
        if (pictures is null) return;
        shown = Math.Clamp(shown, 0, pictures.Pages.Count - 1);
        var page = pictures.Pages[shown];

        if (ImGuiComponents.IconButton("pictures-prev", FontAwesomeIcon.ChevronLeft) && shown > 0) { shown--; follow = false; }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("pictures-next", FontAwesomeIcon.ChevronRight) && shown < pictures.Pages.Count - 1) { shown++; follow = false; }
        ImGui.SameLine();
        ImGui.TextUnformatted($"{shown + 1}/{pictures.Pages.Count}  {page.Title}");
        if (LearnSession.Current?.Lesson is { } lesson)
        {
            ImGui.SameLine();
            LessonVoteButtons.Draw(lesson, $"pic {shown + 1} {page.Title}", LessonFeedback.Toolbox);
        }
        if (!follow)
        {
            ImGui.SameLine();
            using (AsuTheme.PushAccentButton())
                if (ImGui.SmallButton("Follow the run")) { follow = true; JumpToCurrent(); }
        }

        var mine = page.Roles?.GetValueOrDefault(role.ToString());
        var lines = new List<(string Text, bool Accent)>();
        if (mine is { Length: > 0 }) lines.Add(($"You ({role}): {mine}", true));
        if (page.Text is { Length: > 0 }) lines.Add((page.Text, false));
        lines.Add((pictures.Credit, false));

        var avail = ImGui.GetContentRegionAvail();
        var below = lines.Sum(l => ImGui.CalcTextSize(l.Text, false, avail.X).Y + ImGui.GetStyle().ItemSpacing.Y);
        var texture = Plugin.TextureProvider.GetFromManifestResource(typeof(PictureViewer).Assembly, LessonPictures.ResourceName(page.Image));
        if (texture.TryGetWrap(out var wrap, out _))
        {
            var aspect = wrap.Height / (float)Math.Max(1, wrap.Width);
            var width = Math.Min(avail.X, Math.Max(MinImagePx * ImGuiHelpers.GlobalScale, avail.Y - below) / aspect);
            ImGui.Image(wrap.Handle, new Vector2(width, width * aspect));
        }
        else
            using (AsuTheme.PushTextColor(AsuTheme.Muted))
                ImGui.TextUnformatted("Loading picture...");

        foreach (var (text, accent) in lines)
            using (AsuTheme.PushTextColor(accent ? AsuTheme.Accent : AsuTheme.Muted))
                ImGui.TextWrapped(text);
    }
}
