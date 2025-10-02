using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Collections.Generic;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(PlayerData))]
    internal class Patch_PlayerData
    {
        // Eventually patch here rather than prefix PostProcessAll?
        //[HarmonyPatch(nameof(PlayerData.PostProcess))]
        //[HarmonyPostfix]
        //internal static void Postfix_Flag(PlayerData __instance)
        //{
        //}

        internal static void PatchPlayerMaterials()
        {
            Melon<TweaksAndFixes>.Logger.Msg($"************************************************** Patching Player Materials");

            foreach (var pd in G.GameData.players.Values)
            {
                if (pd.type == "major" && pd.PlayerMaterial == null)
                {
                    var newMat = UnityEngine.Resources.Load<Material>(@"Campaign UI\Materials\PlayerMaterials\player-britain");
                    if (newMat == null)
                        continue;
                    pd.PlayerMaterial = Material.Instantiate(newMat);
                    if (pd.PlayerMaterial == null)
                        continue;
                    var col = pd.highlightColor.ChangeA(0.25f);
                    pd.PlayerMaterial.color = col;
                    Melon<TweaksAndFixes>.Logger.Msg($"Applying major-player material to {pd.name}, color = {col}");
                }
            }
        }
    }
}
