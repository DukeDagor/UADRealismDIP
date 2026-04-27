# UADRealismDIP Working Notes

This repository is for modding **Ultimate Admiral: Dreadnoughts**, a Unity/IL2CPP game. The immediate goal in this workspace is exploratory: understand how the existing mod changes game behavior so we can selectively alter annoying behaviors without breaking the parts the user likes.

## Repository Shape

- `UADRealism.sln` is a Visual Studio 2022 solution with two C# projects.
- `TweaksAndFixes/` is the active, maintained MelonLoader mod. The README describes this as "Tweaks And Fixes" / TAF, focused around the Dreadnought Improvement Project.
- `UADRealism/` is older realism-specific code. The README says there are no current plans for UAD Realism, and the current work appears to be extending TAF instead.
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

Before updating the DLL, stop the running game process if it is open. Do not restart the game afterward; the user prefers to start it manually.

```powershell
$gameProc = Get-Process | Where-Object { $_.ProcessName -like 'Ultimate Admiral Dreadnoughts*' -or $_.ProcessName -like 'Ultimate*Dreadnoughts*' }
if ($gameProc) { $gameProc | Stop-Process -Force; Start-Sleep -Seconds 2 }
Copy-Item -LiteralPath 'E:\Codex\UADRealismDIP\TweaksAndFixes\bin\Release\net6.0\TweaksAndFixes.dll' -Destination 'E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll' -Force
Get-Item -LiteralPath 'E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\Mods\TweaksAndFixes.dll' | Select-Object FullName,Length,LastWriteTime
```

After installing, clean generated DLL artifacts that the repo may track:

```powershell
& 'E:\Codex\Git\cmd\git.exe' -c safe.directory=E:/Codex/UADRealismDIP -C 'E:\Codex\UADRealismDIP' checkout HEAD -- TweaksAndFixes/TAF_Nightly/TweaksAndFixes.dll TweaksAndFixes/bin/Release/net6.0/TweaksAndFixes.dll
```

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
- `UADRealism/UADRealismMod.cs`: old/secondary mod entry. It mainly sets `TweaksAndFixes.Config.MaxGunGrade`.

## Where Behavior Changes Likely Live

Use the game concept as the search term, then check both `Harmony/` and `Modified/`.

- Battle speed / simulation restrictions: `TweaksAndFixes/Harmony/BattleManager.cs`
- Campaign turn flow, retirement/end date, scrapping, shared designs, AI design deletion: `TweaksAndFixes/Harmony/CampaignController.cs` and `TweaksAndFixes/Modified/CampaignControllerM.cs`
- Ship generation, generated armor, random designs, component selection: `TweaksAndFixes/Harmony/Ship.cs` and `TweaksAndFixes/Modified/ShipM.cs`
- Dockyard/constructor UI and part mounting behavior: `TweaksAndFixes/Harmony/Ui.cs`, `TweaksAndFixes/Modified/UiM.cs`, and `TweaksAndFixes/Modified/ConstructorM.cs`
- Guns, reloads, weights, range, armor, instability: older realism code in `UADRealism/Harmony/GunData.cs`, `UADRealism/Harmony/Part.cs`, `UADRealism/Harmony/Ship.cs`, plus newer TAF patches if present.
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
- Do not assume `UADRealism/` is the right place for new work. For current TAF behavior, start in `TweaksAndFixes/`.

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
