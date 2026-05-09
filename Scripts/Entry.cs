using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using static Godot.Control;

namespace HideInformation.Scripts;

[ModInitializer("Init")]
public class Entry
{
    public static ModConfig CurrentConfig { get; private set; }
    public static Texture2D cardTexture;

    private static Texture2D lockpic;

    public static void Init()
    {
        CurrentConfig = ModConfig.Load();

        var harmony = new Harmony("hideInformation");
        harmony.PatchAll();

        ModConfigBridge.DeferredRegister();

        lockpic = LoaRelicTexture();
        cardTexture = LoadQuestionTexture();

        Log.Info("hideInformation initialized!");
    }

    private static Texture2D LoadQuestionTexture()
    {
        string configQuestionPath = Path.Combine("mods", "HideInformation", "config", "question.png");

        if (File.Exists(configQuestionPath))
        {
            try
            {
                var image = Image.LoadFromFile(configQuestionPath);
                if (image != null)
                {
                    Log.Info($"Loaded custom question image: {configQuestionPath}");
                    return ImageTexture.CreateFromImage(image);
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load custom question image: {configQuestionPath}, {e.Message}");
            }
        }

        Log.Info("Using built-in question image: res://Images/question.png");
        return ResourceLoader.Load<Texture2D>("res://Images/question.png");
    }

    private static Texture2D LoaRelicTexture()
    {
        string relicPath = Path.Combine("mods", "HideInformation", "config", "relic.png");

        if (File.Exists(relicPath))
        {
            try
            {
                var image = Image.LoadFromFile(relicPath);
                if (image != null)
                {
                    Log.Info($"Loaded custom question image: {relicPath}");
                    return ImageTexture.CreateFromImage(image);
                }
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to load custom question image: {relicPath}, {e.Message}");
            }
        }
        Log.Info("Using built-in question image: res://Images/Lock.png");
        return ResourceLoader.Load<Texture2D>("res://Images/Lock.png");
    }

    //---------------------------------------------------遗物图片---------------------------------------------------
    [HarmonyPatch(typeof(NRelic), "Reload")]
    class NRelic_Reload_Patch
    {
        static void Postfix(NRelic __instance)
        {
            if (!Entry.CurrentConfig.HideRelics) return;

            if (__instance.Icon != null)
            {
                __instance.Icon.Texture = Entry.lockpic;
                if (__instance.Outline != null)
                    __instance.Outline.Visible = false;
            }
        }
    }

    [HarmonyPatch(typeof(NInspectRelicScreen), "UpdateRelicDisplay")]
    class NInspectRelicScreen_UpdateRelicDisplay_Patch
    {
        static void Postfix(NInspectRelicScreen __instance)
        {
            if (!Entry.CurrentConfig.HideRelics) return;

            var relicImage = __instance.GetNodeOrNull<TextureRect>("%RelicImage");
            if (relicImage != null)
                relicImage.Texture = Entry.lockpic;
        }
    }

    [HarmonyPatch(typeof(RelicModel), "get_Icon")]
    class RelicModel_Icon_Patch
    {
        static bool Prefix(ref Texture2D __result)
        {
            if (!Entry.CurrentConfig.HideRelics) return true;
            __result = Entry.lockpic;
            return false;
        }
    }

    [HarmonyPatch(typeof(RelicModel), "get_BigIcon")]
    class RelicModel_BigIcon_Patch
    {
        static bool Prefix(ref Texture2D __result)
        {
            if (!Entry.CurrentConfig.HideRelics) return true;
            __result = Entry.lockpic;
            return false;
        }
    }

    //---------------------------------------------------视觉模型---------------------------------------------------
    [HarmonyPatch(typeof(NCreature), "_Ready")]
    class NCreature_Ready_Patch
    {
        static void Postfix(NCreature __instance)
        {
            if (!Entry.CurrentConfig.HideCreature) return;

            if (__instance.Visuals != null)
                __instance.Visuals.Visible = false;

            var hitbox = __instance.GetNodeOrNull<Control>("%Hitbox");
            if (hitbox == null) return;

            var panel = new Panel();
            var styleBox = new StyleBoxFlat();

            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = Colors.Green;
            styleBox.BgColor = Colors.Transparent;

            panel.AddThemeStyleboxOverride("panel", styleBox);
            panel.Size = hitbox.Size;
            panel.Position = Vector2.Zero;
            panel.Name = "MonsterBorder";
            panel.MouseFilter = MouseFilterEnum.Ignore;

            hitbox.AddChild(panel);
        }
    }

    //---------------------------------------------------君王之剑---------------------------------------------------
    [HarmonyPatch(typeof(NSovereignBladeVfx), "_Ready")]
    class NSovereignBladeVfx_Ready_Patch
    {
        static void Postfix(NSovereignBladeVfx __instance)
        {
            if (!Entry.CurrentConfig.HideCreature) return;

            var spineNode = __instance.GetNodeOrNull<Node2D>("SpineSword");
            var bone = spineNode?.GetNodeOrNull<Node2D>("SwordBone");

            if (bone != null)
                bone.Modulate = Colors.Transparent;

            var hitbox = __instance.GetNodeOrNull<Control>("%Hitbox");
            if (hitbox == null) return;

            var panel = new Panel();
            var styleBox = new StyleBoxFlat();

            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = Colors.Green;
            styleBox.BgColor = Colors.Transparent;

            panel.AddThemeStyleboxOverride("panel", styleBox);
            panel.Size = hitbox.Size;
            panel.Position = Vector2.Zero;
            panel.MouseFilter = Control.MouseFilterEnum.Ignore;
            panel.Name = "SovereignBladeBorder";

            hitbox.AddChild(panel);
        }
    }

    //---------------------------------------------------文本隐藏---------------------------------------------------
    [HarmonyPatch]
    public static class TextHider
    {
        [HarmonyPatch(typeof(MegaLabel), nameof(MegaLabel.SetTextAutoSize))]
        [HarmonyPrefix]
        public static void MegaLabel_SetTextAutoSize_Prefix(ref string text)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (string.IsNullOrEmpty(text)) return;

            text = Entry.CurrentConfig.MegaTextReplacement ?? "";
        }

        [HarmonyPatch(typeof(Label), nameof(Label.Text), MethodType.Setter)]
        [HarmonyPrefix]
        public static void Label_Text_Setter_Prefix(ref string value)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (string.IsNullOrEmpty(value)) return;

            value = Entry.CurrentConfig.PlainTextReplacement ?? "";
        }

        [HarmonyPatch(typeof(RichTextLabel), nameof(RichTextLabel.Text), MethodType.Setter)]
        [HarmonyPrefix]
        public static void RichTextLabel_Text_Setter_Prefix(ref string value)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (string.IsNullOrEmpty(value)) return;

            value = Entry.CurrentConfig.RichTextReplacement ?? "";
        }
    }

    //---------------------------------------------------地图隐藏---------------------------------------------------
    [HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
    class NNormalMapPoint_UpdateIcon_Patch
    {
        static void Postfix(NNormalMapPoint __instance)
        {
            if (!Entry.CurrentConfig.HidePathIcon) return;

            var questIcon = __instance.GetNodeOrNull<TextureRect>("%QuestIcon");
            if (questIcon != null)
                questIcon.Visible = false;

            string unknownIconPath = "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres";
            string unknownOutlinePath = "res://images/atlases/compressed.sprites/map/map_unknown_outline.tres";

            var icon = __instance.GetNodeOrNull<TextureRect>("%Icon");
            if (icon != null)
                icon.Texture = ResourceLoader.Load<Texture2D>(
                    unknownIconPath,
                    null,
                    ResourceLoader.CacheMode.Reuse
                );

            var outline = __instance.GetNodeOrNull<TextureRect>("%Outline");
            if (outline != null)
                outline.Texture = ResourceLoader.Load<Texture2D>(
                    unknownOutlinePath,
                    null,
                    ResourceLoader.CacheMode.Reuse
                );
        }
    }

    //---------------------------------------------------意图隐藏---------------------------------------------------
    [HarmonyPatch(typeof(NIntent), "_Ready")]
    class NIntent_Ready_Patch
    {
        static void Postfix(NIntent __instance)
        {
            if (!Entry.CurrentConfig.HideIntents) return;

            __instance.Visible = false;
            __instance.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    [HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
    class NIntent_UpdateVisuals_Patch
    {
        static void Postfix(NIntent __instance)
        {
            if (!Entry.CurrentConfig.HideIntents) return;

            __instance.Visible = false;
            __instance.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.ShowHoverTips))]
    class NCreature_ShowHoverTips_Patch
    {
        static bool Prefix(NCreature __instance, IEnumerable<IHoverTip> hoverTips)
        {
            if (!Entry.CurrentConfig.HideIntents)
                return true;

            if (__instance.Entity.IsMonster)
                return false;

            return true;
        }
    }

    //---------------------------------------------------卡图隐藏---------------------------------------------------
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
    class CardModel_Portrait_Patch
    {
        static void Postfix(CardModel __instance, ref Texture2D __result)
        {
            if (!Entry.CurrentConfig.HideCardPortraits) return;

            __result = Entry.cardTexture;
        }
    }
}