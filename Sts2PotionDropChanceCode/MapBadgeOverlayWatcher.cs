using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Sts2PotionDropChance;

/// <summary>
/// Single SceneTree node that polls modal/menu state once per frame and toggles
/// every active badge container's visibility accordingly. The badge nodes live
/// deep inside the map node tree, and Godot's CanvasLayer/ZIndex stacking is
/// not enough on its own — modal popups and submenus can sit on a different
/// layer that doesn't visually cover us. So we hide the badges imperatively
/// whenever:
/// <list type="bullet">
///   <item><description><see cref="NModalContainer.Instance"/> reports an open modal (Settings confirms, etc.).</description></item>
///   <item><description>Any <see cref="NSubmenu"/> is visible — covers Pause, Settings, Stats, RunHistory, Modding, Timeline, Profile, GameOver, etc. NMapScreen itself inherits <c>Control</c> (not <c>NSubmenu</c>) so we never flag ourselves.</description></item>
/// </list>
/// </summary>
internal sealed partial class MapBadgeOverlayWatcher : Node
{
    private bool _lastSuppress;
    private readonly HashSet<NSubmenu> _submenus = new();

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
