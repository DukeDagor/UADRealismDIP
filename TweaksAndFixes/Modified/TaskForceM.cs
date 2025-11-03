using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class TaskForceM
    {
        public static void CheckMinefieldOnPath(CampaignController.TaskForce _this)
        {
            if (_this.CheckFullPath == null || _this.CurrentPositionIndex >= _this.CheckFullPath.Count)
                return;

            var mm = CampaignController.Instance.MinesfieldManager;
            var vecList = _this.CheckFullPath[_this.CurrentPositionIndex];
            if (vecList.Count <= 1)
                return;

            CampaignController.TaskForce.GetCircle(vecList, out var center, out var radius);
            if (radius <= 0)
                return;

            var fields = mm.GetMinefieldsInsideRadius(center, radius);
            if (fields == null)
                return;

            if (!MinesFieldManagerM.CanAttackTFNoChange(_this))
                return;

            foreach (var field in fields)
            {
                var dmgRadius = field.GetRadiusForDamage(true);
                for (int i = 1; i < vecList.Count; i += 2)
                {
                    if (!MathfExt.LineCircleIntersection(vecList[i - 1], vecList[i], field.Location, dmgRadius))
                        continue;
                    var player = field.GetPlayer();

                    var displacement = MinesFieldManagerM.DamageTaskForce(mm, _this, player, dmgRadius, field.DamageMultiplier);
                    MinesFieldManagerM.MineHitSpenting(mm, displacement, field);
                    if (_this.Vessels.Count > 0)
                        MinesFieldManagerM.Minesweep(mm, field, _this);
                }
            }

            // check submarine fields. Rather than use GetTaskForceInsideRadius we inline it.
            float mod = MonoBehaviourExt.Param("submarine_minefield_modifier", 10f);
            float max = MonoBehaviourExt.Param("max_minefield_size", 360f);
            foreach (var tf in CampaignController.Instance.CampaignData.TaskForces)
            {
                if (tf.GetVesselType() != VesselEntity.VesselType.Submarine)
                    continue;
                if (tf.Controller == _this.Controller)
                    continue;

                float zone = tf.GetZoneRadius(false);
                float distSqr = (zone + radius) * 0.005f;
                distSqr *= distSqr;
                if ((center - tf.WorldPos).sqrMagnitude > distSqr)
                    continue;

                float totalMinelaying = 0f;
                int subCount = 0;
                foreach (var v in tf.Vessels)
                {
                    ++subCount;
                    totalMinelaying += v.GetMinelayingValue();
                }
                if (subCount == 0)
                    continue;

                float subFieldRadiusInWorld = Mathf.Clamp(mod * totalMinelaying / (float)subCount, 0f, max) * 0.25f * 0.005f;
                for (int i = 1; i < vecList.Count; i += 2)
                {
                    if (!MathfExt.LineCircleIntersection(vecList[i - 1], vecList[i], tf.WorldPos, subFieldRadiusInWorld))
                        continue;
                    MinesFieldManagerM.DamageTaskForce(mm, _this, tf.Controller, subFieldRadiusInWorld, 1f);
                }
            }
        }

        public static float GetMinesweepingCapacity(CampaignController.TaskForce _this)
        {
            float totalWeight = 0f;
            foreach (var v in _this.Vessels)
                totalWeight += v.Weight();

            float defCapacity = MonoBehaviourExt.Param("mine_sweep_default_capacity", 0.01f);
            float avgVal = _this.AverageMinesweepingValue();
            float mod = MonoBehaviourExt.Param("mine_sweepers_power_mod", 0.5f);
            if (_this.sumMinesweepingWeight == -1f)
            {
                _this.sumMinesweepingWeight = 0f;
                foreach (var v in _this.Vessels)
                    _this.sumMinesweepingWeight += v.TechSum("minesweep_weight");
            }
            return defCapacity * totalWeight * avgVal + _this.sumMinesweepingWeight * mod;
        }
    }
}