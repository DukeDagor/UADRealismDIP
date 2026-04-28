# Shipgen Problem Hulls

Working tracker for hulls that repeatedly fail, run slowly, or produce suspicious ship-generation behavior.

Evidence source: `E:\SteamLibrary\steamapps\common\Ultimate Admiral Dreadnoughts\MelonLoader\Latest.log`

Current observed build: `TAF-RC7_GG_Patch_gg154`

## Current Log Summary

- Shipgen results observed: 72
- Confirmed failures: 5
- Confirmed retries: 9
- Partial main turret/barrel failures: none observed
- Vanilla gun-count validation skip is active: `call_vanilla_validate_guns_skipped`
- Main remaining failure class: hard `gun_main=0` or invalid part layouts
- Main remaining performance class: expensive randpart placement/update, especially secondary gun placement

## Confirmed Problem Hulls

| Ship type | Hull | Model | Nation | Year | Symptom | Evidence | Current note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| BB | `b1_russiaold` | `brandenburg_hull_a` | Spain; Russia | 1892, 1893, 1895 | Failed once after 4 attempts; later succeeded | Failure: `49.1s`, `attempts=4/4`, `rejected=invalid parts=2`; repeated retries for `gun_main=0`; later successes in `10.2s` and `15.6s` | Intermittent. Watch gun placement and invalid-part cleanup rather than treating as always broken. |
| BB | `bb_exp_iron2` | `inflex_hull_g` | Italy | 1892 | Hard main-gun failure | Failure: `13.8s`, `attempts=4/4`, `rejected=unmet reqs: gun_main=0 (1~-1)` | Real missing-main-gun problem. The partial turret/barrel bypass does not and should not hide this. |
| CA | `ca_maine_threemast` | `maine_hull_i` | Spain; Greece | 1890, 1895 | Invalid parts once; later succeeded | Failure: `1.0s`, `attempts=1/1`, `rejected=invalid parts=2`, no randpart summary; later success in `12.3s` | Fast failure, but intermittent. Likely early invalid layout/state rather than expensive randpart churn. |
| CL | `cl_1_medium_germany` | `cressy_hull` | Germany | 1890 | Invalid parts | Failure: `1.6s`, `attempts=1/1`, `rejected=invalid parts=3` | Fast failure. Add to watchlist for Cressy-family invalid layouts. |
| TB | `tb_standard` | `jap_tb_hull` | USA; France; Britain; Japan; Spain; Netherlands | 1890, 1891, 1892, 1895 | Invalid parts in one run; successful in others | Failure: `1.1s`, `attempts=1/1`, `rejected=invalid parts=6`; other successes around `2.5-3.5s` | Intermittent. Keep on watchlist, but not clearly a hull-level hard failure. |

## Slow But Successful Hulls

| Ship type | Hull | Model | Nation | Year | Symptom | Evidence | Current note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| BB | `b1_inflex` | `inflex_hull_a` | Spain; Japan | 1891, 1892 | Repeated slow successes; sometimes needs a retry | Successes: `23.4s`, `26.7s`, `15.6s`; retry reason seen: `funnel=0`; placement/update often around `8-11s` | Main cost is randpart placement/update. Gun placement is also heavy, but this hull usually succeeds. |
| BB | `b1_inflex_2` | `inflex_hull_b` | Japan; Spain | 1891, 1892 | Slow success | Successes: `11.6s`, `16.4s`; `gun_main` around `4.6-5.4s`; addparts placement around `5.9-8.8s` | Similar family behavior to `b1_inflex`, but less severe. |
| BB | `b_1_italian_Large_3mast` | `irresistible_hull_b_var3` | Italy | 1892 | Very slow success | Success: `26.6s`; `gun_sec=16.7s`; `grs_gap_before_state_09_wait_update_parts=20.9s`; `addparts_state_02_place_parts=18.8s` | Current biggest confirmed successful performance outlier. Secondary gun placement is the obvious suspect. |
| BB | `b2_friedrich` | `friedrich_hull_a` | Germany | 1895 | Very slow success | Success: `25.5s`; `gun_ter=8.4s`; randpart summary `15.1s`; `grs_gap_before_state_09_wait_update_parts=15.3s`; `addparts_state_01_select_randpart=9.3s`; `addparts_state_02_place_parts=5.3s` | Tertiary gun selection/placement looks unusually expensive. |
| BB | `b_1_usa_var` | `maine_hull_a` | Netherlands | 1896 | Very slow success | Success: `24.6s`; `gun_sec=10.2s`; randpart summary `18.1s`; `grs_gap_before_state_09_wait_update_parts=18.1s`; `addparts_state_02_place_parts=13.7s` | Secondary gun placement is the obvious suspect. |
| BB | `b_2_austria` | `irresistible_hull_a` | Austria | 1896 | Slow success | Success: `19.0s`; `gun_sec=3.8s`; `gun_ter=4.2s`; randpart summary `10.6s`; `addparts_state_02_place_parts=8.5s` | Slow but less pathological than the 24-26s group. Watch if it repeats. |
| BB | `b_1` | `irresistible_hull_b` | Britain | 1891 | Moderately slow success | Success: `12.8s`; `gun_sec=5.0s`; placement/update around `8.7s` | Not broken, but shares the slow secondary-placement pattern. |
| BB | `b_1_usa_var_exp1` | `dreadnought_hull_c_oldbb` | USA | 1894 | Moderately slow success | Success: `13.9s`; placement/update around `6.9s`; addparts placement `4.1s` | Watch only if repeated. Not currently a top offender. |

## Watchlist

| Ship type | Hull | Model | Reason |
| --- | --- | --- | --- |
| CA | `ca_1_rambow_france` | `bouvet_hull_d_var` | Success took `9.0s`, with no main guns placed in summary. May be valid for type/year, but worth checking design quality. |
| CA | `ca_1_small` | `irresistible_hull_d` | Repeated successes around `6.7-7.0s`; not bad, but useful baseline for CA placement cost. |
| BB | `b_1_russia` | `irresistible_hull_b` | Success in `10.7s`; not bad, but same model family as other slower hulls. |
| BB | `b_1_usa` | `irresistible_hull_b` | Success in `9.0s`; not bad, but shares the same model family. |

## Current Non-Hull Issues Affecting Diagnosis

The latest log also contains substantial exception spam:

- `KeyNotFoundException: Not Centerlined Guns`
- `Sequence contains no elements`
- many `assertion failed` entries around `Mount.get_parentPart`, `LoadModel`, and `Fits`

These may not all be caused by the hulls listed above, but they can distort performance and make shipgen logs harder to interpret. Treat exception spam cleanup as a separate diagnostic track from hull tuning.

Latest pre-restart counts:

- `KeyNotFoundException`: 10
- `Sequence contains no elements`: 72
- `assertion failed`: 9064
- `NullReferenceException`: 0

## Tuning Notes

- Do not treat `gun_main=0` as the same issue as partial turret/barrel count failures. `gun_main=0` is still a real hard failure.
- The old partial main turret/barrel gate is not currently appearing in logs.
- Main-gun randpart hard bans are currently disabled; suspicious recipes should remain visible unless we choose a targeted ban later.
- Most expensive successful cases are dominated by `grs_gap_before_state_09_wait_update_parts` and `addparts_state_02_place_parts`.
- The worst successful performance outlier so far is `b_1_italian_Large_3mast` at `26.6s`, closely followed by `b2_friedrich` at `25.5s` and `b_1_usa_var` at `24.6s`.
- Secondary gun placement is the clearest repeated slow pattern, but `b2_friedrich` showed expensive tertiary gun work instead.
