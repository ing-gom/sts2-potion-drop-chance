using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Sts2PotionDropChance;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Sts2PotionDropChance";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; }
        = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        try
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll(typeof(MainFile).Assembly);
            Logger.Info($"[{ModId}] Harmony patches applied.");

            if (Engine.GetMainLoop() is SceneTree tree)
            {
                MapBadgeService.Install(tree);

                // Single watcher node owns modal-suppression bookkeeping for every
                // badge in the tree. Lives at SceneTree.Root so it ticks regardless
                // of which screen is currently active.
                var watcher = new MapBadgeOverlayWatcher { Name = $"{ModId}_OverlayWatcher" };
                tree.Root.CallDeferred(Node.MethodName.AddChild, watcher);

                // Defer ModConfig registration to the next frame so other mods (in
                // particular ModConfig itself) have finished their own Initialize.
                tree.CreateTimer(0.0).Timeout += ModConfigBridge.TryRegister;
            }

            Logger.Info($"[{ModId}] initialized (v0.4.8).");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[{ModId}] init failed: {ex.Message}");
        }
    }
}
