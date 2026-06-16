using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Sts2PotionDropChance;

/// <summary>
/// Single SceneTree node that polls screen-stack state once per frame and toggles
/// every active badge container's visibility accordingly. The badge nodes live
/// deep inside the map node tree, which stays in the SceneTree (only hidden) while
/// other screens are shown over it — and Godot's CanvasLayer/ZIndex stacking is
/// not enough on its own to keep our badges from bleeding through those covering
/// screens.
/// <para>
/// Primary rule (allowlist): show the badges <b>only</b> while the map is the
/// current/topmost interactive screen per <c>ActiveScreenContext.GetCurrentScreen()</c>,
/// which walks the engine's precedence chain (feedback → modal → inspect →
/// capstone/deck view → map → overlay stack [rewards, choose-a-card, choose-a-relic]
/// → rooms [shop, event, combat]). This catches every covering screen — card
/// reward / card-list / deck-view / shop / event included — not just modals.
/// </para>
/// <para>
/// Fallback (denylist) kicks in only if the engine type can't be resolved via
/// reflection (e.g. an API rename in a future build): hide when a modal is open
/// or any <see cref="NSubmenu"/> is visible, the original behavior.
/// </para>
/// </summary>
internal sealed partial class MapBadgeOverlayWatcher : Node
{
    private bool _lastSuppress;
    private readonly HashSet<NSubmenu> _submenus = new();

    // Reflected ActiveScreenContext.GetCurrentScreen() — public vs internal isn't
    // guaranteed across builds, so resolve defensively (mirrors MapBadgeService's
    // IsTraveling/_iconContainer reflection). Null after resolution → fall back to
    // the legacy denylist.
    private static MethodInfo? _activeInstanceGetter;
    private static MethodInfo? _getCurrentScreen;
    private static bool _screenCtxResolved;

    public override void _Ready()
    {
        // Keep ticking even when the SceneTree is paused (PauseMenu pauses by default).
        ProcessMode = ProcessModeEnum.Always;

        var tree = GetTree();
        if (tree != null)
        {
            tree.NodeAdded += OnNodeAdded;
            ScanForSubmenus(tree.Root);
        }
    }

    public override void _ExitTree()
    {
        var tree = GetTree();
        if (tree != null) tree.NodeAdded -= OnNodeAdded;
    }

    private void OnNodeAdded(Node n)
    {
        if (n is NSubmenu sm) _submenus.Add(sm);
    }

    private void ScanForSubmenus(Node n)
    {
        if (n is NSubmenu sm) _submenus.Add(sm);
        foreach (var c in n.GetChildren()) ScanForSubmenus(c);
    }

    public override void _Process(double delta)
    {
        bool suppress;
        try { suppress = ShouldSuppress(); }
        catch { suppress = false; }

        if (suppress == _lastSuppress) return;
        _lastSuppress = suppress;

        try
        {
            MapBadgeService.SetGlobalSuppress(suppress, GetTree());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}] suppress propagation failed: {ex.Message}");
        }
    }

    private bool ShouldSuppress()
    {
        // Primary: badges visible only when the map is the active/topmost screen.
        var mapIsCurrent = MapIsCurrentScreen();
        if (mapIsCurrent.HasValue) return !mapIsCurrent.Value;

        // Fallback when ActiveScreenContext can't be resolved.
        return LegacyShouldSuppress();
    }

    /// <summary>
    /// Returns true/false when <c>ActiveScreenContext</c> resolves, or null when it
    /// can't be queried (no run / map screen absent / reflection failed) so the
    /// caller can fall back. Compares the current screen by reference to the map
    /// screen — the map only wins this comparison when nothing covers it.
    /// </summary>
    private static bool? MapIsCurrentScreen()
    {
        var mapScreen = NMapScreen.Instance;
        if (mapScreen == null) return null; // out of run / map not built — no badges exist anyway.

        if (!_screenCtxResolved)
        {
            _screenCtxResolved = true;
            var t = AccessTools.TypeByName(
                "MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext");
            if (t != null)
            {
                _activeInstanceGetter = AccessTools.PropertyGetter(t, "Instance");
                _getCurrentScreen = AccessTools.Method(t, "GetCurrentScreen");
            }
        }
        if (_activeInstanceGetter == null || _getCurrentScreen == null) return null;

        var ctx = _activeInstanceGetter.Invoke(null, null);
        if (ctx == null) return null;
        var current = _getCurrentScreen.Invoke(ctx, null);
        return ReferenceEquals(current, mapScreen);
    }

    private bool LegacyShouldSuppress()
    {
        var modal = NModalContainer.Instance;
        if (modal != null && modal.OpenModal != null) return true;

        // Drop any submenus that have been freed since last tick.
        _submenus.RemoveWhere(s => !GodotObject.IsInstanceValid(s));
        foreach (var sm in _submenus)
        {
            if (sm.IsVisibleInTree()) return true;
        }
        return false;
    }
}
