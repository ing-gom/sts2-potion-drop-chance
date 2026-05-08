# StS2 Potion Drop Chance

A **Slay the Spire 2** mod that shows the **potion drop probability** for each travelable combat node directly on the map screen — as a colored badge on the node icon.

> No more "did I just get unlucky, or is the pity counter low?" — see the actual number before you commit to a path.

![map with potion drop chance badges: bottom-left monster node 63%, right Unknown node with stacked monster 50% and elite 63% badges](docs/screenshots/unknown-badge.png)

*Bottom-left: a regular Monster node with `[potion] 63%` (lime). Right: an Unknown (`?`) node with two stacked badges — `[monster][potion] 50%` (yellow) above `[elite][potion] 63%` (lime), one per outcome.*

[한국어 README](README.ko.md)

---

## Features

- **Per-node probability badge** on every Monster / Elite / Unknown (`?`) node you can travel to next
- **Pity-counter aware** — same math as the game's `PotionRewardOdds.Roll()`: base 40%, ±10% per drop/miss
- **Elite bonus folded in** — Elite shows `pity + 12.5%` (the effective bonus from `Roll()`)
- **Unknown nodes** show the conditional probability — `M:50%` means *if it resolves to a Monster, drop chance is 50%*
- **Relic-aware**: White Beast Statue → 100% (all combat nodes), Sozu → 0% (procurement blocked)
- **Color scale** red → orange → yellow → lime → teal so you can read it at a glance without reading the number
- **Travelable nodes only** — badges show on nodes you can advance to *next*, not on the whole map
- **Local & read-only** — no game state mutation, no network sync, no impact on other players
- The manifest declares `"affects_gameplay": false` — safe to leave installed during multiplayer
- **Optional ModConfig support** — if [ModConfig](https://www.nexusmods.com/slaythespire2/mods/27) is installed, a *Hide on ? (Unknown) nodes* toggle appears in the `Mods` settings tab. The mod runs fine without ModConfig (the toggle UI is simply skipped)

## How it works

1. The mod patches `MegaCrit.Sts2.Core.Nodes.Screens.Map.NMapPoint.RefreshVisualsInstantly` (and the `_Ready` of `NNormalMapPoint`) with Harmony.
2. On every visual refresh, the patch checks if the node is `Travelable` and is a Monster / Elite / Unknown.
3. If so, the local player's potion drop probability is computed by mirroring `PotionRewardOdds.Roll()`:
   - Read `player.PlayerOdds.PotionReward.CurrentValue` (the pity counter)
   - Add `+0.125` for Elite nodes
   - Call `Hook.ShouldForcePotionReward(...)` → 100% if any relic forces it (e.g., White Beast Statue)
   - Detect Sozu in `player.Relics` → 0% (procurement blocked)
4. A `PanelContainer` + `Label` is added as a child of the map node, with a color-scaled background.

The mod never calls `Roll()` itself — it only reads the same inputs the game uses.

## Color scale

| Probability | Color |
|---|---|
| 0% | red `#dc2626` |
| 25% | orange `#f97316` |
| 50% | yellow `#eab308` |
| 75% | lime `#84cc16` |
| 100% | teal `#0d9488` |

Linear interpolation between adjacent stops. The 100% endpoint is intentionally a cool teal-green (not pure green) so it's visually distinct from the 75% lime.

## Multiplayer

The mod is fully client-side:

- DLL only loads for the user who installed it; other players see an unmodified game.
- `LocalContext.GetMe(...)` is used to scope the displayed odds to *your* `PlayerOdds.PotionReward.CurrentValue` — you never see another player's pity counter.
- No network messages, no shared-state writes — pure UI overlay.

## Installation

1. Download the latest `Sts2PotionDropChance-vX.Y.Z.zip` from [GitHub Releases](../../releases).
2. Extract `Sts2PotionDropChance.dll` and `Sts2PotionDropChance.json` into:
   ```
   <Slay the Spire 2 install>/mods/Sts2PotionDropChance/
   ```
3. Launch the game.

## Building from source

Requirements:
- .NET SDK 9.0
- Godot.NET.Sdk 4.5.1 (resolved automatically)
- A local Slay the Spire 2 install (auto-detected via Steam registry / standard paths by `Sts2PathDiscovery.props`)

```sh
dotnet build Sts2PotionDropChance.csproj -c Release
```

The build automatically copies `Sts2PotionDropChance.dll` and `Sts2PotionDropChance.json` into `<sts2>/mods/Sts2PotionDropChance/`.

## Configuration

Set the environment variable `STS2_POTION_DROP_CHANCE_DISABLED=1` before launching the game to suppress all badges without uninstalling the mod.

## Notes & limits

- **Boss nodes are intentionally not annotated** — they're always on a fixed path, so the badge wouldn't change your decision.
- **Unknown (`?`) nodes** display the conditional probability assuming the node resolves to a Monster. The actual potion roll is only meaningful if the room becomes a combat — Treasure / Shop / Event don't drop potions.
- The probability shown is **what would be rolled if combat ended right now**. Subsequent fights modify the pity counter (±10% per drop / miss); the badge updates automatically when state refreshes.
- The mod never patches `PotionRewardOdds.Roll()` itself — it only reads inputs. If MegaCrit changes the formula in a future patch, this mod's display may become inaccurate; the source should be the first place to update.

## Credits

- **MegaCrit** — for Slay the Spire 2.
- **HarmonyX** — runtime patching library used by this mod (bundled with the game; not redistributed here).

## License

[MIT](LICENSE).
