using UnityEngine;

namespace WorldGen
{
    public static class WorldGenerator
    {
        public static WorldSeedData Generate(int seed)
        {
            // 骨架：返回空种子，后续 Layer 2 实现
            var data = new WorldSeedData();
            data.seedId = "seed_" + seed.ToString("D5");
            data.seedCode = EncodeSeed(seed);
            return data;
        }

        public static WorldSeedData LoadFromResources(string seedId)
        {
            TextAsset json = Resources.Load<TextAsset>("Seeds/" + seedId);
            if (json == null) return null;
            return JsonUtility.FromJson<WorldSeedData>(json.text);
        }

        public static string EncodeSeed(int seed)
        {
            return "RR-" + seed.ToString("X5") + "-" + ((seed * 3) & 0xFFFFF).ToString("X5");
        }

        public static int DecodeSeed(string seedCode)
        {
            // 预留：解码 RR-XXXXX-YYYYY
            return 0;
        }
    }
}