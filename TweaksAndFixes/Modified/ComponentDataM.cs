using Il2Cpp;

#pragma warning disable CS8603

namespace TweaksAndFixes
{
    public class ComponentDataM
    {
        private static readonly Dictionary<int, Dictionary<int, float>> _ComponentWeightCache = new Dictionary<int, Dictionary<int, float>>();

        public static float GetWeight(ComponentData c, ShipType sType)
        {
            int cHash = c.GetHashCode();
            int stHash = sType.GetHashCode();
            float weight = c.weight;
            if (_ComponentWeightCache.TryGetValue(cHash, out var dict))
            {
                if (dict.TryGetValue(stHash, out float cw))
                    return cw;

                return weight;
            }

            dict = new Dictionary<int, float>();
            if (c.paramx.TryGetValue("weight_per", out var pairs))
            {
                //Melon<TweaksAndFixes>.Logger.Msg($"Have weight_per, count is {list.Count}");

                var kvps = Serializer.Human.ParamToParsedKVPs<string, float>(pairs);
                //string logStr = $"Component {c.name} has override weights:";
                foreach (var kvp in kvps)
                {
                    if (!G.GameData.shipTypes.TryGetValue(kvp.Key, out var st))
                        continue;
                    var hash = st.GetHashCode();
                    dict[hash] = kvp.Value;
                    if (hash == stHash)
                        weight = kvp.Value;

                    //logStr += $"  {kvp.Key}={kvp.Value:F0}";
                }
                //Melon<TweaksAndFixes>.Logger.Msg(logStr);
            }
            _ComponentWeightCache[cHash] = dict;
            return weight;
        }
    }
}