using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;

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

        private static bool hasMessagedPatchingPlayerMaterials = false;

        internal static void PatchPlayerMaterials()
        {
            foreach (var pd in G.GameData.players.Values)
            {
                if (pd.type == "major" && pd.PlayerMaterial == null)
                {
                    if (!hasMessagedPatchingPlayerMaterials)
                    {
                        hasMessagedPatchingPlayerMaterials = true;
                        Melon<TweaksAndFixes>.Logger.Msg($"************************************************** Patching Player Materials");
                    }
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
