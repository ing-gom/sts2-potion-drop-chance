using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Sts2PotionDropChance;

/// <summary>
/// Per-node potion drop probability for a given local player.
/// Mirrors PotionRewardOdds.Roll() math without mutating any state.
/// Reference: MegaCrit.Sts2.Core.Odds.PotionRewardOdds — base 0.4f, Elite +0.125f effective bonus.
/// </summary>
internal static class PotionDropCalculator
{
    private const float EliteBonus = 0.125f; // num3 * 0.5f in Roll() with num3 = 0.25f for Elite

    public enum NodeKind { Monster, Elite, UnknownAsMonster }

    /// <summary>Computed odds for one map point. Probability == 1.0 ⇔ White Beast Statue, 0.0 ⇔ Sozu.</summary>
    public readonly record struct Result(NodeKind Kind, float Probability);

    public static Result? Compute(Player player, MapPoint point, IRunState runState)
    {
        var kind = ClassifyKind(point.PointType);
        if (kind is null) return null;

        if (HasSozu(player)) return new Result(kind.Value, 0f);

        var roomTypeForHook = kind.Value == NodeKind.Elite ? RoomType.Elite : RoomType.Monster;
        if (Hook.ShouldForcePotionReward(runState, player, roomTypeForHook))
            return new Result(kind.Value, 1f);

        var pity = player.PlayerOdds.PotionReward.CurrentValue;
        var bonus = kind.Value == NodeKind.Elite ? EliteBonus : 0f;
        return new Result(kind.Value, Clamp01(pity + bonus));
    }

    private static NodeKind? ClassifyKind(MapPointType pt) => pt switch
    {
        MapPointType.Monster => NodeKind.Monster,
        MapPointType.Elite   => NodeKind.Elite,
        MapPointType.Unknown => NodeKind.UnknownAsMonster,
        _ => null,
    };

    private static bool HasSozu(Player player) => player.Relics.OfType<Sozu>().Any();

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
