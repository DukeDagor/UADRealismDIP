using HarmonyLib;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Mount))]
    internal class Patch_Mount
    {
        [HarmonyPatch(nameof(Mount.Fits))]
        [HarmonyPrefix]
        [HarmonyPatch(new Type[] { typeof(PartData), typeof(Il2CppSystem.Collections.Generic.List<string>), typeof(Il2CppSystem.Collections.Generic.List<string>) })]
        internal static bool Prefix_Fits(Mount __instance, PartData data, Il2CppSystem.Collections.Generic.List<string> demandMounts, Il2CppSystem.Collections.Generic.List<string> excludeMounts, ref bool __result)
        {
            if (Config.Param("taf_mounts_disable_hullscale", 0) == 0)
            {
                return true;
            }

            if (!__instance.enabled)
            {
                __result = false;
                return false;
            }

            var _this = __instance;

            _this.m.Clear();

            foreach (var mount in data._mounts_k__BackingField)
            {
                if ((demandMounts == null || demandMounts.Contains(mount)) &&
                    (excludeMounts == null || !excludeMounts.Contains(mount)))
                {
                    _this.m.Add(mount);
                }
            }

            if (_this.center)
            {
                __result =
                    _this.m.Contains("center") &&
                    (_this.caliberMin == 0.0f || _this.caliberMin <= data.GetCaliberInch()) &&
                    (_this.caliberMax == 0.0f || _this.caliberMax >= data.GetCaliberInch()) &&
                    (_this.barrelsMin == 0 || !data._isGun_k__BackingField || _this.barrelsMin <= data.barrels) &&
                    (_this.barrelsMax == 0 || !data._isGun_k__BackingField || _this.barrelsMax >= data.barrels);
                return false;
            }
            else if (_this.side)
            {
                __result =
                    _this.m.Contains("side") &&
                    (_this.caliberMin == 0.0f || _this.caliberMin <= data.GetCaliberInch()) &&
                    (_this.caliberMax == 0.0f || _this.caliberMax >= data.GetCaliberInch()) &&
                    (_this.barrelsMin == 0 || !data._isGun_k__BackingField || _this.barrelsMin <= data.barrels) &&
                    (_this.barrelsMax == 0 || !data._isGun_k__BackingField || _this.barrelsMax >= data.barrels);
                return false;
            }
            else if (_this.barbette)
            {
                __result =
                    _this.m.Contains("barbette") &&
                    (_this.caliberMin == 0.0f || _this.caliberMin <= data.GetCaliberInch()) &&
                    (_this.caliberMax == 0.0f || _this.caliberMax >= data.GetCaliberInch()) &&
                    (_this.barrelsMin == 0 || !data._isGun_k__BackingField || _this.barrelsMin <= data.barrels) &&
                    (_this.barrelsMax == 0 || !data._isGun_k__BackingField || _this.barrelsMax >= data.barrels);
                return false;
            }
            else if (_this.casemate)
            {
                __result =
                    _this.m.Contains("casemate") &&
                    (_this.caliberMin == 0.0f || _this.caliberMin <= data.GetCaliberInch()) &&
                    (_this.caliberMax == 0.0f || _this.caliberMax >= data.GetCaliberInch()) &&
                    (_this.barrelsMin == 0 || !data._isGun_k__BackingField || _this.barrelsMin <= data.barrels) &&
                    (_this.barrelsMax == 0 || !data._isGun_k__BackingField || _this.barrelsMax >= data.barrels);
                return false;
            }
            else if (_this.towerMain)
            {
                __result = _this.m.Contains("tower_main");
                return false;
            }
            else if (_this.towerSec)
            {
                __result = _this.m.Contains("tower_sec");
                return false;
            }
            else if (_this.funnel)
            {
                __result = _this.m.Contains("funnel");
                return false;
            }
            else if (_this.siBarbette)
            {
                __result =
                    _this.m.Contains("si_barbette") &&
                    (_this.caliberMin == 0.0f || _this.caliberMin <= data.GetCaliberInch()) &&
                    (_this.caliberMax == 0.0f || _this.caliberMax >= data.GetCaliberInch()) &&
                    (_this.barrelsMin == 0 || !data._isGun_k__BackingField || _this.barrelsMin <= data.barrels) &&
                    (_this.barrelsMax == 0 || !data._isGun_k__BackingField || _this.barrelsMax >= data.barrels);
                return false;
            }
            else if (_this.subTorpedo)
            {
                __result = _this.m.Contains("sub_torpedo");
                return false;
            }
            else if (_this.deckTorpedo)
            {
                __result = _this.m.Contains("deck_torpedo");
                return false;
            }
            else if (_this.special)
            {
                __result = _this.m.Contains("special");
                return false;
            }

            return false;
        }
    }
}
