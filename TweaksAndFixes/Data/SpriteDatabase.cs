//#define LOGHULLSTATS
//#define LOGHULLSCALES
//#define LOGPARTSTATS
//#define LOGGUNSTATS

using MelonLoader;
using UnityEngine;
using Il2Cpp;

#pragma warning disable CS8600
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable CS8625

namespace TweaksAndFixes
{
    [RegisterTypeInIl2Cpp]
    public class SpriteDatabase : MonoBehaviour
    {
        public class SpriteData
        {
            [Serializer.Field] public string name = string.Empty;
            [Serializer.Field] private string file = string.Empty;
            [Serializer.Field] private int width = 64;
            [Serializer.Field] private int height = 64;
            //[Serializer.Field] private TextureFormat format = TextureFormat.DXT5;

            public Sprite Get()
            {
                var sprite = Instance.GetSprite(name);
                if (sprite == null)
                {
                    string basePath = Path.Combine(Config._BasePath, "Sprites");
                    if (!Directory.Exists(basePath))
                    {
                        Melon<TweaksAndFixes>.Logger.Error("Failed to find Sprites directory: " + basePath);
                        return null;
                    }
                    string filePath = Path.Combine(basePath, file);
                    if (!File.Exists(filePath))
                    {
                        Melon<TweaksAndFixes>.Logger.Error("Failed to find sprite image file " + filePath);
                        return null;
                    }

                    var rawData = File.ReadAllBytes(filePath);
                    // Unity is going to replace this with DXT5 no matter what we put,
                    // if we're loading a PNG, and Texture2D.LoadImage() isn't supported.
                    // So no point in specifying a format in the data.
                    var tex = new Texture2D(width, height, TextureFormat.DXT5, true);
                    if (!ImageConversion.LoadImage(tex, rawData))
                    {
                        Melon<TweaksAndFixes>.Logger.Error("Failed to load sprite image file " + filePath);
                    }
                    sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                    Instance.AddSprite(name, sprite);
                }

                return sprite;
            }
        }

        public SpriteDatabase(IntPtr ptr) : base(ptr) { }

        private static void Ensure()
        {
            if (_Instance == null)
            {
                var go = new GameObject("SpriteDataHolder");
                go.AddComponent<SpriteDatabase>();
            }
        }
        private static SpriteDatabase _Instance = null;
        public static SpriteDatabase Instance
        {
            get
            {
                Ensure();
                return _Instance;
            }
        }

        public static void Recreate()
        {
            if (_Instance != null)
            {
                GameObject.DestroyImmediate(_Instance.gameObject);
                _Instance = null;
                Ensure();
            }
        }

    private Dictionary<string, SpriteData> _spriteData = new Dictionary<string, SpriteData>();
    private Il2CppSystem.Collections.Generic.Dictionary<string, Sprite> _sprites = new Il2CppSystem.Collections.Generic.Dictionary<string, Sprite>();

        private void Awake()
        {
            if (_Instance != null)
                GameObject.Destroy(_Instance);
            _Instance = this;
            LoadData();
        }

        private void OnDestroy()
        {
            if (_Instance == this)
                _Instance = null;
        }

        public void AddSprite(string name, Sprite sprite)
        {
            _sprites[name] = sprite;
        }

        public Sprite GetSprite(string name)
        {
            if (_sprites.TryGetValue(name, out var sprite))
                return sprite;

            return null;
        }

        private void LoadData()
        {
            if (!Config._SpriteFile.Exists)
                return;

            Melon<TweaksAndFixes>.Logger.Msg("Loading sprites database");
            Serializer.CSV.Read<Dictionary<string, SpriteData>, string, SpriteData>(_spriteData, Config._SpriteFile, "name", true);
        }

        public void OverrideResources()
        {
            foreach (var kvp in _spriteData)
            {
                var sprite = kvp.Value.Get();
                if (sprite)
                    Util.resCache[kvp.Key] = sprite;
            }
        }
    }
}
