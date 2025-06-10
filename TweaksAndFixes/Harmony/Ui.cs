using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using static TweaksAndFixes.ModUtils;
using Il2CppSystem;
using System.Reflection.Metadata.Ecma335;
using static MelonLoader.MelonLogger;

#pragma warning disable CS8604
#pragma warning disable CS8625
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(Ui))]
    internal class Patch_Ui
    {
        internal static bool _InUpdateConstructor = false;

        [HarmonyPatch(nameof(Ui.Start))]
        [HarmonyPostfix]
        internal static void Postfix_Start(Ui __instance)
        {
            UpdateVersionString(__instance);
        }

        [HarmonyPatch(nameof(Ui.RefreshVersion))]
        [HarmonyPostfix]
        internal static void Postfix_RefreshVersion(Ui __instance)
        {
            UpdateVersionString(__instance);
        }

        internal static void UpdateVersionString(Ui ui)
        {
            if (G.GameData == null || G.GameData.paramsRaw == null || G.GameData.parms == null)
                return;

            int mode = (int)(Config.Param("taf_versiontext", 0f) + 0.01f);
            if (mode == 0)
                return;

            var vt = ui.overlayUi.Get("Version", false, false).Get<Text>("VersionText", false, false);
            string? text = Config.ParamS("taf_versiontext", string.Empty);
            switch (mode)
            {
                case 1: text = vt.text + " " + text; break;
                case 2: text = GameData.GameVersion + " " + text; break;
                // default: entirely replace
                case 4: text = string.Empty; break;
            }
            vt.text = text;
        }

        [HarmonyPatch(nameof(Ui.ChooseComponentType))]
        [HarmonyPrefix]
        internal static void Prefix_ChooseComponentType()
        {
            SpriteDatabase.Instance.OverrideResources();
        }

        [HarmonyPatch(nameof(Ui.CheckForPeace))]
        [HarmonyPrefix]
        internal static bool Prefix_CheckForPeace(Ui __instance)
        {
            if (!Config.PeaceCheckOverride)
                return true;

            UiM.CheckForPeace(__instance);
            return false;
        }

        [HarmonyPatch(nameof(Ui.UpdateConstructor))]
        [HarmonyPrefix]
        internal static void Prefix_UpdateConstructor()
        {
            _InUpdateConstructor = true;
            Patch_Ui_c.Postfix_16(); // just in case we somehow died after running b15 and before b16
        }

        public static bool nearlyEqual(float a, float b)
        {
            return (int)(Il2CppSystem.Math.Round(a * 1.0f) + 0.01) == (int)(Il2CppSystem.Math.Round(b * 1.0f) + 0.01);
        }

        public static Part SelectedPart = null;
        public static bool NeedsConstructionListsClear = false;
        public static float PartRotation = 0.0f;
        public static float RotationValue = 15.0f;
        public static float DefaultRotation = 0.0f;
        public static bool FixedRotation = false;
        public static bool FixedRotationValue = false;
        public static bool SideGun = false;
        public static bool Casemate = false;
        public static bool Mounted = false;
        public static bool MainTower = false;
        public static bool SecTower = false;
        public static bool Funnel = false;
        public static bool UnderwaterTorpedo = false;

        public static bool UseNewConstructionLogic()
        {
            return _InUpdateConstructor;
        }

        public static void UpdateSelectedPart(Part part)
        {
            // Track the part selected from the toolbox
            if (SelectedPart == null || SelectedPart != part)
            {
                SelectedPart = part;
                Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + SelectedPart.Name() + " : " + SelectedPart.data.type + " : " + SelectedPart.data.name);
            
                Casemate = SelectedPart.data.name.StartsWith("casemate");
                SideGun = SelectedPart.data.name.EndsWith("side");
                UnderwaterTorpedo = SelectedPart.data.name.EndsWith("x0");
                MainTower = SelectedPart.data.isTowerMain;
                SecTower = !SelectedPart.data.isTowerMain && SelectedPart.data.isTowerAny;
                Funnel = SelectedPart.data.isFunnel;

                if (Funnel)
                {
                    PartRotation = 0;
                    FixedRotation = true;
                    FixedRotationValue = true;
                    RotationValue = 0;
                }
                else if(MainTower)
                {
                    PartRotation = 0;
                    FixedRotation = false;
                    RotationValue = 180;
                    FixedRotationValue = true;
                }
                else if (SecTower)
                {
                    PartRotation = 180;
                    FixedRotation = false;
                    RotationValue = 180;
                    FixedRotationValue = true;
                }
                else if (Casemate)
                {
                    FixedRotation = false;
                    PartRotation = 0;
                    FixedRotationValue = false;
                    RotationValue = 45;
                }
                else if (UnderwaterTorpedo)
                {
                    FixedRotation = true;
                    PartRotation = 0;
                    FixedRotationValue = true;
                    RotationValue = 0;
                }
                else
                {
                    FixedRotation = false;
                    FixedRotationValue = false;
                    RotationValue = 45;
                }
            }
        }

        [HarmonyPatch(nameof(Ui.UpdateConstructor))]
        [HarmonyPostfix]
        internal static void Postfix_UpdateConstructor()
        {
            if (NeedsConstructionListsClear)
            {
                Patch_Part.applyMirrorFromTo.Clear();
                Patch_Part.mirroredParts.Clear();
                Patch_Part.unmatchedParts.Clear();
                NeedsConstructionListsClear = false;
            }

            if (UseNewConstructionLogic() && Patch_Ship.LastCreatedShip.parts.Count > 0)
            {
                Patch_Part.TrySkipDestroy = null;
                Part toRemove = null;

                // foreach (Part part in G.ui.placedPartsWarn)
                // {
                //     if (part == null || !part.data.isGun) continue;
                //     Part.FireSectorInfo info = new Part.FireSectorInfo();
                //     part.CalcFireSectorNonAlloc(info);
                //     if (info.shootableAngleTotal < 90) continue;
                //     toRemove = part;
                // }
                // 
                // if (toRemove != null)
                // {
                //     G.ui.placedPartsWarn.Remove(toRemove);
                //     toRemove = null;
                // }
                // 
                // if (G.ui.placedPartsWarn.Count == 0)
                // {
                //     
                // }

                // Update mirrored pairs since mounts have a mind of their own
                foreach (Il2CppSystem.Collections.Generic.KeyValuePair<Part, Part> pair in Patch_Part.applyMirrorFromTo)
                {
                    if (!Patch_Ship.LastCreatedShip.parts.Contains(pair.Key) || !Patch_Ship.LastCreatedShip.parts.Contains(pair.Value))
                    {
                        Melon<TweaksAndFixes>.Logger.Msg("Error: Failed to find parts for mirroring:");
                        Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Value.Name());
                        Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Key.Name());
                        toRemove = pair.key;
                        // Patch_Part.applyMirrorFromTo.Remove(pair.Key);
                        continue;
                    }

                    Vector3 partRot = pair.Key.transform.eulerAngles;
                    pair.Value.transform.eulerAngles = new Vector3(partRot.x, -partRot.y, partRot.z);
                }

                if (toRemove != null)
                {
                    Patch_Part.applyMirrorFromTo.Remove(toRemove);
                }

                // Loop over all current parts
                foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                {
                    if (part == null) continue;

                    if (part == SelectedPart) continue;

                    if ((int)part.transform.position.x == 0) continue;

                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.Name() + " : " + part.visualMode + " : " + part.transform + " : " + part.hasModel);

                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.gameObject.GetChildren()[0].GetChild("Visual").GetComponent<Renderer>().material.color.ToString());
                    // Melon<TweaksAndFixes>.Logger.Msg("Selected part: " + part.gameObject.GetComponent<Renderer>());

                    // Check if part is still mirrored
                    if (Patch_Part.mirroredParts.ContainsKey(part))
                    {
                        Part pair = Patch_Part.mirroredParts[part];
                        bool unpair = false;

                        if (!nearlyEqual(Il2CppSystem.Math.Abs(part.transform.position.x), Il2CppSystem.Math.Abs(pair.transform.position.x))) // Starbord/port
                        {
                            unpair = true;
                        }
                        else if (!nearlyEqual(part.transform.position.y, pair.transform.position.y)) // Up/down
                        {
                            unpair = true;
                        }
                        else if (!nearlyEqual(part.transform.position.z, pair.transform.position.z)) // Fore/aft
                        {
                            unpair = true;
                        }

                        if (unpair)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg("Unpairing: ");
                            Melon<TweaksAndFixes>.Logger.Msg("  " + part.Name());
                            Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Name());
                            Patch_Part.mirroredParts.Remove(part);
                            Patch_Part.mirroredParts.Remove(pair);
                            Patch_Part.unmatchedParts.Add(part);
                            Patch_Part.unmatchedParts.Add(pair);
                        }
                    
                        continue;
                    }

                    // Add unmirrored parts to unmatched parts
                    if (!Patch_Part.unmatchedParts.Contains(part))
                    {
                        Patch_Part.unmatchedParts.Add(part);
                    }

                    // Melon<TweaksAndFixes>.Logger.Msg("Check for new mirrors: ");

                    // Check for new mirrors
                    for (int i = Patch_Part.unmatchedParts.Count - 1; i >= 0; i--)
                    {
                        Part pair = Patch_Part.unmatchedParts[i];
                        bool found = true;

                        if (pair == part) continue;
                        if (!Patch_Ship.LastCreatedShip.parts.Contains(pair))
                        {
                            Patch_Part.unmatchedParts.Remove(pair);
                            continue;
                        }
                        if ((int)pair.transform.position.x == 0) continue;

                        if (!nearlyEqual(Il2CppSystem.Math.Abs(part.transform.position.x), Il2CppSystem.Math.Abs(pair.transform.position.x))) found = false;
                        else if (!nearlyEqual(part.transform.position.y, pair.transform.position.y)) found = false;
                        else if (!nearlyEqual(part.transform.position.z, pair.transform.position.z)) found = false;

                        if (found)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg("Pairing: ");
                            Melon<TweaksAndFixes>.Logger.Msg("  " + part.Name());
                            Melon<TweaksAndFixes>.Logger.Msg("  " + pair.Name());
                            Patch_Part.mirroredParts.Add(pair, part);
                            Patch_Part.mirroredParts.Add(part, pair);
                            Patch_Part.unmatchedParts.Remove(part);
                            Patch_Part.unmatchedParts.Remove(pair);
                            break;
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.LeftAlt) && !FixedRotationValue)
                {
                    RotationValue += 15.0f;
                    if (RotationValue - 0.1 >= 45.0f)
                    {
                        RotationValue = 15.0f;
                    }
                    Melon<TweaksAndFixes>.Logger.Msg("Rotation inc: " + RotationValue);
                }
                
                if (SelectedPart != null && Input.GetKeyDown(KeyCode.R) && !FixedRotation)
                {
                    PartRotation -= RotationValue;
                    SelectedPart.AnimateRotate(-RotationValue);
                    // Melon<TweaksAndFixes>.Logger.Msg("Rotate: " + SelectedPart.transform.eulerAngles.y);
                }
                else if (SelectedPart != null && Input.GetKeyDown(KeyCode.T) && !FixedRotation)
                {
                    PartRotation += RotationValue;
                    SelectedPart.AnimateRotate(RotationValue);
                    // Melon<TweaksAndFixes>.Logger.Msg("Rotate: " + SelectedPart.transform.eulerAngles.y);
                }
                else if (SelectedPart != null && Input.GetKeyDown(KeyCode.F) && !FixedRotation)
                {
                    PartRotation = (SelectedPart.transform.position.z > 0 || Mounted) ? 0 : 180;
                    // Melon<TweaksAndFixes>.Logger.Msg("Auto rotate: " + SelectedPart.transform.eulerAngles.y);
                }
                else if (SelectedPart == null && Input.GetKeyDown(KeyCode.G))
                {
                    // foreach (Mount mount in Patch_Ship.LastCreatedShip.mounts)
                    // {
                    //     string mConcat = "";
                    // 
                    //     foreach (string str in mount.m) mConcat += str + ", ";
                    // 
                    //     if (mConcat.Length > 0) mConcat = mConcat.Substring(0, mConcat.Length - 2);
                    //     else mConcat = "NO MODS";
                    // 
                    //     Melon<TweaksAndFixes>.Logger.Msg(mount.ToString().PadRight(50) + ": " + mount.MountToString().PadRight(25) + " : " + mConcat.PadRight(50) + " : LAYER - " + mount.gameObject.layer + " : PACK # - " + mount.packNumber);
                    // }

                    // Melon<TweaksAndFixes>.Logger.Msg(Patch_Ship.LastCreatedShip.parts[0].gameObject.layer);

                    // int layerObject = 0;
                    // Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    // Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> hits;
                    // hits = Physics.RaycastAll(ray, 10000);
                    // Melon<TweaksAndFixes>.Logger.Msg(Input.mousePosition.ToString() + " : " + G.cam.transform.TransformDirection(Vector3.forward).ToString());
                    // Part partUnderMouse = null;
                    // foreach (RaycastHit hit in hits)
                    // {
                    //     if (hit.collider == null || hit.collider.name != "PartSelect")
                    //     {
                    //         continue;
                    //     }
                    // 
                    //     GameObject hitObj = hit.collider.gameObject.GetParent().GetParent();
                    // 
                    //     if (hitObj != null)
                    //     {
                    //         // Melon<TweaksAndFixes>.Logger.Msg(hitObj.name);
                    // 
                    //         foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                    //         {
                    //             if (part.gameObject == hitObj)
                    //             {
                    //                 Melon<TweaksAndFixes>.Logger.Msg("Hit: " + part.Name());
                    //                 Melon<TweaksAndFixes>.Logger.Msg("   : " + hit.collider.gameObject.name);
                    //                 partUnderMouse = part;
                    //             }
                    //             // Melon<TweaksAndFixes>.Logger.Msg("\n" + ModUtils.DumpHierarchy(part.gameObject));
                    //         }
                    //     }
                    // 
                    //     if (partUnderMouse != null) break;
                    // }

                    // int layerObject = 8;
                    // Vector2 ray = new Vector2(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y);
                    // Melon<TweaksAndFixes>.Logger.Msg(ray);
                    // RaycastHit2D hit = Physics2D.Raycast(ray, new Vector2(0,0), 10000, 0);
                    // if (hit.collider != null)
                    // {
                    //     Melon<TweaksAndFixes>.Logger.Msg("HIT: " + hit.collider.name);
                    // }
                }


                // Melon<TweaksAndFixes>.Logger.Msg("Check selected part:");

                if (SelectedPart != null)
                {
                    if (SelectedPart.mount != null && !MainTower && !SecTower && !Funnel)
                    {
                        DefaultRotation = SelectedPart.mount.transform.rotation.eulerAngles.y;
                        Mounted = true;
                    }
                    else
                    {
                        DefaultRotation = 0;
                        Mounted = false;
                    }

                    Vector3 CurrentRotation = SelectedPart.transform.eulerAngles;
                    CurrentRotation.y = PartRotation + DefaultRotation;
                    SelectedPart.transform.eulerAngles = CurrentRotation;

                    if (Input.GetKey(KeyCode.LeftShift) && !SideGun && SelectedPart.mount == null)
                    {
                        G.ui.fireSectorObj.transform.SetX(0);
                        SelectedPart.Place(new Vector3(0, SelectedPart.transform.position.y, SelectedPart.transform.position.z), false);
                    }
                }
            }

            _InUpdateConstructor = false;
        }

        [HarmonyPatch(nameof(Ui.FindPartUnderMouseCursor))]
        [HarmonyPrefix]
        internal static bool Prefix_FindPartUnderMouseCursor(Ui __instance, ref Part __result)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<RaycastHit> hits;
            hits = Physics.RaycastAll(ray, 10000);
            // Melon<TweaksAndFixes>.Logger.Msg(Input.mousePosition.ToString() + " : " + G.cam.transform.TransformDirection(Vector3.forward).ToString());
            __result = null;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.name != "PartSelect")
                {
                    continue;
                }

                GameObject hitObj = hit.collider.gameObject.GetParent().GetParent();

                if (hitObj != null)
                {
                    // Melon<TweaksAndFixes>.Logger.Msg(hitObj.name);

                    foreach (Part part in Patch_Ship.LastCreatedShip.parts)
                    {
                        if (part.gameObject == hitObj)
                        {
                            // Melon<TweaksAndFixes>.Logger.Msg("Hit: " + part.Name());
                            __result = part;
                        }
                        // Melon<TweaksAndFixes>.Logger.Msg("\n" + ModUtils.DumpHierarchy(part.gameObject));
                    }
                }

                if (__result != null) break;
            }

            return false;
        }

        [HarmonyPatch(nameof(Ui.ConstructorUI))]
        [HarmonyPostfix]
        internal static void Postfix_ConstructorUI(Ui __instance)
        {
            ClearAllButtons(__instance);
            EnsureAllButtons(__instance);
        }

        [HarmonyPatch(nameof(Ui.RefreshConstructorInfo))]
        [HarmonyPrefix]
        internal static void Prefix_RefreshConstructorInfo(Ui __instance)
        {
            ClearAllButtons(__instance);
            SpriteDatabase.Instance.OverrideResources();
        }

        [HarmonyPatch(nameof(Ui.RefreshConstructorInfo))]
        [HarmonyPostfix]
        internal static void Postfix_RefreshConstructorInfo(Ui __instance)
        {
            EnsureAllButtons(__instance);
        }

        [HarmonyPatch(nameof(Ui.NewGameUI))]
        [HarmonyPostfix]
        internal static void Postfix_NewGameUI(Ui __instance)
        {
            if (!GameManager.IsNewGame)
                return;
            Patch_CampaignNewGame.FixDesignUsage(__instance.NewGameWindow);
        }

        private static void ClearAllButtons(Ui ui)
        {
            if (ui == null || ui.gameObject == null)
                return;

            // Would be faster to drill down but this works.
            var objTCs = ui.gameObject.Get("TurretCalibers");
            if (objTCs != null)
                ClearButtons(objTCs);

            var objCase = ui.gameObject.Get("CasemateCalibers");
            if (objCase != null)
                ClearButtons(objCase);

            var objComps = FindArmamentsComponentList(ui);
            if (objComps != null)
                ClearButtons(objComps);
        }

        private static GameObject FindArmamentsComponentList(Ui ui)
        {
            string label = LocalizeManager.Localize("$comptypes_category_armament");
            var objComps = ui.gameObject.Get("Components");
            //Melon<TweaksAndFixes>.Logger.Msg($"Finding complist. Label {label}. Child count {objComps.transform.childCount}");

            for (int i = objComps.transform.childCount - 1; i-- > 0;)
            {
                var subTrf = objComps.transform.GetChild(i);
                //Melon<TweaksAndFixes>.Logger.Msg($"Object: {subTrf.gameObject.name}");
                if (!subTrf.gameObject.name.StartsWith("Header"))
                    continue;
                var text = subTrf.gameObject.GetComponentInChildren<Text>();
                if (text == null)
                    continue;

                //Melon<TweaksAndFixes>.Logger.Msg($"Found header text with {text.text}, compare to {label}");
                if (text.text != label)
                    continue;

                var nextObj = objComps.transform.GetChild(i + 1).gameObject;
                //Melon<TweaksAndFixes>.Logger.Msg($"Next object name is {nextObj.name}");
                if (nextObj.name.StartsWith("Components"))
                    return nextObj;
            }

            //if (GameManager.IsConstructor)
            //    Melon<TweaksAndFixes>.Logger.Error("Could not find Armaments components list!");
            return null;
        }

        private static void ClearButtons(GameObject parent)
        {
            for (int i = parent.transform.childCount; i-- > 0;)
            {
                var subTrf = parent.transform.GetChild(i);
                if (subTrf == null || subTrf.gameObject == null || subTrf.gameObject.name != "ResetGrade")
                    continue;

                GameObject.DestroyImmediate(subTrf.gameObject);
            }
        }

        static List<Ship.TurretCaliber> _Turrets = new List<Ship.TurretCaliber>();
        static List<Ship.TurretCaliber> _Casemates = new List<Ship.TurretCaliber>();

        private static void EnsureAllButtons(Ui ui)
        {
            if (!GameManager.IsConstructor)
                return;

            if (PlayerController.Instance == null)
                return;
            var ship = PlayerController.Instance.Ship;
            if (ship == null)
                return;

            var objTCs = ui.gameObject.Get("TurretCalibers");
            if (objTCs == null)
                return;

            var objCase = ui.gameObject.Get("CasemateCalibers");
            if (objCase == null)
                return;

            if (ship.shipGunCaliber == null)
                return;

            // In case we blew up last execution
            _Turrets.Clear();
            _Casemates.Clear();

            // Part out the TCs
            foreach (var tc in ship.shipGunCaliber)
            {
                if (tc.isCasemateGun)
                    _Casemates.Add(tc);
                else
                    _Turrets.Add(tc);
            }

            _Turrets.Sort((a, b) => b.turretPartData.GetCaliber().CompareTo(a.turretPartData.GetCaliber()));
            EnsureTCButtons(ship, objTCs, _Turrets);

            _Casemates.Sort((a, b) => b.turretPartData.GetCaliber().CompareTo(a.turretPartData.GetCaliber()));
            EnsureTCButtons(ship, objCase, _Casemates);

            _Turrets.Clear();
            _Casemates.Clear();


            EnsureTorpButton(ship, ui);
        }

        private static void EnsureTCButtons(Ship ship, GameObject parent, List<Ship.TurretCaliber> tcs)
        {
            int idx = tcs.Count - 1;
            for (int i = parent.transform.childCount; i-- > 0 && idx >= 0;)
            {
                var subTrf = parent.transform.GetChild(i);
                if (subTrf == null)
                    continue;

                var obj = subTrf.gameObject;
                if (obj == null || !obj.activeSelf)
                    continue;

                var tc = tcs[idx--];
                if (tc == null || tc.turretPartData == null)
                    continue;
                if (!ship.TAFData().IsGradeOverridden(tc.turretPartData))
                    continue;

                var button = AddTCButton(obj, i + 1);
                if (button == null)
                {
                    Melon<TweaksAndFixes>.Logger.Error($"Could not add button for tc for part {tc.turretPartData.name}!");
                    continue;
                }
                button.onClick.RemoveAllListeners();
                float caliber = tc.turretPartData.caliber;
                bool isCasemate = tc.isCasemateGun;
                
                button.onClick.AddListener(new System.Action(() =>
                {
                    ship.TAFData().ResetGunGrade(caliber, isCasemate);
                }));
            }
        }

        private static Button AddTCButton(GameObject obj, int idx)
        {
            var buttonOld = obj.transform.GetChild("Less");
            if (buttonOld == null)
                return null;
            var textOld = obj.transform.GetChild("TextCaliber");
            if (textOld == null)
                return null;

            var buttonNew = GameObject.Instantiate(buttonOld);
            buttonNew.transform.SetParent(obj.transform.parent.transform, true);
            buttonNew.transform.SetSiblingIndex(idx);
            var textNew = GameObject.Instantiate(textOld);
            var le = textNew.GetComponent<LayoutElement>();
            if (le != null)
                GameObject.Destroy(le);
            var image = buttonNew.GetChild("Image");
            if (image != null && image.gameObject != null)
                GameObject.Destroy(image.gameObject);
            textNew.transform.SetParent(buttonNew.transform, true);
            textNew.name = "Text";
            var text = textNew.GetComponent<Text>();
            text.text = LocalizeManager.Localize("$TAF_Ui_Constr_UpgradeMark");
            var trf = textNew.GetComponent<RectTransform>();
            trf.sizeDelta = new Vector2(150, 40);
            text.fontSize = 35;
            text.resizeTextMaxSize = 20;
            trf.anchoredPosition = new Vector2(114, -20);
            var button = buttonNew.GetComponent<Button>();
            button.interactable = true;

            button.gameObject.name = "ResetGrade";

            return button;
        }

        private static void EnsureTorpButton(Ship ship, Ui ui)
        {
            if (!ship.TAFData().IsTorpGradeOverridden())
                return;

            //Melon<TweaksAndFixes>.Logger.Msg("Adding torp upgrade button");
            var sName = ui.gameObject.Get("ShipNew");
            var buttonOld = sName == null ? null : sName.GetChild("Button", true);
            if (buttonOld == null)
            {
                Melon<TweaksAndFixes>.Logger.Error("Could not find button to clone!");
                return;
            }

            var compList = FindArmamentsComponentList(ui);
            if (compList == null)
                return;

            var buttonNew = GameObject.Instantiate(buttonOld);
            buttonNew.transform.SetParent(compList.transform, true);
            buttonNew.name = "ResetGrade";
            var image = buttonNew.GetChild("Image");
            if (image != null && image.gameObject != null)
                GameObject.Destroy(image.gameObject);

            var le = buttonNew.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 75;
            le.preferredWidth = 53;
            var text = buttonNew.transform.GetChild("Text").GetComponent<Text>();
            text.text = text.text = LocalizeManager.Localize("$TAF_Ui_Constr_UpgradeTorpMark");
            text.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            text.resizeTextMinSize = text.resizeTextMaxSize = 10;
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            buttonNew.GetChild("Bg").transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            var button = buttonNew.GetComponent<Button>();
            button.onClick.AddListener(new System.Action(() =>
            {
                ship.TAFData().ResetTorpGrade();
            }));

            buttonNew.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(Ui.__c))]
    internal class Patch_Ui_c
    {
        internal static bool _SetBackToBarbette = false;
        internal static PartData _BarbetteData = null;
        internal static bool _IsFirstCallofB15 = true;

        [HarmonyPatch(nameof(Ui.__c._UpdateConstructor_b__545_15))]
        [HarmonyPostfix]
        internal static void Postfix_15()
        {
            if (Patch_Ui._InUpdateConstructor && _IsFirstCallofB15 && Patch_Ship._GenerateShipState < 0 && G.ui.currentPart != null && G.ui.currentPart.isBarbette
                && G.ui.placingPart != null && !G.ui.placingPart.data.paramx.ContainsKey("center"))
            {
                _SetBackToBarbette = true;
                _BarbetteData = G.ui.currentPart;
                _BarbetteData.isBarbette = false;
                Patch_Part._IgnoreNextActiveBad = true;
            }
            _IsFirstCallofB15 = false;
        }

        [HarmonyPatch(nameof(Ui.__c._UpdateConstructor_b__545_16))]
        [HarmonyPostfix]
        internal static void Postfix_16()
        {
            _IsFirstCallofB15 = true;
            if (_SetBackToBarbette)
            {
                if (!Patch_Ui._InUpdateConstructor)
                    Melon<TweaksAndFixes>.Logger.Warning("Made it to end of UpdateConstructor with unrestored Barbette");

                _SetBackToBarbette = false;
                if (_BarbetteData != null)
                {
                    _BarbetteData.isBarbette = true;
                    _BarbetteData = null;
                }
            }
        }
    }
}
