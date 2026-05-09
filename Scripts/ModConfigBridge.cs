using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HideInformation;

namespace HideInformation.Scripts;

/// <summary>
/// ModConfig-STS2 bridge.
/// 零依赖反射接入：不直接引用 ModConfig.dll。
/// 玩家没装 ModConfig 时，不会影响你的 mod 正常运行。
/// </summary>
internal static class ModConfigBridge
{
    internal const string ModId = "hideInformation";

    private const string DisplayNameEn = "Hide Information";
    private const string DisplayNameZh = "隐藏信息";

    private static bool _available;
    private static bool _registered;

    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    private static int _framesLeft;

    internal static bool IsAvailable => _available;

    internal static void DeferredRegister()
    {
        try
        {
            var tree = Engine.GetMainLoop() as SceneTree;
            if (tree == null) return;

            _framesLeft = 2;
            tree.ProcessFrame += OnProcessFrame;
        }
        catch
        {
            // ModConfig 是可选依赖，失败时不要影响主 mod。
        }
    }

    private static void OnProcessFrame()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null) return;

        _framesLeft--;
        if (_framesLeft > 0) return;

        tree.ProcessFrame -= OnProcessFrame;

        Detect();
        if (!_available) return;

        Register();
        ApplySavedValuesToLocalConfig();
    }

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Type.EmptyTypes;
                    }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");

            _available = _apiType != null && _entryType != null && _configTypeEnum != null;
        }
        catch
        {
            _available = false;
        }
    }

    private static void Register()
    {
        if (_registered) return;
        _registered = true;

        try
        {
            var entries = BuildEntries();
            var displayNames = L(DisplayNameEn, DisplayNameZh);

            var registerMethod = _apiType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Register")
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (registerMethod == null) return;

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModId,
                        DisplayNameEn,
                        displayNames,
                        entries
                    }
                );
            }
            else
            {
                registerMethod.Invoke(
                    null,
                    new object[]
                    {
                        ModId,
                        DisplayNameEn,
                        entries
                    }
                );
            }

            GD.Print("[HideInformation] Registered ModConfig entries.");
        }
        catch (Exception e)
        {
            GD.PrintErr($"[HideInformation] ModConfig registration failed: {e}");
        }
    }

    internal static T GetValue<T>(string key, T fallback)
    {
        if (!_available) return fallback;

        try
        {
            var method = _apiType!.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static);
            var generic = method?.MakeGenericMethod(typeof(T));
            var result = generic?.Invoke(null, new object[] { ModId, key });

            return result is T value ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    internal static void SetValue(string key, object value)
    {
        if (!_available) return;

        try
        {
            _apiType!.GetMethod("SetValue", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { ModId, key, value });
        }
        catch
        {
            // 可选配置，不影响主 mod。
        }
    }

    private static void ApplySavedValuesToLocalConfig()
    {
        if (Entry.CurrentConfig == null) return;

        Entry.CurrentConfig.HideRelics =
            GetValue("hideRelics", Entry.CurrentConfig.HideRelics);

        Entry.CurrentConfig.HideText =
            GetValue("hideText", Entry.CurrentConfig.HideText);

        Entry.CurrentConfig.HideCreature =
            GetValue("hideCreature", Entry.CurrentConfig.HideCreature);

        Entry.CurrentConfig.HidePathIcon =
            GetValue("hidePathIcon", Entry.CurrentConfig.HidePathIcon);

        Entry.CurrentConfig.HideIntents =
            GetValue("hideIntents", Entry.CurrentConfig.HideIntents);

        Entry.CurrentConfig.HideCardPortraits =
            GetValue("hideCardPortraits", Entry.CurrentConfig.HideCardPortraits);

        Entry.CurrentConfig.PlainTextReplacement =
            GetValue("plainTextReplacement", Entry.CurrentConfig.PlainTextReplacement ?? "");

        Entry.CurrentConfig.RichTextReplacement =
            GetValue("richTextReplacement", Entry.CurrentConfig.RichTextReplacement ?? "");

        Entry.CurrentConfig.MegaTextReplacement =
            GetValue("megaTextReplacement", Entry.CurrentConfig.MegaTextReplacement ?? "?");

        Entry.CurrentConfig.Save();
    }

    private static Array BuildEntries()
    {
        var list = new List<object>();

        list.Add(EntryItem(cfg =>
        {
            Set(cfg, "Label", "Hide Information");
            Set(cfg, "Labels", L("Hide Information", "隐藏信息设置"));
            Set(cfg, "Type", EnumVal("Header"));
        }));

        AddToggle(
            list,
            key: "hideText",
            labelEn: "Hide Text",
            labelZh: "隐藏文本",
            descEn: "Hide most labels and rich text in the game.",
            descZh: "隐藏游戏中的大部分普通文本和富文本。",
            defaultValue: true,
            apply: v => Entry.CurrentConfig.HideText = v
        );

        AddTextInput(
            list,
            key: "plainTextReplacement",
            labelEn: "Plain Text Replacement",
            labelZh: "普通文本替换内容",
            descEn: "Replacement for normal Label text. Empty means hide it completely.",
            descZh: "普通 Label 文本的替换内容。留空表示完全隐藏。",
            defaultValue: Entry.CurrentConfig.PlainTextReplacement,
            placeholder: "例如：? / ??? / 留空",
            maxLength: 128,
            apply: v => Entry.CurrentConfig.PlainTextReplacement = v
        );

        AddTextInput(
            list,
            key: "richTextReplacement",
            labelEn: "Rich Text Replacement",
            labelZh: "富文本替换内容",
            descEn: "Replacement for RichTextLabel text. You can use tags like [color=gold]?[/color].",
            descZh: "RichTextLabel 富文本的替换内容。可以写类似 [color=gold]?[/color] 的标签。",
            defaultValue: Entry.CurrentConfig.RichTextReplacement,
            placeholder: "例如：[color=gold]?[/color] / 留空",
            maxLength: 256,
            apply: v => Entry.CurrentConfig.RichTextReplacement = v
        );

        AddTextInput(
            list,
            key: "megaTextReplacement",
            labelEn: "Mega Text / Number Replacement",
            labelZh: "数字与 MegaText 替换内容",
            descEn: "Replacement for MegaLabel auto-size text, often used by numbers or variable text.",
            descZh: "MegaLabel 自动缩放文本的替换内容，常用于数字或变量文本。",
            defaultValue: Entry.CurrentConfig.MegaTextReplacement,
            placeholder: "例如：? / X / 留空",
            maxLength: 64,
            apply: v => Entry.CurrentConfig.MegaTextReplacement = v
        );

        AddToggle(
            list,
            key: "hideCardPortraits",
            labelEn: "Hide Card Portraits",
            labelZh: "隐藏卡图",
            descEn: "Replace card portraits with the question-mark image.",
            descZh: "将卡牌插图替换为问号图。",
            defaultValue: true,
            apply: v => Entry.CurrentConfig.HideCardPortraits = v
        );

        AddToggle(
            list,
            key: "hideRelics",
            labelEn: "Hide Relics",
            labelZh: "隐藏遗物图标",
            descEn: "Replace relic icons with the lock image.",
            descZh: "将遗物图标替换为锁图。",
            defaultValue: true,
            apply: v => Entry.CurrentConfig.HideRelics = v
        );

        AddToggle(
            list,
            key: "hideCreature",
            labelEn: "Hide Creatures",
            labelZh: "隐藏生物模型",
            descEn: "Hide creature visuals and draw a simple hitbox border.",
            descZh: "隐藏生物视觉模型，并显示简单边框。",
            defaultValue: true,
            apply: v => Entry.CurrentConfig.HideCreature = v
        );

        AddToggle(
            list,
            key: "hidePathIcon",
            labelEn: "Hide Map Path Icons",
            labelZh: "隐藏地图路径图标",
            descEn: "Replace normal map point icons with unknown icons.",
            descZh: "将普通地图节点图标替换为未知图标。",
            defaultValue: false,
            apply: v => Entry.CurrentConfig.HidePathIcon = v
        );

        AddToggle(
            list,
            key: "hideIntents",
            labelEn: "Hide Intents",
            labelZh: "隐藏敌人意图",
            descEn: "Hide monster intent icons and related hover tips.",
            descZh: "隐藏敌人意图图标及相关悬停提示。",
            defaultValue: false,
            apply: v => Entry.CurrentConfig.HideIntents = v
        );

        var result = Array.CreateInstance(_entryType!, list.Count);

        for (int i = 0; i < list.Count; i++)
            result.SetValue(list[i], i);

        return result;
    }

    private static void AddToggle(
        List<object> list,
        string key,
        string labelEn,
        string labelZh,
        string descEn,
        string descZh,
        bool defaultValue,
        Action<bool> apply
    )
    {
        var localDefault = GetLocalDefault(key, defaultValue);

        list.Add(EntryItem(cfg =>
        {
            Set(cfg, "Key", key);
            Set(cfg, "Label", labelEn);
            Set(cfg, "Labels", L(labelEn, labelZh));
            Set(cfg, "Type", EnumVal("Toggle"));
            Set(cfg, "DefaultValue", localDefault);
            Set(cfg, "Description", descEn);
            Set(cfg, "Descriptions", L(descEn, descZh));

            Set(cfg, "OnChanged", new Action<object>(v =>
            {
                bool enabled = Convert.ToBoolean(v);
                apply(enabled);
                Entry.CurrentConfig.Save();
            }));
        }));
    }

    private static void AddTextInput(
        List<object> list,
        string key,
        string labelEn,
        string labelZh,
        string descEn,
        string descZh,
        string defaultValue,
        string placeholder,
        int maxLength,
        Action<string> apply
    )
    {
        var localDefault = GetLocalStringDefault(key, defaultValue ?? "");

        list.Add(EntryItem(cfg =>
        {
            Set(cfg, "Key", key);
            Set(cfg, "Label", labelEn);
            Set(cfg, "Labels", L(labelEn, labelZh));
            Set(cfg, "Type", EnumVal("TextInput"));
            Set(cfg, "DefaultValue", localDefault);
            Set(cfg, "Description", descEn);
            Set(cfg, "Descriptions", L(descEn, descZh));
            Set(cfg, "Placeholder", placeholder);
            Set(cfg, "MaxLength", maxLength);

            Set(cfg, "OnChanged", new Action<object>(v =>
            {
                string value = v?.ToString() ?? "";
                apply(value);
                Entry.CurrentConfig.Save();
            }));
        }));
    }

    private static bool GetLocalDefault(string key, bool fallback)
    {
        var cfg = Entry.CurrentConfig;
        if (cfg == null) return fallback;

        return key switch
        {
            "hideRelics" => cfg.HideRelics,
            "hideText" => cfg.HideText,
            "hideCreature" => cfg.HideCreature,
            "hidePathIcon" => cfg.HidePathIcon,
            "hideIntents" => cfg.HideIntents,
            "hideCardPortraits" => cfg.HideCardPortraits,
            _ => fallback
        };
    }

    private static string GetLocalStringDefault(string key, string fallback)
    {
        var cfg = Entry.CurrentConfig;
        if (cfg == null) return fallback;

        return key switch
        {
            "plainTextReplacement" => cfg.PlainTextReplacement ?? "",
            "richTextReplacement" => cfg.RichTextReplacement ?? "",
            "megaTextReplacement" => cfg.MegaTextReplacement ?? "?",
            _ => fallback
        };
    }

    private static object EntryItem(Action<object> configure)
    {
        var inst = Activator.CreateInstance(_entryType!)!;
        configure(inst);
        return inst;
    }

    private static void Set(object obj, string name, object value)
    {
        obj.GetType().GetProperty(name)?.SetValue(obj, value);
    }

    private static Dictionary<string, string> L(string en, string zhs)
    {
        return new Dictionary<string, string>
        {
            ["en"] = en,
            ["zhs"] = zhs
        };
    }

    private static object EnumVal(string name)
    {
        return Enum.Parse(_configTypeEnum!, name);
    }
}