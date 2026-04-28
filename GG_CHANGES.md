# GG Changes

Use this file to keep a running record of notable updates.

## Overall direction / motivation

I am big fan of UAD vanilla and also generally massive fan of the DIP changes, particularly the great game version.
However I've been a bit frustrated that along all the cool changes that it brought, the game became super slow so it effectivelly a choice between subpar but playable version vs superior but too slow to be enjoyable one

This fix-mod is an attempt to close that gap. I tried to avoid any major gameplay / balance changes, and mainly focused on speed and QoL. 
In cases where fidelity and speed conflicted I did bias slightly towards speed - unfortunately some of the cooler ideas in DIP are not fully possible in current game state with reasonable performance

Also - all the changes were generated using Codex - I didn't type a single character manually except for this file. If that sort of thing bothers you, sorry.

## Changes list

### Campaign creation

- added an option to bypass ship design generation completely at startup
  - new campaigns can start in seconds rather than dozens of minutes
  - surprisingly enough doesn't really change much gameplay wise since wars don't start until 3-4 years in and by that time most desings are refreshed anyway

### Campaign UI

- Added a small battle only log above normal log UI
  - this way it's easier to see who fights whom, also has tooltip on hover with more details

- Designs screen show designs from all countries not just players
  - AI designs are only viewable, player can't use / change them
  - Also added basic deployment stats - how many ships are active/building/repairing instead of just active

### Design Generation

- This was probably where I spent most of the time trying to balance speed vs variety vs usability

- As a background - the way AI ship designer works is effectivelly throwing darts and hoping some hit somewhere near the target
  - This is very fast in the basic implementation but results in the wacky and completly unreasonable designs in Vanilla
  - TAF/DIP fix this by adding bunch of restrictions and rules and that works in that it produces better designs but at a cost of much much longer processing speed
    - Basically with DIP the algorithm is 1) throw darts 2) check if they landed in very specific way 3) repeat

- Ok so the way I "fix" this is by making it a simpler problem

- Reduce variability of inputs - AI is no longer allowed to change beam/draught/tonnage
  - we max the tonnage (up to what current tech/docks allow)
  - beam is set to widest for BBs, narrowest for DD/TB, 0 for everything else
  - draught is set to shallowest for DD/TB, 0 for everything else

- Speed is clamped to whole numbers only - no more 17.4 knots

- Guns are clamped as well - no more 12.4 inch gun with 7% extra length
  - Human player can still do that but AI cannot

- Gun tech is clamped to highest available 
  - So if specific design have mark 3 and mark 2 options, mark 2s will be ignored

- Drastic algorithm for weight reduction
  - Basically anything is a fair game if it can get design to be workable - quarters/range etc

- Retry downsizing logic - if a design attempt fails to create a viable design, next attempt only uses smaller parts

- Hard floors for armor, speed and tech - basically makes sure that AI can't generate something very silly

- Hard coded preference for Max AP ammo and best pen caps
  - In DIP armor is king and pen is more valuable than anything else

- Dynamically reorder "dart throwing rules" to make sure we give us the best chance
  - In other words, some times the game is configured to put secondaries before main guns which can break certain hulls
  - The mod rearranges those on the fly

- Relaxation of minimum turrets / barrels checks
  - Certain hulls have very high minimums that are very hard to hit consistently resulting in many retries and wasted time
  - It does lead to somewhat silly designs sometimes but hey that's part of the fun 

- CA and above can't have torpedoes, yes even Japan

- Fast retry logic 
  - Basically if the ship is fundamentally broken, no reason to optimize weight - just try again sooner


### Combat

- Modified auto shell selection logic
  - This is actually a pretty massive change because of how DIP makes AP superior to HE, but AI code isn't aware of it
  - Until now that is
  - Expect to get quite a bit more damage from AI than before especially if your armor isn't up to par
  - On the other hand, expect to deal more damage as well 

- Experemntal TB AI mode
  - More like kamikaze mode

- Basically select a TB/DD/CL
- Press 'k'
- Watch fireworks
  - Any manual command will disable it

### Future ideas

- Country specific generation overrides
  - Give Japan higher basic speed floor or require more torpedoes or something

- Better combat AI

- Design caches
  - Sort of a middle ground between everything pregenerated and nothing
  - Save all good designs globally and be able to reduce across campaigns

### Things I tried and gave up on

- Generating designs in parallel thread - basically impossible with the way game is coded

- Custom design algorithm instead of throwning darts
  - Doable but damn would be a lot of work
  - Also the bigger problem is - the more deterministic it is, the less interesting the results
  - Throwing darts can lead to fun results

- Improving campaign load time - very doable but would probably break save compatibility