# Campaign AI Ship Turn Logic

This note maps the campaign turn path for AI ship design generation and new ship construction.

Primary evidence:

- Cpp2IL diffable dump: `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\CampaignController.cs`
- Cpp2IL diffable dump: `E:\Codex\cpp2il_uad_diffable\DiffableCs\Assembly-CSharp\Ship.cs`
- Cpp2IL ISIL dump: `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType__AiManageFleet_d__201.txt`
- Cpp2IL ISIL dump: `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController_NestedType__GenerateRandomDesigns_d__202.txt`
- Cpp2IL ISIL dump: `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\CampaignController.txt`
- Current mod patches: `TweaksAndFixes/Harmony/CampaignController.cs`
- Current mod replacement/helper code: `TweaksAndFixes/Modified/CampaignControllerM.cs`
- Current runtime evidence: gg140 `Latest.log` AI shipbuilding and shipgen traces

Important limitation: the `DiffableCs` C# dump has method signatures, coroutine state-machine fields, and compiler-generated closure/lambda names, but the readable C# method bodies are stripped to `{ }`. There is also a lower-level `IsilDump` with native/ISIL disassembly. It is harder to read than C#, but it does expose calls, branches, and field offsets for the exact methods we care about. The step-by-step flow below should prefer ISIL when available.

## High-Level Flow

Per AI player, the campaign fleet management coroutine is:

1. `CampaignController.AiManageFleet(Player player, bool prewarming = false)`
2. One phase generates or updates campaign designs.
3. One phase builds ships from the designs currently in `player.designs`.
4. Other phases handle submarines, refits, scrapping, movement, budgets, and shipyard expansion.

Direct evidence:

- `AiManageFleet` is an iterator/coroutine at cpp2il `CampaignController.cs:3051`.
- Its generated state machine is `<AiManageFleet>d__201` at cpp2il `CampaignController.cs:1950`.
- The state machine stores `player`, `CampaignController <>4__this`, and `prewarming`.
- The mod stores the currently executing AI manage-fleet coroutine in `Patch_CampaignController._AiManageFleet`.
- ISIL shows the non-prewarm/normal path:
  - `ScrapOldAiShips(player)`
  - `SetAiShipRoles(player)`
  - `DeleteOldDesigns(player)`
  - either `GenerateRandomDesigns(player, prewarming)` and yield, or `GetPredefinedDesign(player, prewarming)` depending on campaign design mode/prewarm checks
  - after the yield, `Player.CashAndIncome(...)`
  - `BuildNewShips(player, tempPlayerCash)`
  - `BuildNewSubmarines(player, tempPlayerCash)`

Why this matters: design generation and building are not one atomic operation. We should treat them as separate phases inside the AI turn.

## Phase 1: Design Generation

The design-generation phase is:

```csharp
private IEnumerator GenerateRandomDesigns(Player player, bool prewarming)
```

Direct evidence:

- Method signature at cpp2il `CampaignController.cs:3183`.
- Generated state machine `<GenerateRandomDesigns>d__202` at cpp2il `CampaignController.cs:2034`.
- State-machine fields:
  - `player`
  - `CampaignController <>4__this`
  - `prewarming`
  - `<i>5__2`
  - `<shipType>5__3`
  - `<tries>5__4`
  - `<ship>5__5`
  - `<j>5__6`
- Compiler-generated helper/closure class `<>c__DisplayClass202_0` holds:
  - `player`
  - `CampaignController <>4__this`
  - `Func<ShipType, bool>` lambdas
  - `Func<Ship, bool>` lambda
- There are top-level compiler-generated methods named:
  - `<GenerateRandomDesigns>b__202_1(Ship s) => ShipType`
  - `<GenerateRandomDesigns>b__202_4(Ship s) => ShipType`

Inferred behavior:

1. The coroutine loops over ship types or desired design slots.
2. For a selected `ShipType`, it tracks retry count in `<tries>5__4`.
3. It creates or receives a candidate `Ship` in `<ship>5__5`.
4. It likely uses existing `player.designs` and/or generated candidates to decide whether a type is missing or already covered.
5. If a generated candidate passes whatever acceptance checks exist, it is added to campaign design state, most likely `player.designs`.
6. If it fails those checks, shipgen can still log `Shipgen result: Success` while the candidate never becomes a visible design.

Runtime evidence from gg140 supports this split:

- France logged successful TB shipgen, then `BuildNewShips` still showed `designs=4 -> 4` and `building=16 -> 16`.
- Austria logged successful CA shipgen, then later built 5 CAs.
- Some nations started the turn with zero designs, but later turns/UI show designs, so generation is occurring somewhere outside the `BuildNewShips` snapshot.

Current mod behavior:

- `Patch_GenerateRandomDesigns` only skips this coroutine at state `0` when prewarming and `taf_campaign_skip_prewarm_shipbuilding` behavior says to skip prestart random design work.
- It does not currently log normal non-prewarm design acceptance/rejection.

Best next instrumentation target:

- Patch `CampaignController._GenerateRandomDesigns_d__202.MoveNext`.
- Log at coroutine start and end:
  - player
  - date
  - prewarming
  - design count/class breakdown before and after
- Log state transitions when useful:
  - `__1__state`
  - `<shipType>5__3`
  - `<tries>5__4`
  - `<ship>5__5`
  - whether `<ship>5__5` is present in `player.designs`
  - generated ship name/type/tonnage/status/isDesign

This should reveal the missing handoff: successful shipgen but candidate not accepted, or accepted later than the current `BuildNewShips` trace sees.

ISIL notes for `GenerateRandomDesigns`:

- It calls `CampaignController.GetSharedDesign` before generating a new hull.
- If a shared design is available, it calls `TryTakeSharedDesign`.
- If no shared/predefined design is selected, it chooses a `ShipType`, gets a hull with `Ship.GetHull`, creates a candidate with `Ship.Create`, changes hull with `Ship.ChangeHull`, enters constructor with `Ship.EnterConstructor`, and then yields `Ship.GenerateRandomShip`.
- When control resumes after shipgen, it calls `Ship.IsValid`.
- If valid, it calls `Ui.ReportNewDesign`.
- If not valid or not accepted, it calls `Ship.LeaveConstructor` and `VesselEntity.TryToEraseVessel`.
- This confirms that a successful `Shipgen result` log can still be followed by candidate erasure or non-acceptance.

## Shipgen Subphase

There are two related ship generation APIs:

```csharp
public static IEnumerator Ship.CreateRandom(
    ShipType shipType,
    Player player,
    Nullable<bool> error = null,
    bool ignoreHullAvailability = false,
    bool isTempForBattle = false,
    Action<Ship> onDone = null,
    bool checkMainGunsCount = false,
    bool canUseShared = false,
    bool useSmallAmountTries = true)
```

```csharp
public IEnumerator Ship.GenerateRandomShip(
    Action<bool, int, float> onDone,
    bool needWait,
    Nullable<bool> error = null,
    ...,
    bool fromUi = false,
    bool isSimpleRefit = false,
    bool checkMainGunsCount = true,
    bool useSmallAmountTries = false,
    StringBuilder info = null)
```

Direct evidence:

- `Ship.CreateRandom` signature at cpp2il `Ship.cs:5748`.
- `Ship.CreateRandom` state machine `<CreateRandom>d__571` at cpp2il `Ship.cs:2824`.
- `CreateRandom` state-machine fields include:
  - `player`
  - `isTempForBattle`
  - `shipType`
  - `ignoreHullAvailability`
  - `useSmallAmountTries`
  - `canUseShared`
  - `Action<Ship> onDone`
  - `<ship>5__2`
  - `<usedHulls>5__3`
  - `<sharedDesignSelected>5__4`
  - `<i>5__5`
- `Ship.GenerateRandomShip` signature at cpp2il `Ship.cs:5892`.
- `GenerateRandomShip` state machine `<GenerateRandomShip>d__573` at cpp2il `Ship.cs:2931`.

Current mod wrappers:

- `ShipM.Ship_CreateRandom` calls vanilla `Ship.CreateRandom`.
- `ShipM.Ship_GenerateRandomShip` calls vanilla `ship.GenerateRandomShip`.
- gg140 attempted tracing on `Ship._CreateRandom_d__571.MoveNext`.

Runtime finding:

- gg140 produced no `AI CreateRandom begin/end` lines.
- Shipgen traces did appear.

Interpretation:

- The active turn shipgen path may be using `GenerateRandomShip` directly, not `CreateRandom`.
- Or `CreateRandom` is used in a way our state-0 hook did not observe.
- The safer target is `GenerateRandomDesigns`, because it is the campaign design-owner coroutine and has the candidate ship field.

## Design Acceptance: `Ship.IsValid`

`GenerateRandomDesigns` does not accept a generated candidate just because `GenerateRandomShip` reports success. After `GenerateRandomShip` yields back, the coroutine calls:

```csharp
ship.IsValid(autodesign: true)
```

Direct evidence:

- `Ship.IsValid(bool autodesign = false)` is at `E:\Codex\cpp2il_uad_isil\IsilDump\Assembly-CSharp\Ship.txt:238469`.
- `GenerateRandomDesigns` calls `Ship.IsValid` after `Ship.GenerateRandomShip` resumes.
- The relevant helper lambdas are:
  - `Ship.<IsValid>b__1018_0(Part x)` at `Ship.txt:300946`
  - `Ship.<IsValid>b__1018_1(Part x)` at `Ship.txt:301067`
  - `Ship+<>c.<IsValid>b__1018_2(Part x)` at `Ship_NestedType___c.txt:13706`
- `PartData.minMainTurrets` is field offset `0xB0`; `PartData.minMainBarrels` is field offset `0xB4`.

The `IsValid` decision tree is:

1. If `autodesign` is true, count the ship's non-casemate main-caliber gun parts.
2. Also sum the barrel count for those parts.
3. Compare those counts against hull requirements:
   - `hull.data.minMainTurrets`; `-1` means no minimum.
   - `hull.data.minMainBarrels`; `-1` means no minimum.
4. If the autodesign gun-count checks fail, return `false`.
5. Call `IsValidCostReqParts(out reason, out notPassed, out badParts)`.
6. If that returns false, return `false`.
7. Call `IsValidCostWeightBarbette(out reason, out errorBarbettePart)`.
8. Return that result.

The two gun-count lambdas appear identical in the ISIL:

- require `Part.data` to exist
- require the part to be a gun-type part
- require `Ship.IsMainCal(part.data)`
- reject parts whose data tags include `casemate`

The `Ship+<>c.<IsValid>b__1018_2(Part x)` lambda returns `Part.data.numBarrels`, so the second pass is summing barrels across the filtered main-gun parts.

Important implication:

- The mod's shipgen logs already check the same minimum turret/barrel gates in `TweaksAndFixes/Harmony/Ship.cs` via `BuildShipgenIssueFlags`.
- A candidate can still fail after apparently successful part placement if it lacks enough non-casemate main turrets/barrels for the selected hull.
- This matters for campaign design acceptance because `GenerateRandomDesigns` erases candidates that fail the post-shipgen validity gate.

### `IsValidCostReqParts`

Direct evidence:

- `Ship.IsValidCostReqParts(out string reason, out List<ShipType.ReqInfo> notPassed, out Dictionary<Part, string> badParts)` is at `Ship.txt:239494`.
- The method initializes:
  - `reason = ""`
  - `notPassed = new List<ShipType.ReqInfo>()`
  - `badParts = new Dictionary<Part, string>()`
- It calls `Ship.CStats()` before checking ship-type requirements.
- `ShipType.ReqInfo.Pass(float v)` is at `ShipType_NestedType_ReqInfo.txt:3`.

The core requirement logic is:

1. Compute current ship stats with `Ship.CStats()`.
2. Read `ship.shipType.requirementsx`.
3. For each `ReqInfo`, read the current stat total from `ship.stats_`.
4. Call `ReqInfo.Pass(total)`.
5. Put failing requirements into `notPassed`.
6. If any requirements fail, return `false`.

`ReqInfo.Pass(v)` means:

- If `min != -1`, `v` must be greater than or equal to `min`.
- If `max != -1`, `v` must be less than or equal to `max`.
- If both bounds are absent, it passes.

The part-placement logic inside `IsValidCostReqParts` is:

1. For ship states `1` or `2`, iterate placed parts.
2. Filter to active/placed parts.
3. For each part, call `Part.CanPlace(out reason)`.
4. Wrap each result as an anonymous object containing the part, the boolean placement result, and the failure reason string.
5. Keep failed part placements in `badParts`.
6. If any bad parts exist, return `false`.

There is also a constructor/mission cost gate:

- If the game is both mission and constructor, compare `ship.Cost()` against the player's available money/cost limit field.
- It tries `Ship.ReduceCrewTrainning()` once as an automatic cost reduction.
- If cost is still too high, it sets `reason = "over cost"` and returns `false`.

### `IsValidCostWeightBarbette`

Direct evidence:

- `Ship.IsValidCostWeightBarbette(out string reason, out List<Part> errorBarbettePart)` is at `Ship.txt:240328`.
- The method initializes:
  - `reason = ""`
  - `errorBarbettePart = new List<Part>()`

The decision tree is:

1. If `ship.Weight() > ship.Tonnage()`, set `reason = "weight"` and return `false`.
2. In mission/constructor context, run the same cost check pattern:
   - compare `ship.Cost()` to the player's available money/cost limit field
   - try `Ship.ReduceCrewTrainning()`
   - if still over, set `reason = "over cost"` and return `false`
3. Iterate parts looking for barbette-like parts that must carry something.
4. If an empty barbette is found, add that part to `errorBarbettePart`, set `reason = "empty barbette"`, and normally return `false`.
5. Otherwise return `true`.

The empty-barbette scan is fairly detailed in ISIL and still has some field-name uncertainty, but the user-facing behavior is clear: an otherwise generated ship can fail acceptance because a required barbette or mount point is empty.

### `IsValidWeightOffset`

Direct evidence:

- `Ship.IsValidWeightOffset()` is at `Ship.txt:239148`.
- The current mod's shipgen issue summary calls it, but vanilla `Ship.IsValid` does not call it in the decoded path above.

The method recalculates stats with `Ship.CStats()` and then checks instability stats:

- `instability_x`
- `instability_z`

The current mod summarizes failures as `invalid weight offset x=...` and/or `z=...`. This is useful shipgen diagnostics, but based on the decoded `IsValid` body it is not part of the campaign design acceptance gate unless some caller checks it separately.

### Acceptance Logging Implications

For the campaign mystery, the highest-value trace is still inside `GenerateRandomDesigns`, immediately after the `Ship.IsValid(true)` call:

- log the candidate ship summary
- log `IsValid(true)` result
- if false, log:
  - non-casemate main turret count versus `hull.data.minMainTurrets`
  - non-casemate main barrel count versus `hull.data.minMainBarrels`
  - `IsValidCostReqParts` result, `reason`, `notPassed`, and `badParts`
  - `IsValidCostWeightBarbette` result, `reason`, and empty-barbette count

This should explain the exact gap between `Shipgen result: Success` and "design never appears anywhere."

## Ship Type and Hull Selection

There are two distinct choices in `GenerateRandomDesigns`:

1. Which `ShipType` the AI wants a design for.
2. Which concrete hull `PartData` it gives to shipgen for that `ShipType`.

### Ship-Type Selection

Direct evidence:

- The ship-type filtering lambdas are in `CampaignController_NestedType___c__DisplayClass202_0.txt`.
- `<GenerateRandomDesigns>b__0(ShipType t)` and `<GenerateRandomDesigns>b__2(ShipType t)` both:
  - require `t.canBuild`
  - require `Ship.GetHull(player, t, checkTechTonnage: true, ignoreAvailability: false, usedHulls: null) != null`
- `<GenerateRandomDesigns>b__3(Ship s)` compares `GameDate.YearsPassedSince(s.dateCreated)` against a random threshold.
- `<GenerateRandomDesigns>b__202_1(Ship s)` and `<GenerateRandomDesigns>b__202_4(Ship s)` return `s.shipType`.
- The state machine stores the selected type in `<shipType>5__3`, which is field offset `0x48`.

The decoded campaign logic is:

1. Start from the game's full ship-type list.
2. Filter to buildable types that have at least one usable hull for the player.
3. Read `player.designs` and project existing designs to their `shipType`.
4. Prefer ship types that are buildable and hull-available but not already represented in `player.designs`.
5. If no missing ship type is found, look for existing designs old enough to replace.
6. The replacement-age predicate uses `GameDate.YearsPassedSince(s.dateCreated)` against a randomized age threshold, so old designs are probabilistically eligible rather than replaced on a fixed exact age.
7. Pick a type from the resulting candidate set with a random helper.

Observed ISIL details:

- The first candidate set is built by filtering all ship types with `b__0`, then removing/grouping against `player.designs.Select(s => s.shipType)`.
- If that yields no choice, a second candidate set is built with `b__2` and the stale-design predicate `b__3`.
- There is also a guard around low design counts. The ISIL compares `player.designs.Count` to `6`, so the AI appears more willing to fill missing classes while it has few designs.

Current confidence:

- High confidence that missing design classes are preferred first.
- High confidence that stale existing classes are the fallback.
- Medium confidence on the exact random weighting/order because the collection helper calls are not named in the ISIL, but the data flow is clear.

### Concrete Hull Selection: `Ship.GetHull`

Direct evidence:

- `Ship.GetHull(Player player, ShipType shipType, bool checkTechTonnage = false, bool ignoreAvailability = false, List<PartData> usedHulls = null)` is at `Ship.txt:19150`.
- The filter closure is `Ship_NestedType___c__DisplayClass570_0.txt`.
- `PartData` fields:
  - `isHull` at `0x48`
  - `shipType` at `0xC0`
  - `countriesx` at `0xE0`
  - `isCampaignShipyardGood` at `0x170`

The decoded `GetHull` logic is:

1. Store the requested `shipType`, `player`, and `ignoreAvailability` in a closure.
2. If `checkTechTonnage` is true, call `player.TechTonnage(shipType)`.
3. If tech tonnage is exactly `0`, return `null`.
4. Start from the game's full part-data list.
5. Filter to parts where:
   - `part.isHull` is true
   - `part.shipType == requested shipType`
   - if `ignoreAvailability` is true, accept immediately after the type match
   - otherwise require the normal hull availability check for that player
6. If `usedHulls` is non-empty, remove those hulls from the candidate set when possible.
7. Pick one remaining hull with a random helper and return it.

The availability check inside the hull filter calls `Ship.IsAvailable(...)` on the hull and player. Based on the fields and surrounding code, that check is where national hull availability, year/tech unlocks, and probably campaign shipyard suitability are enforced.

How `GenerateRandomDesigns` calls it:

- For the initial "can this type be designed at all?" filter, it calls `GetHull(player, type, checkTechTonnage: true, ignoreAvailability: false, usedHulls: null)`.
- For the actual shipgen hull after a type is selected, it again calls `GetHull` with `checkTechTonnage: true` and `ignoreAvailability: false`.
- It passes the selected hull into `Ship.ChangeHull`, then enters constructor and runs `Ship.GenerateRandomShip`.

Important implication:

- The AI does not appear to score hulls by "best" displacement, newest generation, or class doctrine at this layer. It filters the allowed hull pool and randomly selects one.
- The more strategic choice is the `ShipType`; the concrete hull is mostly the result of availability filters plus randomness.
- If a country has no designs for a class, it may still fail to generate one because `GetHull` returns null for every eligible type, or because a random allowed hull later fails `Ship.IsValid(true)`.

### Hull-Selection Logging Implications

Useful instrumentation points:

- Around `GenerateRandomDesigns` ship-type selection:
  - available ship types after `b__0`
  - existing design ship types
  - missing candidate types
  - stale candidate types
  - selected `shipType`
- Around the actual `Ship.GetHull` call:
  - selected `shipType`
  - returned hull name/model/generation/tonnage range
  - whether `GetHull` returned null
- Optional deeper hook inside `Ship.GetHull`:
  - count candidate hulls before/after availability filtering
  - count after removing `usedHulls`
  - list a compact sample of hull names/models

That would tell us whether a nation is failing because it cannot find a hull, picks a bad hull, or picks a good hull that later fails design acceptance.

## Phase 2: Build New Ships

The build-order phase is:

```csharp
private void BuildNewShips(Player player, float tempPlayerCash)
```

Direct evidence:

- Method signature at cpp2il `CampaignController.cs:3082`.
- Compiler-generated closure class `<>c__DisplayClass198_0` at cpp2il `CampaignController.cs:1472` stores:
  - `player`
  - `Func<ShipType, Ship>`
  - `Func<ShipType, float>`
- It has methods:
  - `<BuildNewShips>b__0(ShipType stype) => Ship`
  - `<BuildNewShips>b__2(ShipType t) => float`
- Closure class `<>c__DisplayClass198_1` at cpp2il `CampaignController.cs:1487` stores:
  - `Func<ShipType, Ship> GetDesign`
  - `float ratioSum`
  - parent locals
- It has methods:
  - `<BuildNewShips>b__1(ShipType t) => bool`
  - `<BuildNewShips>b__3(ShipType t) => bool`
  - `<BuildNewShips>b__4(ShipType t) => bool`
- Closure class `<>c__DisplayClass198_2` at cpp2il `CampaignController.cs:1504` stores:
  - `ShipType t`
- It has:
  - `<BuildNewShips>b__7(Ship s) => bool`
- Top-level compiler-generated helpers include:
  - `<BuildNewShips>b__198_5(Ship x) => float`
  - `<BuildNewShips>b__198_6(Ship d) => bool`

Inferred behavior:

1. `BuildNewShips` creates a local `GetDesign(ShipType)` function.
2. `GetDesign` likely searches `player.designs` for a matching design type.
3. It computes type ratios or weights using `ratioSum` and `Func<ShipType, float>`.
4. It filters candidate ship types with several bool lambdas.
5. For a selected type, it uses the selected design to create one or more actual ships in the build queue.
6. If no design is returned for a desired type, that type cannot be ordered.
7. If budget/capacity/ratio gates fail, no build order is placed even with valid designs.

Current gg140 build tracing observes this phase:

- It snapshots `player.designs` before and after `BuildNewShips`.
- It snapshots building/commissioning ships before and after.
- It reports new designs and new builds found by ID.
- It reports no-build context: construction tonnage, approximate free capacity, design tonnage range, and inferred bucket.

What gg140 already proved:

- `BuildNewShips` can place builds from existing designs without new design count changes.
- Some nations have designs and capacity but still place no orders, so there are budget/ratio/satisfaction gates not yet logged.
- A successful shipgen log does not guarantee a campaign design was accepted.

## Shared/Predefined Design Path

The mod replaces `CampaignController.GetSharedDesign` with `CampaignControllerM.GetSharedDesign`.

Direct evidence:

- Patch at `TweaksAndFixes/Harmony/CampaignController.cs`.
- Replacement method at `TweaksAndFixes/Modified/CampaignControllerM.cs`.

Step-by-step behavior in the replacement:

1. Look up `G.GameData.sharedDesignsPerNation[player.data.name]`.
2. Split stored designs into newer/older buckets for the requested `ShipType`.
3. Ignore shared designs already present in `player.designs`.
4. Reject designs outside the configured future/past year window.
5. Optionally instantiate candidate ships with `Ship.Create(null, null, ...)` and `FromStore`.
6. If not an early saved ship, require `PlayerController.Instance.CanBuildShipsFromDesign`.
7. Score by tech match when `checkTech` is true.
8. Choose from best/ok/min tech coverage buckets.
9. Instantiate the selected design from store.
10. Ensure it has an ID.
11. Return the `Ship`.

Important detail:

- `GetSharedDesign` returns a `Ship`; it does not itself prove that the returned design is added to `player.designs`.
- The add/accept decision likely happens in the caller, probably `GenerateRandomDesigns`.

The mod also patches `CampaignDesigns.RandomShip`:

1. If fast overlay code is not needed, vanilla `RandomShip` runs.
2. Otherwise it returns `PredefinedDesignsData.Instance.GetRandomShip(player, type, desiredYear)`.

This is another possible source of non-random generated designs.

## Current Mod Hooks Affecting This Flow

`Patch_CampaignController.Prefix_BuildNewShips`

- Captures before snapshot.
- If prewarming and `taf_campaign_skip_prewarm_shipbuilding` is enabled, skips vanilla `BuildNewShips`.
- Logs skipped prewarm calls.

`Patch_CampaignController.Postfix_BuildNewShips`

- Captures after snapshot.
- Logs changed/no-change result.

`Patch_GenerateRandomDesigns.Prefix_MoveNext`

- Only skips at state `0` during prewarming.
- Does not log normal design-generation behavior.

`Patch_Ship_CreateRandom`

- Attempts to log `Ship.CreateRandom` coroutine lifecycle.
- gg140 runtime showed zero `AI CreateRandom` lines, so this is not sufficient for the current question.

## Working Model

The most likely AI turn pipeline is:

1. `AiManageFleet(player, prewarming)`
2. Budget/sub-budget setup, shipyard decisions, refit/scrap checks may happen around the fleet phase.
3. `GenerateRandomDesigns(player, prewarming)` ensures the AI has candidate campaign designs.
4. `GenerateRandomDesigns` may use:
   - random generated ships through `GenerateRandomShip`
   - `Ship.CreateRandom` in some cases
   - shared/predefined designs through `GetSharedDesign` or `CampaignDesigns.RandomShip`
5. Accepted designs end up in `player.designs`.
6. Rejected generated ships can still have successful shipgen logs.
7. `BuildNewShips(player, tempPlayerCash)` chooses from `player.designs`.
8. `BuildNewShips` applies type ratio, budget, capacity, and probably desired tonnage/fleet need gates.
9. If it orders ships, actual build-queue ships appear in `player.GetFleetAll()` with `isBuilding` or `isCommissioning`.

## Architecture Idea: Off-Turn Design Generation

Goal:

- Let AI design generation run outside the normal end-turn `AiManageFleet` phase so nations can keep a ready pool of valid designs.
- Ideally reduce end-turn stalls and avoid cases where the build phase has no usable designs.

Important constraint:

- This should not be implemented as true background-thread work. `Ship`, `Part`, `VesselEntity`, Unity objects, Il2Cpp collections, `G.GameData`, constructor state, and UI/reporting calls should be treated as main-thread only.
- The realistic version is a background service implemented as a Unity/MelonLoader coroutine that runs on the main thread over many frames.

Possible implementation shapes:

1. Opportunistic vanilla coroutine runner
   - Periodically select one AI player that needs designs.
   - Run the existing `GenerateRandomDesigns(player, prewarming: false)` coroutine during safe campaign-idle moments.
   - Let it mutate `player.designs` directly if candidates pass validation.
   - This reuses the most vanilla logic, but has the highest risk of side effects because `GenerateRandomDesigns` also calls constructor/shipgen/UI/report paths that were originally meant to run inside `AiManageFleet`.

2. Dedicated design-lab service
   - Reimplement only the small outer scheduling loop:
     - choose AI player
     - choose needed/stale `ShipType`
     - choose hull with `Ship.GetHull`
     - create a temporary ship
     - run `GenerateRandomShip`
     - validate with `Ship.IsValid(true)`
   - If valid, add/store the design.
   - If invalid, erase the temporary vessel cleanly.
   - This is more work, but gives better control over logging, rate limiting, and UI side effects.

3. Precompute candidate recipes, instantiate later
   - During idle frames, only decide desired `ShipType`/hull/seed/metadata.
   - During the normal AI turn, instantiate and validate using vanilla calls.
   - This is safest but gives the least benefit, because expensive shipgen still happens during the turn.

Safe scheduling rules:

- Run only in campaign mode.
- Run only when not in battle, not loading, not already processing end-turn, and not already inside `AiManageFleet`.
- Do not run while the human player is in the ship designer/constructor, unless we prove vanilla constructor state is isolated.
- Allow only one AI design-generation job at a time.
- Hard-cap work per frame or per real-time interval.
- Never call `BuildNewShips` from the service; leave construction orders to the normal turn logic unless we explicitly design a separate build scheduler.

Why the service should pause in unsafe states:

- Vanilla shipgen is not a pure data calculation. It calls `Ship.Create`, `Ship.ChangeHull`, `Ship.EnterConstructor`, `Ship.GenerateRandomShip`, `Part.CanPlace`, `Ship.LeaveConstructor`, and vessel erasure paths. Those touch global constructor state, Unity objects, part placement state, and Il2Cpp collections.
- The human ship designer likely uses the same constructor/global state. If AI shipgen runs while the player is designing a ship, the AI job could change the active constructor ship, part availability, selected hull, UI assumptions, or placement state.
- End-turn processing mutates the same campaign collections: `player.designs`, fleet lists, build queues, finances, and AI state. Removing vanilla end-turn design generation avoids one overlap, but `BuildNewShips`, scrapping, refits, submarine building, and save/autosave paths still read or mutate nearby state.
- Loading, battle scenes, and scene transitions can destroy or replace Unity objects that shipgen expects to exist.
- Save/autosave during a half-constructed temporary design could serialize junk unless the design job has a clean transaction boundary.

Battle-specific risk:

- Battles are not just "time when the campaign UI is hidden." They are mission scenes full of live `Ship`, `Part`, collider, physics, damage, visibility, firing, and AI objects.
- Vanilla shipgen uses the same broad classes as battle simulation: `Ship`, `Part`, `PartData`, `VesselEntity`, placement checks, stats recalculation, and Unity objects.
- `PartData` includes mutable/global-ish fields such as `constructorShip`, and constructor/placement code can temporarily attach context to part data while checking or placing parts.
- If shipgen creates temporary ships or parts in a battle scene, those objects could end up in the active mission scene, interact with physics/layers/raycast checks, or be picked up by battle cleanup/search code.
- Even if correctness is fine, shipgen is CPU-heavy and can run many retries. Running it during real-time battle can cause frame hitches at exactly the worst time.
- Campaign state may not be fully active during battle. Committing `player.designs` while battle resolution is also preparing losses, captures, refits, or post-battle saves would need a clean transaction boundary.

Important nuance:

- The finished design is logically separate from the battle. It should not become a combat unit in the current battle just because it was generated.
- The risky part is how vanilla reaches that finished design. The decoded path calls `Ship.Create`, `Ship.ChangeHull`, `Ship.EnterConstructor`, `Ship.GenerateRandomShip`, `Ship.LeaveConstructor`, and `VesselEntity.TryToEraseVessel`.
- `Ship.EnterConstructor` sets the ship state to constructor and calls `Ship.LoadUnloadModel(true)`.
- `Ship.LeaveConstructor` sets the ship state back and calls `Ship.LoadUnloadModel(false)`.
- `Ship.EnterBattle` and `Ship.LeaveBattle` use the neighboring state values and also call `LoadUnloadModel` plus battle load/unload paths.
- `Ship.ChangeHull` has generated helper code over `GameObject`, so hull changes are not pure CSV/data edits.
- Therefore, campaign designs are conceptually separate from battle units, but vanilla design generation is not obviously separate from the active Unity scene.

What shared state is involved:

- `PartData` is shared game data, not a per-design copy. It includes a mutable `constructorShip` field at `PartData.cs` offset `0x168`; the ISIL getter/setter read/write `[partData + 0x168]`.
- `Ship` has static scene containers:
  - `Ship.shipsCont`
  - `Ship.shipsActiveCont`
- `Part` has static model/container state:
  - `Part.partsReuseCont`
  - `Part.loadedModels`
  - `Part.loadedModelsCont`
- Individual `Ship` objects also hold Unity scene objects:
  - `generalCont`
  - `hullCont`
  - `partsCont`
  - `effectsCont`
  - many battle UI/collision/effect objects
- `Ship.LoadUnloadModel` manipulates model state and containers. It is called by constructor entry/exit and by battle entry/exit.
- `Part.Create`, `Part.LoadModel`, placement, refresh, LOD, and load/unload battle paths all operate on `GameObject`, `Transform`, `Renderer`, `Rigidbody`, colliders, and cached model containers.
- `Part.CanPlace` and `Part.CanPlaceGeneric` check availability/counts against the passed ship, but they also sit in this same object/model ecosystem and can depend on part data, placement state, mount/deck objects, and loaded model/collider state.
- Several validation paths branch on global `GameManager` state, such as `GameManager.IsMission` and `GameManager.IsConstructor`. In a battle scene, those flags may not match the assumptions of campaign constructor shipgen.

So the issue is not that the finished design should appear in battle. It is that the vanilla path temporarily constructs the design using globally shared data objects, static model caches, and active-scene Unity objects.

Could it run during battles anyway?

- Maybe, but only after proving that the design job is isolated from the battle scene.
- A safer battle-compatible version would need to generate in an inactive/offscreen constructor context or a separate additive scene, never instantiate battle-visible vessels, and commit only completed design data back on the campaign map.
- Without that isolation, battle should be treated as a pause condition for any wrapper around vanilla shipgen.

Better wording:

- The service should not necessarily abort a job when an unsafe state starts.
- It can suspend/yield without advancing the shipgen coroutine, then resume when the campaign map is safe again.
- If the job has already created a temporary ship and cannot be safely suspended, it should cleanly call `Ship.LeaveConstructor` and `VesselEntity.TryToEraseVessel`, then retry later.

If we remove vanilla end-turn design generation:

- Patch `GenerateRandomDesigns` during `AiManageFleet` to no-op or only run as a fallback when the design service is disabled.
- Keep `BuildNewShips` in the end-turn path so construction decisions still happen at the normal campaign moment.
- Add a lock/state flag so the service cannot commit a new design while `BuildNewShips` is reading design lists.
- At end-turn start, either:
  - pause the service and let the current committed design pool be used, or
  - finish only if the job is already at a safe commit point.

What "run constantly" can mean safely:

- It can be always scheduled and always ready to work.
- It should not continuously advance through unsafe scenes/states.
- It should advance in small main-thread slices, yielding often, because real background threads are unsafe for vanilla Unity/Il2Cpp shipgen.

Truly uninterrupted generation would require a much larger rewrite:

- clone enough campaign/player/tech data into thread-safe plain data
- implement ship design and placement without Unity constructor objects
- validate without `Part.CanPlace`/constructor side effects
- commit only finished designs back on the main thread

That would be closer to a new headless ship-designer engine than a wrapper around vanilla generation.

State the service would need:

- Queue of AI players needing designs.
- Per-player cooldown, so one nation does not monopolize generation.
- Per-player/per-type failure counters, so repeatedly invalid hull/type combinations do not spin forever.
- Snapshot of design counts/types before and after each job.
- A reentrancy flag such as `tafAiDesignServiceActive`.

Validation/reporting rules:

- Log every selected player/type/hull.
- Log `Ship.IsValid(true)` result and sub-reasons.
- If a design is accepted off-turn, mark/log it clearly as off-turn generated.
- Suppress or redirect UI-facing `Ui.ReportNewDesign` if it is noisy or assumes end-turn context.
- Ensure temporary failed ships call `Ship.LeaveConstructor` and `VesselEntity.TryToEraseVessel`.

Main risk:

- Vanilla design generation appears to rely on constructor/global state. Running it at arbitrary campaign times could disturb the player's current UI or leave stale constructor state if interrupted.

Recommended first experiment before gg141:

1. Do not enable always-on generation yet.
2. Add deeper `GenerateRandomDesigns` tracing to learn all side effects and acceptance/rejection paths.
3. Add a disabled-by-default prototype service that can run exactly one AI design job from a debug param/hotkey while in campaign map idle state.
4. Compare:
   - design count delta
   - build count delta on next turn
   - logs for constructor/UI side effects
   - save/load stability

If the one-job prototype is stable, expand to a rate-limited service. If it is not stable, keep generation inside end-turn and instead improve `GenerateRandomDesigns` reliability and visibility.

## gg141 Yolo Service Experiment

The current experiment intentionally skips the conservative pause/idle-state guardrails above.

Implementation in `TweaksAndFixes/Harmony/CampaignController.cs`:

- `Prefix_OnNewTurn` starts an always-on Melon coroutine when `taf_campaign_ai_design_service_enabled=1`.
- The service loops over active AI major powers and invokes vanilla private `CampaignController.GenerateRandomDesigns(player, prewarming:false)` by reflection.
- The runner drives nested Il2Cpp coroutines, because `GenerateRandomDesigns` yields `Ship.GenerateRandomShip`; simply calling parent `MoveNext()` once per frame would resume the parent before shipgen finished.
- `Patch_GenerateRandomDesigns` skips vanilla state-0 end-turn design generation when `taf_campaign_ai_design_service_disable_endturn_generation=1`, unless the active caller is the service itself.
- `BuildNewShips` remains in the vanilla end-turn flow. This means construction still happens turn-by-turn, but the design pool can change between turns while the service is running.
- There is no battle/loading/designer pause in this yolo build. The test assumption is that the user will not enter battles or the human ship designer while observing the campaign behavior.

Params added:

```csv
taf_campaign_ai_design_service_enabled
taf_campaign_ai_design_service_disable_endturn_generation
taf_campaign_ai_design_service_start_delay_seconds
taf_campaign_ai_design_service_player_delay_seconds
taf_campaign_ai_design_service_cycle_delay_seconds
taf_debug_ai_design_service
```

Expected log signatures when enabled:

- `AI design service scheduled by ...`
- `AI design service loop entered`
- `AI design service cycle ... begin`
- `AI design service begin: <nation> ... designs=N [...]`
- `AI design service end: <nation> ... designs=N->M [...]`
- `Skipping vanilla GenerateRandomDesigns ... AI design service owns generation`

Primary risks to watch in `Latest.log`:

- Reflection failure finding `GenerateRandomDesigns`.
- Coroutine failure while advancing nested shipgen.
- Repeated cycles with no design-count delta for countries that should need designs.
- Designs added by service but not consumed by the next `BuildNewShips`.
- Any constructor/UI/null-reference error after a service shipgen pass.

## gg142 Realtime Wait Fix

Runtime evidence from gg141:

- End-turn vanilla `GenerateRandomDesigns` skipping worked.
- `AI design service started` printed after the turn finished.
- No `AI design service cycle ... begin` line appeared afterward.

Likely cause:

- The service used `WaitForSeconds` for startup/player/cycle delays.
- Campaign/world idle can run with scaled Unity time paused or near-paused, so a scaled-time delay may never finish.

gg142 change:

- Replace service-loop `WaitForSeconds` delays with `WaitForSecondsRealtime`.
- Keep nested shipgen advancement on `WaitForEndOfFrame`, because that part should advance frame-by-frame once the service is inside a vanilla generator coroutine.

## gg143 Manual Realtime Wait Fix

Runtime evidence from gg142:

- `AI design service started` printed after end-turn.
- No `AI design service cycle ... begin` appeared after several real seconds.
- No service exception was logged.

gg143 change:

- Remove Unity wait-instruction objects from the service's outer startup/player/cycle delays.
- Use direct `Time.realtimeSinceStartup` loops that `yield return null` until the target real time has elapsed.
- This tests whether Melon/Unity wait-instruction scheduling is the stall, while keeping the service on the main thread.

## gg144 Existing Coroutine Startup Copy

Runtime evidence from gg143:

- `AI design service started` printed, proving `MelonCoroutines.Start(...)` was reached.
- No `AI design service cycle ... begin` appeared after several real seconds.
- No service exception was logged.

Existing working TAF coroutine patterns:

- `Patch_GameData.Postfix_PostProcessAll` starts `FillDatabase()` with `MelonCoroutines.Start(...)`.
- `FillDatabase()` immediately yields `new WaitForEndOfFrame()` before doing real work.
- `PredefinedDesignsData.FixBSGText()` follows the same immediate `WaitForEndOfFrame` style.
- `Patch_Ui.Postfix_Update` is already the mod's reliable per-frame update pump for campaign helpers such as `CampaignControllerM.Update()`.

gg144 change:

- `OnNewTurn` only records that the service should be running.
- `Patch_Ui.Postfix_Update` calls `Patch_CampaignController.UpdateAiDesignService()`.
- The service is scheduled from that update pump once `CampaignController.Instance`, `CampaignData`, and `CampaignData.Players` exist.
- The service coroutine logs `AI design service loop entered` as its first body action, then yields `WaitForEndOfFrame`.
- Startup/player/cycle delays now loop on `Time.realtimeSinceStartup` while yielding `new WaitForEndOfFrame()`, matching the existing working coroutine style more closely than `yield return null`.
- The experimental end-turn skip remains controlled only by `taf_campaign_ai_design_service_enabled` plus `taf_campaign_ai_design_service_disable_endturn_generation`; there is no extra safety latch while testing.

Runtime evidence from gg144:

- The service did print `AI design service loop entered`, so `MelonCoroutines.Start(...)` and the coroutine body both ran.
- It printed before `Loaded database and config` and before `OnEnterState: World`, so the service was scheduled during loading/menu setup.
- It did not later print `AI design service cycle ... begin`, so it still did not advance into the work loop.

## gg145 Vanilla Nested Coroutine Copy

ISIL evidence from `CampaignController_NestedType__AiManageFleet_d__201.txt`:

- `AiManageFleet.MoveNext` calls `CampaignController.GenerateRandomDesigns`.
- It stores the returned enumerator into its own current field.
- It returns `true`, letting Unity/Il2Cpp drive the nested `GenerateRandomDesigns` coroutine.
- After that nested coroutine completes, `AiManageFleet` resumes and calls `BuildNewShips` and `BuildNewSubmarines`.

ISIL evidence from `CampaignController_NestedType__GenerateRandomDesigns_d__202.txt`:

- `GenerateRandomDesigns.MoveNext` calls `Ship.GenerateRandomShip`.
- It stores the returned enumerator into its own current field.
- It returns `true`, again letting Unity/Il2Cpp drive the nested shipgen coroutine.

gg145 change:

- The service is not scheduled until `GameManager.Instance.isCampaign` is true, `CurrentState == GameManager.GameState.World`, and the loading screen is not active.
- `RunAiDesignServiceForPlayer` now yields the Il2Cpp `GenerateRandomDesigns` enumerator directly instead of using the mod's manual `RunIl2CppCoroutine` walker.
- Startup/player/cycle delays use simple top-level `yield return null` loops rather than a nested managed delay enumerator.
- This is intentionally closer to the vanilla chain: outer coroutine yields private campaign design coroutine, and the campaign design coroutine yields shipgen.

Runtime evidence from gg145:

- The service entered only after `GameManager.GameState.World`, as intended.
- Directly yielding the private Il2Cpp enumerator did start shipgen and produced `Shipgen begin` / `Shipgen result=Success` lines.
- MelonLoader then threw `Unsupported type Il2CppSystem.Collections.IEnumerator` from the IL2CPP-to-managed coroutine wrapper. So the direct-yield path can kick generation, but it is not a stable bridge.

## gg146 Manual Walker Plus Persistence Verification

gg146 keeps the gg145 world-state scheduling guard and changes the service back to a managed main-thread stack walker for Il2Cpp coroutines:

- `RunAiDesignServiceForPlayer` invokes private `GenerateRandomDesigns(player, false)` by reflection.
- `_AiDesignServiceRunningGenerateRandomDesigns` is set while the service advances that coroutine, so the end-turn skip prefix does not block service-owned generation.
- `RunIl2CppCoroutine` calls `MoveNext()` on the current Il2Cpp enumerator, inspects `Current`, and pushes nested Il2Cpp enumerators onto a stack. This avoids returning raw `Il2CppSystem.Collections.IEnumerator` objects to MelonLoader's managed coroutine wrapper.

New verification logs:

- `AI GenerateRandomDesigns begin: ...` records the starting `player.designs` count and class mix for each AI design coroutine.
- `AI GenerateRandomDesigns persisted: ...` means the coroutine finished and at least one new design id is present in `player.designs`.
- `AI GenerateRandomDesigns no persisted designs: ...` means shipgen may have run, but no new design survived into the campaign design list.
- `AI design service verified persisted design(s): ...` is the service-level confirmation that the generated design is visible in the owning player's design collection after the full coroutine completes.

Runtime evidence from gg146:

- The service was scheduled in World state and printed `AI design service loop entered`.
- It never printed `AI design service cycle ... begin`, so the service did not reach the first player pass.
- No `AI GenerateRandomDesigns` begin/end lines appeared, meaning the new persistence instrumentation never ran.
- Pressing Next Turn then hung after a province ownership log. With `taf_campaign_ai_design_service_disable_endturn_generation=1`, vanilla end-turn `GenerateRandomDesigns` could still be skipped even though the service had not proven it was doing the replacement work.

## gg147 Service Safety Fix

gg147 keeps the experiment available but makes the failure mode less severe:

- Service waits now yield `new WaitForEndOfFrame()`, matching existing working Melon coroutine patterns in this repo.
- `taf_campaign_ai_design_service_disable_endturn_generation` defaults to `0`.
- Even if that param is set to `1`, the vanilla end-turn `GenerateRandomDesigns` skip only arms after the service completes at least one full cycle.
- This means a scheduled-but-dead service should no longer starve the vanilla turn path.

## gg148 Unity-Hosted Service Probe

The current performance experiment tries to run campaign design generation through Unity's native coroutine host:

- `AiDesignCoroutineHost` is a registered IL2CPP `MonoBehaviour` created on a persistent `GameObject`.
- `RunAiDesignServiceForPlayer` reflects the private `CampaignController.GenerateRandomDesigns(player, false)` method.
- It keeps both views of the returned object:
  - `CampaignController._GenerateRandomDesigns_d__202`, used for pointer identity and Harmony trace correlation.
  - `Il2CppSystem.Collections.IEnumerator`, passed to `AiDesignCoroutineHost.StartCoroutine(...)`.
- The service stores an `AiDesignServiceJob` keyed by the IL2CPP routine pointer.
- `Patch_GenerateRandomDesigns.Postfix_MoveNext` marks that job complete when Unity finishes the routine and `MoveNext` returns `false`.
- The service waits for completion or `taf_campaign_ai_design_service_job_timeout_seconds` before moving to the next AI nation.

Expected log sequence:

1. `AI design service scheduled by ... in state World`
2. `AI design service loop entered.`
3. `AI design service cycle ... begin`
4. `AI design service begin: <nation>...`
5. `AI design service Unity coroutine started: <nation>...`
6. `AI GenerateRandomDesigns begin: ... service=True`
7. `AI GenerateRandomDesigns persisted...` or `AI GenerateRandomDesigns no persisted designs...`
8. `AI design service Unity coroutine completed...`
9. Optional `AI design service verified persisted design(s)...`

Important test setup:

- `taf_campaign_ai_design_service_enabled=1`
- `taf_debug_ai_design_service=1`
- `taf_debug_ai_shipbuilding=1`
- `taf_campaign_ai_design_service_disable_endturn_generation=1`
- `taf_campaign_ai_design_service_job_timeout_seconds=90`

In `gg148`, the source still had a safety latch: even if the skip param was `1`, vanilla end-turn design generation was not skipped until the service completed one full cycle.

## gg149 Service-Owned Generation

gg149 removes the completed-cycle latch from `ShouldSkipServiceOwnedRandomDesigns()`. With:

- `taf_campaign_ai_design_service_enabled=1`
- `taf_campaign_ai_design_service_disable_endturn_generation=1`

the vanilla state-0 end-turn `GenerateRandomDesigns` coroutine is skipped immediately unless the routine belongs to the service. The intent is to reduce overlap and make the Unity-hosted service the single design-generation owner during the performance test. Construction remains turn-based because `BuildNewShips` is still called by the normal AI turn flow.

Runtime evidence from gg149:

- The service entered the loop.
- Reflection returned `Il2CppSystem.Collections.IEnumerator`, but not a directly castable `_GenerateRandomDesigns_d__202`.
- The service therefore logged repeated `AI design service could not cast GenerateRandomDesigns result` messages.
- No `AI design service Unity coroutine started`, `AI GenerateRandomDesigns begin`, or persistence logs appeared.
- Vanilla end-turn `GenerateRandomDesigns` was skipped as intended, so this build could starve new design generation even though `BuildNewShips` still ran from existing designs.

## gg150 Pending Unity Coroutine Binding

gg150 changes the service-owned coroutine bridge:

- `InvokeGenerateRandomDesigns` only requires the reflected return value to be an `Il2CppSystem.Collections.IEnumerator`.
- `RunAiDesignServiceForPlayer` creates an `AiDesignServiceJob` before calling `StartCoroutine`.
- That job starts in `_AiDesignServicePendingJobs`, keyed by a pending id and matched by player pointer plus `prewarming`.
- `Patch_GenerateRandomDesigns.Prefix_MoveNext` calls `TryBindAiDesignServiceRoutine` before the vanilla skip guard.
- When Unity unwraps the enumerator and enters the typed `_GenerateRandomDesigns_d__202.MoveNext`, the prefix moves the pending job into `_AiDesignServiceJobs` keyed by the typed routine pointer.
- The same prefix then lets the service-owned routine run and starts the normal begin/end persistence trace.

Expected gg150 logs:

1. `AI design service Unity coroutine requested: <nation>...`
2. `AI design service Unity coroutine bound: <nation>...`
3. `AI GenerateRandomDesigns begin: ... service=True`
4. `AI GenerateRandomDesigns persisted...` or `AI GenerateRandomDesigns no persisted designs...`
5. `AI design service Unity coroutine completed...`

Runtime evidence from gg150:

- The pending-bind bridge works: `requested` and `bound` appear, and service-owned shipgen runs.
- The service persisted real designs, including examples such as `CA Laiyuan`, `BB Odin`, `CL Proserpine`, `CL Sirius`, and `CA Afrika`.
- The process hang recorded by Windows was an `Application Hang` event, not a managed exception or timeout. `Latest.log` and Unity `Player-prev.log` stop inside background shipgen, with no Melon stack trace.
- The likely failure mode is main-thread starvation from long vanilla shipgen slices while the game remains interactive. The bridge is functional, but always-on generation can still make the Unity player unresponsive when a hull/randpart path takes many seconds.
- The live params row `taf_debug_ai_design_service` had an unquoted comma in its description, causing a startup CSV parse assertion. Quote the description in both repo defaults and live params.

## Open Questions

These are not answered by the current dump alone:

- Which exact state in `GenerateRandomDesigns` adds a generated ship to `player.designs`.
- What exact rejection checks can discard a successfully generated ship.
- Whether `BuildNewShips` refuses to build because of:
  - current build tonnage versus capacity
  - cash or `tempPlayerCash`
  - existing fleet ratio targets
  - minimum/maximum type quotas
  - port/shipyard constraints
  - war/peace urgency
- Whether `GetSharedDesign` returns are being rejected by caller checks.

## Recommended Next Instrumentation

gg148 adds begin/end persistence tracing plus Unity-hosted service completion logging for `CampaignController._GenerateRandomDesigns_d__202.MoveNext`. If that still leaves gaps, the next deeper trace is per-state logging:

1. At state `0`, log player/date/prewarming and current design classes.
2. On every state transition, log compactly:
   - state before/after
   - current `shipType`
   - `tries`
   - current candidate ship summary
   - candidate status/isDesign/isSharedDesign/id
   - whether candidate is in `player.designs`
3. On coroutine completion, log design count/class delta and new design names.
4. Use rate limiting if needed, but keep full logs while investigating a single clean campaign turn.

Then add targeted `BuildNewShips` gate tracing only after the design acceptance path is clear.
