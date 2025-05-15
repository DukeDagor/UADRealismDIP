# NEW MANAGEMENT!
NathanKell is taking a break from development with an unknown return date. Until they return I'll be supporting future TAF development.

## TAF
My development will be focused around Brother Munro's [Dreadnought Improvement Project](https://www.nexusmods.com/ultimateadmiraldreadnoughts/mods/7?tab=description), though TAF will still be usable as a standalone mod. If you encounter any bugs while using TAF, please report them in as much detail as possible. Suggestions are closed for the time being (I have a very long list already).

## UAD Realism
I have no plans for UAD Realism, as extending TAF will be more than enough work for me to handle. 

# Tweaks And Fixes
A collection of tweaks, fixes, and moddability support features for Ultimate Admiral: Dreadnoughts.

## Supported UAD Version: 1.7.0.0

## Installation
* [Download MelonLoader 0.6.6](https://github.com/LavaGang/MelonLoader/releases/download/v0.6.6/MelonLoader.x64.zip) and unzip it to your UAD folder.
* Download the latest [TweaksAndFixes release](https://github.com/NathanKell/UADRealism/releases/latest) and unzip it to your UAD folder, which will create a Mods folder if it does not already exist. Overwrite all old files, if upgrading.
* Run the game. The first launch will be slower but subsequent launches will be normal.

Note that due to the "support old mark guns/torpedos" feature, shared design and campaign saves will become incompatible with stock UAD when they are next saved, and will need hand editing to make them compatible again.


## Player Features and Fixes
### Better shared design support
Instead of matching purely based on year, shared designs now have year windows where they are selectable by the AI. All ships meeting that window are added, even if they were built with technology unavailable to the AI at that time, so long as that technology does not affect the ship. For example, a ship without torpedoes can be selected even if the AI lacks certain torpedo techs the design was created with. Within this set of possible ships, they are ranked based on tech commonality with the AI's current tech base, so newer ships are more likely to be selected. Various parameters can be added to params to control the behavior of this feature:
* `taf_shareddesign_maxYearsIntoFuture` - the maximum number of years ahead of the current year where a design will be considered. Default 10.
* `taf_shareddesign_yearsInPastForSplit` - ships beyond this number of years in the past will be placed in a lower priority bucket to be used only if there are no newer ships, or some of these ships are very close to the oldest new ship. Default 5.
* `taf_shareddesign_yearClosenessAroundSplit` - if an older ship's creation date is closer than this to the oldest new ship, it is included as well. Default 3.
* `taf_shareddesign_bestTechValue` - if at least one ship has greater than this tech commonality, that set of ships will be randomly selected from as the shared design. Default 0.9
* `taf_shareddesign_okTechValue` - if there are no `best` ships, any ships with greater tech commonality than this will be the next selection group. Default 0.75.
* `taf_shareddesign_minTechValue` - if no better ships are available, any ships with greater tech commonality than this will be the final selection group. If no ships make even this level of commonality, no shared design is returned. Default 0.5.
Lastly, when 1890 is the selected year in the shared design constructor, only actual starting techs will be enabled for the ship, rather than any tech with 1890 as its year. This prevents 1890 shared designs being actually unavailable to the AI on campaign start.

### Keep Gun/Torpedo Marks on refit
When refitting a ship, guns and torpedo launchers will be kept at their original mark until the player actively decides to upgrade them (by clicking the Upgrade Mark button appropriate to the caliber, or by the torpedo size, respectively).

### Barbettes mount freely
Barbettes can be placed freely on the deck (like guns) rather than being snapped to the centerline or to mount points.

### Submarine rnage fix
Submarines can now travel across the map border normally rather than their operational range calculations breaking at the map border in the middle of the Pacific.

### Typo fixes for RandParts
Stock has some typos in its randParts and randPartsRefit assets, and moddders can sometimes have typos too. This detects some of those cases and fixes them.

### Fix too-large Message Boxes
For example, the War Erupts message is too long when there are new nations added. This fix will move text into a scrollbox.

### Improved peace checks
Stock has some minor bugs in its peace treaty offering checks. These are fixed, and in addition TAF will check for wars that last longer than `taf_war_max_months_for_low_vp_war` (default 12) months and have less than `taf_war_low_vp_threshold` (default 0) VP across both parties. In these cases, the peace treaty chance event will keep getting generated so the AI does not stay locked in bloodless wars. Further, the number of months after which economic collapse is checked is now configurable via `taf_war_min_months_for_econ_collapse_peace`, where in stock it is hardcoded to 24. Due to some issues, this feature defaults to disabled. Set `taf_peace_check` to 1 in params to enable the replacement peace code.


## Modder Features
### Replace/Extend/Override assets with CSV files
TAF can replace existing data with csv files, override data with csv files, or do both. TAF will look in the Mods folder (where its dll lives) for any csv files named the same as game data TextAssets (for example parts.csv or shipTypes.csv). If such a file exists, it will be loaded rather than the data in resource.assets. This allows modders to distribute just text files rather than an entire resource.assets file. In addition, TAF will look for files named assetname_override.csv (for example parts_override.csv). Data in these files will override and extend the existing data (which was loaded either from resource.assets or from csv). Any lines with the name column matching an original entry will override that entry; other lines will be appended to the end of the asset before loading. Note that the "default" line can also be overridden in this way, since it has a valid name. Note that all filenames are case sensitive!

### NEW: Config file for changing TAF behavior
Set minimum tonnage for naval invasions, disable fleet tension, set the game end date (WIP), with plenty more to come! The .cfg file is included with each release. Please ensure your .cfg file is up-to-date or the configurations will be ignored!

### Replace Language file lines
TAF will look in the Mods folder for files matching the languages that UAD loads (English.lng plus whatever the current language is). Any lines in these files will override the respective languages ingame. Note that only the changed lines need to be placed in these files, since they override one line at a time (like the xxx_override.csv files as described above).

### Don't force tech for predefined ships
The predefined designs feature in 1.6.0.7+, when used in Fast mode, forces AI tech development to never fall behind the ingame year. Set `taf_no_force_tech_with_predefs` to 1 in params to keep AI tech development unchanged even when playing with Fast designs; only designs which match the AI's current tech level will be selected in that case, by choosing a year set of ships (see below) matching the year of the newest (i.e. greatest-techyear) technology of that nation.

### Override predefined designs
UAD 1.6.0.7 introduced a new feature where the game ships with a large number of predefined ship designs that can be used during campaign start or between turns to speed up ship design (rather than generating ships from scratch). However, there is no built-in way to mod these designs. TAF allows overriding this library of ships. Three new options are added to the File Converter popup in the main menu: one that converts Shared Designs in bindesign format to text (JSON) design format, one that exports all Shared Designs into `predefinedDesigns.bin`, and one that saves predefined designs to Shared Designs (either from `predefinedDesigns.bin` if present, or from vanilla resources).

To actually override these designs, TAF offers two approaches. The simplest is to use a single `predefinedDesigns.bin` file in the Mods folder. If it exists, then TAF will load it rather than the built-in predefined designs. Note that any year that has any ships must have at least one ship for each campaign-available nation, since otherwise the game will break. To prevent this breakage, TAF will fail to load a `predefinedDesigns.bin` that has missing nation(s) in at least one of the years in question. For context, UAD ships with 20 designs per ship type per nation in the camapign start years (up to 40 for destroyers!), and 5-10 designs per ship type per nation in various other years (1893, 1914, 1927, 1946, etc) when major tech changes occur in vanilla. For this reason, saving vanilla predefined designs will take some time since there are almost 20,000 of them.

The more complex version of overriding involves creating a `predefinedDesignsData.csv` file in the Mods folder. It lets modders layer multiple sets of predefined designs together in a priority order. This allows, for example, creating a small set of curated, human-designed ships that are used at high priority by the AI (and do not need to cover all nations, all years, and all shiptypes), and a large set of autogenerated ships as a fallback. The system works by trying the highest-priority set first. If the skip chance is met, or the set does not have a ship that matches the criteria (of the given nation and type, within the year range specified) then the next set is tried, and so forth. Any number of sets can be specified. Here is an example `predefinedDesignsData.csv` file:
```
@name,filename,skipChance,yearRange
Custom,customDesigns.bin,0.25,5
Autogenerated,autoDesigns.bin,0,-1
```
The name is unused and simply for descriptive purposes. The filenmae is the file in Mods. The skipChance is the chance this set of designs will be skipped entirely; a nonzero chance means that players can still have variety in their AI opponents even if high priority sets of designs have very few designs. yearRange is the range that defines which designs are acceptable. If this is set to -1, then TAF will follow vanilla rules, trying the desired year and the closest previous year that has ships. If 0 or greater, then any year set within this many years of the desired years will be checked, in order of closest to the desired year. For non-comprehensive sets (i.e. sets that don't contain every shiptype for every nation in every year set) it is strongly recommended to not use -1.

Note that you can specify a filename of `--` which means to use the built-in predefined designs asset. This lets you layer a small set of custom, high-quality predefined designs on top of the large set of built-in designs. Referencing this file will cause issues if mod(s) are installed that change ship parts and stats such that stock designs are no longer legal or working.

### Disallow predefined designs
For mods that don't want to provide predefined designs, TAF supports the new param `taf_force_no_predef_designs`. Set that to 1 and no mode except Slow can be set on the New Game screen, meaning campaigns will work correctly even with no predefined design overrides.

### Batch ship generation
To ease in creating predefined designs, TAF reimplements the UI for the built-in batch ship generator. In the main menu, press G. You can then show the years panel and toggle on the years you wish to generate ships for, set the nation(s) to generate for, set the ship type(s), and set the number of ships per nation and per type. When you press Generate, the specified designs will be generated. At the end, you will need to quit the game and relaunch it to get back to the main menu.

### Mark 6+ guns
Both the guns and partModels TextAssets can have a `param` column added. For a given gun, any of the mark-based values can be added to the param value with a list of mark followed by value. For example the 5in gun could have a param of `firerate(6;30;7;40), shellV(6;800;7;810)`. This would make it so that the Mark 6 5in gun would have a base fire rate of 30 and shell velocity of 800, and the mark 7 would have 40 and 810 respectively. The mark 5 values are extended for all other mark-based stats if no new value is specified (so mark 5 barrel weight, accuracy, etc would be used for marks 6 and 7. This also holds true for partModels, for example param for a gun could have `model(6;hood_gun_140_x2)` and all other mark 5 parameters would be extended to mark 6 (and 7, if there is a mark 7 of that gun). These new marks can be enabled in the technologies TextAsset just like any other mark.

### Per-shiptype component selection weights
The `param` value of a component now supports `weight_per`. This takes a set of ship types and weights. For example, `weight_per(bb;20;dd;0)` means that for BB ship types, the component's selection weight will be 20, for DD types it will be 0, and for all other types it will be the regular selection weight.

### Overriding beam/draught limits during AI ship autodesigning
A ship hull can have ai_beamdraughtlimits() in its param column. ai_beamdraughtlimits takes 4 values: new beamMin, new beamMax, new draughtMin, new draughtMax. If the value is 0 for one of them, the original value is kept. For example ai_beamdraughtlimits(-1;1;-2;2) would limit the AI to offets of +/-1% each in beam and +/-2% in draught. ai_beamdraughtlimits(-1;1;0;2) would leave min draught unchanged. If you desire no offset, use a very small number like 0.001.

### Tunable conquest events
The hardcoded values in conquest events are tunable now. `taf_conquest_event_chance_mult_starting_duration` (default 0.01) and `taf_conquest_event_chance_mult_full_duration` (default 0.5) control the displayed chance and are the values used at the start and end of the duration. When it's not a land rebellion, `taf_conquest_event_add_chance` (default 0.66) is added to the chance. (This also fixes a bug where the wrong event type checking is used.) Finally `taf_conquest_event_kill_factor` (default 1.0) is the extent to which soldier kill ratio influences conquest chance.

### Sprite overriding
If the file sprites.csv exists in the Mods folder, TAF will override the specified sprites. Component type (`Components/`) and tech group (`TechGroups/`) sprites are supported for now. Example sprites.csv:
```
#resource name to override,filename,image width,image height
# For example to override the engine icon with engine.png, make a
# 64x64 PNG file and place it in Sprites, and add this line to this
# file:
#   Components/engine,engine.png,64,64
# Note that transparency in PNGs is supported, so any transparent
# pixels in the PNG will be transparent ingame.
@name,file,width,height
Components/boilers,boilers.png,64,64
```

### Flag overriding
If the file flags.csv exists in the Mods folder, TAF will override the specified flags with the specified flag files (in Mods\Flags). Example flags.csv:
```
#player name,default civil flag filename,default naval jack filename, per-government flags for civil,for naval
# the government types used by the game are: Monarchy, Democracy, Communists, Fascists
# so for example:
#   britain,britain.png,britain.png,"Communists(britainCommunist.png), Democracy(britainRepublic.png)",
# note that flags can be either PNG (no alpha channel) or JPG, and must be 256x128 pixels
@name,civilFlag,navalFlag,civilGovFlags,navalGovFlags
britain,britain.png,britain.png,,
```
Assuming you put a 256x128 png called `britain.png` in the Flags folder under Mods, then Britain's flag will be replaced in all instances by that new png. If you followed the commented example instead, then Britain's flag would be replaced by brtain.png except for its civil flag when government is communist (which would be britainCommunist.png) and when a democracy (which would be britainRepublic.png). Note that you can do this for any player, not just majors, although only majors have government-specific flags.

### Map overriding
While the ports, provinces, etc. TextAssets aren't actually loaded by the game (oddly canals is), TAF allows overriding some port and province data. Add the param `taf_override_map` to params and set it to 2, and TAF will dump the game's built in data for ports and provinces. Make a backup of these dumps, then edit them as desired. Paste the entirety of ports.csv into the ports TextAsset (and same for provinces). Then set `taf_override_map` to 1 in params. This will override port and province data with the data in those assets. This can be combined with editing players, AI admirals, relations matrix, etc, and adding flags (see above) to make other nations playable.

### Replacement armor generation behavior
TAF supports replacing the game's existing armor generating, both the defaults when switching to a new hull and the armor the auto designer creates for ships. It works by constraining armor based on a set of rules which are per-shiptype and vary by year. If the design year is between two rules, the values are interpolated between the rules that exist. If the year lies outside the rules, the nearest rule is used. For example, consider a ruleset with a battleship rule for year 1900 and a battleship rule for 1920. Any battleship designed before 1900 would use the 1900 rule, any battleship designed after 1920 would use the 1920 rule, and a battleship designed in 1915 would use numbers 3/4 of the way from the 1900 rule to alliance_changesthe 1920 rule. The rules are placed in genarmordata.csv in the Mods folder. The file format is as follows (thicknesses are in inches):
```
shipType,year,beltMin,beltMax,beltExtendedMult,turretSideMult,barbetteMult,deckMin,deckMax,deckExtendedMult,turretTopMult,ctMin,ctMax,superMin,superMax,foreAftVariation,citadelMult
bb,1890,9,14,0.5,1.1,1,2,2.5,0.5,1.2,10,15,2,4,0.1,1
bb,1940,10,15,0.5,1.2,1,5,8,0.5,1.2,10,15,2,4,0,1
```
turretSideMult is the multiplier to belt armor used as the default for turrets. barbetteMult also uses belt as its base value, but turretTopMult uses deck. The belt/deckExtendedMult values are multipliers to belt and deck used by fore/aft belt and deck areas respectively. citadelMult is the portion of maximum possible citadel armor used. foreAftVariation is the maximum multiplier to fore/aft armor by which that armor can vary, so a value of 0.02 means the fore armor can be up to +/-2% as thick as default, and aft armor can be up to -/+2% as thick as default. If `taf_genarmor_use_defaults` is set to 1, then the feature will use fallback defaults even if no `genarmordata.csv` file is present; otherwise the feature is only enabled if the file is present.

### Per-shiptype speed min/max params
TAF supports adding the following to each shipType's param: speedMultByGen_max, speedMultByGen_min, and speedMultByGen_rand. These take a list of ship generation and multiplier (or in the case of rand, maximum randomness). They can also take a single value which will apply to all ships of that type regardless of generation.

For example you could add for ca: `speedMultByGen_max(g1;1.01;g2;1.1;g3;1.02;g4;1), speedMultByGen_min(g1;0.8;g2;0.78;g3;0.82;g4;0.85), speedMultByGen_rand(0.05)` and that would, for gen2 ca hulls, have a min speed of 0.78 +/- 5% times speedLimiter and a max of 1.1 +/- 5% times speedLimiter.

Note that you can also override these on a hull-by-hull basis by putting any or all of those params in the hull's param set. So if a particular ca hull had `speedMultByGen_max(1.03)`, the max speed would be speedLimiter times 1.03 +/-5% rather than that specified in shipTypes.

By default, like stock, the max is a soft maximum: if there is weight left over after everything else, additional tonnage is spent on speed. To enforce maximum speeds, make the value negative. For example `speedMultByGen_max(-1.03)` means a multiplier of 1.03 will not be exceeded, even if there remains spare tonnage.

### Limit caliber counts and size
TAF can limit the number of calibers the autodesigner will use for a given battery (main/secondary/tertiary). Note that due to how various parts have different length multipliers, while the caliber may be kept the same, the caliber length may vary between guns. Also note that the game treats casemated guns and turreted guns differently, so a ship may have for example 6.2in secondary turrets but 6.6in casemates.

In main params:
`taf_shipgen_limit_calibercounts_main` (or sec or ter )- the default limit of calibers of that battery (main/secondary/tertiary) to use if no limit is specified in the shiptype params or hull params.

In shiptype param or hull param, you can add `calCount_main` (or sec or ter). This takes either a single value, which is used always, or a set of year;count pairs, for example `calCount_sec(1890;2;1905;0;1915;1)` or `calCount_main(1)`. Anything in a hull's param will override the shipType value for that battery. Note that the year used is the year the hull unlocks, not the year the ship was designed.

On a per-hull basis, TAF can also limit the maximum caliber the AI autodesigner is allowed to select for each battery. Add `ai_max_caliber_main(X)` (or sec or ter) to the hull's params, where X is the maximum caliber in inches that is allowed. This has no effect on the human player. Limits transfer down, so a main limit of 7in and a sec limit of 10in (or no sec limit at all) will still result in a sec limit of 7in. `ai_min_caliber_main`/`ai_min_caliber_sec`/`ai_min_caliber_ter` are also supported to set minimum calibers for these battery types.

### Improved alliance mechanics
Set taf_alliance_changes to 1 in params to enable modified alliance mechanics: When a nation A goes to war against a nation B, all A's allies declare war on B and B's allies immediately, and all B's allies declare war on A and A's allies immediately. If any nation is an ally of both A and B, it breaks its alliance with both and gets neutral relations with both.

### Allow non-home population for crew pools
Set `taf_crew_pool_colony_pop_ratio` to a nonzero number between 0 and 1. That portion of non-home population will be added to home population when calculation base crew pool and per-turn crew pool income for a nation.

### Replacement scrapping behavior
The AI fleet scrapping behavior is optionally completely replaced. Now the AI will scrap ships based on their scrap score, which is their age minus (a coefficient times their build time in months). Mothballed ships have a bonus to their score. The target tonnage is determined with a minimum base tonnage (`min_fleet_tonnage_for_scrap`) which is increased by a coefficient times (shipbuilding capacity raised to a specified power). Enable by setting `taf_scrap_enable` to 1 in params. This is a somewhat expensive calculation, so by default TAF only checks one nation per turn. If you want to run this on all nations each turn (like stock), set `taf_scrap_spread` to 0 in params. Revised scrapping can be tuned using the following params:
```
taf_scrap_hysteresis - In order to not scrap every turn, the AI will only scrap when its fleet tonnage is greater than the scrap target _plus_ this. Once scrapping, it will scrap down to the scrap target, however.
taf_scrap_buildTimeCoeff - the coefficient to build time
taf_scrap_mothballScoreAddYears - the score to add when a ship is mothballed
taf_scrap_capacityCoeff - the coefficient to (shipbuilding ^ exponent)
taf_scrap_capacityExponent - an exponent to shipbuilding capacity. This is multiplied by the coefficient to get the scrap target tonnage.
```

### Tunable mine behavior
Mine attacks on task forces have been reimplemented so they can be tuned. The following params are supported:
```
taf_mines_max_tf_per_player,-1,Max number of task forces that can be attacked by mines each turn (the same TF can be attacked any number of times by different fields and it counts only once)
taf_mines_max_tf_attacks_per_player,-1,Max number of times mines can attack task forces in a turn (each attack against a TF is a separate instance),,,,,,,
taf_mines_max_ships_per_player,-1,Max number of ships that can be attacked in a turn across all of the task forces of the player and all minefields,,,,,,,
taf_mines_max_ships_per_tf,-1,Max number of ships that can be attacked in a single task force (across any number of minefield attacks),,,,,,,
taf_mines_fleetfactor_mult_war,2,Multiplier to fleet_factor when at war with the field owner,,,,,,,
taf_mines_fleetfactor_mult_peace,2800,Multiplier to fleet_factor when not at war with the field owner,,,,,,,
taf_mines_tonnagefactor_war,5,Multiplier to task force battletonnage when at war (this is used as a cap to minefield damage),,,,,,,
taf_mines_tonnagefactor_peace,0.04,Multiplier to task force battletonnage when at war (this is used as a cap to minefield damage),,,,,,,
taf_mines_max_randomdamagefactor,300000,This is the from value when remapping damage to number of ships hit. The damage is a random value between fleet_factor to tonnagefactor times field radius divided by detection value,,,,,,,
taf_mines_default_max_ships_per_tf,10,When the random damage value is taf_mines_max_randomdamagefactor it gets remapped to this number of ships. A higher value will map to a higher number of ships; there is no clamping,,,,,,,
taf_mines_antimine_min,0.05,The min value to use for the antimine tech,,,,,,,
taf_mines_antimine_max,10,The max value to use for the antimine tech,,,,,,,
taf_mines_ship_damage_percent_min,1,The minimum percent damage to apply to the ship when it hits a mine,,,,,,,
taf_mines_ship_damage_percent_max,200,The maximum percent damage to apply to the ship when it hits a mine. This is before being multiplied by the dmg_factor_ships and by antimine and by the minefield damage mult. Values above 100 are clamped to 100 which means that the default range of 1 to 200 means half the time the ship is sunk and half the time it takes random damage between 1 and 100 percent,,,,,,,
taf_mines_crew_damage_percent_min,5,The minimum percent damage to the crew of the ship,,,,,,,
taf_mines_crew_damage_percent_max,35,The maximum percent damage to the crew of the ship. This is before being multiplied by the dmg_factor_crew and by antimine and by the minefield damage mult.,,,,,,,
```

### Version text
If `taf_versiontext` is set, the version text in the lower right corner of the screen will be replaced. 0 is the default and means no change. 1 means append the supplied text (in the str column) to the existing version string. 2 means replace the text part of the version with the text in the str column but keep the base version number. 3 means entirely replace.

### Serialization support
TAF includes a serialization library for reading and writing CSV files to managed data, as well as the ability to read to BaseData formats on demand from arbitrary files/strings. In addition, a number of the HumanXtoY methods have been reimplemented in managed code, and support exists to read a set of params and update indexed dictionaries (as used by guns, torpedos, and partModels).

### Debug features
TAF supports hot-reloading for quick iteration. set `taf_hot_reload` to 1 in params and press F9 to hot-reload certain files from csv (currently tech, components, parts, randparts). Note not all aspects of these items are reloaded, and no items can be added or removed, just have their data updated. TAF also supports dumping applicable RandParts. With hot reloading on, go to the constructor and press F10. This will dump all RandParts that fit the current hull into the console. If multiple sets of RandParts can be chosen, based on tech vars, then each set will be dumped.

# UADRealism
Realism modding for Ultimate Admiral: Dreadnoughts - coming soon, this is about TAF.

### TAF Changelog
* 3.16.7 - Update to UAD 1.6.1.4 x4
* 3.16.6 - Update to UAD 1.6.1.4
* 3.16.5 - Support enforcing max speed limits (set the multiplier in the shiptype or hull to negative)
* 3.16.4 - Default peace changes to off. Add RandPart dumping for hulls in constructor. Add support for ai_min_caliber_. Update to UAD 1.6.1.2.
* 3.16.3 - Fix peace VP difference param typo. Default white peace param to off. Update to UAD 1.6.1.1 Optx2
* 3.16.2 - Support overriding tech group sprites too. Make scrapping occur once per nation per turn, not all nations at once, for performance (controllable with param). Don't scrap during campaign generation. Update for UAD 1.6.0.9Optx3.
* 3.16.1 - Fixed typo in peace treaty code causing the player to get prompts for AI-AI peace deals
* 3.16.0 - Improve peace treaty generation to account for long wars with no or few VPs. Fix copied designs not getting their grades copied.
* 3.15.2 - Hot-reload components as well as parts/randParts. Fix "force no predefined designs" defaulting to true. Works with Optx4 or Optx5.
* 3.15.1 - Show error messages on launch if TAF is not compatible with this version of UAD, or if TAF was not installed correctly. Update to UAD 1.6.0.7 Optx4.
* 3.15.0 - Include TAFData folder, which includes localization strings for the mod. These can be overridden in <Language name>.lng in the Mods folder, like usual for loc overrides. In addition, support forcing players to only be able to choose AI Design Usage: Slow when creating campaigns, allowing mods to be used with 1.6.0.7 even without predefined designs. Starting from this version, TAF must be downloaded as an archive, not just a dll (see revised installation instructions).
* 3.14.2 - Fix too-large messageboxes (for example, the War Erupts message) by putting some text in the scrollbox
* 3.14.1 - Fix another super dumb typo: actually save the copied gun mark data when refitting ships
* 3.14.0 - New Feature: Support overlaying predefined ships sets. See readme for details (updated description of the overriding predefined ships feature). Update to 1.6.0.7 Optx2
* 3.13.0 - New Feature: Enable Batch Ship Generator. Press G in main menu, see Readme for further details
* 3.12.0 - New Features: Support overriding and exporting predefined designs. Support non-home (colony) population used for crew pool. Support not forcing AI tech to be in lockstep with the year when in Fast mode for AI design usage. Support using fallback improved armor generation rules (for vanilla or DIP) thanks to @brothermunro . Updated for 1.6.0.7.
* 3.11.2 - Updated for 1.6.0.6 Opt x5.
* 3.11.1 - Fixed an exception on startup.
* 3.11.0 - New feature: sprite overriding. For now, just component icons. See readme for details. Also fixed an oversight with the alliance changes, where a nation's allies wouldn't declare war on the other nation's allies, only the other nation. Now allies declare war on each other too.
* 3.10.0 - New feature: Set taf_alliance_changes to 1 and when one nation goes to war, all its allies will join the war immediately. Any nation allied to both sides when two nations go to war will break its alliance with both of them and have relations reset to neutral with both of them.
* 3.9.2 - Fixed a bug where the mines changes would never activate
* 3.9.1 - Fixed guns not being upgraded on instances of refitted ships even if the refit design had upgraded guns. Recompiled for 1.6.0.6, not that anything changed.
* 3.9.0 - Now displays UAD log/error messages in console, if Unity Explorer is not installed. Rewrote scrapping. Updated for 1.6.0.5Optx2 to fix a new incompatibility with that update.
* 3.8.3 - Changed how version text is replaced. Now there is only the taf_versiontext param (no more taf_versiontext_mode). Set both the value and the str columns of it.
* 3.8.2 - Rewrote per-shiptype component weights for speed. Added support for ai_beamdraughtlimits. See readme for details.
* 3.8.1 - Fixed some cases where serializing numbers to disk would not serialize in InvariantCulture
* 3.8.0 - Allow overriding of localization tokens in lng files by placing lng files in the Mods folder. See readme for details.
* 3.7.3 - [Modder Support] New Feature: read the "surv_min" param in shipTypes. It works like range_min. Rework caliber-limiting feature so it now applies per battery like caliber count limits. Params have changed, see the readme. Recompile for 1.6.0.5 (though that doesn't seem to have been necessary).
* 3.7.2 - [Modder Support] New Feature: allow replacing the version text in the lower right corner of the screen.
* 3.7.1 - Allow ai_max_caliber(X) in hull params.
* 3.7.0 - [Modder Support] New Feature: allow limiting the count of distinct calibers (per battery, i.e. main/secondary/tertiary) the autodesigner will use. Turning this feature on will slow down ship generation slightly. See readme for details.
* 3.6.0 - Implemented score-based scrapping per suggestion from @XerMGGW-2 where age of ship is weighed against its build time to determine its scrapping score. See updated scrapping section of readme for details.
* 3.5.4 - Fixed an exception generating armor. No longer allow DD/TB bulkheads to go below the second-highest value when ship generation tweaks are enabled. Fix an issue with Custom Battle flag selection where years were being compared incorrectly (and log error messages when the list of goverments-by-year is formatted incorrectly).  Describe per-shiptype speed params in the readme. Recompile for 1.6.0.4 (no changes seem needed).
* 3.5.3 - [Modder Support] New Feature: control min and max multipliers (to speedLimiter) for speed for ship types and for hulls. Fixed a bug where secondary turret armor could fail to be clamped to main gun values.
* 3.5.2 - Fixed an issue with turret armor generation when using the armor generation feature.
* 3.5.1 - Use invariant culture when parsing numbers. This means . is used for decimals and , as a thousands separator regardless of the user's country. This means that mod files will be loaded consistently.
* 3.5.0 - Rewrote asset replacing/overriding/extending and fixed some issues parsing double-quotes in CSV files. The feature is now simpler to use as well as much safer and cleaner code.
* 3.4.9 - [Modder Support] Support overriding as well as replacing the game's TextAssets, i.e. support parts_override.csv as well as parts.csv. See Readme for details.
* 3.4.8 - Fix some issues with ship generation (including zero turret armor) due to wrong stop conditions when varying speed and armor.
* 3.4.7 - Fix for 1.6.0.3R
* 3.4.6 - Rewrote barbette patch, should allow rotation and highlight properly now.
* 3.4.5 - Fixed a bug in task force mine damaging where it could break on subsequent runs when vessels sunk.
* 3.4.4 - Reworked barbette patch and fixed crash. Now no part changes are needed; barbettes behave exactly as they used to in terms of hull requirements and what is needed in their mountPoints and params, but they can be placed freely on the deck. The crash with funnels is also fixed. If it's desired to restrict barbettes to the centerline (like the old behavior), put `center` in the barbette's param column.
* 3.4.3 - [Modder Support] New Feature: when a barbette has `free` in its mountPoints (e.g. "free, si_barbette") then it can mount freely like a gun. Note that it will need excluding from old hulls via adding need/exclude tags to its params.
* 3.4.2 - Fixed a typo in AdjustHullStats leading to weird initial speeds when manually choosing a hull.
* 3.4.1 - New Feature: minor tweaks to ship autodesign, including making speed selection saner.
* 3.4.0 - [Modder Support] New Feature: allow changing the ship armor generation behavior (see readme).
* 3.3.0 - New Feature: Reimplement some ship autodesign methods, fixing some bugs
* 3.2.0 - Update to UAD 1.6.0.2
* 3.1.4 - Performance increase via changing how GameData is patched in the allow-override-csv-files patch
* 3.1.3 - Fixed highlight color not being assigned for new major nations.
* 3.1.2 - Typo fix. The ports/province overriding was loading portsDump and provincesDump and writing to ports and provinces. Oops. Fixed the reversal.
* 3.1.1 - Fixed a bug with config loading
* 3.1.0 - [Modder Support] New AI fleet scrapping behavior (disabled by default). Also fixed a bug with flags not showing in battle results and fixed a bug override-loading the relations matrix 
* 3.0.1 - Better fix for the bug where length/diameter and armor settings would be lost from guns when upgrading their mark on refit.
* 3.0.0 - [Modder Support] New Features: Support Mark 6+ guns, and support loading from csv files in Mods rather than requiring distribution of an entire resource.assets file. See Source link for readme details on these features.
* 2.5.0 - Fixed a bug where length/diameter and armor settings would be lost from guns when upgrading their mark on refit. Changed serialization behavior to fix some serializer bugs.
* 2.4.1 - Fixed an issue with map overriding on campaign start. Allow outputting map data (set taf_override_map to 2).
* 2.4.0 - [Modder Support] when taf_override_map is added to params and set to 1, the ports and provinces TextAssets will be used by the game. This allows for map modding.
* 2.3.0 - [Modder Support] New Feature: Overriding flags. Just edit a text file and add images and you're good. See here for docs.
* 2.2.0 - [Modder Support] New Feature: Allow tuning of mine behavior by means of adding params to the params file
* 2.1.1,2.1.2 - Keep torpedo marks as well, and fix an issue where the AI would keep old marks on refit.
* 2.1.0 - New feature: Keep gun marks as they are when refitting ships until the player chooses to upgrade them.
* 2.0.5 (and previous) - improvements to serializer. Supports loading BaseData (i.e. the game's own csv-format serialized data) and supports leaving default values in place if no value specified (managed serializer)
* 2.0.0 - [Modder Support] New feature: Add a CSV serializer/deserializer which works with managed (rather than Il2Cpp) classes. Code is from my KSP work.
* 1.3.2 - Enforce setting a part's CaliberInch during game load. Otherwise there's a chance that the first time it's run, it will set the wrong value (based on rounding rather than flooring the caliber), leading to, say, a 5.7in gun being treated as a 6in instead of 5in for stats/grade (mark) purposes.
* 1.3.1 - Fix typos in RandPart listings. Stock has a few of these, but the fix here will also fix most mods' typos if there are typos there too.
* 1.3.0 - When 1890 is selected in the Shared Designs constructor, only 'start' techs will be available, not techs that, while dated 1890, must be unlocked in campaign. This makes it so that the player can create shared designs the AI will use when starting an 1890 campaign.
* 1.2.2 - Fixed a bug with shared designs where ships would not be properly erased during selection
* 1.2.1 - Make shared design selection outside of campaign depend only on year, since tech matching is less reliable here (the game doesn't do it at all, for example).
* 1.2.0 - [Modder Support] New Feature: Added tunable conquest (with bugfix)
* 1.1.0 - New Feature: Added submarine movement range fix
* 1.0.0 - Initial release. Better shared design handling, per-shiptype component weights