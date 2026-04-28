# UADRealismDIP Working Notes

This repository is for modding **Ultimate Admiral: Dreadnoughts**, a Unity/IL2CPP game. The immediate goal in this workspace is exploratory: understand how the existing mod changes game behavior so we can selectively alter annoying behaviors without breaking the parts the user likes.

## Current Handoff - gg150

Current source marker:

- `TAF-RC7 GG Patch gg150`
- `3.20.3-gg150`

`gg147` is a safety fix after `gg146` hung on Next Turn. The live log showed the service was scheduled and entered, but never reached `AI design service cycle ... begin`; because vanilla end-turn `GenerateRandomDesigns` was still being skipped, a scheduled-but-dead service could starve the normal turn flow. `gg147` switches the service's managed yields to `WaitForEndOfFrame`, and the end-turn skip now defaults off and only arms after the service completes at least one cycle.

`gg148` is the next AI design-generation performance experiment. It hosts vanilla `CampaignController.GenerateRandomDesigns(player, false)` on a real Unity/IL2CPP `MonoBehaviour` (`AiDesignCoroutineHost`) instead of trying to yield the IL2CPP enumerator through Melon coroutines.

`gg149` turns vanilla end-turn `GenerateRandomDesigns` off when `taf_campaign_ai_design_service_disable_endturn_generation=1` without waiting for a completed service cycle. The current test intentionally makes the Unity-hosted service the only design-generation owner, while `BuildNewShips` remains in the normal end-turn flow.

`gg150` fixes the service-owned Unity coroutine bridge. Reflection returns only an `Il2CppSystem.Collections.IEnumerator` wrapper at creation time, so the service now starts that raw enumerator, stores a pending job keyed by player/prewarm context, and binds the real `_GenerateRandomDesigns_d__202` pointer from the first Harmony `MoveNext` prefix before the vanilla skip guard runs.

- `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll`

Important current user rule: **do not kill, start, or restart the game unless explicitly asked.** Before copying a DLL, check whether the game is running. If it is running, report that the DLL was built but not copied and ask/let the user close the game. If the game is closed, copy the DLL and verify the marker.

Current active work is split between the campaign Ship Design tab AI-design viewer in `TweaksAndFixes/Harmony/CampaignFleetWindow.cs` and an experimental campaign AI design-generation service in `TweaksAndFixes/Harmony/CampaignController.cs`.

Implemented:

- AI country design viewer on the campaign Ship Design tab.
- Human player plus AI major countries are selectable.
- AI design list displays and clicking rows updates the focused design on the left.
- AI designs cannot be deleted/edited/built/refit via player action buttons.
- Design amount/count column is now `active/building/other`, counted from real `ship.design == design` links.
- Tooltip was added for the count column header explaining `active/building/other`.
- Default viewed design list is sorted by ship type order: `BB, BC, CA, CL, DD, TB, SS, TR, other`.
- Flag buttons have per-country tooltips with design count and ship counts by class.

Known current UI issue:

- `gg140+` keeps the AI-country flag bar as a separate centered strip near the top empty band and lowers the design list/header underneath it with `DesignViewerContentTopGap`.
- The flag sizes should be computed from available width and empire count, with reasonable min/max. Current dynamic sizing code is in `UpdateDesignViewerFlagSizes`.

Active AI-build/design instrumentation:

- Live `Mods\params.csv` should contain `taf_debug_ai_shipbuilding,1`.
- Live `Mods\params.csv` should contain `taf_campaign_ai_design_service_enabled,1`, `taf_campaign_ai_design_service_disable_endturn_generation,1`, `taf_debug_ai_design_service,1`, and `taf_campaign_ai_design_service_job_timeout_seconds,90` for the current Unity-hosted service performance test.
- Live params were backed up before editing to `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\params.csv.bak-20260427-144936`.
- `BuildNewShips` now logs per-AI before/after counts plus no-build context: building tonnage, approximate free capacity, design tonnage range, and inferred reason category.
- `Ship.CreateRandom` coroutine tracing now logs `AI CreateRandom begin` and `AI CreateRandom end` around AI random design generation so we can correlate shipgen success with whether `player.designs` or building counts changed.
- `gg141+` adds an always-on main-thread Melon coroutine that loops over active AI major powers and runs vanilla private `CampaignController.GenerateRandomDesigns(player, false)` through a nested Il2Cpp coroutine runner.
- `gg145` scheduled the service from `Patch_Ui.Postfix_Update` only once the campaign is in `World` state, then yielded vanilla `GenerateRandomDesigns` directly. That proved shipgen could start, but produced MelonLoader `Unsupported type Il2CppSystem.Collections.IEnumerator` trampoline errors.
- `gg146` kept the world-state scheduling and used a managed stack walker for Il2Cpp nested coroutines instead of raw-yielding them, but the service entered and never reached the first cycle in the live log.
- `gg147` uses `WaitForEndOfFrame` for service yields and prevents `taf_campaign_ai_design_service_disable_endturn_generation` from skipping vanilla generation until a service cycle has completed.
- `gg148` invokes `GenerateRandomDesigns` via `AiDesignCoroutineHost.StartCoroutine(...)`, tracks the IL2CPP state-machine pointer, and completes the service wait from the `GenerateRandomDesigns.MoveNext` postfix when Unity reports the routine is done. Watch for `AI design service Unity coroutine started`, `AI GenerateRandomDesigns persisted...`, `AI design service Unity coroutine completed...`, and `AI design service verified persisted design(s)...`.
- `gg149` makes `taf_campaign_ai_design_service_disable_endturn_generation=1` skip vanilla state-0 end-turn `GenerateRandomDesigns` immediately rather than after a proven completed service cycle. `BuildNewShips` remains in the normal end-turn flow.
- `gg150` should produce `AI design service Unity coroutine requested`, then `AI design service Unity coroutine bound`, then `AI GenerateRandomDesigns begin... service=True`, and finally completion/persistence logs. If `requested` appears without `bound`, Unity did not enter the typed coroutine state machine. If `bound` appears and then a timeout appears, the typed coroutine started but did not finish.
- Once started, this yolo service does not pause itself for battles, loading, or human constructor state. The test assumption is that the user will avoid battles/ship designer while observing it.

Recent files touched for this feature:

- `TweaksAndFixes/Harmony/CampaignFleetWindow.cs`
- `TweaksAndFixes/Harmony/CampaignController.cs`
- `TweaksAndFixes/Default_Files/TAF_Files/params_override.csv`
- `TweaksAndFixes/TweaksAndFixes.cs`
- `ship-turn-logic.md`

Be careful: the current implementation contains historical placement helpers (`ReserveDesignViewerToolbarSpace`, `RestoreDesignViewerToolbarSpace`, cached original offsets) from several placement attempts. They may be simplified once the final flag-bar parent/anchor is chosen.

## Repository Shape

- `UADRealism.sln` is a Visual Studio 2022 solution with two C# projects.
- `TweaksAndFixes/` is the active, maintained MelonLoader mod. The README describes this as "Tweaks And Fixes" / TAF, focused around the Dreadnought Improvement Project.
- `UADRealism/` is older realism-specific code. The README says there are no current plans for UAD Realism, and the current work is extending TAF instead. Treat this folder as legacy/reference-only for current behavior unless the user explicitly asks about it or a live `UADRealism.dll` is verified in the game's `Mods` folder/log.
- `Data/` contains large CSV/XLSX data files such as hulls, ports, provinces, guns, and part overrides.
- `TweaksAndFixes/Assets/TAFData/` contains files copied into the game's `Mods/TAFData` folder at build/install time.
- `MelonLoader` is not the gameplay mod. It is the loader injected into the game. It loads compiled mod DLLs from the game's `Mods` folder.

## Runtime Model

The game is loaded through MelonLoader. `TweaksAndFixes.dll` is a Melon mod:

- Entry point: `TweaksAndFixes/TweaksAndFixes.cs`
- Assembly metadata uses:
  - `MelonGame("Game Labs", "Ultimate Admiral Dreadnoughts")`
  - `MelonInfo(..., "TweaksAndFixes-RC7", "3.20.3", ...)`
  - `HarmonyDontPatchAll`
- `OnInitializeMelon()` calls `HarmonyInstance.PatchAll(MelonAssembly.Assembly)`.
- Most behavior changes are Harmony prefixes/postfixes in `TweaksAndFixes/Harmony/`.
- Many patches call larger replacement/helper methods in `TweaksAndFixes/Modified/`.
- Do not trace current/live TAF bugs through `UADRealism/ModifiedClasses/GenerateShip.cs` or other `UADRealism/` replacements unless there is concrete evidence that `UADRealism.dll` is loaded. The active DLL-only workflow builds and installs `TweaksAndFixes.dll`.
- For generated gun armor issues, start from the active TAF/vanilla path: `Ship.AddRandomPartsNew -> Ship.AddShipTurretArmor -> Ship.TurretArmor(partData, ship)`. The per-gun armor fields are copied from `ship.armor[TurretTop/TurretSide/Barbette]` when a gun entry is created; ordinary `Ship.SetArmor` calls do not resync `ship.shipTurretArmor`. See `ship-gen-design.md`.

This means most changes should be approached as runtime patches against game classes from `Assembly-CSharp.dll`, not as normal ownership of the game's source code.

## Build And Install Assumptions

`TweaksAndFixes/TweaksAndFixes.csproj` targets `net6.0` and references game/MelonLoader assemblies via `$(UAD_PATH)`, for example:

- `$(UAD_PATH)MelonLoader/net6/MelonLoader.dll`
- `$(UAD_PATH)MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`
- Unity and Il2CppInterop DLLs under the same game folder.

The build target copies `TweaksAndFixes.dll` into `$(UAD_PATH)Mods/` and copies `TweaksAndFixes/Assets/**/*` into `$(UAD_PATH)Mods/`.

For local builds, expect to need `UAD_PATH` set to the Ultimate Admiral: Dreadnoughts install directory, probably ending with a slash/backslash. Without the actual game install and MelonLoader-generated IL2CPP assemblies, this repo will not compile cleanly.

### Current Build Workflow

The clean local workflow is to build against a staging copy of the game's MelonLoader/IL2CPP assemblies, not directly against the live game folder. This prevents the project build target from copying TAF assets into the live `Mods` folder and accidentally overwriting DIP files.

Known local paths:

- Repo: `E:\Codex\UADRealismDIP`
- Game install: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts`
- Live mod DLL: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll`
- Build staging root: `E:\Codex\UADBuildStage\`
- .NET SDK: `E:\Codex\dotnet\dotnet.exe`
- Git: `E:\Codex\Git\cmd\git.exe`
- Game log: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`

Build command:

```powershell
$env:UAD_PATH='E:\Codex\UADBuildStage\'
$env:DOTNET_ROOT='E:\Codex\dotnet'
$env:DOTNET_CLI_HOME='E:\Codex\.dotnet-home'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:NUGET_PACKAGES='E:\Codex\.nuget\packages'
$env:NUGET_HTTP_CACHE_PATH='E:\Codex\.nuget\http-cache'
$env:NUGET_PLUGINS_CACHE_PATH='E:\Codex\.nuget\plugins-cache'
$env:Path='E:\Codex\dotnet;' + $env:Path
& 'E:\Codex\dotnet\dotnet.exe' build 'E:\Codex\UADRealismDIP\TweaksAndFixes\TweaksAndFixes.csproj' -c Release
```

Builds currently produce many existing warnings, but should have `0 Error(s)`.

### DLL-Only Install Rule

Only copy `TweaksAndFixes.dll` into the live game folder unless the user explicitly asks to install assets/data files too. DIP owns many files under `Mods`, and copying the full TAF output may interfere with DIP.

Before updating the DLL, check whether the game is running. Do **not** kill the process unless the user explicitly asks. If the game is running, do not copy; tell the user the game is running and wait for them to close it. Do not restart the game afterward; the user prefers to start it manually.

```powershell
Get-Process | Where-Object {
  $_.ProcessName -like '*Ultimate Admiral Dreadnoughts*' -or
  $_.ProcessName -like '*Ultimate*Dreadnoughts*' -or
  $_.MainWindowTitle -like '*Ultimate Admiral Dreadnoughts*'
} | Select-Object Id,ProcessName,MainWindowTitle

Copy-Item -LiteralPath 'E:\Codex\UADRealismDIP\TweaksAndFixes\bin\Release\net6.0\TweaksAndFixes.dll' -Destination 'E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll' -Force
$path='E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll'
$bytes=[System.IO.File]::ReadAllBytes($path)
$ascii=[System.Text.Encoding]::ASCII.GetString($bytes)
@('TAF-RC7 GG Patch gg150','3.20.3-gg150','gg150') | ForEach-Object {
  if ($ascii.Contains($_)) { "FOUND $_" } else { "MISSING $_" }
}
```

Do not restore generated DLL artifacts or live params backups unless the user explicitly asks. The current workflow favors leaving built/deployed artifacts in place so they can be inspected and compared during active testing.

### Live Params Rule

For this installed DIP/TAF setup, the active game-side parameter file is:

- `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\params.csv`

Do **not** assume the live file is named `params_override.csv`. The repo source default file `TweaksAndFixes/Default_Files/TAF_Files/params_override.csv` is useful for adding new defaults, but when the user asks to enable or change a runtime flag in the game folder, edit the live `Mods\params.csv` file.

Before editing live params, make a timestamped backup, then change only the requested row. If a newly added `taf_*` key is missing from live `params.csv`, insert it near the related TAF rows. Example from `gg138`:

```csv
taf_debug_ai_shipbuilding,1,"When enabled, print per-nation turn-by-turn AI BuildNewShips before/after summaries, including designs, ships under construction, and new orders.",,,,,,,
```

The DLL copy rule is separate from params edits: still check the process before copying a DLL, but live params can be edited directly when the user asks.

### Verifying A New DLL

After the user starts the game, check the log. A healthy startup should show the expected mod name/version, then load settings/config, and reach `MainMenu` without a Harmony patching exception.

Useful checks:

```powershell
Select-String -LiteralPath 'E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log' -Pattern 'TAF-RC7 GG Patch|Exception patching|Harmony|Version Mismatch|Ambiguous|Undefined target|Could not find method' -Context 2,5
Select-String -LiteralPath 'E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log' -Pattern 'Loaded database and config|OnEnterState|MainMenu|Begin shipgen|prevented generated turret|reset generated diameter|reset generated length' -Context 1,3
```

TAF's UI message titled `Version Mismatch` is generic. In this repo it is shown when `HarmonyInstance.PatchAll(...)` throws during startup, not only when the game/TAF version is truly wrong. Always inspect `Latest.log` for the first `Exception patching with harmony` block.

When patching overloaded game methods, do not rely on `[HarmonyPatch(nameof(MethodName))]` unless there is only one overload. It can fail with `Ambiguous match`. For overloaded methods, use a separate `[HarmonyPatch]` class with `TargetMethod()` or `TargetMethods()` and select the exact overload(s) via `AccessTools.GetDeclaredMethods(...)`.

## Important Files

- `TweaksAndFixes/TweaksAndFixes.cs`: MelonLoader lifecycle, Harmony patching, Unity log forwarding, install/version error messages.
- `TweaksAndFixes/Data/Config.cs`: central config and file path definitions. Reads feature flags from `G.GameData.parms` using keys like `taf_*`.
- `TweaksAndFixes/Harmony/GameData.cs`: important load-order patch. Loads config/data, patches player materials, fills the internal database, applies UI modifications, starts cheat menu support, and optionally starts hot reload.
- `TweaksAndFixes/Utils/Database.cs`: builds lookup tables for techs, parts, hulls, guns, torpedoes, and component availability.
- `TweaksAndFixes/Utils/Serializer.cs`: CSV/data serialization helpers.
- `TweaksAndFixes/Modified/`: larger substitute implementations and helpers used by Harmony patches.
- `UADRealism/UADRealismMod.cs`: old/secondary mod entry. It mainly sets `TweaksAndFixes.Config.MaxGunGrade`; ignore it for current TAF behavior unless `UADRealism.dll` is explicitly in play.

## Where Behavior Changes Likely Live

Use the game concept as the search term, then check both `Harmony/` and `Modified/`.

- Battle speed / simulation restrictions: `TweaksAndFixes/Harmony/BattleManager.cs`
- Campaign turn flow, retirement/end date, scrapping, shared designs, AI design deletion: `TweaksAndFixes/Harmony/CampaignController.cs` and `TweaksAndFixes/Modified/CampaignControllerM.cs`
- Ship generation, generated armor, random designs, component selection: `TweaksAndFixes/Harmony/Ship.cs` and `TweaksAndFixes/Modified/ShipM.cs`
- Dockyard/constructor UI and part mounting behavior: `TweaksAndFixes/Harmony/Ui.cs`, `TweaksAndFixes/Modified/UiM.cs`, and `TweaksAndFixes/Modified/ConstructorM.cs`
- Guns, reloads, weights, range, armor, instability: current behavior should be traced in `TweaksAndFixes/` first. `UADRealism/Harmony/GunData.cs`, `UADRealism/Harmony/Part.cs`, and `UADRealism/Harmony/Ship.cs` are legacy/reference-only unless `UADRealism.dll` is verified loaded.
- Map, ports, provinces, naval invasions: `TweaksAndFixes/Harmony/CampaignMap.cs`, `CampaignNavalInvasionPopupUi.cs`, `ProvinceBattleManager.cs`, and related CSV files in `Data/`.
- Politics, alliances, tension, peace checks: `CampaignPoliticsWindow.cs`, `PoliticsRelationshipElement.cs`, `CampaignController.cs`, `Ui.cs`.
- Localization text: root `English.lng` and `TweaksAndFixes/Assets/TAFData/locText.lng`.

## Config And Data

TAF behavior is partly controlled by parameters loaded into `G.GameData.parms` and `G.GameData.paramsRaw`. `Config.Param(...)` and `[ConfigParse]` fields in `Config.cs` are the main access path.

Examples already present:

- `taf_disable_battle_simulation_speed_restrictions`
- `taf_disable_fleet_tension`
- `taf_ai_disable_tech_priorities`
- `taf_campaign_end_retirement_date`
- `taf_shipgen_tweaks`
- `taf_peace_check`
- `taf_naval_invasion_tweaks`
- `taf_dockyard_new_logic`
- `taf_dockyard_remove_mount_restrictions`

Before hardcoding behavior, first check whether a `taf_*` parameter or CSV file already controls it. Prefer adding a parameterized toggle if the user may want to keep switching between vanilla, TAF, and custom behavior.

## Shipgen Experiments

Current GG ship generation work lives mostly in `TweaksAndFixes/Harmony/Ship.cs` and `TweaksAndFixes/Modified/ShipM.cs`. Keep new behavior gated behind `taf_shipgen_hull_profiles` where possible, plus an explicit enable switch for experimental replacement generators.

The profile parser accepts entries like:

```csv
maine_hull_a:max_displacement=1|main_gun_max=9|tower_tier_max=1
```

As of `gg74`, all ship types are hardcoded through the normal generator to use the maximum legal displacement during ship generation. "Legal" means clamped to hull max, `Player.TonnageLimit(shipType)`, and campaign shipyard capacity when present; do not use `Player.IsTonnageAllowedByTech` for selecting the forced max, because it can cap early TB hulls below the generator/UI limit. Shipgen geometry is also hardcoded before max tonnage is calculated: BBs use maximum beam and 0 draught; TBs/DDs use minimum beam and minimum draught; every other ship type uses 0 beam and 0 draught, clamped to the hull's legal beam/draught range. Disassembly showed `Ship.Tonnage()` returns `BeamDraughtBonus * rawTonnage`, so `SetShipgenTonnage` must store `displayTarget / BeamDraughtBonus`; writing the display target directly makes modified-geometry hulls show too small, e.g. 275t requested becomes 239t. If `Ship.SetTonnage` still clamps below that legal target during shipgen, `SetShipgenTonnage` assigns the backing `ship.tonnage` field and refreshes hull stats. The old relaxed shipgen weight acceptance is disabled again; generated ships should pass the game's real weight validation. Shell-size reduction is allowed for all ship types as soon as overweight reduction runs. `OptimizeComponents` also forces AI shipgen toward DIP-friendly armament components: max AP shell distribution for main and secondary guns, best available penetrating AP/HE shell type, and max available torpedo diameter. Shipgen hard-bans main-gun randparts `49/`, `52/`, and `368/`; these early-BB centerline randparts repeatedly accepted candidates but never reached placement, so they are filtered before candidate selection and omitted from the applicable-main-gun diagnostic list. Since campaign generation usually gives only four attempts, default downsize behavior is aggressive: start after the first failed attempt, reduce main-gun cap by 2 inches per step, and reduce tower-family tier caps by 2 tiers per step while preserving floors from seen/accepted candidates. Successful shipgen summaries print grouped final main/other gun part names so future hull-specific prioritization can be based on observed working parts. Avoid adding string-list rows for this to live `params.csv`; the game can fail while replacing the built-in params asset.

`jap_tb_hull:generator=gg_tb_minimal` is a shelved experimental special-hull path. It replaces the random part-placement phase for `tb` ships using the `jap_tb_hull` model, while leaving the game's normal hull setup, components, weight/stat passes, coroutine flow, and final validation in place. As of `gg61`, do not route into it by default: `taf_shipgen_special_tb_generator_enabled` defaults to `0`, and the default hull profile no longer includes `jap_tb_hull:generator=gg_tb_minimal`.

To deliberately re-enable it for debugging, set both:

```csv
taf_shipgen_special_tb_generator_enabled,1
taf_shipgen_hull_profiles,maine_hull_a:max_displacement=1|main_gun_max=9|tower_tier_max=1; jap_tb_hull:generator=gg_tb_minimal
```

As of `gg58`, `gg_tb_minimal` uses a bounded backtracking search instead of a purely greedy placement pass:

- Apply narrow defaults first: minimum beam, minimum draught, and cramped quarters.
- Clear generated parts and refresh mounts.
- Enumerate candidate main-tower placements, shuffle them, then try one.
- From that tower state, enumerate candidate standard-funnel placements, shuffle them, then try one.
- From that tower+funnel state, try the constrained main-gun placement helper.
- If required main guns are present, optionally add a torpedo launcher and return the ship to normal validation.
- If a branch fails, remove the parts from that branch, refresh mounts, and continue until `taf_shipgen_special_tb_search_limit` is reached.

When editing this area:

- Prefer adding new special generators through the hull profile parser instead of global behavior.
- Keep special generators narrow to a hull model or hull data name, and usually also check `ship.shipType.name`.
- Avoid installing live `params_override.csv`; source defaults go in `TweaksAndFixes/Default_Files/TAF_Files/params_override.csv`, but live tests should keep using the installed `params.csv` unless the user explicitly asks otherwise.
- If a special generator fails, log enough to know whether candidate filtering, mount fitting, `CreateFromStore`, `CanPlace`, or final validation rejected the design.

## Editing Guidance

- Preserve MelonLoader/Harmony patterns already used in the repo.
- Keep changes narrowly scoped to the behavior being investigated.
- Add or reuse config switches for gameplay behavior that may be preference-based.
- Be careful with Harmony prefixes that return `false`; they skip the original game method.
- Be careful with IL2CPP generated names such as `_GenerateRandomShip_d__573` or compiler display classes. These may change between game versions.
- Avoid touching large CSV/XLSX data files unless the requested behavior is clearly data-driven.
- Do not assume `UADRealism/` is the right place for new work. For current TAF behavior, start in `TweaksAndFixes/`; only use `UADRealism/` as historical reference unless deployment/log evidence says that assembly is active.

## Current Local Setup Notes

- The repo is cloned at `E:\Codex\UADRealismDIP`.
- MelonLoader source is cloned alongside it at `E:\Codex\MelonLoader` for reference.
- Git is available in this workspace at `E:\Codex\Git\cmd\git.exe`.
- This filesystem may trigger Git's "dubious ownership" warning. Use a temporary override for read-only checks, for example:

```powershell
& 'E:\Codex\Git\cmd\git.exe' -c safe.directory=E:/Codex/UADRealismDIP -C 'E:\Codex\UADRealismDIP' status --short --branch
```

## Investigation Workflow

1. Identify the exact in-game behavior and whether it occurs in battle, campaign, dockyard, ship generation, politics, or UI.
2. Search for related game method names, config keys, and visible text with `rg`.
3. Check matching Harmony patch files first, then their corresponding `Modified/*M.cs` helpers.
4. Determine whether the behavior is already behind a `taf_*` parameter.
5. If changing code, prefer a small patch guarded by a config value.
6. Build only after `UAD_PATH` is known and points to a MelonLoader-prepared game install.
7. Test in game with a disposable save when touching campaign, ship generation, or save serialization behavior.
