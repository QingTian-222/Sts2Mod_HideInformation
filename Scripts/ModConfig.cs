using System.IO;
using System.Text.Json;

namespace HideInformation
{
    public class ModConfig
    {
        // 使用 .txt 后缀，避免被游戏当作模组清单
        private static readonly string ConfigDir = Path.Combine("mods", "HideInformation", "config");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.txt");

        public bool HideRelics { get; set; } = true;
        public bool HideText { get; set; } = true;
        public bool HideCreature { get; set; } = true;
        public bool HidePathIcon { get; set; } = false;
        public bool HideIntents { get; set; } = false;

        public static ModConfig Load()
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<ModConfig>(json) ?? new ModConfig();
            }
            else
            {
                Directory.CreateDirectory(ConfigDir);
                var cfg = new ModConfig();
                cfg.Save();
                return cfg;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}