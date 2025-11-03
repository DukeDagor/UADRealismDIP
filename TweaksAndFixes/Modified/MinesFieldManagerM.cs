using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class MinesFieldManagerM
    {
        private static int _currentTurn = -1;
        private static readonly Dictionary<PlayerData, HashSet<CampaignController.TaskForce>> _attackedTFs = new Dictionary<PlayerData, HashSet<CampaignController.TaskForce>>();
        private static readonly Dictionary<PlayerData, int> _numTFAttacks = new Dictionary<PlayerData, int>();
        private static readonly Dictionary<PlayerData, int> _numShips = new Dictionary<PlayerData, int>();
        private static readonly Dictionary<CampaignController.TaskForce, int> _numAttacksPerTF = new Dictionary<CampaignController.TaskForce, int>();

        private static void CheckTurn()
        {
            var turn = CampaignController.Instance.CurrentDate.turn;
            if (_currentTurn == turn)
                return;
            _currentTurn = turn;

            foreach (var set in _attackedTFs.Values)
                set.Clear();
            _numShips.Clear();
            _numAttacksPerTF.Clear();
        }

        // We use this for earlying out in TF.CheckMinefieldOnPath()
        // but we don't set values, because there's an early-out in the
        // damage check and we don't want to count the TF until it passes
        // that check.
        public static bool CanAttackTFNoChange(CampaignController.TaskForce tf)
        {
            CheckTurn();

            var maxTF = Config.Param("taf_mines_max_tf_per_player", -1);
            if (maxTF >= 0 && _attackedTFs.ValueOrNew(tf.Controller.data) is HashSet<CampaignController.TaskForce> set && set.Count >= maxTF && !set.Contains(tf))
                return false;

            var maxTFAttacks = Config.Param("taf_mines_max_tf_attacks_per_player", -1);
            if (maxTFAttacks >= 0 && _numTFAttacks.GetValueOrDefault(tf.Controller.data) >= maxTFAttacks)
                return false;

            var maxShip = Config.Param("taf_mines_max_ships_per_player", -1);
            if (maxShip >= 0 && _numShips.GetValueOrDefault(tf.Controller.data) >= maxShip)
                return false;

            return true;
        }

        public static bool CanAttackTF(CampaignController.TaskForce tf)
        {
            if (!CanAttackInTF(tf))
                return false;

            _attackedTFs.ValueOrNew(tf.Controller.data).Add(tf);
            _numTFAttacks.IncrementValueFor(tf.Controller.data);

            // Only increment TFs, don't increment ship count. We check that
            // only as an early-out.

            return true;
        }

        public static bool CanAttackInTF(CampaignController.TaskForce tf)
        {
            CheckTurn();

            var maxShip = Config.Param("taf_mines_max_ships_per_player", -1);
            if (maxShip >= 0 && _numShips.GetValueOrDefault(tf.Controller.data) >= maxShip)
                return false;

            var maxAttack = Config.Param("taf_mines_max_ships_per_tf", -1);
            if (maxAttack >= 0 && _numAttacksPerTF.GetValueOrDefault(tf) >= maxAttack)
                return false;

            _numShips.IncrementValueFor(tf.Controller.data);
            _numAttacksPerTF.IncrementValueFor(tf);
            return true;
        }

        private static readonly List<VesselEntity> _TempVessels = new List<VesselEntity>();
        public static float DamageTaskForce(MinesFieldManager _this, CampaignController.TaskForce taskForce, Player mineFieldOwner, float minefieldRadiusKm, float damageMultiplier = 1f)
        {
            if (!CanAttackTFNoChange(taskForce))
                return 0f;

            var rel = RelationExt.Between(CampaignController.Instance.CampaignData.Relations, mineFieldOwner, taskForce.Controller);
            bool isWar = rel == null ? true : rel.isWar;
            var sweepPrevent = (MonoBehaviourExt.Param("mine_sweep_prevention", 0.1f) * taskForce.AverageMinesweepingValue()) + 1f;
            var detectVal = taskForce.AverageMineDetectValue();
            var fleetFactor = MonoBehaviourExt.Param("minefield_fleet_factor", 100000f);

            float factor = fleetFactor * -(isWar ? Config.Param("taf_mines_fleetfactor_mult_war", 2f) : Config.Param("taf_mines_fleetfactor_mult_peace", 2800f));
            float tonnage = taskForce.BattleTonnage() * (isWar ? Config.Param("taf_mines_tonnagefactor_war", 5f) : Config.Param("taf_mines_tonnagefactor_peace", 0.04f));

            float randomDamageFactor = ModUtils.Range(factor, tonnage * minefieldRadiusKm / detectVal);
            if (MonoBehaviourExt.Param("minefield_chance_factor", 150000f) >= randomDamageFactor)
                return 0f;
            if (!CanAttackTF(taskForce))
                return 0f;

            float remappedRand = Util.Remap(randomDamageFactor, 0f, Config.Param("taf_mines_max_randomdamagefactor", 300000f), 0f, Config.Param("taf_mines_default_max_ships_per_tf", 10f), false);
            int shipsDamaged = Mathf.RoundToInt(remappedRand);
            taskForce.DamagedWithMinesTurn = CampaignController.Instance.CurrentDate.turn;

            float shipDmgFactor = MonoBehaviourExt.Param("minefield_dmg_factor_ships", 0.1f);
            float crewDmgFactor = MonoBehaviourExt.Param("minefield_dmg_factor_crew", 0.1f);

            float totalDisp = 0f;

            foreach (var v in taskForce.Vessels)
                _TempVessels.Add(v);
            _TempVessels.Shuffle();

            var antimineMin = Config.Param("taf_mines_antimine_min", 0.05f);
            var antimineMax = Config.Param("taf_mines_antimine_max", 10f);
            var shipDamagePctMin = Config.Param("taf_mines_ship_damage_percent_min", 1f);
            var shipDamagePctMax = Config.Param("taf_mines_ship_damage_percent_max", 200f);
            var crewDamagePctMin = Config.Param("taf_mines_crew_damage_percent_min", 5f);
            var crewDamagePctMax = Config.Param("taf_mines_crew_damage_percent_max", 35f);

            shipsDamaged = Math.Min(shipsDamaged, _TempVessels.Count);
            for (int i = 0; i < shipsDamaged && CanAttackInTF(taskForce); ++i)
            {
                var curV = _TempVessels[i];
                float antimine = curV.TechR("antimine");

                float clampedAnti = Mathf.Clamp(antimine, antimineMin, antimineMax);
                float damageShip = ModUtils.Range(shipDamagePctMin, shipDamagePctMax) * shipDmgFactor * clampedAnti * damageMultiplier;
                float damageCrew = ModUtils.Range(crewDamagePctMin, crewDamagePctMax) * 0.01f * crewDmgFactor * clampedAnti * damageMultiplier;

                var name = curV.Name(false, false, false, false, false);
                Debug.LogFormat("[Damage] Mines damage vessel {0} damage {1}", name, damageShip.ToString("F2"));
                curV.HP -= damageShip;
                curV.CrewPercents -= damageCrew;

                totalDisp += curV.Weight();

                if (curV.HP <= 0f || curV.CrewPercents <= 0f)
                {
                    curV.SetStatus(VesselEntity.Status.Sunk);
                    curV.CrewPercents = 0f;
                    curV.CrewTrainingAmount = 0f;
                    CampaignController.Instance.CampaignData.RemoveVesselFromTaskForce(curV);
                    damageShip = 100f;
                    damageCrew = 1f;
                }

                _this.AddInfo(isWar ? _this.enemyMineFieldDamage : _this.friendlyMineFieldDamage, curV, damageShip, damageCrew);
            }
            _TempVessels.Clear();
            return totalDisp;
        }

        public static void MineHitSpenting(MinesFieldManager _this, float displacement, MinesField minefield)
        {
            float param = MonoBehaviourExt.Param("minefield_hit_spenting", 0.02f);
            float remapped = Util.Remap(param * displacement, 0f, 7000f, 0f, 400f, false);
            minefield.ChangeRadius(-remapped, 1f);
        }

        public static void Minesweep(MinesFieldManager _this, MinesField minefield, CampaignController.TaskForce taskForce)
        {
            var rel = RelationExt.Between(CampaignController.Instance.CampaignData.Relations, minefield.GetPlayer(), taskForce.Controller);
            if (rel == null || !rel.isWar)
                return;

            minefield.ChangeRadius(-TaskForceM.GetMinesweepingCapacity(taskForce), 1f);
        }

        public static void CheckFields(MinesFieldManager _this, Il2CppSystem.Text.StringBuilder infoEnemyMines, Il2CppSystem.Text.StringBuilder infoFriendlyMines)
        {
            var cData = CampaignController.Instance.CampaignData;

            infoEnemyMines.Length = 0;
            infoFriendlyMines.Length = 0;
            foreach (var mf in _this.minesPerPort.Values)
            {
                if (mf.Port != null)
                {
                    if (mf.Owner.Id != mf.Port.CurrentProvince.ControllerPlayer.data.Id)
                    {
                        var rel = RelationExt.Between(cData.Relations, PlayerController.Instance, mf.GetOwner.Player());
                        if (rel != null && rel.isWar)
                        {
                            mf.meshRenderer.sharedMaterial = CampaignMap.Instance.UIMap.MinesEnemyMaterial;
                            mf.IsEnemy = true;
                        }
                        else
                        {
                            mf.meshRenderer.sharedMaterial = CampaignMap.Instance.UIMap.MinesFriendlyMaterial;
                            mf.IsEnemy = false;
                        }
                    }
                }
            }

            for (int i = 0, iC = cData.PlayersMajor.Count; i < iC; ++i)
            {
                var player = cData.PlayersMajor[i];
                var pData = player.data;
                if (pData.enabledForCampaign && player.IsAtWar() && cData.VesselsByPlayer.TryGetValue(pData, out var vessels))
                {
                    foreach (var v in vessels)
                    {
                        if (v.isSunk || v.isScrapped || v.status == VesselEntity.Status.Mothballed || v.status == VesselEntity.Status.LowCrew)
                            continue;

                        var mVal = v.GetMinelayingValue();
                        if (mVal > 0)
                        {
                            if (v.PortLocation != null)
                            {
                                if (!_this.minesPerPort.TryGetValue(v.PortLocation, out var portMines) || portMines.gameObject == null)
                                {
                                    portMines = _this.New(v.PortLocation.WorldCoord, v.PortLocation.CurrentProvince.ControllerPlayer, Il2CppSystem.Guid.Empty, v.PortLocation);
                                }
                                else
                                {
                                    var rel = RelationExt.Between(cData.Relations, PlayerController.Instance, v.PortLocation.CurrentProvince.ControllerPlayer);
                                    if (rel != null && rel.isWar)
                                    {
                                        portMines.meshRenderer.sharedMaterial = CampaignMap.Instance.UIMap.MinesEnemyMaterial;
                                        portMines.IsEnemy = true;
                                    }
                                    else
                                    {
                                        portMines.meshRenderer.sharedMaterial = CampaignMap.Instance.UIMap.MinesFriendlyMaterial;
                                        portMines.IsEnemy = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            for(int i = cData.TaskForces.Count; i-- > 0;)
            {
                var tf = cData.TaskForces[i];

                // FIXME: Feels like the moving check is inverted??
                // tf.CheckMinefieldOnPath should already be handling
                // moving through fields, surely?
                // And the submarine code below checks not-moving.
                // So I'm gonna change this to not-moving.
                if (tf.Vessels.Count == 0 || tf.IsMoving())
                    continue;

                for (int j = _this.mines.Count; j-- > 0;)
                {
                    var mf = _this.mines[j];
                    if (mf.gameObject == null || !mf.IsActive())
                        continue;
                    if (mf.GetOwner.name == tf.Controller.data.name)
                        continue;

                    bool hitMines = false;
                    
                    var maxDist = ((mf.Radius * 0.25f) + tf.GetZoneRadius()) * 0.005f;
                    var distance = Vector3.Distance(tf.WorldPos, mf.gameObject.transform.position);
                    if (maxDist > distance)
                    {
                        hitMines = true;
                        var displacement = DamageTaskForce(_this, tf, mf.GetPlayer(), mf.Radius, mf.DamageMultiplier);
                        MineHitSpenting(_this, displacement, mf);
                    }
                    if (tf.Vessels.Count > 0 && (hitMines || mf.Radius * 0.005f + tf.GetMinesweepingRadius(true) > distance))
                        Minesweep(_this, mf, tf);
                }
            }

            for (int i = cData.TaskForces.Count; i-- > 0;)
            {
                var tf = cData.TaskForces[i];
                if (tf.Vessels.Count == 0 || tf.GetVesselType() != VesselEntity.VesselType.Submarine)
                    continue;

                var mfRadius = tf.GetTaskForceMinefieldRadius() * 0.25f;
                var mfRad2 = mfRadius * mfRadius;
                for (int j = cData.TaskForces.Count; j-- > 0;)
                {
                    var tf2 = cData.TaskForces[j];
                    if (tf2 == tf || tf2.IsMoving())
                        continue;
                    // FIXME weirdly this doesn't check the controllers are
                    // different (!) which I think is a bug. So I'm checking.
                    if (tf.Controller == tf2.Controller)
                        continue;

                    if ((tf.WorldPos - tf2.WorldPos).sqrMagnitude > mfRad2)
                        continue;

                    DamageTaskForce(_this, tf2, tf.Controller, mfRadius, 1f);
                }
            }

            for (int i = _this.mines.Count; i-- > 0;)
            {
                var mf = _this.mines[i];
                bool nonAcitve = false;
                if (!mf.gameObject.activeSelf || mf.Radius < MinesFieldManager.MinFieldSize)
                {
                    nonAcitve = true;
                }
                else
                {
                    if (cData.PlayersByData.TryGetValue(mf.Owner, out var player) && player != null && !player.IsAtWar())
                    {
                        var randval = UnityEngine.Random.value;
                        mf.ChangeRadius(randval * -250f, 1f);

                    }
                }
                if (nonAcitve || !mf.IsActive())
                {
                    if (!mf.TaskForceId.Equals(Il2CppSystem.Guid.Empty))
                    {
                        if (_this.minesPerTaskForce.TryGetValue(mf.TaskForceId, out var list))
                            list.Remove(mf);
                    }
                    else
                    {
                        if (mf.Port != null)
                        {
                            _this.minesPerPort.Remove(mf.Port);
                        }
                    }
                    var owner = mf.GetOwner;
                    if (owner != null)
                    {
                        if (_this.MinesPerPlayer.TryGetValue(owner, out var list))
                            list.Remove(mf);
                    }

                    mf.DestroyField();
                    _this.mines.RemoveAt(i);
                }
            }

            _this.FillInfo(infoEnemyMines, _this.enemyMineFieldDamage);
            _this.FillInfo(infoFriendlyMines, _this.friendlyMineFieldDamage);
        }
    }
}