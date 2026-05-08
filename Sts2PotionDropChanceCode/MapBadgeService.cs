using System;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2PotionDropChance;

/// <summary>
/// Owns badge UI logic. Patches call <see cref="EnsureBadgeUpdated"/> on a
/// NMapPoint when its visuals refresh; we attach (or update) a child PanelContainer
/// with the player's potion drop chance for that node. Idempotent — same call
/// reuses the existing badge node.
/// Disable via env var <c>STS2_POTION_DROP_CHANCE_DISABLED=1</c>.
/// </summary>
internal static class MapBadgeService
{
    private const string BadgeNodeName = "Sts2PotionDropChance_Badge";
    private const string LabelNodeName = "Label";
    private const int FontSize = 14;
    private const int CornerRadius = 4;
    private const int PaddingPx = 4;

    private static readonly bool _disabled =
        System.Environment.GetEnvironmentVariable("STS2_POTION_DROP_CHANCE_DISABLED") == "1";

    public static void Install(SceneTree _) { /* no scene-tree node needed; everything driven by Harmony */ }

    /// <summary>Called from NNormalMapPoint patches. Safe to call repeatedly.</summary>
    public static void EnsureBadgeUpdated(NNormalMapPoint nmp)
    {
        if (_disabled) { RemoveBadge(nmp); return; }

        try
        {
            // Only show on nodes the player can travel to right now.
            if (nmp.State != MapPointState.Travelable) { HideBadge(nmp); return; }

            var rm = RunManager.Instance;
            var state = rm?.State;
            if (state == null || nmp.Point == null) { HideBadge(nmp); return; }

            var me = LocalContext.GetMe(state.Players);
            if (me == null) { HideBadge(nmp); return; }

            var result = PotionDropCalculator.Compute(me, nmp.Point, state);
            if (result == null) { HideBadge(nmp); return; }

            var badge = GetOrCreateBadge(nmp);
            UpdateBadge(badge, result.Value);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] badge update failed: {ex.Message}");
        }
    }

    private static PanelContainer GetOrCreateBadge(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<PanelContainer>(BadgeNodeName);
        if (existing != null) return existing;

        var panel = new PanelContainer
        {
            Name = BadgeNodeName,
            Position = new Vector2(28, -28),
            ZIndex = 100,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        var label = new Label
        {
            Name = LabelNodeName,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.7f));
        label.AddThemeConstantOverride("outline_size", 3);
        panel.AddChild(label);
        nmp.AddChild(panel);
        return panel;
    }

    private static void UpdateBadge(PanelContainer badge, PotionDropCalculator.Result r)
    {
        var label = badge.GetNodeOrNull<Label>(LabelNodeName);
        if (label == null) return;

        label.Text = FormatText(r);
        var bgColor = ColorScale.For(r.Probability);
        bgColor.A = 0.92f;
        var stylebox = new StyleBoxFlat
        {
            BgColor = bgColor,
            CornerRadiusTopLeft = CornerRadius,
            CornerRadiusTopRight = CornerRadius,
            CornerRadiusBottomLeft = CornerRadius,
            CornerRadiusBottomRight = CornerRadius,
            ContentMarginLeft = PaddingPx,
            ContentMarginRight = PaddingPx,
            ContentMarginTop = 2,
            ContentMarginBottom = 2,
        };
        badge.AddThemeStyleboxOverride("panel", stylebox);
        badge.Visible = true;
    }

    private static string FormatText(PotionDropCalculator.Result r)
    {
        var pct = $"{r.Probability * 100f:0}%";
        return r.Kind == PotionDropCalculator.NodeKind.UnknownAsMonster ? $"M:{pct}" : pct;
    }

    private static void HideBadge(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<PanelContainer>(BadgeNodeName);
        if (existing != null) existing.Visible = false;
    }

    private static void RemoveBadge(NNormalMapPoint nmp)
    {
        var existing = nmp.GetNodeOrNull<PanelContainer>(BadgeNodeName);
        existing?.QueueFree();
    }
}
