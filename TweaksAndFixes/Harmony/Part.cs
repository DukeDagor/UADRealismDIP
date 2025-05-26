using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader.NativeUtils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Startup;
using static Il2Cpp.Ship;
using static MelonLoader.MelonLogger;

#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Part))]
    internal class Patch_Part
    {
        //private static string? _MountErrorLoc = null;
        internal static bool _IgnoreNextActiveBad = false;

        [HarmonyPatch(nameof(Part.AutoRotatePart))]
        [HarmonyPrefix]
        internal static bool Prefix_AutoRotatePart(Part __instance, bool leftRight, bool forwardBack)
        {
            //if (__instance != null) Melon<TweaksAndFixes>.Logger.Msg("AutoRotatePart: " + __instance.Name() + " : " + __instance.localRotationBackup?.ToString());
            // Melon<TweaksAndFixes>.Logger.Msg("AutoRotatePart: " + __instance.Name());

            if (!Patch_Ui._InUpdateConstructor)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Not being placed by a human!");
                return true;
            }

            return false;

            // if (chosenPart != null)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg("Chosen Part: " + chosenPart.nameUi);
            // 
            // }
        }

        // [HarmonyPatch(nameof(Part.AnimateRotate))]
        // [HarmonyPrefix]
        // internal static bool Prefix_AnimateRotatet(Part __instance, float angle)
        // {
        //     if ((int)Math.Abs(angle) != (int)15)
        //     {
        //         Melon<TweaksAndFixes>.Logger.Msg("Does not equal rotation override: " + angle);
        //         return false;
        //     }
        // 
        //     return true;
        // }

        public static Part MirrorA = null;
        public static Part MirrorB = null;

        [HarmonyPatch(nameof(Part.Place))]
        [HarmonyPostfix]
        internal static void Postfix_Place(Part __instance, Vector3 pos, bool autoRotate = true)
        {
            if (!Patch_Ui._InUpdateConstructor)
            {
                Melon<TweaksAndFixes>.Logger.Msg("Not being placed by a human!");
                return;
            }

            if (__instance.visualMode == Part.VisualMode.Normal)
            {
                if (pos.x == 0.0f)
                {
                    return;
                }

                Melon<TweaksAndFixes>.Logger.Msg("Matching Parts:");

                Part placedPart = __instance;
                Part mirroredPart = null;

                foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                {
                    if (part == null) continue;
                    if (part.transform == null) continue;

                    Vector3 partPos = part.transform.position;
                    if (partPos.y != pos.y) continue;
                    if (partPos.z != pos.z) continue;

                    Melon<TweaksAndFixes>.Logger.Msg("  " + part.Name() + ": " + partPos.ToString() + " : " + part.visualMode);

                    if (part != __instance)
                    {
                        mirroredPart = part;
                    }
                }

                if (mirroredPart == null)
                {
                    Melon<TweaksAndFixes>.Logger.Msg("Part not mirrored.");
                }
                else
                {
                    Vector3 partRot = mirroredPart.transform.eulerAngles;
                    placedPart.transform.eulerAngles = new Vector3(partRot.x, -partRot.y, partRot.z);

                    if (!placedPart.CanPlace() || !mirroredPart.CanPlace())
                    {
                        Melon<TweaksAndFixes>.Logger.Msg("Failed to mirror part: Invalid mirror.");
                        return;
                    }

                    Melon<TweaksAndFixes>.Logger.Msg("Part mirrored successfully");
                    MirrorA = placedPart;
                    MirrorB = mirroredPart;
                }

                Melon<TweaksAndFixes>.Logger.Msg("");
            }
            // else if (__instance.visualMode == Part.VisualMode.Active)
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg(__instance.Name() + ": Being placed");
            // }
        }

        [HarmonyPatch(nameof(Part.SetVisualMode))]
        [HarmonyPrefix]
        internal static void Prefix_SetVisualMode(Part __instance, ref Part.VisualMode m)
        {
            // if (__instance.visualMode != m && !(m == Part.VisualMode.Highlight && __instance.visualMode == Part.VisualMode.Normal) && !(m == Part.VisualMode.Normal && __instance.visualMode == Part.VisualMode.Highlight)) Melon<TweaksAndFixes>.Logger.Msg(__instance.Name() + ": " + __instance.visualMode + " : Changed to : " + m);

            // if ((__instance == MirrorA || __instance == MirrorB) && !(m == Part.VisualMode.Normal || m == Part.VisualMode.Highlight || m == Part.VisualMode.BadOverlap || m == Part.VisualMode.Bad || m == Part.VisualMode.Warn))
            // {
            //     Melon<TweaksAndFixes>.Logger.Msg(__instance.Name() + ": Moved : " + m);
            //     if (__instance == MirrorA)
            //     {
            //         Patch_Ship.LastCreatedShip.RemovePart(MirrorB);
            //     }
            //     else
            //     {
            //         Patch_Ship.LastCreatedShip.RemovePart(MirrorA);
            //     }
            //     MirrorB = null;
            //     MirrorA = null;
            // }

            if (m == Part.VisualMode.ActiveBad && _IgnoreNextActiveBad) //Patch_Ui._InUpdateConstructor && ((Patch_Ui_c._SetBackToBarbette && __instance.data == Patch_Ui_c._BarbetteData) || __instance.data.isBarbette))
            {
                _IgnoreNextActiveBad = false;
                //if (_MountErrorLoc == null)
                //    _MountErrorLoc = LocalizeManager.Localize("$Ui_Constr_MustPlaceOnMount");

                //if (G.ui.constructorCentralText2.text.Contains(_MountErrorLoc) || G.ui.constructorCentralText2.text == "mount1")
                if (Part.CanPlaceGeneric(__instance.data, __instance.ship == null ? G.ui.mainShip : __instance.ship, true, out _) && !__instance.CanPlace(out var deny) && (deny == "mount 1" || deny == "mount1"))
                {
                    m = Part.VisualMode.Highlight;
                    //if (/*(__instance.data.isWeapon || __instance.data.isBarbette) &&*/ G.ui.placingPart == __instance)
                    //{
                    if (!Util.FocusIsInInputField())
                    {
                        if (GameManager.CanHandleKeyboardInput())
                        {
                            var b = G.settings.Bindings;
                            float angle = UnityEngine.Input.GetKeyDown(b.RotatePartLeft.Code) ? -45f :
                                UnityEngine.Input.GetKeyDown(b.RotatePartRight.Code) ? 45f : 0f;
                            if (angle != 0f)
                            {
                                __instance.transform.Rotate(Vector3.up, angle);
                                __instance.AnimateRotate(angle);
                                G.ui.OnConShipChanged(false);
                            }
                        }
                    }
                    //}
                }
            }
        }

        //[HarmonyPatch(nameof(Part.TryFindMount))]
        //[HarmonyPostfix]
        internal static void Postfix_TryFindMount(Part __instance, bool autoRotate)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Called TryFindMount on {__instance.name} ({__instance.data.name}) {(__instance.mount != null ? "Mounted" : string.Empty)}");
            if (!__instance.CanPlace(out string denyReason))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Can't place. Deny reason {(denyReason == null ? "<null>" : denyReason)}");
            }
        }
        //[HarmonyPatch(nameof(Part.Mount))]
        //[HarmonyPostfix]
        internal static void Postfix_Mount(Part __instance, Mount mount)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"Mounting part {__instance.name} to {(mount == null ? "<<nothing>>" : (mount.parentPart == null ? (mount.name + " (no parent)") : (mount.name + " on " + mount.parentPart.name)))}");
        }
    }

    // We can't target ref arguments in an attribute, so
    // we have to make this separate class to patch with a
    // TargetMethod call.
    // [HarmonyPatch(typeof(Part))]
    // internal class Patch_Part_CanPlaceGeneric
    // {
    //     internal static MethodBase TargetMethod()
    //     {
    //         //return AccessTools.Method(typeof(Part), nameof(Part.CanPlace), new Type[] { typeof(string).MakeByRefType(), typeof(List<Part>).MakeByRefType(), typeof(List<Collider>).MakeByRefType() });
    // 
    //         // Do this manually
    //         var methods = AccessTools.GetDeclaredMethods(typeof(Part));
    //         foreach (var m in methods)
    //         {
    //             if (m.Name != nameof(Part.CanPlaceGeneric))
    //                 continue;
    // 
    //             return m;
    //         }
    // 
    //         return null;
    //     }
    // 
    //     internal static bool Prefix(Part __instance, PartData data, Ship ship, bool partIsReal, string denyReason, ref bool __result)
    //     {
    //         if (__instance == null)
    //         {
    //             Melon<TweaksAndFixes>.Logger.Msg("Skipping check can place!");
    //             __result = false;
    //             return false;
    //         }
    //         return true;
    //     }
    // }

    // We can't target ref arguments in an attribute, so
    // we have to make this separate class to patch with a
    // TargetMethod call.
    // [HarmonyPatch(typeof(Part))]
    // internal class Patch_Part_CanPlace
    // {
    //     internal static MethodBase TargetMethod()
    //     {
    //         //return AccessTools.Method(typeof(Part), nameof(Part.CanPlace), new Type[] { typeof(string).MakeByRefType(), typeof(List<Part>).MakeByRefType(), typeof(List<Collider>).MakeByRefType() });
    // 
    //         // Do this manually
    //         var methods = AccessTools.GetDeclaredMethods(typeof(Part));
    //         foreach (var m in methods)
    //         {
    //             if (m.Name != nameof(Part.CanPlace))
    //                 continue;
    // 
    //             if (m.GetParameters().Length == 3)
    //                 return m;
    //         }
    // 
    //         return null;
    //     }
    // 
    //     internal static bool Prefix(Part __instance, ref bool __result) //, out List<Part> overlapParts, out List<Collider> overlapBorders)
    //     {
    //         if (__instance == null)
    //         {
    //             Melon<TweaksAndFixes>.Logger.Msg("Skipping check can place!");
    //             __result = false;
    //             return false;
    //         }
    //         return true;
    //         // We could try to be fancier, but let's just clobber.
    //         // Note we won't necessarily be in the midst of the barbette patch, so
    //         // we can't rely on checking that. But it's possible the reset failed,
    //         // so we take the setback case too.
    //         //if (Patch_Ui._InUpdateConstructor && ((Patch_Ui_c._SetBackToBarbette && __instance.data == Patch_Ui_c._BarbetteData) || __instance.data.isBarbette))
    //         //{
    //         //    if (denyReason == "mount1")
    //         //        __result = true;
    //         //}
    // 
    //     }
    // }
}
