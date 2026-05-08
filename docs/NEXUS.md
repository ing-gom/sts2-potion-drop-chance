# Nexus Mods listing copy

Reference text for the Nexus Mods page. Nexus uses BBCode in the long
description; the summary field is plain text. The two blocks below are
ready to paste.

---

## Summary (≤200 chars)

Show the potion drop probability for each travelable map node — pity-aware, relic-aware, color-scaled badge. Local + read-only, safe in multiplayer (`affects_gameplay: false`).

## Description (BBCode)

```bbcode
[size=4][b]What it does[/b][/size]

Slay the Spire 2 rolls potions at the [b]end[/b] of combat using a hidden pity counter — base 40%, ±10% per drop / miss. This mod simply reads that counter and shows what the next roll [i]would[/i] be, as a colored badge on every map node you can travel to next.

[size=4][b]Features[/b][/size]
[list]
[*][b]Pity-aware[/b] — same math as the game's [i]PotionRewardOdds.Roll()[/i]. Base 40%, ±10% per drop / miss.
[*][b]Elite bonus included[/b] — Elite nodes show [i]pity + 12.5%[/i].
[*][b]Relic-aware[/b] — White Beast Statue → 100%, Sozu → 0%. Calls the game's hooks, so future relics that affect potion drops should also be picked up.
[*][b]Unknown ([?][/?]) nodes get two stacked badges[/b] — one for the Monster outcome, one for the Elite outcome, each with its respective type icon.
[*][b]Color-scaled[/b]: red → orange → yellow → lime → teal. Distinct enough to read at a glance without checking the number.
[*][b]Travelable nodes only[/b] — badges only on nodes you can advance to next, not the whole map.
[*][b]Local + read-only[/b]. No game state mutation, no network sync. The mod manifest declares [i]affects_gameplay: false[/i] — safe to leave installed in multiplayer.
[*][b]Boss nodes are intentionally not annotated[/b] — fixed path, badge wouldn't change your decision.
[/list]

[size=4][b]Installation[/b][/size]
[list=1]
[*]Download the latest [b]Sts2PotionDropChance-vX.Y.Z.zip[/b] from the [url=https://github.com/ing-gom/sts2-potion-drop-chance/releases]GitHub Releases page[/url] (or the Files tab here).
[*]Extract the [i]Sts2PotionDropChance/[/i] folder into [i]<Slay the Spire 2 install>/mods/[/i].
[*]Launch the game.
[/list]

You should end up with:
[code]
<Slay the Spire 2>/mods/Sts2PotionDropChance/Sts2PotionDropChance.dll
<Slay the Spire 2>/mods/Sts2PotionDropChance/Sts2PotionDropChance.json
[/code]

[size=4][b]Multiplayer notes[/b][/size]
The mod is fully client-side. Other players see an unmodified game — your DLL only loads on your machine, doesn't write to game state, and doesn't send any network messages. Each player has their own pity counter and the badge is scoped to [i]your[/i] counter via [i]LocalContext[/i].

[size=4][b]Configuration[/b][/size]
Set the environment variable [i]STS2_POTION_DROP_CHANCE_DISABLED=1[/i] before launching the game to suppress all badges without uninstalling the mod.

[size=4][b]Caveats[/b][/size]
[list]
[*]The displayed probability is what would be rolled if combat ended right now. Subsequent fights modify the pity counter (±10% per drop / miss); the badge updates automatically when state refreshes.
[*]Unknown displays both Monster + Elite hypotheses. Internally the game has Elite disabled for Unknowns ([i]UnknownMapPointOdds[/i] Elite=-1) but both are shown for completeness.
[*]The mod doesn't patch [i]PotionRewardOdds.Roll()[/i] itself — only reads inputs. If MegaCrit changes the formula in a future patch, this mod's display may need a source update.
[/list]

[size=4][b]Credits / Source[/b][/size]
[list]
[*]MegaCrit — Slay the Spire 2.
[*]HarmonyX — runtime patching library used by this mod (bundled with the game; not redistributed).
[*]Source: [url=https://github.com/ing-gom/sts2-potion-drop-chance]github.com/ing-gom/sts2-potion-drop-chance[/url] · [url=https://github.com/ing-gom/sts2-potion-drop-chance/blob/main/LICENSE]MIT License[/url]
[*]한국어 설명: [url=https://github.com/ing-gom/sts2-potion-drop-chance/blob/main/README.ko.md]README.ko.md[/url]
[/list]
```
