using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Player))]
    internal class Patch_Player
    {
        [HarmonyPatch(nameof(Player.Flag), new Type[] { typeof(PlayerData), typeof(bool), typeof(Player), typeof(int) })]
        [HarmonyPrefix]
        internal static bool Prefix_Flag(PlayerData data, bool naval, Player player, int newYear, ref Sprite __result)
        {
            var newSprite = FlagDatabase.Instance.GetFlag(data, naval, player, newYear);
            if (newSprite != null)
            {
                __result = newSprite;
                return false;
            }

            return true;
        }

        private static Dictionary<Player, float> GDPMultiplier = new();
        private static Dictionary<Player, float> GDPMultiplierNext = new();
        private static int TurnForGDPMultipler = -1;

        public static void RequestChangePlayerGDP(Player player, float multiplier)
        {
            if (TurnForGDPMultipler == -1) TurnForGDPMultipler = CampaignController.Instance.CurrentDate.turn;

            if (TurnForGDPMultipler < CampaignController.Instance.CurrentDate.turn)
            {
                if (!GDPMultiplierNext.ContainsKey(player)) GDPMultiplierNext[player] = 0;

                GDPMultiplierNext[player] += multiplier;

                // Melon<TweaksAndFixes>.Logger.Msg($"Next: {player.Name(false)} - {multiplier} -> {GDPMultiplierNext[player]} | {CampaignController.Instance.CurrentDate.turn}");
            }
            else
            {
                if (!GDPMultiplier.ContainsKey(player)) GDPMultiplier[player] = 0;

                GDPMultiplier[player] += multiplier;

                // Melon<TweaksAndFixes>.Logger.Msg($"Curr: {player.Name(false)} - {multiplier} -> {GDPMultiplier[player]} | {CampaignController.Instance.CurrentDate.turn}");
            }

        }

        public static void ResetChangePlayerGDP()
        {
            GDPMultiplier.Clear();

            TurnForGDPMultipler = CampaignController.Instance.CurrentDate.turn;

            // Melon<TweaksAndFixes>.Logger.Msg($"Reset GDP Multiplier: {CampaignController.Instance.CurrentDate.turn}");

            foreach (var pair in GDPMultiplierNext)
            {
                // Melon<TweaksAndFixes>.Logger.Msg($"  Transfer multiplier: {pair.Key.Name(false)} : {pair.Value}");
                GDPMultiplier[pair.Key] = pair.Value;
            }

            GDPMultiplierNext.Clear();
        }

        [HarmonyPatch(nameof(Player.WealthGrowthEffective))]
        [HarmonyPostfix]
        internal static void Postfix_WealthGrowthEffective(Player __instance, ref float __result)
        {
            if (GDPMultiplier.ContainsKey(__instance))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Apply event GDP% modifier to {__instance.Name(false)} : Base {__result} | Modifier {GDPMultiplier[__instance]}");
            
                __result += GDPMultiplier[__instance];
            }

            // if (CampaignController.Instance.CurrentDate.turn > TurnForGDPMultipler)
            // {
            //     ResetChangePlayerGDP();
            // }
        }

        // These are done as postfix so that TotalPopulation gets set properly
        [HarmonyPatch(nameof(Player.InitCrewPool))]
        [HarmonyPostfix]
        internal static void Postfix_InitCrewPool(Player __instance)
        {
            if (Config.UseColonyInCrewPool)
                PlayerM.InitCrewPool(__instance);
        }

        [HarmonyPatch(nameof(Player.GetBaseCrewPool))]
        [HarmonyPostfix]
        internal static void Postfix_GetBaseCrewPool(Player __instance, ref int __result)
        {
            if (Config.UseColonyInCrewPool)
                __result = PlayerM.GetBaseCrewPool(__instance);
        }

        [HarmonyPatch(nameof(Player.CrewPoolIncome))]
        [HarmonyPostfix]
        internal static void Postfix_CrewPoolIncome(Player __instance, ref int __result)
        {
            if (Config.UseColonyInCrewPool)
                __result = PlayerM.CrewPoolincome(__instance);
        }
    }
}
