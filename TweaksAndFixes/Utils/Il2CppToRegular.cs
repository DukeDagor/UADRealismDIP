/*using Il2Cpp;
using MessagePack;
using System;
using UnityEngine;
using static Il2Cpp.Ship;
using static Il2Cpp.VesselEntity;

namespace TweaksAndFixes.Utils
{

    [MessagePackObject]
    public class MockQuaternion
    {
        [Key(0)]
        public float x { get; set; }
        [Key(1)]
        public float y { get; set; }
        [Key(2)]
        public float z { get; set; }
        [Key(3)]
        public float w { get; set; }

        public MockQuaternion()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 0;
        }

        public MockQuaternion(Quaternion store)
        {
            x = store.x;
            y = store.y;
            z = store.z;
            w = store.w;
        }
    }


    [MessagePackObject]
    public class MockVector3
    {
        [Key(0)]
        public float x { get; set; }
        [Key(1)]
        public float y { get; set; }
        [Key(2)]
        public float z { get; set; }

        public MockVector3()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public MockVector3(Vector3 store)
        {
            x = store.x;
            y = store.y;
            z = store.z;
        }
    }


    [MessagePackObject]
    public class MockGameDate
    {
        [Key(0)]
        public int turn { get; set; }

        public MockGameDate()
        {
            turn = 0;
        }

        public MockGameDate(GameDate store)
        {
            turn = store.turn;
        }
    }

    [MessagePackObject]
    public class MockTurretArmorStore
    {
        [Key(0)]
        public float topTurretArmor { get; set; }
        [Key(1)]
        public float sideTurretArmor { get; set; }
        [Key(2)]
        public float barbetteArmor { get; set; }
        [Key(3)]
        public string turretPartDataName { get; set; }
        [Key(4)]
        public bool isCasemateGun { get; set; }

        public MockTurretArmorStore()
        {
            topTurretArmor = 0;
            sideTurretArmor = 0;
            barbetteArmor = 0;
            turretPartDataName = String.Empty;
            isCasemateGun = false;
        }

        public MockTurretArmorStore(TurretArmor.Store store)
        {
            topTurretArmor = store.topTurretArmor;
            sideTurretArmor = store.sideTurretArmor;
            barbetteArmor = store.barbetteArmor;
            turretPartDataName = store.turretPartDataName;
            isCasemateGun = store.isCasemateGun;
        }

        public Ship.TurretArmor.Store toStore()
        {
            var store = new Ship.TurretArmor.Store();

            store.topTurretArmor = topTurretArmor;
            store.sideTurretArmor = sideTurretArmor;
            store.barbetteArmor = barbetteArmor;
            store.turretPartDataName = turretPartDataName;
            store.isCasemateGun = isCasemateGun;

            return store;
        }
    }

    [MessagePackObject]
    public class MockTurretCaliberStore
    {
        [Key(0)]
        public float diameter { get; set; }
        [Key(1)]
        public int length { get; set; }
        [Key(2)]
        public string turretPartDataName { get; set; }
        [Key(3)]
        public bool isCasemateGun { get; set; }

        public MockTurretCaliberStore()
        {
            diameter = 0;
            length = 0;
            turretPartDataName = String.Empty;
            isCasemateGun = false;
        }
        
        public MockTurretCaliberStore(TurretCaliber.Store store)
        {
            diameter = store.diameter;
            length = store.length;
            turretPartDataName = store.turretPartDataName;
            isCasemateGun = store.isCasemateGun;
        }

        public Ship.TurretCaliber.Store toStore()
        {
            var store = new Ship.TurretCaliber.Store();

            store.diameter = diameter;
            store.length = length;
            store.turretPartDataName = turretPartDataName;
            store.isCasemateGun = isCasemateGun;

            return store;
        }
    }

    [MessagePackObject]
    public class MockPartStore
    {
        [Key(0)]
        public string name { get; set; }
        [Key(1)]
        public Guid Id { get; set; }
        [Key(2)]
        public MockVector3 position { get; set; }
        [Key(3)]
        public MockQuaternion rotation { get; set; }

        public MockPartStore()
        {
            name = String.Empty;
            Id = Guid.Empty;
        }

        public MockPartStore(Part.Store store)
        {
            name = store.name;
            Id = new Guid(store.Id.ToString());
            position = new MockVector3(store.position);
            rotation = new MockQuaternion(store.rotation);
        }

        public Part.Store toStore()
        {
            var store = new Part.Store();

            store.name = name;
            store.Id = new Il2CppSystem.Guid(Id.ToString());
            store.position = new Vector3(position.x, position.y, position.z);
            store.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);

            return store;
        }
    }

    [MessagePackObject]
    public class MockAmmoStore
    {
        [Key(0)]
        public int ap { get; set; }
        [Key(1)]
        public int he { get; set; }
        [Key(2)]
        public int maxAP { get; set; }
        [Key(3)]
        public int maxHE { get; set; }

        public MockAmmoStore()
        {
            ap = 0;
            he = 0;
            maxAP = 0;
            maxHE = 0;
        }

        public MockAmmoStore(Ammo.Store store)
        {
            ap = store.ap;
            he = store.he;
            maxAP = store.maxAP;
            maxHE = store.maxHE;
        }
    }

    [MessagePackObject]
    public class MockShipsStore
    {
        [Key(0)]
        public Guid id { get; set; }
        [Key(1)]
        public string shipType { get; set; }
        [Key(2)]
        public List<KeyValuePair<string, string>> components { get; set; }
        [Key(3)]
        public List<string> techs { get; set; }
        [Key(4)]
        public string hullName { get; set; }
        [Key(5)]
        public float hullPartSizeZ { get; set; }
        [Key(6)]
        public float hullPartSizeY { get; set; }
        [Key(7)]
        public float hullPartMinZ { get; set; }
        [Key(8)]
        public float hullPartMaxZ { get; set; }
        [Key(9)]
        public float tonnage { get; set; }
        [Key(10)]
        public float beam { get; set; }
        [Key(11)]
        public float draught { get; set; }
        [Key(12)]
        public float instability_x { get; set; }
        [Key(13)]
        public float instability_z { get; set; }
        [Key(14)]
        public float instability_xx { get; set; }
        [Key(15)]
        public float instability_zz { get; set; }
        [Key(16)]
        public Survivability survivability { get; set; }
        [Key(17)]
        public List<float> CrewSkillDisperion { get; set; }
        [Key(18)]
        public CrewQuarters crewQuarters { get; set; }
        [Key(19)]
        public List<KeyValuePair<A, float>> armor { get; set; }
        [Key(20)]
        public List<MockTurretArmorStore> turretArmors { get; set; }
        [Key(21)]
        public List<MockTurretCaliberStore> turretCalibers { get; set; }
        [Key(22)]
        public List<MockPartStore> parts { get; set; }
        [Key(23)]
        public bool isSharedDesign { get; set; }
        [Key(24)]
        public string vesselName { get; set; }
        [Key(25)]
        public string playerName { get; set; }
        [Key(26)]
        public string playerOriginal { get; set; }
        [Key(27)]
        public float speedMax { get; set; }
        [Key(28)]
        public OpRange opRange { get; set; }
        [Key(29)]
        public float CrewTrainingValue { get; set; }
        [Key(30)]
        public int YearCreated { get; set; }
        [IgnoreMember]
        public int DataFileOrigin { get; set; }

        public MockShipsStore() { }

        public MockShipsStore(Ship.Store store)
        {
            id = new Guid(store.id.ToString());
            // designId = new Guid(store.designId.ToString());
            shipType = store.shipType;
            // refitProgress = store.refitProgress;
            // isRefitPaused = store.isRefitPaused;
            // isRefitSimple = store.isRefitSimple;
            // refitDesignName = store.refitDesignName;
            hullName = store.hullName;
            hullPartSizeZ = store.hullPartSizeZ;
            hullPartSizeY = store.hullPartSizeY;
            hullPartMinZ = store.hullPartMinZ;
            hullPartMaxZ = store.hullPartMaxZ;
            tonnage = store.tonnage;
            beam = store.beam;
            draught = store.draught;
            // designRefitTime = store.designRefitTime;
            instability_x = store.instability_x;
            instability_z = store.instability_z;
            instability_xx = store.instability_xx;
            instability_zz = store.instability_zz;
            // overweight = store.overweight;
            // underweight = store.underweight;
            // hull_defects = store.hull_defects;
            // weapon_defects = store.weapon_defects;
            survivability = store.survivability;

            CrewSkillDisperion = new();
            if (store.CrewSkillDisperion != null) foreach (var did in store.CrewSkillDisperion)
                {
                    CrewSkillDisperion.Add(did);
                }

            crewQuarters = store.crewQuarters;
            // ShipAmmoReplenished = store.ShipAmmoReplenished;
            // mission = store.mission;
            // isShipChoisedInCustomBattle = store.isShipChoisedInCustomBattle;
            // currentRole = store.currentRole;
            // ForSaleTo = store.ForSaleTo;
            // SaleProfit = store.SaleProfit;
            isSharedDesign = store.isSharedDesign;
            // isReparationDesign = store.isReparationDesign;
            // dateCreatedRefit = new MockGameDate(store.dateCreatedRefit);

            components = new();
            if (store.components != null) foreach (var did in store.components)
                {
                    components.Add(new KeyValuePair<string, string>(did.Key, did.Value));
                }

            techs = new();
            if (store.techs != null) foreach (var did in store.techs)
                {
                    techs.Add(did);
                }

            // statWeaponAndHullDefect = new();
            // if (store.statWeaponAndHullDefect != null) foreach (var did in store.statWeaponAndHullDefect)
            //     {
            //         statWeaponAndHullDefect.Add(new KeyValuePair<string, float>(did.Key, did.Value));
            //     }

            armor = new();
            if (store.armor != null) foreach (var did in store.armor)
                {
                    armor.Add(new KeyValuePair<A, float>(did.Key, did.Value));
                }
            
            turretArmors = new();
            if (store.turretArmors != null) foreach (var did in store.turretArmors)
                {
                    turretArmors.Add(new MockTurretArmorStore(did));
                }
            
            turretCalibers = new();
            if (store.turretCalibers != null) foreach (var did in store.turretCalibers)
                {
                    turretCalibers.Add(new MockTurretCaliberStore(did));
                }
            
            parts = new();
            if (store.parts != null) foreach (var did in store.parts)
                {
                    parts.Add(new MockPartStore(did));
                }

            // AmmoTotal = new();
            // if (store.AmmoTotal != null) foreach (var did in store.AmmoTotal)
            //     {
            //         AmmoTotal.Add(new KeyValuePair<string, int>(did.Key, did.Value));
            //     }
            // 
            // Ammo = new();
            // if (store.Ammo != null) foreach (var did in store.Ammo)
            //     {
            //         Ammo.Add(new KeyValuePair<string, MockAmmoStore>(did.Key, new MockAmmoStore(did.Value)));
            //     }
            // 
            // missionChoice = 0;
            // 
            // refitDesignListID = new();
            // if (store.refitDesignListID != null) foreach (var did in store.refitDesignListID)
            //     {
            //         refitDesignListID.Add(did);
            //     }

            vesselName = store.vesselName;
            playerName = store.playerName;
            playerOriginal = store.playerOriginal;
            speedMax = store.speedMax;
            opRange = store.opRange;
            CrewTrainingValue = store.CrewTrainingValue;
            YearCreated = store.YearCreated;
        }
        
        public Ship.Store toStore(Ship.Store Il2CppStore = null)
        {
            if (Il2CppStore == null) Il2CppStore = new();

            Il2CppStore.id = Il2CppSystem.Guid.Empty;
            Il2CppStore.designId = Il2CppSystem.Guid.Empty;
            Il2CppStore.shipType = shipType;
            Il2CppStore.refitProgress = 0;
            Il2CppStore.isRefitPaused = false;
            Il2CppStore.isRefitSimple = false;
            Il2CppStore.refitDesignName = null;
            Il2CppStore.hullName = hullName;
            Il2CppStore.hullPartSizeZ = hullPartSizeZ;
            Il2CppStore.hullPartSizeY = hullPartSizeY;
            Il2CppStore.hullPartMinZ = hullPartMinZ;
            Il2CppStore.hullPartMaxZ = hullPartMaxZ;
            Il2CppStore.tonnage = tonnage;
            Il2CppStore.beam = beam;
            Il2CppStore.draught = draught;
            Il2CppStore.designRefitTime = 0;
            Il2CppStore.instability_x = instability_x;
            Il2CppStore.instability_z = instability_z;
            Il2CppStore.instability_xx = instability_xx;
            Il2CppStore.instability_zz = instability_zz;
            Il2CppStore.overweight = 0;
            Il2CppStore.underweight = 0;
            Il2CppStore.hull_defects = 0;
            Il2CppStore.weapon_defects = 0;
            Il2CppStore.survivability = survivability;

            Il2CppStore.CrewSkillDisperion = new(4);
            int i = 0;
            if (CrewSkillDisperion != null) foreach (var did in CrewSkillDisperion)
                {
                    Il2CppStore.CrewSkillDisperion[i] = did;
                    i++;
                }

            Il2CppStore.crewQuarters = crewQuarters;
            Il2CppStore.ShipAmmoReplenished = 0;
            Il2CppStore.mission = null;
            Il2CppStore.isShipChoisedInCustomBattle = false;
            Il2CppStore.currentRole = 0;
            Il2CppStore.ForSaleTo = null;
            Il2CppStore.SaleProfit = 0;
            Il2CppStore.isSharedDesign = isSharedDesign;
            Il2CppStore.isReparationDesign = false;
            Il2CppStore.dateCreatedRefit = new GameDate();

            Il2CppStore.components = new();
            if (components != null) foreach (var did in components)
                {
                    Il2CppStore.components.Add(new Il2CppSystem.Collections.Generic.KeyValuePair<string, string>(did.Key, did.Value));
                }

            Il2CppStore.techs = new();
            if (techs != null) foreach (var did in techs)
                {
                    Il2CppStore.techs.Add(did);
                }

            Il2CppStore.statWeaponAndHullDefect = new();

            Il2CppStore.armor = new();
            if (armor != null) foreach (var did in armor)
                {
                    Il2CppStore.armor.Add(new Il2CppSystem.Collections.Generic.KeyValuePair<A, float>(did.Key, did.Value));
                }

            Il2CppStore.turretArmors = new();
            if (turretArmors != null) foreach (var did in turretArmors)
                {
                    Il2CppStore.turretArmors.Add(did.toStore());
                }

            Il2CppStore.turretCalibers = new();
            if (turretCalibers != null) foreach (var did in turretCalibers)
                {
                    Il2CppStore.turretCalibers.Add(did.toStore());
                }

            Il2CppStore.parts = new();
            if (parts != null) foreach (var did in parts)
                {
                    Il2CppStore.parts.Add(did.toStore());
                }

            Il2CppStore.AmmoTotal = null;

            Il2CppStore.Ammo = null;

            // Il2CppStore.missionChoice = new Il2CppSystem.Nullable<int>();

            Il2CppStore.refitDesignListID = new();

            Il2CppStore.vesselName = vesselName;
            Il2CppStore.seaGoupId = Il2CppSystem.Guid.Empty;
            Il2CppStore.playerName = playerName;
            Il2CppStore.playerOriginal = playerOriginal;

            Il2CppStore.dateCreated = new GameDate();
            Il2CppStore.dateFinished = new GameDate();
            Il2CppStore.status = 0;
            Il2CppStore.buildingProgress = 0;
            Il2CppStore.isBuildingPaused = false;
            Il2CppStore.repairingProgress = 0;
            Il2CppStore.isRepairingPaused = false;
            Il2CppStore.location = null;
            Il2CppStore.sailingTo = null;
            Il2CppStore.prevPortLocation = Il2CppSystem.String.Empty;
            Il2CppStore.portLocation = Il2CppSystem.String.Empty;
            Il2CppStore.speedMax = speedMax;
            Il2CppStore.opRange = opRange;
            Il2CppStore.CrewTrainingValue = CrewTrainingValue;
            Il2CppStore.Fuel = 100;
            Il2CppStore.ShipFuelReplenised = 0;
            Il2CppStore.lowCrewWasNotified = false;
            Il2CppStore.shipBuildingPortLocation = null;
            Il2CppStore.DelayedForBattle = false;
            Il2CppStore.LastBattleParticipating = 0;
            Il2CppStore.ReinforcementBattleId = Il2CppSystem.Guid.Empty;
            // Il2CppStore.CrewAmountPercents = new Il2CppSystem.Nullable<float>(1);
            Il2CppStore.returnAfterRepairToGroup = Il2CppSystem.Guid.Empty;
            Il2CppStore.IsTemporaryForWar = false;
            Il2CppStore.YearCreated = YearCreated;
            Il2CppStore.manualMothballed = false;
            Il2CppStore.techLevels = new();

            return Il2CppStore;
        }

        public Ship toInstance()
        {
            Ship ship = new();

            ship.id = Il2CppSystem.Guid.Empty;
            ship.shipType = G.GameData.shipTypes[shipType];
            ship.refitProgress = 0;
            ship.isRefitPaused = false;
            ship.isRefitSimple = false;
            ship.refitDesignName = null;
            // ship.hull = G.GameData.parts[hullName];
            ship.hullPartSizeZ = hullPartSizeZ;
            ship.hullPartSizeY = hullPartSizeY;
            ship.hullPartMinZ = hullPartMinZ;
            ship.hullPartMaxZ = hullPartMaxZ;
            ship.tonnage = tonnage;
            ship.beam = beam;
            ship.draught = draught;
            ship.designRefitTime = 0;
            ship.instability_x = instability_x;
            ship.instability_z = instability_z;
            ship.instability_xx = instability_xx;
            ship.instability_zz = instability_zz;
            ship.survivability = survivability;

            return ship;
        }
    }

    [MessagePackObject]
    public class MockShipsPerPlayerStore
    {
        [Key(0)]
        public Dictionary<string, List<MockShipsStore>> shipsPerType { get; set; }

        [IgnoreMember]
        public int shipCount { get; set; }

        public MockShipsPerPlayerStore()
        {
            shipsPerType = new();
        }

        public MockShipsPerPlayerStore(ShipsPerPlayer.Store store)
        {
            shipsPerType = new(store.shipsPerType.Count);

            foreach (var spy in store.shipsPerType)
            {
                List<MockShipsStore> ships = new(spy.Value.Count);

                foreach (var s in spy.Value)
                {
                    ships.Add(new MockShipsStore(s));
                }

                shipsPerType.Add(spy.Key, ships);
            }
        }

        public void Merge(MockShipsPerPlayerStore rhs)
        {
            foreach (var spt in rhs.shipsPerType)
            {
                shipCount += spt.Value.Count;

                if (shipsPerType.ContainsKey(spt.Key))
                {
                    foreach (var s in shipsPerType[spt.Key])
                    {
                        shipsPerType[spt.Key].Add(s);
                    }
                }
                else
                {
                    shipsPerType.Add(spt.Key, spt.Value);
                }
            }
        }

        public void CountData()
        {
            shipCount = 0;

            foreach (var spt in shipsPerType)
            {
                shipCount += spt.Value.Count;
            }
        }

        public void SetDataOrigin(int index)
        {
            foreach (var spt in shipsPerType)
            {
                foreach (var s in shipsPerType[spt.Key])
                {
                    s.DataFileOrigin = index;
                }
            }
        }
    }

    [MessagePackObject]
    public class MockShipsPerYearStore
    {
        [Key(0)]
        public Dictionary<string, MockShipsPerPlayerStore> shipsPerPlayer { get; set; }

        [Key(1)]
        public int year { get; set; }

        [IgnoreMember]
        public int shipCount { get; set; }

        public MockShipsPerYearStore()
        {
            shipsPerPlayer = new();
            year = 0;
        }

        public MockShipsPerYearStore(ShipsPerYear.Store store)
        {
            shipsPerPlayer = new(store.shipsPerPlayer.Count);
            year = store.year;

            foreach (var spp in store.shipsPerPlayer)
            {
                shipsPerPlayer.Add(spp.Key, new MockShipsPerPlayerStore(spp.Value));
            }
        }

        public void Merge(MockShipsPerYearStore rhs)
        {
            foreach (var spp in rhs.shipsPerPlayer)
            {
                shipCount += spp.Value.shipCount;

                if (shipsPerPlayer.ContainsKey(spp.Key))
                {
                    shipsPerPlayer[spp.Key].Merge(spp.Value);
                }
                else
                {
                    shipsPerPlayer.Add(spp.Key, spp.Value);
                }
            }
        }

        public void CountData()
        {
            shipCount = 0;

            foreach (var spp in shipsPerPlayer)
            {
                spp.Value.CountData();

                shipCount += spp.Value.shipCount;
            }
        }

        public void SetDataOrigin(int index)
        {
            foreach (var spp in shipsPerPlayer)
            {
                spp.Value.SetDataOrigin(index);
            }
        }
    }

    [MessagePackObject]
    public class MockCampaignDesignsStore
    {
        [Key(0)]
        public Dictionary<int, MockShipsPerYearStore> shipsPerYear { get; set; }

        [Key(1)]
        public int year { get; set; }

        [IgnoreMember]
        public int dataIndex { get; set; }

        [IgnoreMember]
        public int shipCount { get; set; }

        [IgnoreMember]
        public int yearMin { get; set; }

        [IgnoreMember]
        public int yearMax { get; set; }

        public MockCampaignDesignsStore()
        {
            shipsPerYear = new();
            year = 0;
        }

        public MockCampaignDesignsStore(CampaignDesigns.Store store)
        {
            shipsPerYear = new(store.shipsPerYear.Count);
            year = store.year;

            foreach (var spy in store.shipsPerYear)
            {
                shipsPerYear.Add(spy.Key, new MockShipsPerYearStore(spy.Value));
            }
        }

        public void Merge(MockCampaignDesignsStore rhs)
        {
            foreach (var spy in rhs.shipsPerYear)
            {
                shipCount += spy.Value.shipCount;
                if (yearMin > spy.Value.year) yearMin = spy.Value.year;
                if (yearMax < spy.Value.year) yearMax = spy.Value.year;

                if (shipsPerYear.ContainsKey(spy.Key))
                {
                    shipsPerYear[spy.Key].Merge(spy.Value);
                }
                else
                {
                    shipsPerYear.Add(spy.Key, spy.Value);
                }
            }
        }

        public void CountData()
        {
            shipCount = 0;
            yearMin = int.MaxValue;
            yearMax = int.MinValue;

            foreach (var spy in shipsPerYear)
            {
                spy.Value.CountData();

                shipCount += spy.Value.shipCount;
                if (yearMin > spy.Key) yearMin = spy.Key;
                if (yearMax < spy.Key) yearMax = spy.Key;
            }
        }

        public void SetDataOrigin(int index)
        {
            dataIndex = index;

            foreach (var spy in shipsPerYear)
            {
                spy.Value.SetDataOrigin(index);
            }
        }

        public Dictionary<string, MockCampaignDesignsStore> SplitByPlayer()
        {
            Dictionary<string, MockCampaignDesignsStore> results = new();

            foreach (var player in G.GameData.playersMajor)
            {
                results[player.Key] = new();
            }

            foreach (var spy in shipsPerYear)
            {
                foreach (var spp in spy.Value.shipsPerPlayer)
                {
                    results[spp.Key].shipsPerYear.ValueOrNew(spy.Key)
                        .shipsPerPlayer.Add(spp.Key, spp.Value);
                }
            }

            return results;
        }

        public MockShipsStore GetRandomShip(Player player, ShipType type, int desiredYear)
        {
            if (!shipsPerYear.ContainsKey(desiredYear)) return null;

            if (!shipsPerYear[desiredYear].shipsPerPlayer.ContainsKey(player.data.name)) return null;

            if (!shipsPerYear[desiredYear].shipsPerPlayer[player.data.name].shipsPerType.ContainsKey(type.name)) return null;

            var list = shipsPerYear[desiredYear].shipsPerPlayer[player.data.name].shipsPerType[type.name];

            if (list.Count == 0) return null;

            return list[(int)(UnityEngine.Random.value * list.Count - 0.01)];
        }
    }
}
*/