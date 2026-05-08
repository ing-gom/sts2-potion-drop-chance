using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Sts2PotionDropChance.Patches;

/// <summary>
/// NNormalMapPoint hosts Monster/Elite/Unknown/Treasure/Shop/RestSite. Boss and
/// Ancient have their own classes and are intentionally not patched.
/// _Ready fires when the map screen builds the node; RefreshVisualsInstantly
/// fires on State changes (Untravelable → Travelable → Traveled). Both points
/// drive the same idempotent badge update.
/// </summary>
[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Ready))]
internal static class NNormalMapPoint_Ready_Patch
{
    private static void Postfix(NNormalMapPoint __instance) =>
        MapBadgeService.EnsureBadgeUpdated(__instance);
}

[HarmonyPatch(typeof(NMapPoint), nameof(NMapPoint.RefreshVisualsInstantly))]
internal static class NMapPoint_RefreshVisuals_Patch
{
    private static void Postfix(NMapPoint __instance)
    {
        if (__instance is NNormalMapPoint nmp)
            MapBadgeService.EnsureBadgeUpdated(nmp);
    }
}

// OnRelease commits the click. If travel actually starts (screen.IsTraveling
// flips true), sibling Travelable nodes' RefreshVisuals doesn't fire until
// the screen transitions — leaving their badges visible during the fade.
// Hide everything on the screen the moment travel begins.
[HarmonyPatch(typeof(NMapPoint), "OnRelease")]
internal static class NMapPoint_OnRelease_Patch
{
    private static void Postfix(NMapPoint __instance) =>
        MapBadgeService.HideAllBadgesIfTraveling(__instance);
}
