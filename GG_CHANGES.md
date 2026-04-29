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

- Designs screen show designs from all countries not just players
  - AI designs are only viewable, player can't use / change them
  - Also added basic deployment stats - how many ships are active/building/repairing instead of just active

### Design Generation

- This was probably where I spent most of the time trying to balance speed vs variety vs usability

- Tried many different approaches - making improvements to TAF algorithm, making my own algorithm, running ship generation in background... you name it. In the end the simplest solution turned out to be simplest one

- I effectively reverted the game to use Vanilla ship generator but with few small improvements
  - Ported armor generation logic from TAF to have AI generate reasonable armor
  - Ported parts of TAF speed logic to keep ships speed not insane
  - Sanity defaults to prevent AI creating truly bizzare ships
    - TB/DDs are restricted to min beam/draught and max tonnage
    - No torps on CA and above
    - Guns are restricted to whole calibers (so no more 13.2 inches)
    - Shells are max AP with best penetrating caps chosen (to capitalize on DIP AP bias)

- It works.. surprisingly well.. there are still few issues that I am seeing but I can live with those
  - Wacky gun placement - can still happen but to me that's part of the charm instead of always looking at the same ship layouts - most of these are reasonable and not entirely broken anyway
  - Some hulls / tech level combinations are super frustrating and don't have any margins for AI to experiment - good examples are early TB hulls with 275 tons max and funnels that take up half the ship length
  - Outside of that though.. it's all fairly reasonable imho, but let me know if you find something truly broken and perhaps we can add special rules to address

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

- Better combat AI and more "combat modes"

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