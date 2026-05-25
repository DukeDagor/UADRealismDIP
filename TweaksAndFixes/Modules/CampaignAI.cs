using Il2Cpp;
using MelonLoader;
using System.Data;
using UnityEngine;

namespace TweaksAndFixes.Modules
{
    internal class CampaignAI
    {
        public class FleetRole : Serializer.IPostProcess
        {
            public enum PortType
            {
                all,
                main,
                sec
            }

            public enum GoalType
            {
                combat,
                raiding,
                scouting,
                defense,
                convoy
            }

            public static Dictionary<string, FleetRole> fleetRoles = new Dictionary<string, FleetRole>();

            [Serializer.Field]
            public string name;

            [Serializer.Field]
            public string nation;

            [Serializer.Field]
            public string portType;

            [Serializer.Field]
            public string goals;

            [Serializer.Field]
            public string growth;

            public PlayerData nationData;

            public PortType portTypeParsed;

            public List<GoalType> goalsParsed;

            public FleetRole()
            {
                name = string.Empty;
                nation = string.Empty;
                portType = string.Empty;
                goals = string.Empty;
                growth = string.Empty;
                nationData = new PlayerData();
                portTypeParsed = PortType.all;
                goalsParsed = new List<GoalType>();
            }

            public void PostProcess()
            {
                if (fleetRoles.ContainsKey(name) || !G.GameData.playersMajor.ContainsKey(nation))
                {
                    return;
                }
                nationData = G.GameData.playersMajor[nation];
                switch (portType)
                {
                    case "all":
                        portTypeParsed = PortType.all;
                        break;
                    case "main":
                        portTypeParsed = PortType.main;
                        break;
                    case "sec":
                        portTypeParsed = PortType.sec;
                        break;
                }
                string[] goalList = goals.Replace(" ", "").Split(",");
                for (int i = 0; i < goalList.Length; i++)
                {
                    switch (goalList[i])
                    {
                        case "combat":
                            goalsParsed.Add(GoalType.combat);
                            break;
                        case "raiding":
                            goalsParsed.Add(GoalType.raiding);
                            break;
                        case "scouting":
                            goalsParsed.Add(GoalType.scouting);
                            break;
                        case "defense":
                            goalsParsed.Add(GoalType.defense);
                            break;
                        case "convoy":
                            goalsParsed.Add(GoalType.convoy);
                            break;
                    }
                }
                fleetRoles.Add(name, this);
            }
        }

        public class FleetShip : Serializer.IPostProcess
        {
            public enum FormationType
            {
                battleLine,
                scout,
                follow,
                screen,
                defend
            }

            public static Dictionary<string, List<FleetShip>> fleetShips = new Dictionary<string, List<FleetShip>>();

            [Serializer.Field]
            public string fleetName;

            [Serializer.Field]
            public string formationType;

            [Serializer.Field]
            public string ships;

            [Serializer.Field]
            public int startYear;

            [Serializer.Field]
            public int endYear;

            public FormationType formationTypeParsed;

            public Dictionary<ShipClassCatagory, Tuple<string, float>> shipsParsed;

            public FleetShip()
            {
                fleetName = string.Empty;
                formationType = string.Empty;
                ships = string.Empty;
                shipsParsed = new Dictionary<ShipClassCatagory, Tuple<string, float>>();
            }

            public void PostProcess()
            {
                switch (formationType)
                {
                    case "battleLine":
                        formationTypeParsed = FormationType.battleLine;
                        break;
                    case "scout":
                        formationTypeParsed = FormationType.scout;
                        break;
                    case "follow":
                        formationTypeParsed = FormationType.follow;
                        break;
                    case "screen":
                        formationTypeParsed = FormationType.screen;
                        break;
                    case "defend":
                        formationTypeParsed = FormationType.defend;
                        break;
                }
                string[] shipList = ships.Split(",");
                foreach (string ship in shipList)
                {
                    int bodyStart = ship.IndexOf("(");
                    int bodyEnd = ship.IndexOf(")");

                    if (bodyStart == -1)
                    {
                        continue;
                    }
                    
                    string catagoryName = ship.Substring(0, bodyStart);
                    if (!ShipClassCatagory.shipClassGroups.ContainsKey(catagoryName))
                    {
                        continue;
                    }
                    
                    ShipClassCatagory catagory = ShipClassCatagory.shipClassGroups[catagoryName];
                    if (!shipsParsed.ContainsKey(catagory))
                    {
                        string[] args = ship.Substring(bodyStart + 1, bodyEnd - bodyStart - 1).Split(";");
                        if (args.Length == 2 && float.TryParse(args[1], out var result))
                        {
                            shipsParsed.Add(catagory, new Tuple<string, float>(args[0], result));
                        }
                    }
                }

                if (startYear == -1)
                {
                    startYear = Config.StartingYear;
                }

                if (endYear == -1)
                {
                    endYear = int.MaxValue;
                }
                
                fleetShips.ValueOrNew(fleetName).Add(this);
            }

            public Dictionary<ShipClassCatagory, Tuple<int, float>> DesiredShips(int x)
            {
                Dictionary<ShipClassCatagory, Tuple<int, float>> result = new();
                foreach (KeyValuePair<ShipClassCatagory, Tuple<string, float>> ship in shipsParsed)
                {
                    int item = Convert.ToInt32(new DataTable().Compute(ship.Value.Item1.Replace("x", x.ToString()), null));
                    result.Add(ship.Key, new Tuple<int, float>(item, ship.Value.Item2));
                }
                return result;
            }
        }

        public class ShipClassCatagory : Serializer.IPostProcess
        {
            public class Requirement
            {
                public enum RequirementType
                {
                    armor_max,
                    armor_min,
                    speed_max,
                    speed_min,
                    highest_tonnage,
                    lowest_tonnage,
                    highest_price,
                    lowest_price
                }

                public RequirementType type;

                public Dictionary<string, float> requirementByClass;

                public Requirement(string strType, string[] reqs)
                {
                    requirementByClass = new Dictionary<string, float>();
                    switch (strType)
                    {
                        case "armor_max":
                            type = RequirementType.armor_max;
                            break;
                        case "armor_min":
                            type = RequirementType.armor_min;
                            break;
                        case "speed_max":
                            type = RequirementType.speed_max;
                            break;
                        case "speed_min":
                            type = RequirementType.speed_min;
                            break;
                        case "highest_tonnage":
                            type = RequirementType.highest_tonnage;
                            break;
                        case "lowest_tonnage":
                            type = RequirementType.lowest_tonnage;
                            break;
                        case "highest_price":
                            type = RequirementType.highest_price;
                            break;
                        case "lowest_price":
                            type = RequirementType.lowest_price;
                            break;
                    }

                    string shipType = "all";
                    foreach (string req in reqs)
                    {
                        if (!float.TryParse(req, out var result))
                        {
                            shipType = req;
                        }
                        requirementByClass.Add(shipType, result);
                    }
                }

                public bool MeetsRequirements(Ship.Store ship)
                {
                    string shipType = ship.shipType;
                    if (requirementByClass.ContainsKey("all"))
                    {
                        shipType = "all";
                    }
                    else if (!requirementByClass.ContainsKey(shipType))
                    {
                        return false;
                    }
                    switch (type)
                    {
                        case RequirementType.armor_max:
                            if (ship.armor[2].Value > requirementByClass[shipType])
                            {
                                return false;
                            }
                            break;
                        case RequirementType.armor_min:
                            if (ship.armor[2].Value < requirementByClass[shipType])
                            {
                                return false;
                            }
                            break;
                        case RequirementType.speed_max:
                            if (ship.speedMax > requirementByClass[shipType])
                            {
                                return false;
                            }
                            break;
                        case RequirementType.speed_min:
                            if (ship.speedMax < requirementByClass[shipType])
                            {
                                return false;
                            }
                            break;
                    }
                    return true;
                }
            }

            public static Dictionary<string, ShipClassCatagory> shipClassGroups = new Dictionary<string, ShipClassCatagory>();

            [Serializer.Field]
            public string id;

            [Serializer.Field]
            public string classList;

            [Serializer.Field]
            public string requirements;

            public List<string> classListParsed;

            public Dictionary<string, Requirement> requirementsParsed;

            public ShipClassCatagory()
            {
                id = string.Empty;
                classList = string.Empty;
                requirements = string.Empty;
                classListParsed = new List<string>();
                requirementsParsed = new Dictionary<string, Requirement>();
            }

            public void PostProcess()
            {
                if (shipClassGroups.ContainsKey(id))
                {
                    return;
                }
                classList = classList.Replace(" ", "");
                classListParsed.AddRange(classList.Split(","));
                requirements = requirements.Replace(" ", "");
                string[] reqs = requirements.Split(",");
                foreach (string req in reqs)
                {
                    int bodyStart = req.IndexOf("(");
                    int bodyEnd = req.IndexOf(")");
                    if (bodyStart == -1)
                    {
                        requirementsParsed.Add(req, new Requirement(req, Array.Empty<string>()));
                        continue;
                    }
                    string reqName = req.Substring(0, bodyStart);
                    if (!requirementsParsed.ContainsKey(reqName))
                    {
                        requirementsParsed.Add(reqName, new Requirement(reqName, req.Substring(bodyStart + 1, bodyEnd - bodyStart - 1).Split(";")));
                    }
                }
            }

            public bool MeetsRequirements(Ship.Store ship)
            {
                foreach (KeyValuePair<string, Requirement> item in requirementsParsed)
                {
                    if (!item.Value.MeetsRequirements(ship))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public class PersistantTaskForce
        {
            public enum Type
            {
                Battle,
                Raid,
                Convoy,
                Defend
            }

            public Type type { get; set; }

            public Dictionary<ShipType, List<Ship>> ships { get; set; }

            public List<CampaignController.TaskForce> taskForces { get; set; }

            public List<PortElement> homePorts { get; set; }

            public PersistantTaskForce(Type type)
            {
                this.type = type;
                ships = new Dictionary<ShipType, List<Ship>>();
                foreach(var shipType in G.GameData.shipTypes)
                {
                    ships.Add(shipType.Value, new List<Ship>());
                }
                taskForces = new List<CampaignController.TaskForce>();
                homePorts = new List<PortElement>();
            }

            public void AddShip(Ship ship)
            {
                ships.ValueOrNew(ship.shipType).Add(ship);
            }

            public int CountShipsOfClassRole()
            {
                return 0;
            }

            public int GetTaskForceDevValue()
            {
                return 0;
            }

            public Dictionary<ShipClassCatagory, Tuple<int, float>> DesiredShips(Player player)
            {
                if (type == Type.Battle)
                {
                    foreach (KeyValuePair<ShipType, List<Ship>> ship in ships)
                    {
                        _ = ship;
                    }
                }

                else if (type == Type.Defend)
                {
                    int smallShipCount = 0;
                    smallShipCount += ships[G.GameData.shipTypes["dd"]].Count;
                    smallShipCount += ships[G.GameData.shipTypes["tb"]].Count;

                    int portCount = 0;
                    foreach (var province in player.provincesWithPort)
                    {
                        portCount += province.Ports.Count;
                    }

                    if (smallShipCount < portCount)
                    {
                        return new Dictionary<ShipClassCatagory, Tuple<int, float>> {
                            {
                                new ShipClassCatagory(),
                                new Tuple<int, float>(portCount - smallShipCount, 100f)
                            }
                        };
                    }
                }

                return new Dictionary<ShipClassCatagory, Tuple<int, float>>();
            }
        }

        /*
        Expand loop
        1. Create task force of each type
        2. Pick from fleetRoles.csv
        3. Build dev level to taf_maxFleetDevLevel (account for minor losses)
        4. Create new force of type, repeat from 2

        
        */

        public class PlayerFleetManager
        {
            public Player player { get; set; }

            public Dictionary<ShipType, List<Ship>> ships { get; set; }

            public Dictionary<PersistantTaskForce.Type, List<PersistantTaskForce>> taskForces { get; set; }

            public PlayerFleetManager(Player player)
            {
                this.player = player;
                ships = new Dictionary<ShipType, List<Ship>>();
                taskForces = new Dictionary<PersistantTaskForce.Type, List<PersistantTaskForce>>();
            }

            public void AddShip(Ship ship, PersistantTaskForce.Type tfType, int tfID)
            {
                ships.ValueOrNew(ship.shipType).Add(ship);
                List<PersistantTaskForce> taskForcesOfType = taskForces.ValueOrNew(tfType);
                if (taskForcesOfType.Count < tfID)
                {
                    taskForcesOfType[tfID].AddShip(ship);
                }
            }
        }

        public static List<string> GetHubPorts(PlayerData player, float threash, int min = 2, List<string>? ignore = null)
        {
            Melon<TweaksAndFixes>.Logger.Msg("Get hub ports for " + player.nameUi + ":");
            List<string> portList = new List<string>(CampaignMap.Instance.Ports.PortById.Keys.Count);
            foreach (var port in CampaignMap.Instance.Ports.PortById)
            {
                if ((ignore == null || !ignore.Contains(port.Key))
                    && CampaignMap.Instance.Provinces.ProvinceById[port.Value.ProvinceId].ControllerPlayer.data == player)
                {
                    portList.Add(port.Key);
                }
            }

            portList.Sort(
                (string a, string b) => 
                    CampaignMap.Instance.Ports.PortById[b]
                        .GetPortCapacityWithoutDamage()
                        .CompareTo(CampaignMap.Instance.Ports.PortById[a].GetPortCapacityWithoutDamage())
            );

            float capacityOfLargestPort = CampaignMap.Instance.Ports.PortById[portList[0]].GetPortCapacityWithoutDamage();
            List<string> hubPorts = new List<string> { portList[0] };
            Melon<TweaksAndFixes>.Logger.Msg(
                $"Largest port {CampaignMap.Instance.Ports.PortById[portList[0]].Name} has cap of {capacityOfLargestPort}"
            );
            
            Dictionary<string, float> relitivePortCapacities = new Dictionary<string, float>();
            for (int i = 0; i < portList.Count; i++)
            {
                float portCapacity = CampaignMap.Instance.Ports.PortById[portList[i]].GetPortCapacityWithoutDamage();
                relitivePortCapacities.Add(portList[i], portCapacity / capacityOfLargestPort);
            }

            Melon<TweaksAndFixes>.Logger.Msg("Finding hubs:");
            Il2CppSystem.Collections.Generic.List<Il2CppSystem.Collections.Generic.List<Vector3>> simplifiedPath = new();
            bool result = false;
            Melon<TweaksAndFixes>.Logger.Msg("====== Added " + CampaignMap.Instance.Ports.PortById[portList[0]].Name + " as hub! ======");
            while (hubPorts.Count < min && threash > 0f)
            {
                foreach (KeyValuePair<string, float> port in relitivePortCapacities)
                {
                    if (port.Value < threash)
                    {
                        continue;
                    }

                    PortElement portElem = CampaignMap.Instance.Ports.PortById[port.Key];
                    Melon<TweaksAndFixes>.Logger.Msg("  Checking " + portElem.Name + "...");
                    bool bad = false;
                    foreach (string hubPort in hubPorts)
                    {
                        PortElement hubPortElem = CampaignMap.Instance.Ports.PortById[hubPort];
                        float pathLength = ModUtils.distance(hubPortElem.NearestNavmeshPoint, portElem.NearestNavmeshPoint);
                        if (pathLength < 20f)
                        {
                            Melon<TweaksAndFixes>.Logger.Msg($"   [><] {hubPortElem.Name} -> {portElem.Name} = {pathLength}");
                            Pathfinding.SimplifiedPath(
                                Pathfinding.Find(
                                    hubPortElem.NearestNavmeshPoint,
                                    portElem.NearestNavmeshPoint,
                                    out result, null, allowPartial: false
                                ),
                                1f, out simplifiedPath, out pathLength
                            );

                            if (pathLength < 20f)
                            {
                                Melon<TweaksAndFixes>.Logger.Msg($"       [><] {hubPortElem.Name} -> {portElem.Name} = {pathLength}");
                                bad = true;
                                break;
                            }
                        }
                        Melon<TweaksAndFixes>.Logger.Msg($"   [./] {hubPortElem.Name} -> {portElem.Name} = {pathLength}");
                    }

                    if (!bad)
                    {
                        hubPorts.Add(port.Key);
                        Melon<TweaksAndFixes>.Logger.Msg("====== Added " + portElem.Name + " as hub! ======");
                    }
                }
                threash -= 0.05f;
            }
            Melon<TweaksAndFixes>.Logger.Msg("");
            return hubPorts;
        }

        public static float CashWithoutReserve(Player player)
        {
            return player.CashAndIncome() - player.Budget();
        }

        public static void UpdatePlayerFinances(Player player)
        {
            Melon<TweaksAndFixes>.Logger.Msg($"{player.transportCapacityBudget} : {player.trainingBudget} : {player.techBudget}");
            player.transportCapacityBudget = 0f;
            player.trainingBudget = 0f;
            player.techBudget = 0f;

            if (player.transportCapacity < 2f || player.IsAtWar())
            {
                float gdp = player.NationYearIncome();
                float trCapExpense = 
                    Mathf.Lerp(1f, 500f, gdp * 2E-12f)
                    * Config.Param("transport_capacity_factor", 10.0f)
                    * Mathf.Lerp(3f, 0.7f, player.transportCapacity * 0.5f)
                    / (gdp * 5E-07f);

                Melon<TweaksAndFixes>.Logger.Msg($"{player.transportCapacityBudget} -> {trCapExpense} / {2f - player.transportCapacity} = {(2f - player.transportCapacity) / trCapExpense}");
                float atWarBonus = player.IsAtWar() ? 0.01f : 0f;
                float targetGrowth = ModUtils.Clamp((2f - player.transportCapacity + atWarBonus) / trCapExpense, 0f, 1f);
                player.transportCapacityBudget = targetGrowth;
                if (CashWithoutReserve(player) < 0f)
                {
                    float trCostPerPercent = 
                        Config.Param("tr_capacity_cost", 0.01f) * gdp * 0.5f
                        * Mathf.Lerp(1.25f, 0.75f, player.transportCapacity * 0.5f)
                        * Mathf.Lerp(1f, 0.75f, gdp / 1E+12f)
                        * Mathf.Lerp(0.75f, 1.875f, player.StateBudget() / 5E+12f);
                    float trBudget = CashWithoutReserve(player) + player.ExpensesTransportCapacity();
                    float maxTrGrowthPercent = trBudget / trCostPerPercent - 1f;
                    Melon<TweaksAndFixes>.Logger.Msg($"{trBudget} / {trCostPerPercent} - 1 = {maxTrGrowthPercent}");
                    player.transportCapacityBudget = ModUtils.Clamp(Math.Min(targetGrowth, maxTrGrowthPercent), 0f, 1f);
                }
            }

            float budget = CashWithoutReserve(player);

            player.trainingBudget = 1f;
            if (player.ExpensesTrainingBudget() > budget * 0.5f)
            {
                player.trainingBudget = 0f;
            }
            else if (player.ExpensesTrainingBudget() > budget * 0.25f)
            {
                player.trainingBudget = 0.5f;
            }

            if (player.shipyardBuildMonthLeft == 0)
            {
                Melon<TweaksAndFixes>.Logger.Msg($"{player.shipyard} : {player.MaxShipyard()}");
                Technology technology = null;
                foreach (var tech in player.technologies)
                {
                    if (tech.data.name.StartsWith("hull_strength") && !tech.isResearched)
                    {
                        technology = tech;
                        break;
                    }
                }

                Melon<TweaksAndFixes>.Logger.Msg("Found: " + ((technology == null) ? "Null" : technology.data.name));
                if (technology != null && technology.data.effects.ContainsKey("unlock"))
                {
                    List<PartData> list = new List<PartData>();
                    float maxTonnage = 0f;
                    float totalTonnage = 0f;
                    float averageTonnage = 0f;
                    foreach (var unlocks in technology.data.effects["unlock"][0])
                    {
                        PartData partData = G.GameData.parts[unlocks];
                        if (partData.countriesx.Contains(player.data))
                        {
                            list.Add(partData);
                            if (partData.tonnageMax > maxTonnage)
                            {
                                maxTonnage = partData.tonnageMax;
                            }
                            totalTonnage += partData.tonnageMin + (partData.tonnageMax - partData.tonnageMin) / 2f;
                            averageTonnage = totalTonnage / (float)list.Count;
                        }
                    }

                    float researchSpeed = CampaignController.Instance.GetResearchSpeed(player, technology);
                    Melon<TweaksAndFixes>.Logger.Msg($"{technology.progress} : {researchSpeed} | {maxTonnage} : {averageTonnage}");
                    
                    int minBuildTime = Config.Param("shipyard_dev_min_time_months", 6);
                    int buildTimeRange = Config.Param("shipyard_dev_max_time_months", 24) - minBuildTime;
                    int minBuildTonnage = Config.Param("shipyard_dev_min_amount_tons", 1000);
                    int maxBuildTonnage = Config.Param("shipyard_dev_max_amount_tons", 4000);
                    
                    float minExpansionSize = CampaignController.GetBaseYearMultiplier(8f, 1f, useCurrentYear: true) * (float)minBuildTonnage;
                    float maxExpansionSize = CampaignController.GetBaseYearMultiplier(8f, 1f, useCurrentYear: true) * (float)maxBuildTonnage;
                    float expansionSizeRange = maxExpansionSize - minExpansionSize;
                    
                    if (maxTonnage - player.shipyard > minExpansionSize)
                    {
                        float desiredTonnage = ModUtils.Clamp(maxTonnage - player.shipyard, maxExpansionSize * 0.75f, maxExpansionSize);
                        
                        // float percentOfMaxExpansion = (maxTonnage - player.shipyard) / maxExpansionSize;
                        // if ((double)percentOfMaxExpansion - Math.Floor(percentOfMaxExpansion) < 0.5)
                        // {
                        //     desiredTonnage = maxExpansionSize * 0.66f;
                        // }

                        float expansionBuildTime = (float)minBuildTime + (desiredTonnage - minExpansionSize) / expansionSizeRange * (float)buildTimeRange;
                        float expansionCost = Config.Param("shipyard_dev_cost", 25f) * desiredTonnage * Mathf.Lerp(1f, 20f, player.shipyard / 350000f);
                        Melon<TweaksAndFixes>.Logger.Msg($"{minExpansionSize} : {maxExpansionSize} | {player.shipyard} : {maxTonnage} | {desiredTonnage} Tons : {expansionBuildTime} Mo. : {expansionCost} $ : {expansionCost / expansionBuildTime} $/Mo.");
                    }
                }
            }

            float techReduction = Mathf.Lerp(1f, 0.35f, player.NationYearIncome() / 1E+12f) * (player.Budget() / 100f);
            player.techBudget = ModUtils.Clamp(CashWithoutReserve(player) / techReduction, 0f, 50f);
            Melon<TweaksAndFixes>.Logger.Msg($"{CashWithoutReserve(player)} / {techReduction} = {player.techBudget}");
        }
    }
}
