using System.IO;
using System.Text.Json;

namespace HideInformation
{
    public class ModConfig
    {
        private static readonly string ConfigDir = Path.Combine("mods", "HideInformation", "config");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.txt");

        public bool HideRelics { get; set; } = true;
        public bool HideText { get; set; } = true;
        public bool HideCreature { get; set; } = true;
        public bool HidePathIcon { get; set; } = false;
        public bool HideIntents { get; set; } = false;
        public bool HideCardPortraits { get; set; } = true;

        // 普通文本替换内容：用于 Godot.Label。
        // 例如："?"、"隐藏"、""。
        public string PlainTextReplacement { get; set; } = "";

        // 富文本替换内容：用于 Godot.RichTextLabel。
        // 可以写 Godot 富文本 / BBCode 风格内容，例如：
        // "[color=gold]?[/color]"
        public string RichTextReplacement { get; set; } = "";

        // MegaLabel / 数字文本替换内容。
        // 这里默认给 "?"，避免数字类文本完全消失后看不出占位。
        public string MegaTextReplacement { get; set; } = "?";

        public static ModConfig Load()
        {
            Directory.CreateDirectory(ConfigDir);

            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<ModConfig>(json) ?? new ModConfig();

                // 重新保存一次，让旧配置文件自动补上新增字段。
                cfg.Save();
                return cfg;
            }

            var newCfg = new ModConfig();
            newCfg.Save();
            return newCfg;
        }

        public void Save()
        {
            Directory.CreateDirectory(ConfigDir);

            string json = JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(ConfigPath, json);
        }
    }
}