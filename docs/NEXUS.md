# Nexus Mods listing copy

Reference text for the Nexus Mods page. Nexus uses BBCode in the long
description; the summary field is plain text. The blocks below are
ready to paste.

---

## Summary (≤200 chars)

Show the potion drop probability for each travelable map node — pity-aware, relic-aware, color-scaled badge. Local + read-only, safe in multiplayer (`affects_gameplay: false`).

## Description (BBCode, English)

```bbcode
[size=4][b]What it does[/b][/size]

Slay the Spire 2 rolls potions at the [b]end[/b] of combat using a hidden pity counter — base 40%, ±10% per drop / miss. This mod simply reads that counter and shows what the next roll [i]would[/i] be, as a colored badge on every map node you can travel to next.

[size=4][b]Features[/b][/size]
[list]
[*][b]Pity-aware[/b] — same math as the game's [i]PotionRewardOdds.Roll()[/i]. Base 40%, ±10% per drop / miss.
[*][b]Elite bonus included[/b] — Elite nodes show [i]pity + 12.5%[/i].
[*][b]Relic-aware[/b] — White Beast Statue → 100%, Sozu → 0%. Calls the game's hooks, so future relics that affect potion drops should also be picked up.
[*][b]Unknown ([?]) nodes get two stacked badges[/b] — one for the Monster outcome, one for the Elite outcome, each with its respective type icon.
[*][b]Color-scaled[/b]: red → orange → yellow → lime → teal. Distinct enough to read at a glance without checking the number.
[*][b]Travelable nodes only[/b] — badges only on nodes you can advance to next, not the whole map.
[*][b]Hides automatically on menus[/b] (v0.4.8+) — when you open the Pause menu, Settings, Stats, Modding, Run History, Timeline, or any other in-game submenu / modal popup, the badges disappear so they don't bleed through the UI. They reappear when you return to the map.
[*][b]Optional ModConfig integration[/b] (v0.4.8+) — if [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url] is installed, two options appear in the in-game [i]Settings → Mods[/i] tab and apply immediately: [i]Hide on ? (Unknown) nodes[/i] toggle and [i]Badge position[/i] dropdown (Right / Left / Above / Below). The mod runs unchanged without ModConfig (the UI is simply skipped — no hard dependency).
[*][b]Local + read-only[/b]. No game state mutation, no network sync. The mod manifest declares [i]affects_gameplay: false[/i] — safe to leave installed in multiplayer.
[*][b]Boss nodes are intentionally not annotated[/b] — fixed path, badge wouldn't change your decision.
[/list]

[size=4][b]Installation[/b][/size]
[list=1]
[*]Download the latest [b]Sts2PotionDropChance-vX.Y.Z.zip[/b] from the [url=https://github.com/ing-gom/sts2-potion-drop-chance/releases]GitHub Releases page[/url] (or the Files tab here).
[*]Extract the [i]Sts2PotionDropChance/[/i] folder into [i]<Slay the Spire 2 install>/mods/[/i].
[*](Optional) install [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url] to expose the in-game settings toggle.
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
[list]
[*][b]In-game (recommended, v0.4.8+)[/b] — install [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url], then open [i]Settings → Mods → Potion Drop Chance[/i] for the [i]Hide on ? (Unknown) nodes[/i] toggle and the [i]Badge position[/i] dropdown. Changes apply immediately.
[*][b]Environment variable[/b] — set [i]STS2_POTION_DROP_CHANCE_DISABLED=1[/i] before launching the game to suppress all badges without uninstalling the mod.
[/list]

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

## Description (BBCode, 한국어)

```bbcode
[size=4][b]이 모드는 무엇을 하나요[/b][/size]

Slay the Spire 2 는 전투가 [b]끝날 때[/b] 숨겨진 pity 카운터로 포션 드롭을 결정합니다 — 베이스 40%, 드롭/미드롭마다 ±10%. 이 모드는 그 카운터를 읽어서, 다음 roll 의 결과가 [i]어떻게 될지[/i] 진입 가능한 모든 맵 노드 옆에 색상 배지로 표시합니다.

[size=4][b]주요 기능[/b][/size]
[list]
[*][b]Pity 카운터 반영[/b] — 게임의 [i]PotionRewardOdds.Roll()[/i] 과 동일한 수식. 베이스 40%, 드롭/미드롭마다 ±10%.
[*][b]엘리트 보너스 포함[/b] — 엘리트 노드는 [i]pity + 12.5%[/i] 로 표시.
[*][b]유물 인식[/b] — White Beast Statue → 100%, Sozu → 0%. 게임의 hook 을 호출하므로 향후 추가되는 포션 관련 유물도 자동 반영될 가능성이 높습니다.
[*][b]Unknown ([?]) 노드는 두 배지로 분할 표시[/b] — 몬스터 가정과 엘리트 가정에 각각 타입 아이콘과 함께 별도 배지.
[*][b]색상 스케일[/b]: 빨강 → 주황 → 노랑 → 라임 → 청록. 숫자 안 봐도 한눈에 위계 파악.
[*][b]진입 가능한 노드만[/b] — 지도 전체가 아닌, 바로 다음에 갈 수 있는 노드에만 배지 표시.
[*][b]메뉴 진입 시 자동 숨김[/b] (v0.4.8+) — 일시정지(중단), 설정, 통계, 모드 메뉴, 런 히스토리, 타임라인 등 인게임 서브메뉴 또는 modal popup 이 떠있는 동안 배지가 자동으로 숨겨집니다. 맵 화면으로 돌아오면 다시 표시.
[*][b]선택적 ModConfig 지원[/b] (v0.4.8+) — [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url] 가 설치되어 있으면 게임 [i]설정 → Mods[/i] 탭에 두 가지 옵션이 노출되고 변경 즉시 반영됩니다: [i]Hide on ? (Unknown) nodes[/i] 토글 + [i]Badge position[/i] 드롭다운 (Right / Left / Above / Below). ModConfig 미설치 환경에서도 모드는 정상 작동 (UI 만 숨김 — 의존성 강제 없음).
[*][b]로컬 + 읽기 전용[/b]. 게임 상태 수정 없음, 네트워크 동기화 없음. 매니페스트에 [i]affects_gameplay: false[/i] 표기 — 멀티플레이 중에도 안전.
[*][b]보스 노드는 의도적으로 미표시[/b] — 경로가 정해져 있어 배지가 의사결정에 영향을 주지 않습니다.
[/list]

[size=4][b]설치[/b][/size]
[list=1]
[*][url=https://github.com/ing-gom/sts2-potion-drop-chance/releases]GitHub Releases 페이지[/url] (또는 본 페이지의 Files 탭) 에서 최신 [b]Sts2PotionDropChance-vX.Y.Z.zip[/b] 다운로드.
[*][i]Sts2PotionDropChance/[/i] 폴더를 [i]<Slay the Spire 2 설치 경로>/mods/[/i] 안에 압축 해제.
[*](선택) 인게임 설정 토글을 사용하려면 [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url] 도 함께 설치.
[*]게임 실행.
[/list]

다음 경로에 파일이 위치해야 합니다:
[code]
<Slay the Spire 2>/mods/Sts2PotionDropChance/Sts2PotionDropChance.dll
<Slay the Spire 2>/mods/Sts2PotionDropChance/Sts2PotionDropChance.json
[/code]

[size=4][b]멀티플레이[/b][/size]
완전한 클라이언트 사이드 모드입니다. 다른 플레이어는 변경 없는 게임을 봅니다 — DLL 은 본인 클라이언트에만 로드되고, 게임 상태에 쓰지 않으며, 네트워크 메시지도 보내지 않습니다. 각 플레이어가 자신의 pity 카운터를 가지며, [i]LocalContext[/i] 로 [i]본인의[/i] 카운터에 한정해 표시합니다.

[size=4][b]설정[/b][/size]
[list]
[*][b]인게임 설정 (권장, v0.4.8+)[/b] — [url=https://www.nexusmods.com/slaythespire2/mods/27]ModConfig[/url] 설치 후 [i]설정 → Mods → Potion Drop Chance[/i] 에서 [i]Hide on ? (Unknown) nodes[/i] 토글과 [i]Badge position[/i] 드롭다운 (Right / Left / Above / Below) 사용. 변경 즉시 반영.
[*][b]환경 변수[/b] — 게임 실행 전 [i]STS2_POTION_DROP_CHANCE_DISABLED=1[/i] 설정 시 모든 배지 비활성화 (모드 제거 없이).
[/list]

[size=4][b]주의사항[/b][/size]
[list]
[*]표시되는 확률은 "지금 전투가 끝나면" 의 roll 결과 기준입니다. 이후 전투마다 pity 카운터가 ±10% 변동하며, 배지는 게임 상태 갱신 시 자동으로 업데이트됩니다.
[*]Unknown 노드는 몬스터/엘리트 두 가정 모두 표시합니다. 내부적으로 [i]UnknownMapPointOdds[/i] 의 Elite=-1 로 엘리트가 비활성이지만, 완전성을 위해 두 가정 모두 노출.
[*]모드는 [i]PotionRewardOdds.Roll()[/i] 자체를 패치하지 않으며 입력값만 읽습니다. MegaCrit 가 추후 패치에서 공식을 변경하면 모드 갱신이 필요할 수 있습니다.
[/list]

[size=4][b]크레딧 / 소스[/b][/size]
[list]
[*]MegaCrit — Slay the Spire 2.
[*]HarmonyX — 런타임 패치 라이브러리 (게임에 번들, 재배포 안 함).
[*]소스: [url=https://github.com/ing-gom/sts2-potion-drop-chance]github.com/ing-gom/sts2-potion-drop-chance[/url] · [url=https://github.com/ing-gom/sts2-potion-drop-chance/blob/main/LICENSE]MIT License[/url]
[*]English description: [url=https://github.com/ing-gom/sts2-potion-drop-chance/blob/main/README.md]README.md[/url]
[/list]
```
