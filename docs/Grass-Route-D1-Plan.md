# Grass Route D1 — implementation status and remaining plan

Handoff note for continuing the D1 build of [GDD v2.2](Grass-Route-GDD.md) on another machine. Numbers come from
[Balance D1](Grass-Route-Balance-D1.md), levels from [Levels D1](Grass-Route-Levels-D1.md), events from
[Tracking D1](Grass-Route-Tracking-D1.md). The Google Drive folder (public link) is the source of truth for all four:
https://drive.google.com/drive/folders/1JeNsdOE9d0hx5ievZZX7W4w7Gji3fD8L

## Owner decisions (2026-10-06)
- Logic cell is 0.5 m: one cell is one Balance harvest unit.
- Field area outside plant regions is pre-mown lawn: walkable, not cuttable, not counted.
- Star 3 on a level without a side quota = clear at least 90% of cuttable plants (shown in Preview).
- D1 includes replay: tap a won garden tile on the main menu to replay it.
- Tutorial star 2 checks protected hits only; fruit-tree quotas count trees; slow hint uses 2(R + r).
- Out of D1: coin, shop, rewards, Heavy, Power Blade, Zen, test field.
- Missing 3D models use KayKit CC0 placeholders (Assets/KayKit/Packs); the owner replaces them later.
- Commit after each phase through the grass-commit skill, one topic per commit.

## Done
| Phase | Content |
|---|---|
| P0 | GDD v2.2 and the three D1 sheets mirrored in docs/ |
| P1 | v2.2 rules: GameRules asset, level types, untimed tutorial/relax levels, independent star flags, per-bed protected hits, no machine caps, save v2 without coins |
| P2 | Plant catalog, 0.5 m cells, tier 2-4 plant objects (never block), rocks/fences with sliding and blade occlusion, lock and slow-down icons, KayKit placeholders |
| P3 | Level pipeline: levels-d1.csv + ASCII layouts (Data/Levels/Layouts) + seeded baker (GrassSimulation.Editor, menu Grass Simulation/Levels) + validator (GDD §9 rules) |
| P4 | Standard and Wide machines, Loadout state and screen, unlocks on win (L4 Extra Time, L6 Turbo, L10 Wide), save v3 |
| P5 | Turbo and Extra Time, assisted runs (star 1 only), booster stock/gifts (save v4), booster HUD with left-side option |
| P7 | Layouts and baked assets for all 50 levels, all passing the validator |

## Remaining

### P6 — level types in Preview, Cleanup "dọn nốt", replay, timer A/B rule
#### Work items
1. Preview: badge for Khó (Hard) and Thư giãn (Relax); untimed levels show "Không giới hạn thời gian" and hide the
   timer value; star tiles explain per type (Relax/Tutorial star 2 = "Không chạm luống bảo vệ"; levels without a
   side quota show star 3 = "Dọn ≥ 90% cây"; with side quota show the side quota kind/amount); show the level's
   decision text (LevelDefinition.Decision) as a one-line hint; show unlock reward ("Thắng để mở: Máy Wide").
2. timer_enabled = false (remote config A/B): levels that are normally timed become untimed but star 2 then uses
   completion time: star 2 requires ElapsedTime ≤ 0.8 × the level's timer (i.e. the same 20% margin). Extra Time
   hidden. Add this to LevelRules/StarRules with tests.
3. Cleanup "Dọn nốt": pure CleanupSweep (G/Session) computing remaining cuttable units (field cells + object
   plants, excluding protected and plants above the cleanup tier) and connected clusters; button appears when
   remaining ≤ 1% of valid units AND every cluster ≤ 4 units. Pressing it clears all remaining eligible units with
   a short FX/sound, no quota/star effect. Cleanup exit publishes CleanupEndedMsg(levelId, duration, clearedPct)
   with clearedPct counting objects too. Rename the current "finish" button semantics: one button "Dọn nốt"
   (conditional) and the existing exit button.
4. Replay: main menu garden tiles for completed levels become tappable and open that level's Preview; tiles show
   earned star flags (3 small stars, filled per flag). Next-level button still opens the next unwon level.
   Progression attempt counter and is_replay flag (level already completed) exposed for analytics.
5. Result screen: on win show "Dọn tiếp" (Cleanup) and "Màn tiếp"; on loss show cause, remaining quota, "Chơi lại"
   and "Đổi máy" (P4). Verify it matches GDD §3; fix gaps.
6. HUD per GDD §11: Cleanup and untimed levels hide timer and boosters; quota chips tick when done (P1); timer small
   centre-top with warning at 15 s.

#### Tests
CleanupSweepTests (threshold, cluster size, excluded kinds), LevelRules/StarRules timer_enabled=false cases,
Preview/MainMenu format tests, replay route tests. All GrassSimulation assemblies green; Play Mode smoke: win L01,
replay it from the menu, enter Cleanup, reach the "Dọn nốt" condition (dev shortcut allowed).

### P8 — tutorial hints
- Generalize UI/Screens/Gameplay/OnboardingHintModel into TutorialHintModel: a list of hint steps, each with a
  trigger (level + condition), a text, an optional world/HUD anchor, and a "done" condition that skips it if the
  player already performed the action. Never lock input; never show more than one hint at a time; short text.
- Steps (GDD §9 order): L1 drag to steer (existing); L2 "Tìm hoa đỏ — chỉ hoa tính quota" until the first flower is
  cut; L4 first locked-plant touch → "Cần cấp 2 — cắt cỏ lấy XP" + pulse the XP bar; first upgrade popup → one-line
  explanation of Lưỡi rộng vs Động cơ khỏe; L4 first run with Extra Time in stock → point at the booster button;
  L6 "Có hai đường: lấy XP trước hay hoa trước" + Turbo pointer; L8 first protected touch → "Tránh luống hoa bảo vệ";
  L11 Loadout with Wide owned → point at Wide card; L15 first tier-3 locked touch → "Cần cấp 3"; L20/21 tier-4 →
  "Cây rất lớn cần cấp 4".
- Idle hint after GameRules.IdleHint (8 s) of no input in any Playing level (not only level 1); shows a context hint
  (nearest incomplete quota direction arrow or "Kéo để lái").
- Seen-hint flags persisted in the progression save (bump version + migration) so hints don't repeat.
- Publish IdleHintShownMsg (for analytics E13).
- Tests: model per trigger, skip-when-done, persistence.

### P9 — remote config + analytics (stub service layer, Firebase later)
- G/Rules: IRemoteConfigSource with the 8 keys of tab 08 (protected_mode, protected_fail_limit, timer_enabled,
  timer_multiplier, auto_slow_cut, slow_hint_threshold, booster_gift_turbo, booster_gift_extratime);
  DefaultRemoteConfig built from GameRules; a dev override ScriptableObject (in DB, not in builds by default);
  resolution into GameRulesValues at boot; ab_variant string.
- New module GrassSimulation.Analytics (asmdef referencing Gameplay, Progression, EncosyTower.PubSub):
  IAnalyticsSink, DevLogAnalyticsSink (logs via EncosyTower StaticDevLogger), AnalyticsTracker that subscribes to
  game messages and emits the events E02–E14 with EXACT names and parameter names/types from Tracking tab 01/02;
  user properties ab_variant, build_version, device_tier (from SystemInfo memory/cores heuristic),
  max_level_won. PerfSampler for perf_sample every 30 s while Playing.
- Supporting data that may be missing: attempt per level and is_replay (save), upgrades sequence "B,E"
  (session), quota_pct, result=quit via a LevelAbandonedMsg on Pause → Quit, duration_s excluding Pause and upgrade
  choice, tutorial_begin on first L1 start and tutorial_complete on first L3 win, cleanup_end, unlock, slow_hint /
  locked_plant_touch once per plant type per run (already throttled in P2).
- Tests: fake sink + per-test Messenger asserting event names and parameter sets for each event.

### Commit hint
P8: UI (hint model + view), Progression (seen flags), Sandbox wiring. P9: Gameplay (remote config), Analytics
(new module), Progression/Gameplay supporting fields, Sandbox wiring, assets.

## Working notes
- Drive the Editor only through the grass-unity-editor skill. Wait for recompile_status to report
  completed/up_to_date before run_tests: chaining them crashed the Editor once.
- Run tests per assembly: `run_tests --mode editor --filter GrassSimulation.<X>.Tests --filter_type assembly`
  (Gameplay, Progression, UI, Audio, Editor). Two EncosyTower tests (DotnetProcessRunnerTests,
  DotnetWorkspaceTests) fail on this setup because they shell out to dotnet; they are unrelated to the game.
- Never touch the player's real save (LocalLow/Laicasaane/Grass Route/Progress). For Play Mode tests set
  GrassSandbox._progressFolder to a throwaway folder at runtime and delete that folder afterwards.
- Dev shortcut: F7 in the sandbox grants +100 XP.
- To re-bake levels after editing a layout or the CSV: menu Grass Simulation/Levels/Bake All, then Validate All.
- Layout follow-ups from P7: the L35 maze reads as thin vertical walls in the preview (add cross-walls or
  fences to make it legible); the L33 flower patch is a lump rather than a ring.
