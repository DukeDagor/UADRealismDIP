using HarmonyLib;
using UnityEngine;
using Il2Cpp;

namespace TweaksAndFixes.Harmony
{

    [HarmonyPatch(typeof(Torpedo))]
    internal class Patch_Torpedo
    {
        public static float RandomRange(float root, float range)
        {
            return (float)System.Random.Shared.NextDouble() * Math.Abs(range) + root - range / 2;
        }

        public static Torpedo reference = null;
        public static int lastTime = 0;

        [HarmonyPatch(nameof(Torpedo.Create))]
        [HarmonyPrefix]
        internal static void Prefix_Create(Part from, Vector3 torpedoStart, ref Vector3 direction, ref float duration, ref float speed)
        {
            float durationVariation = Config.Param("taf_torpedo_duration_variation", 0.1f);
            float speedVariation = Config.Param("taf_torpedo_speed_variation", 0.1f);

            duration *= RandomRange(1f, durationVariation);

            speed *= RandomRange(1f, speedVariation);

            double angle = Math.Atan2(direction.z, direction.x) / Math.PI * 180d;
            angle += RandomRange(0, 2);
            double a = Math.Sin(angle / 180d * Math.PI);
            double b = Math.Cos(angle / 180d * Math.PI);

            //Ship enemy = from.ship.GetEnemy(from.data);

            //Melon<TweaksAndFixes>.Logger.Msg($"{from.name}: (firing at {enemy.Name(false, false)})");
            //Melon<TweaksAndFixes>.Logger.Msg($"  {torpedoStart} x {direction}");
            //Melon<TweaksAndFixes>.Logger.Msg($"  {direction} = {Math.Atan2(direction.z, direction.x) / Math.PI * 180d} ~ {angle} = ({b}, 0, {a})");
            //Melon<TweaksAndFixes>.Logger.Msg($"  {duration} x {speed} = {duration * speed}");

            // 1m 30s = 25s
            // 100m/s = 84m/s
            // 20km   = 9km

            direction = new Vector3((float)b, 0, (float)a);
        }


        [HarmonyPatch(nameof(Torpedo.Create))]
        [HarmonyPostfix]
        internal static void Postfix_Create(Part from, Vector3 torpedoStart, ref Vector3 direction, ref float duration, ref float speed)
        {
            // Melon<TweaksAndFixes>.Logger.Msg($"{Torpedo.AllTorpedo.Count}");
            // 
            // foreach (Torpedo torp in Torpedo.AllTorpedo)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg($"  {torp.timer.duration}");
            // }

            Torpedo.AllTorpedo[^1].timer = new CountdownTimer(duration);
        }

        // [HarmonyPatch(nameof(Torpedo.Update))]
        // [HarmonyPrefix]
        // internal static void Prefix_Update(Torpedo __instance)
        // {
        //     if (reference == null)
        //     {
        //         reference = __instance;
        //     }
        //     else if (reference != null && reference != __instance)
        //     {
        //         return;
        //     }
        // 
        //     if (lastTime != (int)__instance.timer.pastTime)
        //     {
        //         lastTime = (int)__instance.timer.pastTime;
        //         Melon<TweaksAndFixes>.Logger.Msg($"{__instance.timer.progress}\t{__instance.timer.pastTime}/{__instance.timer.duration}");
        //     }
        // 
        //     if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"BEF {__instance.timer.progress}\t{__instance.timer.pastTime}/{__instance.timer.duration}");
        // }
        // 
        // [HarmonyPatch(nameof(Torpedo.Update))]
        // [HarmonyPostfix]
        // internal static void Postfix_Update(Torpedo __instance)
        // {
        //     if (reference != null && reference != __instance)
        //     {
        //         return;
        //     }
        // 
        //     if (Input.GetKey(KeyCode.J)) Melon<TweaksAndFixes>.Logger.Msg($"AFT {__instance.timer.progress}\t{__instance.timer.pastTime}/{__instance.timer.duration}");
        // }
    }

}
