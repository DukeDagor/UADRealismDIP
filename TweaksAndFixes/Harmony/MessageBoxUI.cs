using System;
using System.Collections.Generic;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Il2Cpp;

namespace TweaksAndFixes
{
    [HarmonyPatch(typeof(MessageBoxUI))]
    internal class Patch_MessageBoxUI
    {
        private static unsafe int SplitStringIdx(string text, out int scrollStartIdx)
        {
            int len = text.Length;
            int start = 0;
            if (len > 600)
                start = 500;

            int newlineCount = 0;
            const int maxLinesMin = 15; // 15 lines.
            const int maxLinesMax = 20; // if there's more than this after the 15
            bool shouldSplit = start > 0;
            int splitIdx = -1;
            scrollStartIdx = -1;
            fixed (char* pCh = text)
            {
                for (int i = 0; i < len; ++i)
                {
                    char c = pCh[i];
                    if (c == '\n')
                    {
                        ++newlineCount;
                        if (i < len - 1 && pCh[i + 1] == '\n')
                        {
                            splitIdx = i;
                            for (int j = i; j < len; ++j)
                            {
                                char c2 = pCh[j];
                                if (c2 == '\n')
                                    continue;

                                scrollStartIdx = i;
                                break;
                            }
                        }
                        // We should only hit this after we find scrollStartIdx (if there's a double newline
                        // before we NEED to split)
                        if (!shouldSplit && newlineCount >= maxLinesMin)
                        {
                            // check there's actually more stuff
                            int maxCharToCheck = len - 1;
                            for (int j = i; j < len; ++j)
                            {
                                char c2 = pCh[j];
                                if (c2 == '\n' && ++newlineCount >= maxLinesMax)
                                {
                                    shouldSplit = true;
                                    maxCharToCheck = j;
                                    break;
                                }
                            }
                            // If we are not length-limited, and there's little after
                            // the max \n, we're done
                            if (!shouldSplit)
                                return -1;

                            // Otherwise find the double-\n to break on, if we can.
                            // if we already found a double \n, that's a good breakpoint.
                            if (scrollStartIdx >= 0)
                                return splitIdx;
                            // but if not, split at the first \n
                            splitIdx = i;
                            scrollStartIdx = i + 1;
                            // and walk scrollstart forward.
                            for (int j = i; j < len; ++j)
                            {
                                char c2 = pCh[j];
                                if (c2 == '\n')
                                    continue;

                                scrollStartIdx = i;
                                break;
                            }
                        }
                    }
                }
                return splitIdx;
            }
        }

        [HarmonyPatch(nameof(MessageBoxUI.Show))]
        [HarmonyPrefix]
        internal static bool Prefix_MessageBoxUI(string header, ref string text, Sprite image, bool backround, string ok, string cancel, Il2CppSystem.Action onConfirm, Il2CppSystem.Action onCancel,
            Il2CppSystem.Action<MessageBoxUI> onShow, Il2CppSystem.Action<MessageBoxUI, float> onSliderValueChanged, ref string scrollData, bool canBeClosed, bool skipUiRefresh, bool showShipToolTip, Ship ship)
        {
            if (text == "Retirement")
                return false;
            if (scrollData != null || text == null)
                return true;
            int splitIdx = SplitStringIdx(text, out int startIdx);
            if (splitIdx < 0)
                return true;
            string oldText = text;
            text = oldText.Substring(0, splitIdx);
            if (startIdx < oldText.Length)
                scrollData = oldText.Substring(startIdx);
            return true;
        }
    }
}
