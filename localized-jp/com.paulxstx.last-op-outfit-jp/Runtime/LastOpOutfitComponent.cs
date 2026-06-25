// LastOpOutfitComponent.cs
// 最终操作换装生成器：通用套装项目切换 + 通用混搭版配置组件

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Paulxstx.LastOpOutfitPlugin
{
    [Serializable]
    public class ClothesCloseOption
    {
        [InspectorName("关闭开关名称")]
        public string displayName = "关闭部件A";

        [InspectorName("要关闭的部件")]
        [Tooltip("一个开关可以拖多个部件。打开这个开关时，这些物体会被 MA 物体开关关闭。")]
        public List<GameObject> objects = new List<GameObject> { null };

        [InspectorName("保存状态")]
        public bool saved = true;

        [InspectorName("网络同步")]
        public bool synced = true;
    }

    [Serializable]
    public class MixDefaultSetting
    {
        [InspectorName("混搭项目名称")]
        [Tooltip("需要和“混搭项目列表”里的名称一致，例如：鞋子、袜子、内衣、帽子。")]
        public string mixGroupName = "鞋子";

        [HideInInspector]
        public string parameterName = "Shoes";

        [InspectorName("选中时是否修改")]
        [Tooltip("关闭后，选中这个套装项目切换项时不会改这个混搭项目。")]
        public bool enabled = true;

        [InspectorName("自动跟随当前选项值")]
        [Tooltip("开启后，这个默认混搭值会自动等于当前套装项目切换选项的参数值。例如衣服B=2，则鞋子/袜子默认也会变成2。关闭后可以手动自定义值。")]
        public bool autoFollowItemValue = true;

        [InspectorName("默认参数值")]
        [Tooltip("-1 = 不修改；0 = 不穿/关闭；1/2/3... = 切到对应选项。")]
        [Range(-1, 254)]
        public int value = 1;
    }

    [Serializable]
    public class SuperSwitchItemOption
    {
        [InspectorName("菜单显示名")]
        public string displayName = "衣服A";

        [InspectorName("参数值")]
        [Range(1, 254)]
        public int value = 1;

        [InspectorName("默认混搭设置")]
        [Tooltip("选中这个套装项目切换项时，自动写入哪些混搭项目的默认值。")]
        public List<MixDefaultSetting> defaultMixValues = new List<MixDefaultSetting>
        {
            new MixDefaultSetting { mixGroupName = "鞋子", parameterName = "Shoes", enabled = true, autoFollowItemValue = true, value = 1 },
            new MixDefaultSetting { mixGroupName = "袜子", parameterName = "Socks", enabled = true, autoFollowItemValue = true, value = 1 },
        };

        [InspectorName("物体")]
        [Tooltip("只拖这个套装项目切换项自己的物体，不要把混搭项目里的物体放进来。")]
        public List<GameObject> objects = new List<GameObject> { null };

        [Header("这个项目的子级关闭菜单")]
        [InspectorName("关闭部件开关列表")]
        public List<ClothesCloseOption> closeOptions = new List<ClothesCloseOption>();
    }

    [Serializable]
    public class SuperSwitchGroupOption
    {
        [InspectorName("套装项目切换名称")]
        [Tooltip("例如：衣服、发型、套装、身体形态。它会生成一个主菜单。")]
        public string displayName = "衣服";

        [InspectorName("菜单名称")]
        public string menuName = "衣服";

        [InspectorName("参数名")]
        public string parameterName = "Clothes";

        [InspectorName("内部：已应用参数名")]
        [Tooltip("为空时会自动使用 LW_LastApplied_参数名。")]
        public string lastAppliedParameterName = "LW_LastApplied_Clothes";

        [InspectorName("关闭部件菜单名称")]
        public string closeSubMenuName = "关闭部件";

        [InspectorName("部件菜单后缀")]
        public string partsMenuSuffix = "部件";

        [InspectorName("生成“不穿/关闭”")]
        [Tooltip("通常建议关闭，因为再次点击当前项可以用于恢复默认混搭。")]
        public bool generateNone = false;

        [InspectorName("再次点击当前项时恢复默认混搭")]
        public bool sameItemClickResetsDefaultMix = true;

        [InspectorName("选项列表")]
        public List<SuperSwitchItemOption> items = new List<SuperSwitchItemOption>
        {
            new SuperSwitchItemOption { displayName = "衣服A", value = 1 },
            new SuperSwitchItemOption { displayName = "衣服B", value = 2 },
        };
    }

    [Serializable]
    public class PartOption
    {
        [InspectorName("菜单显示名")]
        public string displayName = "选项A";

        [InspectorName("参数值")]
        [Range(1, 254)]
        public int value = 1;

        [InspectorName("物体")]
        public List<GameObject> objects = new List<GameObject> { null };

        [Header("这个混搭选项的子级关闭菜单")]
        [InspectorName("关闭部件开关列表")]
        [Tooltip("用于鞋袜套装这类绑定混搭。比如鞋袜A整体切换，同时可以关闭鞋子、袜子、袜带、蝴蝶结等子部件。")]
        public List<ClothesCloseOption> closeOptions = new List<ClothesCloseOption>();
    }

    [Serializable]
    public class MixGroupOption
    {
        [InspectorName("混搭项目名称")]
        [Tooltip("例如：鞋子、袜子、内衣、帽子、配件。")]
        public string displayName = "鞋子";

        [InspectorName("菜单名称")]
        public string menuName = "鞋子";

        [InspectorName("参数名")]
        public string parameterName = "Shoes";

        [InspectorName("生成“不穿/关闭”")]
        public bool generateNone = true;

        [InspectorName("套装项目关闭时同时关闭")]
        [Tooltip("如果某个套装项目切换启用了“不穿/关闭”，是否同时把这个混搭项目设为 0。")]
        public bool superNoneAlsoTurnsOff = true;

        [InspectorName("选项列表")]
        public List<PartOption> items = new List<PartOption>
        {
            new PartOption { displayName = "鞋子A", value = 1 },
            new PartOption { displayName = "鞋子B", value = 2 },
        };
    }

    [Serializable]
    public class CustomCloseButtonOption
    {
        [InspectorName("启用")]
        public bool enabled = true;

        [InspectorName("按钮名称")]
        public string displayName = "一键全关（含鞋袜）";

        [InspectorName("包含的混搭项目名称")]
        public List<string> includedMixGroupNames = new List<string> { "鞋子", "袜子" };

        [InspectorName("保存状态")]
        public bool saved = true;

        [InspectorName("网络同步")]
        public bool synced = true;
    }

    [AddComponentMenu("最终操作换装/通用混搭换装生成器")]
    [DisallowMultipleComponent]
    public class LastOpOutfitComponent : MonoBehaviour
    {
        [Header("角色根节点")]
        [InspectorName("角色根节点")]
        public GameObject avatarRoot;

        [Header("生成资源")]
        [InspectorName("生成资源目录")]
        public string outputFolder = "Assets/最终操作换装生成";

        [InspectorName("按角色名生成独立资源目录")]
        [Tooltip("开启后，实际生成目录会变成：生成资源目录/角色名。这样不同模型重新生成时不会互相覆盖 Assets 里的动画控制器资源。")]
        public bool useAvatarNameSubFolder = true;

        [InspectorName("管理对象名称")]
        public string managerObjectName = "最终操作换装管理器";

        [InspectorName("重新生成时清理旧管理对象")]
        public bool replaceOldManagerObject = true;

        [InspectorName("让已有 MA 开关优先于本插件")]
        [Tooltip("开启后，生成的管理器会放到角色根节点最前面。Hierarchy 后面的已有 MA Object Toggle 会后执行，通常优先级更高。")]
        public bool putGeneratedManagerFirst = true;

        [Header("菜单")]
        [InspectorName("根菜单名称")]
        public string rootMenuName = "最终操作换装";

        [InspectorName("生成“一键关闭全部”")]
        [Tooltip("生成只关闭套装项目切换部件的一键按钮，不影响任何混搭项目。")]
        public bool generateCloseAllPartsButton = true;

        [InspectorName("一键关闭全部名称")]
        public string closeAllPartsButtonName = "一键关闭全部";

        [InspectorName("一键全关按钮只在套装项目切换菜单生成一次")]
        [Tooltip("开启后，一键关闭全部和自定义一键全关按钮只会在每个套装项目切换菜单末尾生成一次。关闭后会在每个选项的部件菜单里各生成一份。")]
        public bool closeAllButtonsOnlyOnceInSuperMenu = true;

        [Header("关闭部件参数选项")]
        [InspectorName("关闭部件默认保存")]
        public bool closePartSavedByDefault = true;

        [InspectorName("关闭部件默认同步")]
        public bool closePartSyncedByDefault = true;

        [InspectorName("自动修正旧关闭项的保存/同步")]
        public bool autoFixCloseOptionSaveSync = true;

        [Header("关闭部件易用性")]
        [InspectorName("生成前自动补充混搭子级部件开关")]
        [Tooltip("默认关闭，避免生成大量部件开关导致卡顿。开启后也只会自动补充混搭项目的子级关闭菜单，不会自动补充套装项目切换的部件开关。")]
        public bool autoGenerateCloseOptionsFromFirstLevelChildren = false;

        [InspectorName("自动生成时跳过骨骼/无渲染物体")]
        public bool skipBonesWhenAutoGenerateCloseOptions = true;

        [InspectorName("只在该选项没有部件开关时自动创建")]
        public bool onlyAutoGenerateCloseOptionsWhenEmpty = true;

        [InspectorName("允许手动补充套装项目切换的部件开关")]
        [Tooltip("仅点击手动补充按钮时使用。默认关闭，避免以后更新或重新生成时又自动塞入大量套装项目部件开关。")]
        public bool allowManualGenerateSuperSwitchCloseOptions = false;

        [InspectorName("新增关闭开关时自动创建物体引用框")]
        public bool autoCreateObjectSlotForNewCloseOption = true;

        [InspectorName("所有物体列表默认保留一个引用框")]
        public bool keepOneObjectSlotForEveryObjectList = true;

        [Header("默认状态")]
        [InspectorName("默认保持当前穿着")]
        public bool keepCurrentOutfitByDefault = true;

        [Header("上传安全")]
        [InspectorName("上传前移除配置组件")]
        public bool stripBuilderComponentOnUpload = true;

        [InspectorName("上传前自动重新生成")]
        public bool buildOnUpload = false;

        [Header("生成资源名称")]
        [InspectorName("动画控制器资源名")]
        public string fxAnimatorAssetName = "最终操作_通用混搭_动画控制器";

        [Header("套装项目切换列表：衣服只是默认项目，可新增发型/套装/身体等")]
        public List<SuperSwitchGroupOption> superSwitchGroups = new List<SuperSwitchGroupOption>
        {
            new SuperSwitchGroupOption
            {
                displayName = "衣服",
                menuName = "衣服",
                parameterName = "Clothes",
                lastAppliedParameterName = "LW_LastApplied_Clothes",
                closeSubMenuName = "关闭部件",
                partsMenuSuffix = "部件",
                generateNone = false,
                sameItemClickResetsDefaultMix = true,
                items = new List<SuperSwitchItemOption>
                {
                    new SuperSwitchItemOption { displayName = "衣服A", value = 1 },
                    new SuperSwitchItemOption { displayName = "衣服B", value = 2 },
                }
            }
        };

        [Header("混搭项目列表：鞋子/袜子只是默认项目，可新增或删除")]
        public List<MixGroupOption> mixGroups = new List<MixGroupOption>
        {
            new MixGroupOption
            {
                displayName = "鞋子",
                menuName = "鞋子",
                parameterName = "Shoes",
                generateNone = true,
                superNoneAlsoTurnsOff = true,
                items = new List<PartOption>
                {
                    new PartOption { displayName = "鞋子A", value = 1 },
                    new PartOption { displayName = "鞋子B", value = 2 },
                }
            },
            new MixGroupOption
            {
                displayName = "袜子",
                menuName = "袜子",
                parameterName = "Socks",
                generateNone = true,
                superNoneAlsoTurnsOff = true,
                items = new List<PartOption>
                {
                    new PartOption { displayName = "袜子A", value = 1 },
                    new PartOption { displayName = "袜子B", value = 2 },
                }
            },
        };

        [Header("自定义一键全关按钮：可自由选择要联动关闭的混搭项目")]
        public List<CustomCloseButtonOption> customCloseButtons = new List<CustomCloseButtonOption>
        {
            new CustomCloseButtonOption
            {
                enabled = true,
                displayName = "一键全关（含鞋袜）",
                includedMixGroupNames = new List<string> { "鞋子", "袜子" },
                saved = true,
                synced = true
            }
        };

        private void OnValidate()
        {
            RefreshDerivedData();
        }

        public void RefreshDerivedData()
        {
            if (avatarRoot == null) avatarRoot = gameObject;

            EnsureLists();
            SyncLastAppliedNames();
            SyncDefaultMixValues();

            if (keepOneObjectSlotForEveryObjectList)
            {
                EnsureSuperObjectSlots();
                EnsureMixGroupObjectSlots();
            }

            EnsureCloseOptions();
            ForceAppearanceOptionsSavedSynced();
        }

        private void ForceAppearanceOptionsSavedSynced()
        {
            // 会导致模型外观变化、玩家能直接感知的外显自定义参数，默认必须保存并同步。
            closePartSavedByDefault = true;
            closePartSyncedByDefault = true;

            if (customCloseButtons != null)
            {
                foreach (var button in customCloseButtons)
                {
                    if (button == null) continue;
                    button.saved = true;
                    button.synced = true;
                }
            }

            if (superSwitchGroups != null)
            {
                foreach (var group in superSwitchGroups)
                {
                    if (group == null || group.items == null) continue;

                    foreach (var item in group.items)
                    {
                        if (item == null || item.closeOptions == null) continue;

                        foreach (var close in item.closeOptions)
                        {
                            if (close == null) continue;
                            close.saved = true;
                            close.synced = true;
                        }
                    }
                }
            }

            if (mixGroups != null)
            {
                foreach (var group in mixGroups)
                {
                    if (group == null || group.items == null) continue;

                    foreach (var item in group.items)
                    {
                        if (item == null || item.closeOptions == null) continue;

                        foreach (var close in item.closeOptions)
                        {
                            if (close == null) continue;
                            close.saved = true;
                            close.synced = true;
                        }
                    }
                }
            }
        }

        private void EnsureLists()
        {
            if (superSwitchGroups == null) superSwitchGroups = new List<SuperSwitchGroupOption>();
            if (mixGroups == null) mixGroups = new List<MixGroupOption>();
            if (customCloseButtons == null) customCloseButtons = new List<CustomCloseButtonOption>();
        }

        private void SyncLastAppliedNames()
        {
            foreach (var group in superSwitchGroups)
            {
                if (group == null) continue;
                if (string.IsNullOrWhiteSpace(group.lastAppliedParameterName))
                    group.lastAppliedParameterName = "LW_LastApplied_" + Sanitize(group.parameterName);
            }
        }

        private void SyncDefaultMixValues()
        {
            var validGroups = mixGroups
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.displayName) && !string.IsNullOrWhiteSpace(g.parameterName))
                .ToList();

            foreach (var super in superSwitchGroups)
            {
                if (super == null || super.items == null) continue;

                foreach (var item in super.items)
                {
                    if (item == null) continue;
                    if (item.defaultMixValues == null) item.defaultMixValues = new List<MixDefaultSetting>();

                    item.defaultMixValues.RemoveAll(v =>
                        v == null ||
                        validGroups.All(g => g.displayName != v.mixGroupName && g.parameterName != v.parameterName));

                    foreach (var group in validGroups)
                    {
                        var setting = item.defaultMixValues.FirstOrDefault(v =>
                            v != null && (v.mixGroupName == group.displayName || v.parameterName == group.parameterName));

                        if (setting == null)
                        {
                            item.defaultMixValues.Add(new MixDefaultSetting
                            {
                                mixGroupName = group.displayName,
                                parameterName = group.parameterName,
                                enabled = true,
                                autoFollowItemValue = true,
                                value = Mathf.Clamp(item.value, 1, 254)
                            });
                        }
                        else
                        {
                            setting.mixGroupName = group.displayName;
                            setting.parameterName = group.parameterName;

                            if (setting.autoFollowItemValue)
                            {
                                setting.value = Mathf.Clamp(item.value, 1, 254);
                            }
                        }
                    }
                }
            }
        }

        private void EnsureSuperObjectSlots()
        {
            foreach (var group in superSwitchGroups)
            {
                if (group == null || group.items == null) continue;
                foreach (var item in group.items)
                {
                    if (item == null) continue;
                    if (item.objects == null) item.objects = new List<GameObject>();
                    EnsureObjectSlot(item.objects);

                    if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();

                    foreach (var close in item.closeOptions)
                    {
                        if (close == null) continue;

                        // 只修复混搭子级关闭菜单：
                        // 鞋袜套装这类混搭项内部的“关闭鞋子/关闭袜子”等参数必须默认保存并同步。
                        close.saved = true;
                        close.synced = true;

                        if (autoCreateObjectSlotForNewCloseOption || keepOneObjectSlotForEveryObjectList)
                        {
                            if (close.objects == null) close.objects = new List<GameObject>();
                            EnsureObjectSlot(close.objects);
                        }
                    }
                }
            }
        }

        private void EnsureMixGroupObjectSlots()
        {
            foreach (var group in mixGroups)
            {
                if (group == null) continue;
                if (group.items == null) group.items = new List<PartOption>();

                foreach (var item in group.items)
                {
                    if (item == null) continue;
                    if (item.objects == null) item.objects = new List<GameObject>();
                    EnsureObjectSlot(item.objects);
                }
            }
        }

        private void EnsureCloseOptions()
        {
            foreach (var group in superSwitchGroups)
            {
                if (group == null || group.items == null) continue;
                foreach (var item in group.items)
                {
                    if (item == null) continue;
                    if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();

                    foreach (var close in item.closeOptions)
                    {
                        if (close == null) continue;

                        if (autoFixCloseOptionSaveSync)
                        {
                            close.saved = closePartSavedByDefault;
                            close.synced = closePartSyncedByDefault;
                        }

                        if (autoCreateObjectSlotForNewCloseOption || keepOneObjectSlotForEveryObjectList)
                        {
                            if (close.objects == null) close.objects = new List<GameObject>();
                            EnsureObjectSlot(close.objects);
                        }
                    }
                }
            }
        }

        private static void EnsureObjectSlot(List<GameObject> objects)
        {
            if (objects == null) return;
            if (objects.Count == 0) objects.Add(null);
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Param";
            return new string(value.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray());
        }
    }
}
