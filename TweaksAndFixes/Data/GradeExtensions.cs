//#define LOGHULLSTATS
//#define LOGHULLSCALES
//#define LOGPARTSTATS
//#define LOGGUNSTATS

using MelonLoader;
using Il2Cpp;

#pragma warning disable CS0649
#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8625

namespace TweaksAndFixes
{
    public static class GradeExtensions
    {
        public class GunDataExtension : Serializer.IPostProcess
        {
            [Serializer.Field] string name;
            [Serializer.Field] string param;

            public void PostProcess()
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(param))
                    return;

                if (!G.GameData.guns.TryGetValue(name, out var gd))
                    return;

                // To make life easier for modders, let them use the column names
                // in the csv rather than the names of the fields.
                param = param.Replace("firerate(", "firerates(");
                param = param.Replace("barrelW(", "barrelWeights(");
                param = param.Replace("firerate(", "firerates(");
                param = param.Replace("shellW(", "shellWeights(");
                param = param.Replace("shellV(", "shellVelocities(");
                param = param.Replace("range(", "ranges(");
                param = param.Replace("accuracy(", "accuracies(");
                param = param.Replace("penetration(", "penetrations(");

                Serializer.Human.FillIndexedDicts(gd, param, true);

                int max = GetMaxGrade(gd);
                Config.MaxGunGrade = Math.Max(Config.MaxGunGrade, max);
            }

            public static int GetMaxGrade(GunData gd)
            {
                int max = 6;
                for (; ; ++max)
                {
                    if (gd.accuracies.ContainsKey(max)
                        || gd.barrelWeights.ContainsKey(max)
                        || gd.firerates.ContainsKey(max)
                        || gd.penetrations.ContainsKey(max)
                        || gd.ranges.ContainsKey(max)
                        || gd.shellVelocities.ContainsKey(max)
                        || gd.shellWeights.ContainsKey(max)
                        || gd.accuracies.ContainsKey(max))
                        continue;

                    break;
                }
                return max - 1;
            }

            public static void LoadData()
            {
                var text = Serializer.CSV.GetTextFromFileOrAsset("guns");
                if (text != null)
                {
                    List<GunDataExtension> list = new List<GunDataExtension>();
                    Serializer.CSV.Read<List<GunDataExtension>, GunDataExtension>(text, list, true, true);
                }

                // We need to ensure that everything goes up to the max grade.
                foreach (var gd in G.GameData.guns.Values)
                {
                    ModUtils.FillGradeData(gd.accuracies, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.barrelWeights, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.firerates, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.penetrations, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.ranges, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.shellVelocities, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.shellWeights, Config.MaxGunGrade);
                    ModUtils.FillGradeData(gd.accuracies, Config.MaxGunGrade);
                }
                if (Config.MaxGunGrade > 5)
                {
                    GunData.gradesTotal = Config.MaxGunGrade;
                    Melon<TweaksAndFixes>.Logger.Msg("Max gun grade is now " + Config.MaxGunGrade);
                }
            }
        }

        public class PartModelExtension : Serializer.IPostProcess
        {
            [Serializer.Field] string name;
            [Serializer.Field] string param;

            public void PostProcess()
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(param))
                    return;

                if (!G.GameData.partModels.TryGetValue(name, out var pm))
                    return;

                // To make life easier for modders, let them use the column names
                // in the csv rather than the names of the fields.
                param = param.Replace("model(", "models(");
                param = param.Replace("scale(", "scales(");
                param = param.Replace("max_scale(", "maxScales(");
                param = param.Replace("weight_modifier(", "weightModifiers(");
                param = param.Replace("caliber_length_modifier(", "caliberLengthModifiers(");

                Serializer.Human.FillIndexedDicts(pm, param, true);
            }

            public static void LoadData()
            {
                var text = Serializer.CSV.GetTextFromFileOrAsset("partModels");
                if (text != null)
                {
                    List<PartModelExtension> list = new List<PartModelExtension>();
                    Serializer.CSV.Read<List<PartModelExtension>, PartModelExtension>(text, list, true, true);
                }

                // We need to ensure that everything goes up to the max grade.
                foreach (var pm in G.GameData.partModels.Values)
                {
                    // Skip partmodels that aren't guns
                    if (pm.caliberLengthModifiers.count < 5)
                        continue;

                    ModUtils.FillGradeData(pm.caliberLengthModifiers, Config.MaxGunGrade);
                    ModUtils.FillGradeData(pm.maxScales, Config.MaxGunGrade);
                    ModUtils.FillGradeData(pm.models, Config.MaxGunGrade);
                    ModUtils.FillGradeData(pm.scales, Config.MaxGunGrade);
                    ModUtils.FillGradeData(pm.weightModifiers, Config.MaxGunGrade);
                }
            }
        }

        public static void LoadData()
        {
            GunDataExtension.LoadData();
            PartModelExtension.LoadData();
        }
    }
}