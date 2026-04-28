# Campaign Load Delay Investigation

## Context

The campaign load screen appeared to pause for a long time after showing world/campaign load messages, especially after the game entered `World`. Initial suspicion was around the visible loading labels such as battles, but instrumentation showed the delay was later in the load sequence.

Instrumentation was enabled in `gg151` and checked against:

```text
E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log
```

## Latest Observed Load

Latest timed campaign load from the `gg151` log:

```text
Total campaign load:              83.4s
UpdateAllShipsWeightCost(true):   70.0s
ships-design-id-fixups:            8.0s
player-ship-textures:              1.0s
```

The important post-World section was:

```text
Campaign load timing method begin:
  state=19 (navmesh-passable-areas-second-and-predefs)
  method=UpdateAllShipsWeightCost
  force=True

Campaign load timing method end:
  method=UpdateAllShipsWeightCost
  elapsedMs=70020.6
```

Other operations in the same final callback were tiny:

```text
UpdateNavmeshPassableAreas:       3.6ms
MissionsWindowUI.UpdateInfo:      2.5ms
CompleteLoadingScreen:            0.5ms
CenterCampaignCamera:             0.1ms
MinesFieldManager.FromStore:      0.2ms
```

## Current Conclusion

The long post-`OnEnterState: World` gap is not battles, navmesh, mines, or UI. It is almost entirely:

```text
CampaignController.UpdateAllShipsWeightCost(force=True)
```

That method forces all loaded campaign ships through weight/cost/stat recalculation before handing control back to the world UI.

## What The Method Does

Based on the decompiled code, `UpdateAllShipsWeightCost(force: true)` roughly does:

```csharp
if (force)
{
    foreach (Ship ship in allShips)
    {
        ship.CalcCitadelValues();
        // Refresh/reset related ship stat caches.
        ship.stats.Clear();
    }
}

Parallel.ForEach(allShips, ship =>
{
    ship.CalcWeightAndCost(force: true, updateCached: false);
});
```

The expensive work is inside each ship's `Ship.CalcWeightAndCost(true)`, which recalculates derived values such as:

```text
ship total weight
ship cost
part weights/costs
material costs
crew/quarters weight
mine/depth-charge weights/costs
derived stat effects
cached ship stat totals
```

## Cache / Save Findings

`Ship` runtime objects have private cached fields such as:

```text
weight
cost
weightsValid
weightCache
costCahce
```

But `Ship.Store` appears to save only source-of-truth ship design/state data, such as:

```text
hull
parts
armor
components
techs
tonnage
beam/draught
defects
ammo
mission/status fields
```

The derived `weight` and `cost` runtime cache values do not appear to be part of the vanilla MessagePack ship store.

That likely explains why the game recalculates on load: it rebuilds derived ship economics from the current data files and mod rules rather than trusting potentially stale saved values.

## Force False Hypothesis

Changing the load-time call from `force=true` to `force=false` probably would not remove the full delay by itself unless many loaded ships already have valid runtime weight/cost values before the call.

Freshly loaded ships likely start with invalid/default cache values because the cache is not saved. In that case, `force=false` would still recalculate most ships.

The next diagnostic, if this is resumed, should be:

```text
Before UpdateAllShipsWeightCost(true), count how many loaded ships already have valid weight/cost versus -1.
```

If most are invalid, `force=false` is not enough. If many are valid, there may be a cheaper path.

## Possible Future Direction

Saving weight/cost is feasible, but the safest approach is probably not to extend vanilla `Ship.Store` directly. The game uses generated IL2CPP MessagePack formatters, so changing the core store schema could be brittle.

A safer TAF-only approach would be a sidecar cache keyed by:

```text
save name
ship id / design id
design fingerprint
data/mod fingerprint
cache schema version
```

The cache could store:

```text
weight
cost
weightCache
costCache
```

Load behavior could then be:

```text
No cache: recalc normally.
Cache exists but fingerprint mismatch: recalc normally.
Cache exists and fingerprint matches: inject cached values and skip/short-circuit the forced recalc for that ship.
```

This would allow old saves to continue working, because missing cache data would simply fall back to the normal recalculation path.

No implementation has been done for this cache idea.
