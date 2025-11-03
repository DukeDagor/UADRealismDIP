using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public static class PlayerM
    {
        public static IEnumerable<Ship> GetFleetAll(this Player player)
        {
            if (CampaignController.Instance == null || !CampaignController.Instance.CampaignData.VesselsByPlayer.TryGetValue(player.data, out var allVessels))
                yield break;

            foreach (var v in allVessels)
            {
                if (v.vesselType != VesselEntity.VesselType.Ship)
                    continue;
                var s = v.TryCast<Ship>();
                if (s == null || s.isDesign)
                    continue;

                yield return s;
            }
        }

        public static IEnumerable<Ship> GetFleet(this Player player, bool includeMothballedAndLowCrew = false)
        {
            foreach (var s in player.GetFleetAll())
            {
                if (s.isSunk || s.isScrapped)
                    continue;
                if (!includeMothballedAndLowCrew && (s.status == VesselEntity.Status.Mothballed || s.status == VesselEntity.Status.LowCrew))
                    continue;

                yield return s;
            }
        }

        private static float PopWithColonies(Player _this)
        {
            float ratio = Config.Param("taf_crew_pool_colony_pop_ratio", 0f);

            float pop = 0f;
            var provs = _this.provinces;
            foreach (var p in provs)
                pop += p.GetPopulation(false) * ((p.Type == "home" && (p.ControllerPlayer == _this || p.InitialController == _this.data)) ? 1f : ratio);
            return pop;
        }

        public static void InitCrewPool(Player _this)
        {
            int pool = GetBaseCrewPool(_this);
            _this.crewPool = pool;
            _this.baseCrewPool = pool;
        }

        public static int GetBaseCrewPool(Player _this)
        {
            var modifier = MonoBehaviourExt.Param("crew_pool_modifier", 0.01f);
            return (int)(PopWithColonies(_this) * modifier);
        }

        public static int CrewPoolincome(Player _this)
        {
            float pop = PopWithColonies(_this);
            float mult = MonoBehaviourExt.Param("crew_pool_income_modifier_max", 0.005f);
            float existingPoolMult = Mathf.Lerp(2f, 0.13f, _this.crewPool / 90000f); // stock appears to do integer division here, which is probably a typo.
            float popPortion = Mathf.Clamp01(_this.crewPool * 25000f / pop);
            float val = Mathf.Lerp(1f, 0.1f, popPortion) * _this.trainingBudget * pop * mult * existingPoolMult;
            if (_this.isAi)
                val *= CampaignController.Instance.AiIncomeMultiplier * 1.15f;

            return (int)val;
        }
    }
}