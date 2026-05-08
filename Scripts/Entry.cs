
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.InspectScreens;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using System.Reflection;
using static Godot.Control;

namespace HideInformation.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer("Init")]
public class Entry
{
    public static ModConfig CurrentConfig { get; private set; }

    // 初始化函数
    private static Texture2D lockpic;
    public static void Init()
    {
        ModConfig config = ModConfig.Load();
        CurrentConfig = ModConfig.Load();

        var harmony = new Harmony("hideInformation");
        harmony.PatchAll();

        lockpic = ResourceLoader.Load<Texture2D>("res://Images/Lock.png");


        Log.Info("hideInformation initialized!");
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

            // 创建边框面板
            var hitbox = __instance.GetNodeOrNull<Control>("%Hitbox");

            var panel = new Panel();
            var styleBox = new StyleBoxFlat();
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = Colors.Green;
            styleBox.BgColor = Colors.Transparent;
            panel.AddThemeStyleboxOverride("panel", styleBox);
            panel.Size = hitbox?.Size ?? new Vector2(100, 100);
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
            var bone = spineNode.GetNodeOrNull<Node2D>("SwordBone");

            if (bone != null)
                bone.Modulate = Colors.Transparent;

            var hitbox = __instance.GetNodeOrNull<Control>("%Hitbox");

            var panel = new Panel();
            var styleBox = new StyleBoxFlat();
            styleBox.BorderWidthLeft = 2;
            styleBox.BorderWidthRight = 2;
            styleBox.BorderWidthTop = 2;
            styleBox.BorderWidthBottom = 2;
            styleBox.BorderColor = Colors.Green;
            styleBox.BgColor = Colors.Transparent;
            panel.AddThemeStyleboxOverride("panel", styleBox);
            panel.Size = hitbox?.Size ?? new Vector2(100, 100);
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
        
        //数字变量
        [HarmonyPatch(typeof(MegaCrit.Sts2.addons.mega_text.MegaLabel), nameof(MegaCrit.Sts2.addons.mega_text.MegaLabel.SetTextAutoSize))]
        [HarmonyPrefix]
        public static void MegaLabel_SetTextAutoSize_Prefix(ref string text)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (!string.IsNullOrEmpty(text))
                text = "?";   // 直接替换为问号
        }


        // 一切文本
        [HarmonyPatch(typeof(Label), nameof(Label.Text), MethodType.Setter)]
        [HarmonyPrefix]
        public static void Label_Text_Setter_Prefix(ref string value)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (!string.IsNullOrEmpty(value))
                value = "";
        }


        [HarmonyPatch(typeof(RichTextLabel), nameof(RichTextLabel.Text), MethodType.Setter)]
        [HarmonyPrefix]
        public static void RichTextLabel_Text_Setter_Prefix(ref string value)
        {
            if (!Entry.CurrentConfig.HideText) return;
            if (!string.IsNullOrEmpty(value))
                value = "";
        }
    }
    //---------------------------------------------------地图隐藏---------------------------------------------------
    [HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
    class NNormalMapPoint_UpdateIcon_Patch
    {
        static void Postfix(NNormalMapPoint __instance)
        {
            var questIcon = __instance.GetNodeOrNull<TextureRect>("%QuestIcon");
            if (questIcon != null)
                questIcon.Visible = false;

            string unknownIconPath = "res://images/atlases/ui_atlas.sprites/map/icons/map_unknown.tres";
            string unknownOutlinePath = "res://images/atlases/compressed.sprites/map/map_unknown_outline.tres";

            var icon = __instance.GetNodeOrNull<TextureRect>("%Icon");
            if (icon != null)
                icon.Texture = ResourceLoader.Load<Texture2D>(unknownIconPath, null, ResourceLoader.CacheMode.Reuse);

            var outline = __instance.GetNodeOrNull<TextureRect>("%Outline");
            if (outline != null)
                outline.Texture = ResourceLoader.Load<Texture2D>(unknownOutlinePath, null, ResourceLoader.CacheMode.Reuse);
        }
    }

    //---------------------------------------------------意图隐藏---------------------------------------------------
    [HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
    class NIntent_UpdateVisuals_Patch
    {
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
}

