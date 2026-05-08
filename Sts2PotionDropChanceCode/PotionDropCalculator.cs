using System;
using System.Collections.Generic;
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
/// Unknown nodes return two hypotheses (Monster + Elite); the actual outcome is rolled at travel time.
/// </summary>
internal static class PotionDropCalculator
{
    private const float EliteBonus = 0.125f;

    public enum NodeKind { Monster, Elite }

    public readonly record struct Result(NodeKind Kind, float Probability);

    private static readonly Result[] _empty = Array.Empty<Result>();

    public static IReadOnlyList<Result> ComputeAll(Player player, MapPoint point, IRunState runState)
    {
        var kinds = MapKinds(point.PointType);
        if (kinds == null) return _empty;

        var pity = player.PlayerOdds.PotionReward.CurrentValue;
        var sozu = HasSozu(player);
        var forceMonster = !sozu && Hook.ShouldForcePotionReward(runState, player, RoomType.Monster);
        var forceElite   = !sozu && Hook.ShouldForcePotionReward(runState, player, RoomType.Elite);

        var results = new List<Result>(kinds.Length);
        foreach (var kind in kinds)
        {
            float p;
            if (sozu) p = 0f;
            else if (kind == NodeKind.Monster && forceMonster) p = 1f;
            else if (kind == NodeKind.Elite && forceElite) p = 1f;
            else p = Clamp01(pity + (kind == NodeKind.Elite ? EliteBonus : 0f));
            results.Add(new Result(kind, p));
        }
        return results;
    }

    private static NodeKind[]? MapKinds(MapPointType pt) => pt switch
    {
        MapPointType.Monster => new[] { NodeKind.Monster },
        MapPointType.Elite   => new[] { NodeKind.Elite },
        MapPointType.Unknown => new[] { NodeKind.Monster, NodeKind.Elite },
        _ => null,
    };

    private static bool HasSozu(Player player) => player.Relics.OfType<Sozu>().Any();

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
