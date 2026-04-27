using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

#pragma warning disable CS8603
#pragma warning disable CS8604

namespace TweaksAndFixes
{
    internal static class StrikeCommand
    {
        private enum StrikePhase
        {
            Approach,
            Withdraw,
        }

        private sealed class StrikeOrder
        {
            public Division source;
            public StrikePhase phase = StrikePhase.Approach;
            public float nextUpdate;

            public StrikeOrder(Division source)
            {
                this.source = source;
            }
        }

        private static readonly Dictionary<IntPtr, StrikeOrder> _Orders = new();
        private static bool _IssuingStrikeMove;

        internal static bool IsIssuingStrikeMove => _IssuingStrikeMove;

        internal static void Update()
        {
            if (!GameManager.IsBattle)
            {
                ClearAll();
                return;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                ToggleFromSelectedDivision();
            }

            foreach (var key in _Orders.Keys.ToList())
            {
                if (!_Orders.TryGetValue(key, out var order))
                    continue;

                if (!UpdateOrder(order))
                {
                    _Orders.Remove(key);
                }
            }
        }

        internal static void Cancel(Division division, string reason = "manual order")
        {
            if (division == null)
                return;

            if (_Orders.Remove(division.Pointer))
            {
                Melon<TweaksAndFixes>.Logger.Msg($"Strike command cancelled for {DivisionName(division)}: {reason}");
            }

        }

        private static void ClearAll()
        {
            _Orders.Clear();
        }

        private static void ToggleFromSelectedDivision()
        {
            Division source = SelectedDivision();
            if (source == null || !HasReadyStrikeShip(source))
            {
                Melon<TweaksAndFixes>.Logger.Msg("Strike command: select a player division with torpedoes first.");
                return;
            }

            if (_Orders.ContainsKey(source.Pointer))
            {
                Cancel(source, "toggled off");
                return;
            }

            _Orders[source.Pointer] = new StrikeOrder(source);
            Melon<TweaksAndFixes>.Logger.Msg($"Strike command enabled for {DivisionName(source)}. Using current weapon target.");
        }

        private static bool UpdateOrder(StrikeOrder order)
        {
            if (order == null || !IsValidStrikeSource(order.source))
                return false;

            if (Time.time < order.nextUpdate)
                return true;

            order.nextUpdate = Time.time + 1f;

            Ship sourceLeader = ValidLeader(order.source);
            Ship targetLeader = CurrentStrikeTarget(order.source);
            if (targetLeader == null)
                return true;

            float torpedoRange = MaxTorpedoRange(order.source);
            if (torpedoRange <= 0f)
                return false;

            Vector3 sourcePos = sourceLeader.transform.position;
            Vector3 targetPos = targetLeader.transform.position;
            Vector3 fromTarget = sourcePos - targetPos;
            fromTarget.y = 0f;

            if (fromTarget.sqrMagnitude < 1f)
                fromTarget = -targetLeader.transform.forward;

            Vector3 awayDir = fromTarget.normalized;
            float distance = Vector3.Distance(new Vector3(sourcePos.x, 0f, sourcePos.z), new Vector3(targetPos.x, 0f, targetPos.z));
            float strikeRange = torpedoRange * 0.8f;
            float withdrawRange = Math.Max(torpedoRange * 1.15f, strikeRange + 1500f);

            if (order.phase == StrikePhase.Approach && distance <= strikeRange)
            {
                order.phase = StrikePhase.Withdraw;
            }
            else if (order.phase == StrikePhase.Withdraw && distance >= withdrawRange && AnyTorpedoReady(order.source))
            {
                order.phase = StrikePhase.Approach;
            }

            Vector3 destination = order.phase == StrikePhase.Approach
                ? targetPos + awayDir * (strikeRange * 0.9f)
                : sourcePos + awayDir * Math.Max(2500f, torpedoRange * 0.45f);

            IssueMoveTo(order.source, destination);
            return true;
        }

        private static Division SelectedDivision()
        {
            if (UIShipManagerBase.SelectedElement == null)
                return null;

            UIDivision selectedDivision = UIShipManagerBase.SelectedElement.TryCast<UIDivision>();
            if (selectedDivision != null)
                return selectedDivision.CurrentDivision;

            UIShip selectedShip = UIShipManagerBase.SelectedElement.TryCast<UIShip>();
            return selectedShip?.CurrentShip?.division;
        }

        private static void IssueMoveTo(Division division, Vector3 destination)
        {
            try
            {
                _IssuingStrikeMove = true;
                division.MoveTo(destination);
            }
            finally
            {
                _IssuingStrikeMove = false;
            }
        }

        private static bool IsValidStrikeSource(Division source)
        {
            Ship sourceLeader = ValidLeader(source);
            return sourceLeader != null
                && sourceLeader.player != null
                && HasReadyStrikeShip(source);
        }

        private static Ship CurrentStrikeTarget(Division division)
        {
            if (division?.ships == null)
                return null;

            Ship sourceLeader = ValidLeader(division);
            if (sourceLeader == null)
                return null;

            Ship bestTarget = null;
            float bestDistanceSqr = float.PositiveInfinity;

            foreach (Ship ship in division.ships)
            {
                if (ship == null || !ship.isAlive || !ship.haveTorpedoes || ship.torpedoesAll == null)
                    continue;

                foreach (Part torpedo in ship.torpedoesAll)
                {
                    Ship target = torpedo?.data == null ? null : ship.GetEnemy(torpedo.data);
                    if (!IsValidTargetFor(ship, target))
                        continue;

                    float distanceSqr = (ship.transform.position - target.transform.position).sqrMagnitude;
                    if (distanceSqr < bestDistanceSqr)
                    {
                        bestDistanceSqr = distanceSqr;
                        bestTarget = target;
                    }
                }

                if (bestTarget != null)
                    continue;

                Ship fallback = CurrentAnyWeaponTarget(ship);
                if (!IsValidTargetFor(ship, fallback))
                    continue;

                float fallbackDistanceSqr = (ship.transform.position - fallback.transform.position).sqrMagnitude;
                if (fallbackDistanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = fallbackDistanceSqr;
                    bestTarget = fallback;
                }
            }

            return bestTarget;
        }

        private static Ship CurrentAnyWeaponTarget(Ship ship)
        {
            if (ship?.enemies != null)
            {
                foreach (var aim in ship.enemies.Values)
                {
                    if (aim?.target != null)
                        return aim.target;
                }
            }

            if (ship?.enemiesLeftSide != null)
            {
                foreach (var aim in ship.enemiesLeftSide.Values)
                {
                    if (aim?.target != null)
                        return aim.target;
                }
            }

            return null;
        }

        private static bool IsValidTargetFor(Ship source, Ship target)
        {
            return source != null
                && target != null
                && source != target
                && target.isAlive
                && source.player != null
                && target.player != null
                && source.player != target.player;
        }

        private static bool HasReadyStrikeShip(Division division)
        {
            if (division == null || division.ships == null)
                return false;

            foreach (Ship ship in division.ships)
            {
                if (ship != null && ship.isAlive && ship.haveTorpedoes)
                    return true;
            }

            return false;
        }

        private static Ship ValidLeader(Division division)
        {
            if (division == null)
                return null;

            Ship leader = division.leader;
            if (leader == null || !leader.isAlive)
                return null;

            return leader;
        }

        private static float MaxTorpedoRange(Division division)
        {
            float range = 0f;
            if (division?.ships == null)
                return range;

            foreach (Ship ship in division.ships)
            {
                if (ship == null || !ship.isAlive || !ship.haveTorpedoes || ship.torpedoesAll == null)
                    continue;

                foreach (Part torpedo in ship.torpedoesAll)
                {
                    if (torpedo?.data == null || ship.weaponRangesCache == null)
                        continue;

                    if (ship.weaponRangesCache.TryGetValue(torpedo.data, out float weaponRange))
                        range = Math.Max(range, weaponRange);
                }
            }

            return range;
        }

        private static bool AnyTorpedoReady(Division division)
        {
            if (division?.ships == null)
                return false;

            foreach (Ship ship in division.ships)
            {
                if (ship == null || !ship.isAlive || !ship.haveTorpedoes || ship.torpedoesAll == null)
                    continue;

                foreach (Part torpedo in ship.torpedoesAll)
                {
                    if (torpedo != null && (torpedo.reloading == null || torpedo.reloading.isDone))
                        return true;
                }
            }

            return false;
        }

        private static string DivisionName(Division division)
        {
            Ship leader = ValidLeader(division);
            return leader == null ? "<unknown division>" : leader.Name(true, false, true, false, true);
        }

    }

    [HarmonyPatch(typeof(Ui), nameof(Ui.UpdateBattle))]
    internal static class Patch_Ui_UpdateBattle_StrikeCommand
    {
        [HarmonyPostfix]
        internal static void Postfix()
        {
            StrikeCommand.Update();
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.MoveTo))]
    internal static class Patch_Division_MoveTo_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            if (!StrikeCommand.IsIssuingStrikeMove)
                StrikeCommand.Cancel(__instance);
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.MoveDir))]
    internal static class Patch_Division_MoveDir_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            if (!StrikeCommand.IsIssuingStrikeMove)
                StrikeCommand.Cancel(__instance);
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.MoveStop))]
    internal static class Patch_Division_MoveStop_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            if (!StrikeCommand.IsIssuingStrikeMove)
                StrikeCommand.Cancel(__instance);
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.SetFollow))]
    internal static class Patch_Division_SetFollow_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            StrikeCommand.Cancel(__instance);
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.SetScoutDivision))]
    internal static class Patch_Division_SetScoutDivision_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            StrikeCommand.Cancel(__instance);
        }
    }

    [HarmonyPatch(typeof(Division), nameof(Division.SetScreenDivision))]
    internal static class Patch_Division_SetScreenDivision_StrikeCancel
    {
        [HarmonyPrefix]
        internal static void Prefix(Division __instance)
        {
            StrikeCommand.Cancel(__instance);
        }
    }
}
