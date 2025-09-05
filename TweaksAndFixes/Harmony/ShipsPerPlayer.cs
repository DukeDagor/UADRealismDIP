using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;

#pragma warning disable CS8625

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(ShipsPerPlayer))]
    internal class Patch_ShipsPerPlayer
    {
        private static readonly List<Ship.Store> _ShipOptions = new List<Ship.Store>();

        [HarmonyPatch(nameof(ShipsPerPlayer.RandomShipOfType))]
        [HarmonyPrefix]
        internal static bool Prefix_RandomShipOfType(ShipsPerPlayer __instance, ref Ship.Store __result, Player player, ShipType shipType)
        {
            // Melon<TweaksAndFixes>.Logger.Error($"{player.Name(false)} has {__instance.validDesigns.Count} valid designs.");
        
            if (!__instance.shipsPerType.ContainsKey(shipType.name))
            {
                Melon<TweaksAndFixes>.Logger.Error($"{player.Name(false)} has no valid predefined designs of type {shipType}! Either fill the predef file with this shiptype or enable `taf_force_no_predef_designs`.");
                return false;
            }

            var shipList = __instance.shipsPerType[shipType.name];
        
            if (shipList.Count == 0) return false;
        
            int index = UnityEngine.Random.RandomRange(0, shipList.Count - 1);
        
            __result = shipList[index];

            return false;
        }

        // Patching this rather than PlayerController.CampaignCanUsePredefinedDesign
        // so that we only need to jump out to managed code once, and collect techs once.
        // [HarmonyPatch(nameof(ShipsPerPlayer.RandomShipOfType))]
        // [HarmonyPostfix]
        // internal static void Postfix_RandomShipOfType(ShipsPerPlayer __instance, Player player, ShipType shipType, ref Ship.Store __result)
        // {
        //     // Some designs are marked "invalid" for one reason or another.
        //     //   I don't give a crap about missing techs or whatever, just gimme the damn ship.
        //     if (__result == null)
        //     {
        //         var shipList = __instance.shipsPerType[shipType.name];
        // 
        //         if (shipList.Count == 0) return;
        // 
        //         int index = UnityEngine.Random.RandomRange(0, shipList.Count - 1);
        // 
        //         __result = shipList[index];
        //     }
        // 
        //     // if (IgnoreTechCheckForNextRandomShip)
        //     // {
        //     //     IgnoreTechCheckForNextRandomShip = false;
        //     //     return;
        //     // }
        //     // 
        //     // if (!Config.DontClobberTechForPredefs)
        //     //     return;
        //     // 
        //     // if (CampaignControllerM.TechMatchRatio(__result) < 0)
        //     // {
        //     //     _ShipOptions.Clear();
        //     //     foreach (var s in __instance.validDesigns)
        //     //     {
        //     //         if (s == __result || CampaignControllerM.TechMatchRatio(s) < 0)
        //     //             continue;
        //     //         _ShipOptions.Add(s);
        //     //     }
        //     //     if (_ShipOptions.Count > 0)
        //     //         __result = _ShipOptions.Random();
        //     //     else
        //     //         __result = null;
        //     // }
        // }
    }
}
