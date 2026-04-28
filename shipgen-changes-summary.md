# Ship Generation Improvements Product Brief

## Purpose

Make campaign starts and early AI ship design generation feel faster, more predictable, and less frustrating while preserving the parts of vanilla generation that actually matter.

The recent work focuses on three player-visible outcomes:

- Start a campaign with a true empty-world fleet state when desired.
- Let AI nations begin designing ships naturally after the campaign starts.
- Reduce design-generation churn caused by overly strict early-era gun layout requirements.

## User-Facing Changes

### Blank Slate Campaign Start

The New Campaign fleet selection now supports a third option: **Blank Slate**.

When selected, the campaign starts without pre-generated starting warships. Nations enter the campaign without their usual starting fleet and begin building ships through the normal campaign flow.

Why this matters:

- Useful for testing AI shipbuilding from a clean baseline.
- Avoids long startup generation work when the goal is to watch the campaign develop naturally.
- Makes the "no starting fleets" behavior explicit instead of hidden behind debug flags.

### Normal Campaign Starts Stay Normal

We fixed an issue where normal campaign setup could accidentally rotate into the Blank Slate option. Auto-generated starts and Create Own starts should no longer silently skip ship creation.

Why this matters:

- The new option is opt-in.
- Standard campaign starts remain trustworthy.
- Testing Blank Slate no longer risks confusing later normal campaign runs.

### More Forgiving Early Ship Designs

Early ship generation was rejecting ships that had usable main guns but did not meet strict hull metadata counts for turret or barrel totals.

The new behavior keeps the important requirement: a ship still needs a real main gun when its ship type requires one. But it no longer rejects otherwise usable ships just because they have fewer main turrets or barrels than the hull metadata asks for.

Why this matters:

- Early battleship and cruiser designs should fail less often.
- Generation should spend less time repeatedly trying near-identical layouts.
- Odd historical hulls can accept imperfect but usable layouts instead of stalling on idealized gun counts.

### Cleaner Randpart Rule Selection

Several randpart rules were valid on paper but consistently produced bad practical outcomes. They matched the right hull tags or ship types, but in live generation they either placed no useful guns, repeatedly failed validation, or pushed the generator into expensive retry loops.

We investigated a small set of suspicious main-gun randpart recipes, but the current source is **not hard-banning any main-gun randpart IDs**. The hard-ban hook still exists for future targeted testing, but its active list is intentionally empty so recipe behavior remains visible in logs.

Why this matters:

- We can still trace whether suspicious recipes fail locally or survive.
- The current tuning avoids hiding useful evidence behind broad recipe bans.
- If a recipe proves bad across multiple hulls, it can be added back as a targeted, reversible rule.

Important principle:

- A randpart being applicable does not mean it is good.
- We use repeated failure evidence, not just tag matching, to decide whether a recipe deserves a future targeted ban.
- Current non-hard-ban skips still exist for broader categories such as torpedoes on CA/BC/BB hulls and unsupported "gun other" recipes.

### Main-Gun Recipe Ordering

The generator now prioritizes a more reliable placement order around core ship requirements: towers, funnels, main guns, then remaining recipes.

Why this matters:

- Required structure appears earlier in the attempt.
- Main-gun failure becomes visible sooner.
- Fast retry can stop hopeless attempts earlier instead of waiting for a full late-stage failure.

This is meant to improve generation flow without rewriting the entire randpart system.

### Better Generated Gun Armor Handling

Generated guns have their own armor values separate from the ship's general armor settings. We added a narrow sync step so generated turret armor can be repaired after guns are placed without re-running broad hull adjustment logic.

Why this matters:

- Reduces the chance of generated guns ending up with stale or zero armor values.
- Keeps the fix narrowly scoped to gun armor.
- Avoids reintroducing larger post-parts adjustment behavior that was causing other design-generation problems.

## Expected Benefits

### Faster Turns During Heavy Generation

From the archived test run, bypassing the soft main turret/barrel count gate would have saved roughly **2.5 minutes** across 46 generated designs.

This is not a universal benchmark, but it shows the change targets a real source of wasted work.

### Fewer Dead-End Designs

The generator should spend less time chasing "perfect" early layouts that some hulls cannot reliably place. It should accept more designs that are battle-ready enough for campaign use.

### Cleaner Debugging

The logs now better separate:

- hard failures, such as no required main gun
- soft layout misses, such as fewer turrets or barrels than requested
- armor sync behavior
- campaign start skip behavior

This should make future tuning faster and less guessy.

## Guardrails

The changes are intentionally conservative:

- Blank Slate only applies when explicitly selected.
- The main-gun count bypass does not allow ships with no required main gun.
- Main-gun randpart hard bans are currently disabled; suspicious recipes remain visible for tracing.
- Category-level randpart filters remain available where they protect the generator from known bad broad classes.
- AI design service experiments remain disabled by default.
- The turret armor fix syncs gun armor only; it does not broadly reshape the ship.
- The behavior is controlled by parameters so it can be disabled during testing if needed.

## Current Defaults

Current intended defaults:

- Blank Slate support: enabled as an option.
- Skip prewarm shipbuilding: enabled only when Blank Slate is selected.
- Ignore strict main turret/barrel counts: enabled.
- Require actual main guns: still enforced.
- Known-bad main-gun randpart recipes: not currently hard-banned.
- Main-gun-focused randpart ordering: enabled.
- AI design service: disabled.
- Shipgen debug logging: available, but quiet unless enabled.

## What To Watch In Testing

### Good Signs

- Blank Slate campaigns start without generated fleets.
- Auto-generated campaigns still generate ships normally.
- Fewer repeated failures for early BB/CA hulls.
- Suspicious main-gun recipes are visible in logs when selected, placed, skipped, or failed.
- Failures still appear when a ship truly has no required main gun.
- Generated designs look imperfect but usable, not empty or weaponless.
- Turn times during ship-generation-heavy months trend lower.

### Risk Areas

- Some early designs may look under-gunned compared with hull metadata expectations.
- A suspicious randpart recipe may still fail repeatedly because we are currently tracing rather than banning it.
- The turret armor sync still depends on meaningful source armor values being available.
- If a hull consistently accepts too-weak layouts, it may need targeted hull/randpart tuning rather than a global rule change.

## Status

The source has been updated and the staged build passed with zero errors.

The built DLL has not been copied into the live game folder as part of this brief.
