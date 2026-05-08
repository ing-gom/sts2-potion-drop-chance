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
                MapBadgeService.Install(tree);

            Logger.Info($"[{ModId}] initialized (v0.4.1).");
        }
        catch (Exception ex)
        {
            Logger.Warn($"[{ModId}] init failed: {ex.Message}");
        }
    }
}
