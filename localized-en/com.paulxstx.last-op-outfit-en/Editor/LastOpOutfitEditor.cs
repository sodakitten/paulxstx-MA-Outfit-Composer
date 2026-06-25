// LastOpOutfitEditor.cs
// Paulxstx MA Outfit Composer: Universal Super Switch + Mix-and-Match

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Paulxstx.LastOpOutfitPlugin;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase.Editor.BuildPipeline;
using Object = UnityEngine.Object;

namespace Paulxstx.LastOpOutfitPlugin.Editor
{
    public static class LastOpOutfitMenuItems
    {
        private const string AddMenuPath = "GameObject/Paulxstx Outfit/Add Outfit Generator";
        private const string AddAndBuildMenuPath = "GameObject/Paulxstx Outfit/Add and Generate Menu";
        private const string ToolsAddMenuPath = "Tools/Paulxstx Outfit/Add to Selected Avatar";

        [MenuItem(AddMenuPath, false, 10)]
        private static void AddFromGameObjectMenu(MenuCommand command)
        {
            var go = command.context as GameObject;
            if (go == null) go = Selection.activeGameObject;
            var component = AddOrGetComponent(go);
            if (component == null) return;
            Selection.activeObject = component.gameObject;
            EditorGUIUtility.PingObject(component.gameObject);
        }

        [MenuItem(AddMenuPath, true)]
        private static bool ValidateAddFromGameObjectMenu()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(AddAndBuildMenuPath, false, 11)]
        private static void AddAndBuildFromGameObjectMenu(MenuCommand command)
        {
            var go = command.context as GameObject;
            if (go == null) go = Selection.activeGameObject;
            var component = AddOrGetComponent(go);
            if (component == null) return;

            try
            {
                LastOpOutfitGenerator.Build(component, true);
                Selection.activeObject = component.gameObject;
                EditorGUIUtility.PingObject(component.gameObject);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Paulxstx Outfit - Build Failed", e.Message, "确定");
            }
        }

        [MenuItem(AddAndBuildMenuPath, true)]
        private static bool ValidateAddAndBuildFromGameObjectMenu()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(ToolsAddMenuPath, false, 100)]
        private static void AddFromToolsMenu()
        {
            var component = AddOrGetComponent(Selection.activeGameObject);
            if (component == null) return;
            Selection.activeObject = component.gameObject;
            EditorGUIUtility.PingObject(component.gameObject);
        }

        [MenuItem(ToolsAddMenuPath, true)]
        private static bool ValidateAddFromToolsMenu()
        {
            return Selection.activeGameObject != null;
        }

        private static LastOpOutfitComponent AddOrGetComponent(GameObject go)
        {
            if (go == null)
            {
                EditorUtility.DisplayDialog("Paulxstx Outfit", "Please select an avatar root first.", "确定");
                return null;
            }

            var component = go.GetComponent<LastOpOutfitComponent>();
            if (component == null)
            {
                component = Undo.AddComponent<LastOpOutfitComponent>(go);
                Undo.RecordObject(component, "添加Paulxstx Outfit Composer");
            }

            component.avatarRoot = go;
            component.RefreshDerivedData();
            EditorUtility.SetDirty(component);
            return component;
        }
    }

    [CustomPropertyDrawer(typeof(ClothesCloseOption))]
    public class ClothesCloseOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ForceSavedSynced(property);

            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "显示名称");

                var objects = property.FindPropertyRelative("objects");
                if (objects != null)
                {
                    objects.isExpanded = true;
                    y = DrawProperty(position, y, objects, "要关闭的部件");
                }

                var saved = property.FindPropertyRelative("saved");
                var synced = property.FindPropertyRelative("synced");

                if (saved != null)
                {
                    saved.boolValue = true;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        y = DrawProperty(position, y, saved, "保存状态");
                    }
                }

                if (synced != null)
                {
                    synced.boolValue = true;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        y = DrawProperty(position, y, synced, "网络同步");
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;

            if (!property.isExpanded) return height;

            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("objects")) + space;
            height += GetHeight(property.FindPropertyRelative("saved")) + space;
            height += GetHeight(property.FindPropertyRelative("synced")) + space;

            return height;
        }

        private static void ForceSavedSynced(SerializedProperty property)
        {
            var saved = property.FindPropertyRelative("saved");
            if (saved != null && saved.propertyType == SerializedPropertyType.Boolean && !saved.boolValue)
            {
                saved.boolValue = true;
            }

            var synced = property.FindPropertyRelative("synced");
            if (synced != null && synced.propertyType == SerializedPropertyType.Boolean && !synced.boolValue)
            {
                synced.boolValue = true;
            }
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomPropertyDrawer(typeof(SuperSwitchItemOption))]
    public class SuperSwitchItemOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "显示名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("value"), "值");

                var objects = property.FindPropertyRelative("objects");
                if (objects != null)
                {
                    objects.isExpanded = true;
                    y = DrawProperty(position, y, objects, "主要物体（拖入当前选项本体）");
                    var helpHeight = EditorGUIUtility.singleLineHeight * 2.2f;
                    EditorGUI.HelpBox(new Rect(position.x, y, position.width, helpHeight), "这里拖入当前选项的主要物体，例如衣服A本体；鞋子、袜子、内衣等混搭物体不要拖到这里。", MessageType.None);
                    y += helpHeight + space;
                }

                y = DrawProperty(position, y, property.FindPropertyRelative("defaultMixValues"), "默认混搭设置");
                y = DrawProperty(position, y, property.FindPropertyRelative("closeOptions"), "这个项目的子级关闭菜单");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;

            if (!property.isExpanded) return height;

            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("value")) + space;
            height += GetHeight(property.FindPropertyRelative("objects")) + space;
            height += line * 2.2f + space;
            height += GetHeight(property.FindPropertyRelative("defaultMixValues")) + space;
            height += GetHeight(property.FindPropertyRelative("closeOptions")) + space;

            return height;
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomPropertyDrawer(typeof(PartOption))]
    public class PartOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "显示名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("value"), "值");

                var objects = property.FindPropertyRelative("objects");
                if (objects != null)
                {
                    objects.isExpanded = true;
                    y = DrawProperty(position, y, objects, "混搭主体物体（拖入当前组合本体）");

                    var helpHeight = EditorGUIUtility.singleLineHeight * 2.2f;
                    EditorGUI.HelpBox(new Rect(position.x, y, position.width, helpHeight), "这里可以拖入绑定组合本体，例如鞋袜A整体：鞋子A、袜子A、配套装饰等。", MessageType.None);
                    y += helpHeight + space;
                }

                y = DrawProperty(position, y, property.FindPropertyRelative("closeOptions"), "这个混搭选项的子级关闭菜单（默认保存同步）");

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;
            if (!property.isExpanded) return height;

            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("value")) + space;
            height += GetHeight(property.FindPropertyRelative("objects")) + space;
            height += line * 2.2f + space;
            height += GetHeight(property.FindPropertyRelative("closeOptions")) + space;
            return height;
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }



    [CustomPropertyDrawer(typeof(MixDefaultSetting))]
    public class MixDefaultSettingDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                y = DrawProperty(position, y, property.FindPropertyRelative("mixGroupName"), "混搭项目名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("enabled"), "选中套装项时修改它");
                y = DrawProperty(position, y, property.FindPropertyRelative("autoFollowItemValue"), "自动跟随当前选项值");
                y = DrawProperty(position, y, property.FindPropertyRelative("value"), "默认参数值");
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;
            if (!property.isExpanded) return height;
            height += GetHeight(property.FindPropertyRelative("mixGroupName")) + space;
            height += GetHeight(property.FindPropertyRelative("enabled")) + space;
            height += GetHeight(property.FindPropertyRelative("autoFollowItemValue")) + space;
            height += GetHeight(property.FindPropertyRelative("value")) + space;
            return height;
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomPropertyDrawer(typeof(SuperSwitchGroupOption))]
    public class SuperSwitchGroupOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "套装项目切换名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("menuName"), "菜单名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("parameterName"), "参数名");
                y = DrawProperty(position, y, property.FindPropertyRelative("lastAppliedParameterName"), "内部：已应用参数名");
                y = DrawProperty(position, y, property.FindPropertyRelative("closeSubMenuName"), "关闭部件菜单名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("partsMenuSuffix"), "部件菜单后缀");
                y = DrawProperty(position, y, property.FindPropertyRelative("generateNone"), "生成“不穿/关闭”");
                y = DrawProperty(position, y, property.FindPropertyRelative("sameItemClickResetsDefaultMix"), "再次点击当前项时恢复默认混搭");
                y = DrawProperty(position, y, property.FindPropertyRelative("items"), "选项列表");
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;
            if (!property.isExpanded) return height;
            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("menuName")) + space;
            height += GetHeight(property.FindPropertyRelative("parameterName")) + space;
            height += GetHeight(property.FindPropertyRelative("lastAppliedParameterName")) + space;
            height += GetHeight(property.FindPropertyRelative("closeSubMenuName")) + space;
            height += GetHeight(property.FindPropertyRelative("partsMenuSuffix")) + space;
            height += GetHeight(property.FindPropertyRelative("generateNone")) + space;
            height += GetHeight(property.FindPropertyRelative("sameItemClickResetsDefaultMix")) + space;
            height += GetHeight(property.FindPropertyRelative("items")) + space;
            return height;
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomPropertyDrawer(typeof(MixGroupOption))]
    public class MixGroupOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "混搭项目名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("menuName"), "菜单名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("parameterName"), "参数名");
                y = DrawProperty(position, y, property.FindPropertyRelative("generateNone"), "生成“不穿/关闭”");
                y = DrawProperty(position, y, property.FindPropertyRelative("superNoneAlsoTurnsOff"), "套装项目关闭时同时关闭");
                y = DrawProperty(position, y, property.FindPropertyRelative("items"), "选项列表");
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;
            if (!property.isExpanded) return height;
            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("menuName")) + space;
            height += GetHeight(property.FindPropertyRelative("parameterName")) + space;
            height += GetHeight(property.FindPropertyRelative("generateNone")) + space;
            height += GetHeight(property.FindPropertyRelative("superNoneAlsoTurnsOff")) + space;
            height += GetHeight(property.FindPropertyRelative("items")) + space;
            return height;
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomPropertyDrawer(typeof(CustomCloseButtonOption))]
    public class CustomCloseButtonOptionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ForceSavedSynced(property);
            EditorGUI.BeginProperty(position, label, property);

            var y = position.y;
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;

            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width, line), property.isExpanded, label, true);
            y += line + space;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                y = DrawProperty(position, y, property.FindPropertyRelative("enabled"), "启用");
                y = DrawProperty(position, y, property.FindPropertyRelative("displayName"), "按钮名称");
                y = DrawProperty(position, y, property.FindPropertyRelative("includedMixGroupNames"), "包含的混搭项目名称");

                var saved = property.FindPropertyRelative("saved");
                var synced = property.FindPropertyRelative("synced");
                if (saved != null)
                {
                    saved.boolValue = true;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        y = DrawProperty(position, y, saved, "保存状态");
                    }
                }
                if (synced != null)
                {
                    synced.boolValue = true;
                    using (new EditorGUI.DisabledScope(true))
                    {
                        y = DrawProperty(position, y, synced, "网络同步");
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var space = EditorGUIUtility.standardVerticalSpacing;
            var height = line + space;
            if (!property.isExpanded) return height;
            height += GetHeight(property.FindPropertyRelative("enabled")) + space;
            height += GetHeight(property.FindPropertyRelative("displayName")) + space;
            height += GetHeight(property.FindPropertyRelative("includedMixGroupNames")) + space;
            height += GetHeight(property.FindPropertyRelative("saved")) + space;
            height += GetHeight(property.FindPropertyRelative("synced")) + space;
            return height;
        }

        private static void ForceSavedSynced(SerializedProperty property)
        {
            var saved = property.FindPropertyRelative("saved");
            if (saved != null && saved.propertyType == SerializedPropertyType.Boolean && !saved.boolValue)
            {
                saved.boolValue = true;
            }

            var synced = property.FindPropertyRelative("synced");
            if (synced != null && synced.propertyType == SerializedPropertyType.Boolean && !synced.boolValue)
            {
                synced.boolValue = true;
            }
        }

        private static float DrawProperty(Rect position, float y, SerializedProperty prop, string label)
        {
            if (prop == null) return y;
            var height = EditorGUI.GetPropertyHeight(prop, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), prop, new GUIContent(label), true);
            return y + height + EditorGUIUtility.standardVerticalSpacing;
        }

        private static float GetHeight(SerializedProperty prop)
        {
            return prop == null ? 0 : EditorGUI.GetPropertyHeight(prop, true);
        }
    }

    [CustomEditor(typeof(LastOpOutfitComponent), true)]
    [CanEditMultipleObjects]
    public class LastOpOutfitComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (targets != null && targets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Multiple objects selected. “Paulxstx Outfit Composer”。\n\n" +
                    "Multi-object editing is not supported. Select only one generator.\n\n" +
                    "Only prevents multi-select editing; single-avatar use unaffected.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox(
                "通用套装项目切换 + 通用混搭版：\n\n" +
                "“衣服”只是默认套装项目切换。你可以新增“发型 / 整套套装 / 身体形态”等同类大分类。\n\n" +
                "“鞋子 / 袜子”只是默认混搭项目。你可以新增“内衣 / 帽子 / 饰品”等混搭项目。\n\n" +
                "No longer auto-generates Super Switch part toggles; manually configured ones are preserved.",
                MessageType.Info);

            serializedObject.Update();

            var config = (LastOpOutfitComponent)target;

            // 注意：
            // 不要在 OnInspectorGUI 每次刷新时调用 RefreshDerivedData / SetDirty。
            // Unity Inspector 会频繁重绘；如果这里持续修改序列化对象，就会出现 Hold on 卡在 OnInspectorGUI。
            // 自动修正改到 OnValidate、Executed by build and explicit buttons.

            绘制分组标题("角色根节点");
            绘制属性("avatarRoot", "角色根节点");

            绘制分组标题("Build Assets");
            绘制属性("outputFolder", "Build Assets目录");
            绘制属性("useAvatarNameSubFolder", "按角色名生成独立资源目录");
            if (config != null && config.avatarRoot != null && config.useAvatarNameSubFolder)
            {
                EditorGUILayout.HelpBox("实际生成目录：" + LastOpOutfitGenerator.GetActualOutputFolder(config), MessageType.None);
            }
            绘制属性("managerObjectName", "管理对象名称");
            绘制属性("replaceOldManagerObject", "重新生成时清理旧管理对象");
            绘制属性("putGeneratedManagerFirst", "让已有 MA 开关优先于本插件");
            EditorGUILayout.HelpBox("开启后，本插件生成的管理器会放在 Avatar Root 最前面。Hierarchy 后面的已有 MA Object Toggle 通常会后执行，所以已有开关优先级更高。", MessageType.None);

            绘制分组标题("菜单");
            绘制属性("rootMenuName", "根菜单名称");
            绘制属性("generateCloseAllPartsButton", "生成“一键关闭全部”");
            绘制属性("closeAllPartsButtonName", "一键关闭全部名称");
            绘制属性("closeAllButtonsOnlyOnceInSuperMenu", "一键全关按钮只在套装项目切换菜单生成一次");

            绘制分组标题("关闭部件参数选项");
            绘制属性("closePartSavedByDefault", "关闭部件默认保存");
            绘制属性("closePartSyncedByDefault", "关闭部件默认同步");
            绘制属性("autoFixCloseOptionSaveSync", "自动修正旧关闭项的保存/同步");

            绘制分组标题("关闭部件易用性");
            绘制属性("autoGenerateCloseOptionsFromFirstLevelChildren", "生成前自动补充混搭子级部件开关");
            绘制属性("skipBonesWhenAutoGenerateCloseOptions", "自动生成时跳过骨骼/无渲染物体");
            绘制属性("onlyAutoGenerateCloseOptionsWhenEmpty", "只在该选项没有部件开关时自动创建");
            绘制属性("allowManualGenerateSuperSwitchCloseOptions", "允许手动补充套装项目切换的部件开关");
            绘制属性("autoCreateObjectSlotForNewCloseOption", "新增关闭开关时自动创建物体引用框");
            绘制属性("keepOneObjectSlotForEveryObjectList", "所有物体列表默认保留一个引用框");

            绘制分组标题("默认状态");
            绘制属性("keepCurrentOutfitByDefault", "默认保持当前穿着");

            绘制分组标题("上传安全");
            绘制属性("stripBuilderComponentOnUpload", "上传前移除配置组件");
            绘制属性("buildOnUpload", "上传前自动重新生成");

            绘制分组标题("Build Assets名称");
            绘制属性("fxAnimatorAssetName", "动画控制器资源名");

            var superCountBefore = config.superSwitchGroups != null ? config.superSwitchGroups.Count : 0;
            var mixCountBefore = config.mixGroups != null ? config.mixGroups.Count : 0;

            Draw Super Switch group list(config);
            Draw Mix Group list(config);
            Draw custom Close-All button list(config);

            var changedByInspector = serializedObject.ApplyModifiedProperties();

            if (changedByInspector)
            {
                config.RefreshDerivedData();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }

            var initializedNewTopLevelItems = Initialize new item(config, superCountBefore, mixCountBefore);
            if (initializedNewTopLevelItems)
            {
                config.RefreshDerivedData();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }

            if ((changedByInspector || initializedNewTopLevelItems) && Fix copy-new-item numbering(config))
            {
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }

            EditorGUILayout.Space(8);
            绘制分组标题("Safety Tools");
            EditorGUILayout.HelpBox(
                "Export before major changes or upgrades. JSON 备份。\n" +
                "Detection is read-only; your config will not be modified.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Config Backup JSON", GUILayout.Height(30)))
            {
                serializedObject.ApplyModifiedProperties();
                LastOpOutfitBackupUtility.ExportConfig(config);
            }

            if (GUILayout.Button("Import Config Backup JSON", GUILayout.Height(30)))
            {
                if (LastOpOutfitBackupUtility.ImportConfig(config))
                {
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Conflict Detection / Config Check", GUILayout.Height(30)))
            {
                serializedObject.ApplyModifiedProperties();
                LastOpOutfitConflictChecker.ShowReport(config);
            }

            if (GUILayout.Button("Fix copy-new-item numbering (D 后继续 E/F/G）", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "Fix copy-new-item numbering");
                if (Fix copy-new-item numbering(config))
                {
                    config.RefreshDerivedData();
                    EditorUtility.SetDirty(config);
                    serializedObject.Update();
                    EditorUtility.DisplayDialog("Paulxstx Outfit", "Fixed duplicate names from copy-new-item/重复参数值。", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("Paulxstx Outfit", "No copy-new-item duplicates found.", "确定");
                }
            }

            EditorGUILayout.Space(8);
            if (GUILayout.Button("同步每个选项的默认混搭设置", GUILayout.Height(28)))
            {
                Undo.RecordObject(config, "同步每个选项的默认混搭设置");
                config.RefreshDerivedData();
                EditorUtility.SetDirty(config);
                serializedObject.Update();
            }

            if (GUILayout.Button("Fill Mix sub-level toggles from 1st-level children", GUILayout.Height(28)))
            {
                var added = LastOpOutfitGenerator.AutoGenerateCloseOptionsFromFirstLevelChildren(config, true, false);
                LastOpOutfitGenerator.EnsureObjectSlots(config);
                EditorUtility.SetDirty(config);
                serializedObject.Update();
                EditorUtility.DisplayDialog("Paulxstx Outfit", "Generated mix sub-level close toggles for: " + added + " 个。\n\nThis button does not fill Super Switch toggles.", "确定");
            }

            if (config != null && config.allowManualGenerateSuperSwitchCloseOptions)
            {
                if (GUILayout.Button("Fill Super Switch toggles from 1st-level children", GUILayout.Height(28)))
                {
                    var added = LastOpOutfitGenerator.AutoGenerateCloseOptionsFromFirstLevelChildren(config, true, true);
                    LastOpOutfitGenerator.EnsureObjectSlots(config);
                    EditorUtility.SetDirty(config);
                    serializedObject.Update();
                    EditorUtility.DisplayDialog("Paulxstx Outfit", "已手动补充套装项目切换/混搭项目的关闭部件开关：" + added + " 个。", "确定");
                }
            }

            EditorGUILayout.Space(12);
            if (GUILayout.Button("Generate Outfit Mix & Match System", GUILayout.Height(38)))
            {
                try
                {
                    config.RefreshDerivedData();
                    EditorUtility.SetDirty(config);
                    LastOpOutfitGenerator.Build(config, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    EditorUtility.DisplayDialog("Paulxstx Outfit - Build Failed", e.Message, "确定");
                }
            }
        }

        private void 绘制分组标题(string title)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        private void 绘制属性(string propertyName, string label)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                EditorGUILayout.HelpBox("找不到字段：" + propertyName, MessageType.Warning);
                return;
            }

            绘制属性字段(property, label, true);
        }

        private static void 绘制属性字段(SerializedProperty property, string label, bool includeChildren = true)
        {
            if (property == null) return;
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.42f, 150f, 260f);
            EditorGUILayout.PropertyField(property, new GUIContent(label), includeChildren);
            EditorGUIUtility.labelWidth = oldWidth;
        }

        private static string 取字符串(SerializedProperty parent, string childName, string fallback = "")
        {
            var p = parent != null ? parent.FindPropertyRelative(childName) : null;
            return p != null && p.propertyType == SerializedPropertyType.String ? p.stringValue : fallback;
        }

        private static void 写字符串(SerializedProperty parent, string childName, string value)
        {
            var p = parent != null ? parent.FindPropertyRelative(childName) : null;
            if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = value;
        }

        private static int 绘制整数输入(SerializedProperty parent, string childName, string label, int min, int max, int fallback = 0)
        {
            var p = parent != null ? parent.FindPropertyRelative(childName) : null;
            if (p == null || p.propertyType != SerializedPropertyType.Integer) return fallback;
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.42f, 150f, 260f);
            var next = EditorGUILayout.IntField(new GUIContent(label), p.intValue);
            EditorGUIUtility.labelWidth = oldWidth;
            p.intValue = Mathf.Clamp(next, min, max);
            return p.intValue;
        }

        private static void 绘制布尔输入(SerializedProperty parent, string childName, string label)
        {
            var p = parent != null ? parent.FindPropertyRelative(childName) : null;
            if (p == null || p.propertyType != SerializedPropertyType.Boolean) return;
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.42f, 150f, 260f);
            EditorGUILayout.PropertyField(p, new GUIContent(label), false);
            EditorGUIUtility.labelWidth = oldWidth;
        }

        private void Draw Super Switch group list(LastOpOutfitComponent config)
        {
            var groups = serializedObject.FindProperty("superSwitchGroups");
            if (groups == null || !groups.isArray) return;

            绘制分组标题("Super Switch Groups: Clothes is default. Add Hairstyle, /套装/身体等");

            EditorGUILayout.HelpBox(
                "套装项目切换 = 像“衣服”这样，一次只能选一个的大分类。\n\n" +
                "例如：衣服、发型、套装、身体形态。每个套装项目切换都有自己的菜单、参数和选项列表。",
                MessageType.None);

            groups.isExpanded = EditorGUILayout.Foldout(groups.isExpanded, "Super Switch Groups (Count: " + groups.arraySize + "）", true);
            if (!groups.isExpanded)
            {
                if (GUILayout.Button("+ Add Super Switch Group", GUILayout.Height(26)))
                    AddSuperSwitchGroup(groups, config);
                return;
            }

            EditorGUI.indentLevel++;

            for (var i = 0; i < groups.arraySize; i++)
            {
                var element = groups.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("套装项目切换 " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    groups.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(element.FindPropertyRelative("displayName"), new GUIContent("套装项目切换名称"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("menuName"), new GUIContent("菜单名称"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("parameterName"), new GUIContent("参数名"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("lastAppliedParameterName"), new GUIContent("内部：已应用参数名"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("closeSubMenuName"), new GUIContent("关闭部件菜单名称"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("partsMenuSuffix"), new GUIContent("部件菜单后缀"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("generateNone"), new GUIContent("生成“不穿/关闭”"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("sameItemClickResetsDefaultMix"), new GUIContent("再次点击当前项时恢复默认混搭"));

                var items = element.FindPropertyRelative("items");
                DrawSuperSwitchItemsList(items, config, GetString(element, "displayName", "套装项目切换" + 序号转字母(i)));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Super Switch Group", GUILayout.Height(26)))
                AddSuperSwitchGroup(groups, config);

            EditorGUI.indentLevel--;
        }

        private void Draw Mix Group list(LastOpOutfitComponent config)
        {
            var groups = serializedObject.FindProperty("mixGroups");
            if (groups == null || !groups.isArray) return;

            绘制分组标题("混搭项目列表：鞋子/袜子只是默认项目，可新增或删除");

            EditorGUILayout.HelpBox(
                "混搭项目 = 可以覆盖套装项目切换默认搭配的小分类。\n\n" +
                "例如：鞋子、袜子、内衣、帽子、饰品、尾巴。每个混搭项目都会生成自己的菜单和参数。",
                MessageType.None);

            groups.isExpanded = EditorGUILayout.Foldout(groups.isExpanded, "Mix Groups (Count: " + groups.arraySize + "）", true);
            if (!groups.isExpanded)
            {
                if (GUILayout.Button("+ Add Mix Group", GUILayout.Height(26)))
                    AddMixGroup(groups);
                return;
            }

            EditorGUI.indentLevel++;

            for (var i = 0; i < groups.arraySize; i++)
            {
                var element = groups.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("混搭项目 " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    groups.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(element.FindPropertyRelative("displayName"), new GUIContent("混搭项目名称"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("menuName"), new GUIContent("菜单名称"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("parameterName"), new GUIContent("参数名"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("generateNone"), new GUIContent("生成“不穿/关闭”"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("superNoneAlsoTurnsOff"), new GUIContent("套装项目关闭时同时关闭"));

                var items = element.FindPropertyRelative("items");
                DrawMixItemsList(items, GetString(element, "displayName", "混搭" + 序号转字母(i)));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Mix Group", GUILayout.Height(26)))
                AddMixGroup(groups);

            EditorGUI.indentLevel--;
        }

        private void Draw custom Close-All button list(LastOpOutfitComponent config)
        {
            var buttons = serializedObject.FindProperty("customCloseButtons");
            if (buttons == null || !buttons.isArray) return;

            绘制分组标题("自定义一键全关按钮");

            EditorGUILayout.HelpBox(
                "除“一键关闭全部”外，这里可以自由创建“一键全关（含XXX）”。\n\n" +
                "例：\n" +
                "一键全关（含鞋） → 勾选鞋子\n" +
                "一键全关（含袜） → 勾选袜子\n" +
                "一键全关（含内衣和袜） → 勾选内衣、袜子",
                MessageType.None);

            buttons.isExpanded = EditorGUILayout.Foldout(buttons.isExpanded, "Custom Close-All Buttons (Count: " + buttons.arraySize + "）", true);
            if (!buttons.isExpanded)
            {
                if (GUILayout.Button("+ Add Close-All Button", GUILayout.Height(26)))
                    AddCustomCloseButton(buttons, config);
                return;
            }

            EditorGUI.indentLevel++;

            for (var i = 0; i < buttons.arraySize; i++)
            {
                var element = buttons.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("一键全关按钮 " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    buttons.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(element.FindPropertyRelative("enabled"), new GUIContent("启用"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("displayName"), new GUIContent("按钮名称"));

                var included = element.FindPropertyRelative("includedMixGroupNames");
                if (included != null && included.isArray)
                {
                    EditorGUILayout.LabelField("包含的混搭项目", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
                    {
                        if (group == null || string.IsNullOrWhiteSpace(group.displayName)) continue;
                        var has = StringArrayContains(included, group.displayName);
                        var next = EditorGUILayout.Toggle("含 " + group.displayName, has);
                        if (next != has)
                        {
                            if (next) AddStringToArray(included, group.displayName);
                            else RemoveStringFromArray(included, group.displayName);
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(element.FindPropertyRelative("saved"), new GUIContent("保存状态"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("synced"), new GUIContent("网络同步"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Close-All Button", GUILayout.Height(26)))
                AddCustomCloseButton(buttons, config);

            EditorGUI.indentLevel--;
        }

        private void AddSuperSwitchGroup(SerializedProperty groups, LastOpOutfitComponent config)
        {
            var index = groups.arraySize;
            groups.InsertArrayElementAtIndex(index);
            var element = groups.GetArrayElementAtIndex(index);

            var name = "套装项目切换" + 序号转字母(index);
            SetString(element, "displayName", name);
            SetString(element, "menuName", name);
            SetString(element, "parameterName", "Super_" + (index + 1));
            SetString(element, "lastAppliedParameterName", "LW_LastApplied_Super_" + (index + 1));
            SetString(element, "closeSubMenuName", "关闭部件");
            SetString(element, "partsMenuSuffix", "部件");
            SetBool(element, "generateNone", false);
            SetBool(element, "sameItemClickResetsDefaultMix", true);

            var items = element.FindPropertyRelative("items");
            if (items != null && items.isArray)
            {
                items.ClearArray();
                for (var i = 0; i < 2; i++)
                {
                    items.InsertArrayElementAtIndex(i);
                    var item = items.GetArrayElementAtIndex(i);
                    SetString(item, "displayName", name + 序号转字母(i));
                    SetInt(item, "value", i + 1);
                    EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);

                    var defaultMix = item.FindPropertyRelative("defaultMixValues");
                    if (defaultMix != null && defaultMix.isArray)
                    {
                        defaultMix.ClearArray();
                        foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
                        {
                            if (group == null || string.IsNullOrWhiteSpace(group.displayName)) continue;
                            var idx = defaultMix.arraySize;
                            defaultMix.InsertArrayElementAtIndex(idx);
                            var setting = defaultMix.GetArrayElementAtIndex(idx);
                            SetString(setting, "mixGroupName", group.displayName);
                            SetString(setting, "parameterName", group.parameterName);
                            SetBool(setting, "enabled", true);
                            SetBool(setting, "autoFollowItemValue", true);
                            SetInt(setting, "value", i + 1);
                        }
                    }
                }
            }
        }

        private void AddMixGroup(SerializedProperty groups)
        {
            var index = groups.arraySize;
            groups.InsertArrayElementAtIndex(index);
            var element = groups.GetArrayElementAtIndex(index);

            var name = "混搭" + 序号转字母(index);
            SetString(element, "displayName", name);
            SetString(element, "menuName", name);
            SetString(element, "parameterName", "Mix_" + (index + 1));
            SetBool(element, "generateNone", true);
            SetBool(element, "superNoneAlsoTurnsOff", true);

            var items = element.FindPropertyRelative("items");
            if (items != null && items.isArray)
            {
                items.ClearArray();
                for (var i = 0; i < 2; i++)
                {
                    items.InsertArrayElementAtIndex(i);
                    var item = items.GetArrayElementAtIndex(i);
                    SetString(item, "displayName", name + 序号转字母(i));
                    SetInt(item, "value", i + 1);
                    EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);
                }
            }
        }


        private void DrawSuperSwitchItemsList(SerializedProperty items, LastOpOutfitComponent config, string parentName)
        {
            if (items == null || !items.isArray) return;

            items.isExpanded = EditorGUILayout.Foldout(items.isExpanded, "选项列表（数量：" + items.arraySize + "）", true);
            if (!items.isExpanded)
            {
                if (GUILayout.Button("+ Add Super Option", GUILayout.Height(24)))
                    AddSuperSwitchItem(items, config, parentName);
                return;
            }

            EditorGUI.indentLevel++;
            for (var i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选项 " + 序号转字母(i) + "：" + 取字符串(item, "displayName", parentName + 序号转字母(i)), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    items.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawSuperSwitchItemCard(item, config, parentName, i);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ Add Super Option", GUILayout.Height(24)))
                AddSuperSwitchItem(items, config, parentName);

            EditorGUI.indentLevel--;
        }

        private void DrawMixItemsList(SerializedProperty items, string parentName)
        {
            if (items == null || !items.isArray) return;

            items.isExpanded = EditorGUILayout.Foldout(items.isExpanded, "选项列表（数量：" + items.arraySize + "）", true);
            if (!items.isExpanded)
            {
                if (GUILayout.Button("+ 添加选项", GUILayout.Height(24)))
                    AddMixItem(items, parentName);
                return;
            }

            EditorGUI.indentLevel++;
            for (var i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选项 " + 序号转字母(i) + "：" + 取字符串(item, "displayName", parentName + 序号转字母(i)), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    items.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                DrawMixItemCard(item, parentName, i);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            if (GUILayout.Button("+ 添加选项", GUILayout.Height(24)))
                AddMixItem(items, parentName);

            EditorGUI.indentLevel--;
        }

        private void DrawSuperSwitchItemCard(SerializedProperty item, LastOpOutfitComponent config, string parentName, int index)
        {
            绘制属性字段(item.FindPropertyRelative("displayName"), "选项名称", false);
            绘制整数输入(item, "value", "参数值", 1, 254, index + 1);

            DrawGameObjectList(
                item.FindPropertyRelative("objects"),
                "主要物体",
                "这里拖当前选项自己的物体，例如衣服A本体；鞋子、袜子、内衣等混搭物体不要拖到这里。");

            DrawDefaultMixValuesCompact(item.FindPropertyRelative("defaultMixValues"), config, "默认混搭设置");
            DrawCloseOptionsCompact(item.FindPropertyRelative("closeOptions"), "这个项目的子级关闭菜单（可选）");
        }

        private void DrawMixItemCard(SerializedProperty item, string parentName, int index)
        {
            绘制属性字段(item.FindPropertyRelative("displayName"), "选项名称", false);
            绘制整数输入(item, "value", "参数值", 1, 254, index + 1);

            DrawGameObjectList(
                item.FindPropertyRelative("objects"),
                "混搭主体物体",
                "这里拖当前混搭选项的物体，例如鞋袜A整体里的鞋子A、袜子A、装饰等。");

            DrawCloseOptionsCompact(item.FindPropertyRelative("closeOptions"), "这个混搭选项的子级关闭菜单（默认保存同步）");
        }

        private void DrawDefaultMixValuesCompact(SerializedProperty list, LastOpOutfitComponent config, string title)
        {
            if (list == null || !list.isArray) return;

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, title + "（数量：" + list.arraySize + "）", true);
            if (!list.isExpanded) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("这里控制：选中这个套装选项时，要不要自动切换鞋子、袜子、内衣等混搭项目。", MessageType.None);

            for (var i = 0; i < list.arraySize; i++)
            {
                var setting = list.GetArrayElementAtIndex(i);
                var mixName = 取字符串(setting, "mixGroupName", "混搭项目" + (i + 1));

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("混搭项目：" + mixName, EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                绘制属性字段(setting.FindPropertyRelative("mixGroupName"), "对应混搭项目名称", false);
                绘制布尔输入(setting, "enabled", "选中这个套装项时修改它");
                绘制布尔输入(setting, "autoFollowItemValue", "默认值跟随本选项参数值");
                绘制整数输入(setting, "value", "默认切换到参数值", -1, 254, 1);

                var parameterName = setting.FindPropertyRelative("parameterName");
                if (parameterName != null && parameterName.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(parameterName.stringValue))
                {
                    EditorGUILayout.LabelField("对应内部参数名", parameterName.stringValue, EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Default Mix Setting", GUILayout.Height(22)))
            {
                var idx = list.arraySize;
                list.InsertArrayElementAtIndex(idx);
                var setting = list.GetArrayElementAtIndex(idx);
                var firstMix = config != null && config.mixGroups != null && config.mixGroups.Count > 0 && config.mixGroups[0] != null
                    ? config.mixGroups[0]
                    : null;
                写字符串(setting, "mixGroupName", firstMix != null ? firstMix.displayName : "混搭项目");
                写字符串(setting, "parameterName", firstMix != null ? firstMix.parameterName : "Mix");
                SetBool(setting, "enabled", true);
                SetBool(setting, "autoFollowItemValue", true);
                SetInt(setting, "value", 1);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGameObjectList(SerializedProperty list, string title, string help)
        {
            if (list == null || !list.isArray) return;

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, title + "（数量：" + list.arraySize + "）", true);
            if (!list.isExpanded) return;

            EditorGUI.indentLevel++;
            if (!string.IsNullOrEmpty(help)) EditorGUILayout.HelpBox(help, MessageType.None);

            for (var i = 0; i < list.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var element = list.GetArrayElementAtIndex(i);
                绘制属性字段(element, "物体 " + (i + 1), false);
                if (GUILayout.Button("-", GUILayout.Width(28)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add object reference slot", GUILayout.Height(22)))
            {
                var idx = list.arraySize;
                list.InsertArrayElementAtIndex(idx);
                var element = list.GetArrayElementAtIndex(idx);
                if (element != null && element.propertyType == SerializedPropertyType.ObjectReference)
                    element.objectReferenceValue = null;
            }
            EditorGUI.indentLevel--;
        }

        private void DrawCloseOptionsCompact(SerializedProperty list, string title)
        {
            if (list == null || !list.isArray) return;

            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, title + "（数量：" + list.arraySize + "）", true);
            if (!list.isExpanded)
            {
                if (GUILayout.Button("+ Add Close Part Toggle", GUILayout.Height(22)))
                    AddCloseOption(list);
                return;
            }

            EditorGUI.indentLevel++;
            for (var i = 0; i < list.arraySize; i++)
            {
                var close = list.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("关闭部件开关 " + (i + 1), EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    list.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                绘制属性字段(close.FindPropertyRelative("displayName"), "开关名称", false);
                DrawGameObjectList(close.FindPropertyRelative("objects"), "要关闭的部件", "打开这个关闭开关时，下面这些物体会被关闭。");

                var saved = close.FindPropertyRelative("saved");
                var synced = close.FindPropertyRelative("synced");
                if (saved != null && saved.propertyType == SerializedPropertyType.Boolean) saved.boolValue = true;
                if (synced != null && synced.propertyType == SerializedPropertyType.Boolean) synced.boolValue = true;
                using (new EditorGUI.DisabledScope(true))
                {
                    绘制属性字段(saved, "保存状态", false);
                    绘制属性字段(synced, "网络同步", false);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Close Part Toggle", GUILayout.Height(22)))
                AddCloseOption(list);

            EditorGUI.indentLevel--;
        }

        private void AddCloseOption(SerializedProperty list)
        {
            if (list == null || !list.isArray) return;
            var idx = list.arraySize;
            list.InsertArrayElementAtIndex(idx);
            var close = list.GetArrayElementAtIndex(idx);
            写字符串(close, "displayName", "关闭部件" + 序号转字母(idx));
            SetBool(close, "saved", true);
            SetBool(close, "synced", true);
            EnsureObjectSlotProperty(close.FindPropertyRelative("objects"), true);
        }

        private void AddSuperSwitchItem(SerializedProperty items, LastOpOutfitComponent config, string parentName)
        {
            if (items == null || !items.isArray) return;

            var index = items.arraySize;
            items.InsertArrayElementAtIndex(index);
            var item = items.GetArrayElementAtIndex(index);

            var nextValue = GetNextValue(items, index);
            SetString(item, "displayName", parentName + 序号转字母(index));
            SetInt(item, "value", nextValue);
            EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);

            var closeOptions = item.FindPropertyRelative("closeOptions");
            if (closeOptions != null && closeOptions.isArray) closeOptions.ClearArray();

            var defaultMix = item.FindPropertyRelative("defaultMixValues");
            if (defaultMix != null && defaultMix.isArray)
            {
                defaultMix.ClearArray();
                foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
                {
                    if (group == null || string.IsNullOrWhiteSpace(group.displayName)) continue;
                    var idx = defaultMix.arraySize;
                    defaultMix.InsertArrayElementAtIndex(idx);
                    var setting = defaultMix.GetArrayElementAtIndex(idx);
                    SetString(setting, "mixGroupName", group.displayName);
                    SetString(setting, "parameterName", group.parameterName);
                    SetBool(setting, "enabled", true);
                    SetBool(setting, "autoFollowItemValue", true);
                    SetInt(setting, "value", nextValue);
                }
            }
        }

        private void AddMixItem(SerializedProperty items, string parentName)
        {
            if (items == null || !items.isArray) return;

            var index = items.arraySize;
            items.InsertArrayElementAtIndex(index);
            var item = items.GetArrayElementAtIndex(index);

            SetString(item, "displayName", parentName + 序号转字母(index));
            SetInt(item, "value", GetNextValue(items, index));
            EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);

            var closeOptions = item.FindPropertyRelative("closeOptions");
            if (closeOptions != null && closeOptions.isArray) closeOptions.ClearArray();
        }

        private static int GetNextValue(SerializedProperty items, int newIndex)
        {
            var max = 0;
            for (var i = 0; i < newIndex; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                var value = item != null ? item.FindPropertyRelative("value") : null;
                if (value != null && value.propertyType == SerializedPropertyType.Integer)
                    max = Mathf.Max(max, value.intValue);
            }

            return Mathf.Clamp(max + 1, 1, 254);
        }

        private void AddCustomCloseButton(SerializedProperty buttons, LastOpOutfitComponent config)
        {
            var index = buttons.arraySize;
            buttons.InsertArrayElementAtIndex(index);
            var element = buttons.GetArrayElementAtIndex(index);

            var firstGroup = config.mixGroups != null && config.mixGroups.Count > 0 && config.mixGroups[0] != null
                ? config.mixGroups[0].displayName
                : "混搭项目";

            SetBool(element, "enabled", true);
            SetString(element, "displayName", "一键全关（含" + firstGroup + "）");
            SetBool(element, "saved", true);
            SetBool(element, "synced", true);

            var included = element.FindPropertyRelative("includedMixGroupNames");
            if (included != null && included.isArray)
            {
                included.ClearArray();
                AddStringToArray(included, firstGroup);
            }
        }

        private bool Initialize new item(LastOpOutfitComponent config, int oldSuperCount, int oldMixCount)
        {
            if (config == null) return false;

            var changed = false;

            if (config.superSwitchGroups != null && config.superSwitchGroups.Count > oldSuperCount)
            {
                Undo.RecordObject(config, "Initialize new Super Switch group");
                for (var i = oldSuperCount; i < config.superSwitchGroups.Count; i++)
                {
                    Initialize Super Switch group(config, config.superSwitchGroups[i], i);
                }
                changed = true;
            }

            if (config.mixGroups != null && config.mixGroups.Count > oldMixCount)
            {
                Undo.RecordObject(config, "Initialize new Mix Group");
                for (var i = oldMixCount; i < config.mixGroups.Count; i++)
                {
                    Initialize Mix Group(config.mixGroups[i], i);
                }
                changed = true;
            }

            if (changed) config.RefreshDerivedData();
            return changed;
        }

        private static void Initialize Super Switch group(LastOpOutfitComponent config, SuperSwitchGroupOption item, int index)
        {
            if (item == null) return;

            var suffix = 序号转字母(index);
            var name = "套装项目切换" + suffix;
            item.displayName = name;
            item.menuName = name;
            item.parameterName = "Super_" + (index + 1);
            item.lastAppliedParameterName = "LW_LastApplied_Super_" + (index + 1);
            item.closeSubMenuName = "关闭部件";
            item.partsMenuSuffix = "部件";
            item.generateNone = false;
            item.sameItemClickResetsDefaultMix = true;
            item.items = new List<SuperSwitchItemOption>
            {
                CreateSuperItem(config, name + "A", 1),
                CreateSuperItem(config, name + "B", 2),
            };
        }

        private static SuperSwitchItemOption CreateSuperItem(LastOpOutfitComponent config, string displayName, int value)
        {
            var item = new SuperSwitchItemOption
            {
                displayName = displayName,
                value = value,
                objects = new List<GameObject> { null },
                closeOptions = new List<ClothesCloseOption>(),
                defaultMixValues = new List<MixDefaultSetting>()
            };

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null || string.IsNullOrWhiteSpace(group.displayName)) continue;
                item.defaultMixValues.Add(new MixDefaultSetting
                {
                    mixGroupName = group.displayName,
                    parameterName = group.parameterName,
                    enabled = true,
                    autoFollowItemValue = true,
                    value = value
                });
            }

            return item;
        }

        private static void Initialize Mix Group(MixGroupOption item, int index)
        {
            if (item == null) return;

            var suffix = 序号转字母(index);
            var name = "混搭" + suffix;

            item.displayName = name;
            item.menuName = name;
            item.parameterName = "Mix_" + (index + 1);
            item.generateNone = true;
            item.superNoneAlsoTurnsOff = true;
            item.items = new List<PartOption>
            {
                new PartOption { displayName = name + "A", value = 1, objects = new List<GameObject> { null }, closeOptions = new List<ClothesCloseOption>() },
                new PartOption { displayName = name + "B", value = 2, objects = new List<GameObject> { null }, closeOptions = new List<ClothesCloseOption>() },
            };
        }


        private static bool Fix copy-new-item numbering(LastOpOutfitComponent config)
        {
            if (config == null) return false;

            var changed = false;

            foreach (var group in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (group == null) continue;
                var parentName = string.IsNullOrWhiteSpace(group.displayName) ? "套装项目切换" : group.displayName;
                changed |= Fix Super option copy numbering(config, group.items, parentName);
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null) continue;
                var parentName = string.IsNullOrWhiteSpace(group.displayName) ? "混搭" : group.displayName;
                changed |= Fix Mix option copy numbering(group.items, parentName);
            }

            return changed;
        }

        private static bool Fix Super option copy numbering(LastOpOutfitComponent config, List<SuperSwitchItemOption> items, string parentName)
        {
            if (items == null) return false;

            var changed = false;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                if (i > 0 && items[i - 1] != null)
                {
                    var previous = items[i - 1];
                    var copiedName = !string.IsNullOrWhiteSpace(item.displayName) && item.displayName == previous.displayName;
                    var copiedValue = item.value == previous.value;

                    if (copiedName)
                    {
                        item.displayName = parentName + 序号转字母(i);
                        changed = true;
                    }

                    if (copiedValue)
                    {
                        item.value = GetNextFreeItemValue(items, i);
                        changed = true;
                    }

                    if (copiedName || copiedValue)
                    {
                        if (item.objects == null) item.objects = new List<GameObject> { null };
                        if (item.objects.Count == 0) item.objects.Add(null);
                        if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();
                        if (item.defaultMixValues == null) item.defaultMixValues = new List<MixDefaultSetting>();

                        foreach (var setting in item.defaultMixValues)
                        {
                            if (setting != null && setting.autoFollowItemValue)
                                setting.value = Mathf.Clamp(item.value, 1, 254);
                        }

                        if (item.defaultMixValues.Count == 0 && config != null)
                        {
                            foreach (var mix in config.mixGroups ?? new List<MixGroupOption>())
                            {
                                if (mix == null || string.IsNullOrWhiteSpace(mix.displayName)) continue;
                                item.defaultMixValues.Add(new MixDefaultSetting
                                {
                                    mixGroupName = mix.displayName,
                                    parameterName = mix.parameterName,
                                    enabled = true,
                                    autoFollowItemValue = true,
                                    value = Mathf.Clamp(item.value, 1, 254)
                                });
                            }
                        }
                    }
                }

                changed |= Fix close item copy numbering(item.closeOptions);
            }

            return changed;
        }

        private static bool Fix Mix option copy numbering(List<PartOption> items, string parentName)
        {
            if (items == null) return false;

            var changed = false;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                if (i > 0 && items[i - 1] != null)
                {
                    var previous = items[i - 1];
                    var copiedName = !string.IsNullOrWhiteSpace(item.displayName) && item.displayName == previous.displayName;
                    var copiedValue = item.value == previous.value;

                    if (copiedName)
                    {
                        item.displayName = parentName + 序号转字母(i);
                        changed = true;
                    }

                    if (copiedValue)
                    {
                        item.value = GetNextFreePartValue(items, i);
                        changed = true;
                    }

                    if (copiedName || copiedValue)
                    {
                        if (item.objects == null) item.objects = new List<GameObject> { null };
                        if (item.objects.Count == 0) item.objects.Add(null);
                        if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();
                    }
                }

                changed |= Fix close item copy numbering(item.closeOptions);
            }

            return changed;
        }

        private static bool Fix close item copy numbering(List<ClothesCloseOption> closeOptions)
        {
            if (closeOptions == null) return false;

            var changed = false;
            for (var i = 0; i < closeOptions.Count; i++)
            {
                var close = closeOptions[i];
                if (close == null) continue;

                if (i > 0 && closeOptions[i - 1] != null && close.displayName == closeOptions[i - 1].displayName)
                {
                    close.displayName = "关闭部件" + 序号转字母(i);
                    changed = true;
                }

                if (close.objects == null)
                {
                    close.objects = new List<GameObject> { null };
                    changed = true;
                }
                else if (close.objects.Count == 0)
                {
                    close.objects.Add(null);
                    changed = true;
                }

                if (!close.saved)
                {
                    close.saved = true;
                    changed = true;
                }

                if (!close.synced)
                {
                    close.synced = true;
                    changed = true;
                }
            }

            return changed;
        }

        private static int GetNextFreeItemValue(List<SuperSwitchItemOption> items, int endExclusive)
        {
            var used = new HashSet<int>();
            for (var i = 0; i < endExclusive && i < items.Count; i++)
            {
                if (items[i] != null) used.Add(items[i].value);
            }

            for (var value = 1; value <= 254; value++)
            {
                if (!used.Contains(value)) return value;
            }

            return 254;
        }

        private static int GetNextFreePartValue(List<PartOption> items, int endExclusive)
        {
            var used = new HashSet<int>();
            for (var i = 0; i < endExclusive && i < items.Count; i++)
            {
                if (items[i] != null) used.Add(items[i].value);
            }

            for (var value = 1; value <= 254; value++)
            {
                if (!used.Contains(value)) return value;
            }

            return 254;
        }

        private static void EnsureInspectorDefaults(SerializedObject so)
        {
            var keepEveryListSlot = so.FindProperty("keepOneObjectSlotForEveryObjectList");
            var keepEveryListEnabled = keepEveryListSlot == null || keepEveryListSlot.boolValue;
            if (!keepEveryListEnabled) return;

            EnsureSuperSwitchObjectSlots(so.FindProperty("superSwitchGroups"));
            EnsureMixGroupItemObjectSlots(so.FindProperty("mixGroups"));
        }

        private static void EnsureSuperSwitchObjectSlots(SerializedProperty groups)
        {
            if (groups == null || !groups.isArray) return;

            for (var i = 0; i < groups.arraySize; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                if (group == null) continue;

                var items = group.FindPropertyRelative("items");
                if (items == null || !items.isArray) continue;

                for (var j = 0; j < items.arraySize; j++)
                {
                    var item = items.GetArrayElementAtIndex(j);
                    if (item == null) continue;

                    // 只自动展开 Objects，避免 Close Options / Default Mix Values 的折叠箭头被每帧强制打开。
                    EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);

                    var closeOptions = item.FindPropertyRelative("closeOptions");
                    if (closeOptions != null && closeOptions.isArray)
                    {
                        for (var k = 0; k < closeOptions.arraySize; k++)
                        {
                            var close = closeOptions.GetArrayElementAtIndex(k);
                            if (close == null) continue;
                            ForceCloseOptionSavedSynced(close);
                            EnsureObjectSlotProperty(close.FindPropertyRelative("objects"), true);
                        }
                    }
                }
            }
        }

        private static void EnsureMixGroupItemObjectSlots(SerializedProperty groups)
        {
            if (groups == null || !groups.isArray) return;

            for (var i = 0; i < groups.arraySize; i++)
            {
                var group = groups.GetArrayElementAtIndex(i);
                if (group == null) continue;

                var items = group.FindPropertyRelative("items");
                if (items == null || !items.isArray) continue;

                for (var j = 0; j < items.arraySize; j++)
                {
                    var item = items.GetArrayElementAtIndex(j);
                    if (item == null) continue;

                    // 只自动展开 Objects，其他折叠箭头交给用户自己控制。
                    EnsureObjectSlotProperty(item.FindPropertyRelative("objects"), true);

                    var closeOptions = item.FindPropertyRelative("closeOptions");
                    if (closeOptions != null && closeOptions.isArray)
                    {
                        for (var k = 0; k < closeOptions.arraySize; k++)
                        {
                            var close = closeOptions.GetArrayElementAtIndex(k);
                            if (close == null) continue;
                            ForceCloseOptionSavedSynced(close);
                            EnsureObjectSlotProperty(close.FindPropertyRelative("objects"), true);
                        }
                    }
                }
            }
        }

        private static void ForceCloseOptionSavedSynced(SerializedProperty close)
        {
            if (close == null) return;

            var saved = close.FindPropertyRelative("saved");
            if (saved != null && saved.propertyType == SerializedPropertyType.Boolean)
            {
                saved.boolValue = true;
            }

            var synced = close.FindPropertyRelative("synced");
            if (synced != null && synced.propertyType == SerializedPropertyType.Boolean)
            {
                synced.boolValue = true;
            }
        }

        private static void EnsureObjectSlotProperty(SerializedProperty objects, bool expand)
        {
            if (objects == null || !objects.isArray) return;
            if (expand) objects.isExpanded = true;

            if (objects.arraySize == 0)
            {
                objects.InsertArrayElementAtIndex(0);
                var item = objects.GetArrayElementAtIndex(0);
                if (item != null && item.propertyType == SerializedPropertyType.ObjectReference)
                    item.objectReferenceValue = null;
            }
        }

        private static string 序号转字母(int index)
        {
            index = Mathf.Max(0, index);
            var result = "";
            index++;
            while (index > 0)
            {
                index--;
                result = (char)('A' + (index % 26)) + result;
                index /= 26;
            }
            return result;
        }

        private static string GetString(SerializedProperty e, string name, string fallback)
        {
            var p = e != null ? e.FindPropertyRelative(name) : null;
            if (p != null && p.propertyType == SerializedPropertyType.String && !string.IsNullOrWhiteSpace(p.stringValue))
                return p.stringValue;
            return fallback;
        }

        private static void SetString(SerializedProperty e, string name, string value)
        {
            var p = e.FindPropertyRelative(name);
            if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = value;
        }

        private static void SetInt(SerializedProperty e, string name, int value)
        {
            var p = e.FindPropertyRelative(name);
            if (p != null && p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
        }

        private static void SetBool(SerializedProperty e, string name, bool value)
        {
            var p = e.FindPropertyRelative(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
        }

        private static bool StringArrayContains(SerializedProperty array, string value)
        {
            if (array == null || !array.isArray) return false;
            for (var i = 0; i < array.arraySize; i++)
            {
                var item = array.GetArrayElementAtIndex(i);
                if (item != null && item.propertyType == SerializedPropertyType.String && item.stringValue == value)
                    return true;
            }
            return false;
        }

        private static void AddStringToArray(SerializedProperty array, string value)
        {
            if (array == null || !array.isArray || StringArrayContains(array, value)) return;
            var index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            var item = array.GetArrayElementAtIndex(index);
            if (item != null && item.propertyType == SerializedPropertyType.String)
                item.stringValue = value;
        }

        private static void RemoveStringFromArray(SerializedProperty array, string value)
        {
            if (array == null || !array.isArray) return;
            for (var i = array.arraySize - 1; i >= 0; i--)
            {
                var item = array.GetArrayElementAtIndex(i);
                if (item != null && item.propertyType == SerializedPropertyType.String && item.stringValue == value)
                    array.DeleteArrayElementAtIndex(i);
            }
        }
    }


    public static class LastOpOutfitBackupUtility
    {
        [Serializable]
        private class BackupData
        {
            public int formatVersion = 1;
            public string plugin = "com.paulxstx.last-op-outfit-cn";
            public string avatarRootName;
            public string outputFolder;
            public bool useAvatarNameSubFolder;
            public string managerObjectName;
            public bool replaceOldManagerObject;
            public bool putGeneratedManagerFirst;
            public string rootMenuName;
            public bool generateCloseAllPartsButton;
            public string closeAllPartsButtonName;
            public bool closeAllButtonsOnlyOnceInSuperMenu;
            public bool closePartSavedByDefault;
            public bool closePartSyncedByDefault;
            public bool autoFixCloseOptionSaveSync;
            public bool autoGenerateCloseOptionsFromFirstLevelChildren;
            public bool skipBonesWhenAutoGenerateCloseOptions;
            public bool onlyAutoGenerateCloseOptionsWhenEmpty;
            public bool allowManualGenerateSuperSwitchCloseOptions;
            public bool autoCreateObjectSlotForNewCloseOption;
            public bool keepOneObjectSlotForEveryObjectList;
            public bool keepCurrentOutfitByDefault;
            public bool stripBuilderComponentOnUpload;
            public bool buildOnUpload;
            public string fxAnimatorAssetName;
            public List<BackupSuperGroup> superSwitchGroups = new List<BackupSuperGroup>();
            public List<BackupMixGroup> mixGroups = new List<BackupMixGroup>();
            public List<BackupCustomCloseButton> customCloseButtons = new List<BackupCustomCloseButton>();
        }

        [Serializable]
        private class BackupCloseOption
        {
            public string displayName;
            public List<string> objectPaths = new List<string>();
            public bool saved;
            public bool synced;
        }

        [Serializable]
        private class BackupMixDefaultSetting
        {
            public string mixGroupName;
            public string parameterName;
            public bool enabled;
            public bool autoFollowItemValue;
            public int value;
        }

        [Serializable]
        private class BackupSuperItem
        {
            public string displayName;
            public int value;
            public List<BackupMixDefaultSetting> defaultMixValues = new List<BackupMixDefaultSetting>();
            public List<string> objectPaths = new List<string>();
            public List<BackupCloseOption> closeOptions = new List<BackupCloseOption>();
        }

        [Serializable]
        private class BackupSuperGroup
        {
            public string displayName;
            public string menuName;
            public string parameterName;
            public string lastAppliedParameterName;
            public string closeSubMenuName;
            public string partsMenuSuffix;
            public bool generateNone;
            public bool sameItemClickResetsDefaultMix;
            public List<BackupSuperItem> items = new List<BackupSuperItem>();
        }

        [Serializable]
        private class BackupPartOption
        {
            public string displayName;
            public int value;
            public List<string> objectPaths = new List<string>();
            public List<BackupCloseOption> closeOptions = new List<BackupCloseOption>();
        }

        [Serializable]
        private class BackupMixGroup
        {
            public string displayName;
            public string menuName;
            public string parameterName;
            public bool generateNone;
            public bool superNoneAlsoTurnsOff;
            public List<BackupPartOption> items = new List<BackupPartOption>();
        }

        [Serializable]
        private class BackupCustomCloseButton
        {
            public bool enabled;
            public string displayName;
            public List<string> includedMixGroupNames = new List<string>();
            public bool saved;
            public bool synced;
        }

        public static void ExportConfig(LastOpOutfitComponent config)
        {
            if (config == null)
            {
                EditorUtility.DisplayDialog("Paulxstx Outfit", "Config component not found.", "确定");
                return;
            }

            config.RefreshDerivedData();

            var fileName = "Paulxstx Outfit Config Backup";
            if (config.avatarRoot != null && !string.IsNullOrWhiteSpace(config.avatarRoot.name))
                fileName += "_" + SafeFileName(config.avatarRoot.name);

            var path = EditorUtility.SaveFilePanel("导出Paulxstx Outfit Config Backup", Application.dataPath, fileName + ".json", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = Capture(config);
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json, Encoding.UTF8);

            EditorUtility.DisplayDialog(
                "Paulxstx Outfit",
                "Config backup exported: \n" + path + "\n\n" +
                "Backup stores paths relative to avatar root. When importing, first set “角色根节点”设置成同一个模型或相同层级结构的模型。",
                "确定");
        }

        public static bool ImportConfig(LastOpOutfitComponent config)
        {
            if (config == null)
            {
                EditorUtility.DisplayDialog("Paulxstx Outfit", "Config component not found.", "确定");
                return false;
            }

            var path = EditorUtility.OpenFilePanel("导入Paulxstx Outfit Config Backup", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path)) return false;

            BackupData data;
            try
            {
                data = JsonUtility.FromJson<BackupData>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Paulxstx Outfit - 导入失败", "JSON 读取失败：\n" + e.Message, "确定");
                return false;
            }

            if (data == null || data.formatVersion <= 0)
            {
                EditorUtility.DisplayDialog("Paulxstx Outfit - 导入失败", "这个 JSON 看起来不是本插件导出的配置备份。", "确定");
                return false;
            }

            Undo.RecordObject(config, "导入Paulxstx Outfit Config Backup");
            Apply(config, data);
            config.RefreshDerivedData();
            EditorUtility.SetDirty(config);

            EditorUtility.DisplayDialog(
                "Paulxstx Outfit",
                "Config backup imported.\n\n" +
                "Unresolved references usually mean hierarchy/name mismatch. Run “Conflict Detection / Config Check”。",
                "确定");

            return true;
        }

        private static BackupData Capture(LastOpOutfitComponent config)
        {
            var root = config.avatarRoot != null ? config.avatarRoot : config.gameObject;
            var data = new BackupData
            {
                avatarRootName = root != null ? root.name : "",
                outputFolder = config.outputFolder,
                useAvatarNameSubFolder = config.useAvatarNameSubFolder,
                managerObjectName = config.managerObjectName,
                replaceOldManagerObject = config.replaceOldManagerObject,
                putGeneratedManagerFirst = config.putGeneratedManagerFirst,
                rootMenuName = config.rootMenuName,
                generateCloseAllPartsButton = config.generateCloseAllPartsButton,
                closeAllPartsButtonName = config.closeAllPartsButtonName,
                closeAllButtonsOnlyOnceInSuperMenu = config.closeAllButtonsOnlyOnceInSuperMenu,
                closePartSavedByDefault = config.closePartSavedByDefault,
                closePartSyncedByDefault = config.closePartSyncedByDefault,
                autoFixCloseOptionSaveSync = config.autoFixCloseOptionSaveSync,
                autoGenerateCloseOptionsFromFirstLevelChildren = config.autoGenerateCloseOptionsFromFirstLevelChildren,
                skipBonesWhenAutoGenerateCloseOptions = config.skipBonesWhenAutoGenerateCloseOptions,
                onlyAutoGenerateCloseOptionsWhenEmpty = config.onlyAutoGenerateCloseOptionsWhenEmpty,
                allowManualGenerateSuperSwitchCloseOptions = config.allowManualGenerateSuperSwitchCloseOptions,
                autoCreateObjectSlotForNewCloseOption = config.autoCreateObjectSlotForNewCloseOption,
                keepOneObjectSlotForEveryObjectList = config.keepOneObjectSlotForEveryObjectList,
                keepCurrentOutfitByDefault = config.keepCurrentOutfitByDefault,
                stripBuilderComponentOnUpload = config.stripBuilderComponentOnUpload,
                buildOnUpload = config.buildOnUpload,
                fxAnimatorAssetName = config.fxAnimatorAssetName,
            };

            data.superSwitchGroups = (config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
                .Where(g => g != null)
                .Select(g => CaptureSuperGroup(root, g))
                .ToList();

            data.mixGroups = (config.mixGroups ?? new List<MixGroupOption>())
                .Where(g => g != null)
                .Select(g => CaptureMixGroup(root, g))
                .ToList();

            data.customCloseButtons = (config.customCloseButtons ?? new List<CustomCloseButtonOption>())
                .Where(b => b != null)
                .Select(b => new BackupCustomCloseButton
                {
                    enabled = b.enabled,
                    displayName = b.displayName,
                    includedMixGroupNames = new List<string>(b.includedMixGroupNames ?? new List<string>()),
                    saved = b.saved,
                    synced = b.synced
                })
                .ToList();

            return data;
        }

        private static BackupSuperGroup CaptureSuperGroup(GameObject root, SuperSwitchGroupOption group)
        {
            return new BackupSuperGroup
            {
                displayName = group.displayName,
                menuName = group.menuName,
                parameterName = group.parameterName,
                lastAppliedParameterName = group.lastAppliedParameterName,
                closeSubMenuName = group.closeSubMenuName,
                partsMenuSuffix = group.partsMenuSuffix,
                generateNone = group.generateNone,
                sameItemClickResetsDefaultMix = group.sameItemClickResetsDefaultMix,
                items = (group.items ?? new List<SuperSwitchItemOption>())
                    .Where(i => i != null)
                    .Select(i => new BackupSuperItem
                    {
                        displayName = i.displayName,
                        value = i.value,
                        defaultMixValues = (i.defaultMixValues ?? new List<MixDefaultSetting>())
                            .Where(v => v != null)
                            .Select(v => new BackupMixDefaultSetting
                            {
                                mixGroupName = v.mixGroupName,
                                parameterName = v.parameterName,
                                enabled = v.enabled,
                                autoFollowItemValue = v.autoFollowItemValue,
                                value = v.value
                            })
                            .ToList(),
                        objectPaths = CaptureObjectPaths(root, i.objects),
                        closeOptions = CaptureCloseOptions(root, i.closeOptions)
                    })
                    .ToList()
            };
        }

        private static BackupMixGroup CaptureMixGroup(GameObject root, MixGroupOption group)
        {
            return new BackupMixGroup
            {
                displayName = group.displayName,
                menuName = group.menuName,
                parameterName = group.parameterName,
                generateNone = group.generateNone,
                superNoneAlsoTurnsOff = group.superNoneAlsoTurnsOff,
                items = (group.items ?? new List<PartOption>())
                    .Where(i => i != null)
                    .Select(i => new BackupPartOption
                    {
                        displayName = i.displayName,
                        value = i.value,
                        objectPaths = CaptureObjectPaths(root, i.objects),
                        closeOptions = CaptureCloseOptions(root, i.closeOptions)
                    })
                    .ToList()
            };
        }

        private static List<BackupCloseOption> CaptureCloseOptions(GameObject root, List<ClothesCloseOption> closeOptions)
        {
            return (closeOptions ?? new List<ClothesCloseOption>())
                .Where(c => c != null)
                .Select(c => new BackupCloseOption
                {
                    displayName = c.displayName,
                    objectPaths = CaptureObjectPaths(root, c.objects),
                    saved = c.saved,
                    synced = c.synced
                })
                .ToList();
        }

        private static List<string> CaptureObjectPaths(GameObject root, List<GameObject> objects)
        {
            return (objects ?? new List<GameObject>()).Select(o => MakeObjectPath(root, o)).ToList();
        }

        private static void Apply(LastOpOutfitComponent config, BackupData data)
        {
            var root = config.avatarRoot != null ? config.avatarRoot : config.gameObject;

            config.outputFolder = data.outputFolder;
            config.useAvatarNameSubFolder = data.useAvatarNameSubFolder;
            config.managerObjectName = data.managerObjectName;
            config.replaceOldManagerObject = data.replaceOldManagerObject;
            config.putGeneratedManagerFirst = data.putGeneratedManagerFirst;
            config.rootMenuName = data.rootMenuName;
            config.generateCloseAllPartsButton = data.generateCloseAllPartsButton;
            config.closeAllPartsButtonName = data.closeAllPartsButtonName;
            config.closeAllButtonsOnlyOnceInSuperMenu = data.closeAllButtonsOnlyOnceInSuperMenu;
            config.closePartSavedByDefault = true;
            config.closePartSyncedByDefault = true;
            config.autoFixCloseOptionSaveSync = data.autoFixCloseOptionSaveSync;
            config.autoGenerateCloseOptionsFromFirstLevelChildren = data.autoGenerateCloseOptionsFromFirstLevelChildren;
            config.skipBonesWhenAutoGenerateCloseOptions = data.skipBonesWhenAutoGenerateCloseOptions;
            config.onlyAutoGenerateCloseOptionsWhenEmpty = data.onlyAutoGenerateCloseOptionsWhenEmpty;
            config.allowManualGenerateSuperSwitchCloseOptions = data.allowManualGenerateSuperSwitchCloseOptions;
            config.autoCreateObjectSlotForNewCloseOption = data.autoCreateObjectSlotForNewCloseOption;
            config.keepOneObjectSlotForEveryObjectList = data.keepOneObjectSlotForEveryObjectList;
            config.keepCurrentOutfitByDefault = data.keepCurrentOutfitByDefault;
            config.stripBuilderComponentOnUpload = data.stripBuilderComponentOnUpload;
            config.buildOnUpload = data.buildOnUpload;
            config.fxAnimatorAssetName = data.fxAnimatorAssetName;

            config.superSwitchGroups = (data.superSwitchGroups ?? new List<BackupSuperGroup>()).Select(g => ApplySuperGroup(root, g)).ToList();
            config.mixGroups = (data.mixGroups ?? new List<BackupMixGroup>()).Select(g => ApplyMixGroup(root, g)).ToList();
            config.customCloseButtons = (data.customCloseButtons ?? new List<BackupCustomCloseButton>())
                .Select(b => new CustomCloseButtonOption
                {
                    enabled = b.enabled,
                    displayName = b.displayName,
                    includedMixGroupNames = new List<string>(b.includedMixGroupNames ?? new List<string>()),
                    saved = true,
                    synced = true
                })
                .ToList();
        }

        private static SuperSwitchGroupOption ApplySuperGroup(GameObject root, BackupSuperGroup group)
        {
            return new SuperSwitchGroupOption
            {
                displayName = group.displayName,
                menuName = group.menuName,
                parameterName = group.parameterName,
                lastAppliedParameterName = group.lastAppliedParameterName,
                closeSubMenuName = group.closeSubMenuName,
                partsMenuSuffix = group.partsMenuSuffix,
                generateNone = group.generateNone,
                sameItemClickResetsDefaultMix = group.sameItemClickResetsDefaultMix,
                items = (group.items ?? new List<BackupSuperItem>())
                    .Select(i => new SuperSwitchItemOption
                    {
                        displayName = i.displayName,
                        value = i.value,
                        defaultMixValues = (i.defaultMixValues ?? new List<BackupMixDefaultSetting>())
                            .Select(v => new MixDefaultSetting
                            {
                                mixGroupName = v.mixGroupName,
                                parameterName = v.parameterName,
                                enabled = v.enabled,
                                autoFollowItemValue = v.autoFollowItemValue,
                                value = v.value
                            })
                            .ToList(),
                        objects = ResolveObjectPaths(root, i.objectPaths),
                        closeOptions = ApplyCloseOptions(root, i.closeOptions)
                    })
                    .ToList()
            };
        }

        private static MixGroupOption ApplyMixGroup(GameObject root, BackupMixGroup group)
        {
            return new MixGroupOption
            {
                displayName = group.displayName,
                menuName = group.menuName,
                parameterName = group.parameterName,
                generateNone = group.generateNone,
                superNoneAlsoTurnsOff = group.superNoneAlsoTurnsOff,
                items = (group.items ?? new List<BackupPartOption>())
                    .Select(i => new PartOption
                    {
                        displayName = i.displayName,
                        value = i.value,
                        objects = ResolveObjectPaths(root, i.objectPaths),
                        closeOptions = ApplyCloseOptions(root, i.closeOptions)
                    })
                    .ToList()
            };
        }

        private static List<ClothesCloseOption> ApplyCloseOptions(GameObject root, List<BackupCloseOption> closeOptions)
        {
            return (closeOptions ?? new List<BackupCloseOption>())
                .Select(c => new ClothesCloseOption
                {
                    displayName = c.displayName,
                    objects = ResolveObjectPaths(root, c.objectPaths),
                    saved = true,
                    synced = true
                })
                .ToList();
        }

        private static List<GameObject> ResolveObjectPaths(GameObject root, List<string> paths)
        {
            var result = (paths ?? new List<string>()).Select(path => ResolveObjectPath(root, path)).ToList();
            if (result.Count == 0) result.Add(null);
            return result;
        }

        private static string MakeObjectPath(GameObject root, GameObject obj)
        {
            if (obj == null) return "";
            if (root != null && obj == root) return "$$$AVATAR_ROOT$$$";
            if (root != null && obj.transform.IsChildOf(root.transform))
                return GetRelativePath(root.transform, obj.transform);
            return "SCENE:" + GetFullPath(obj.transform);
        }

        private static GameObject ResolveObjectPath(GameObject root, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (path == "$$$AVATAR_ROOT$$$") return root;

            if (root != null)
            {
                var found = root.transform.Find(path);
                if (found != null) return found.gameObject;
            }

            if (path.StartsWith("SCENE:", StringComparison.Ordinal))
            {
                var found = GameObject.Find(path.Substring("SCENE:".Length));
                if (found != null) return found;
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null) return "";
            if (root == target) return "$$$AVATAR_ROOT$$$";

            var stack = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return current == root ? string.Join("/", stack.ToArray()) : "";
        }

        private static string GetFullPath(Transform target)
        {
            if (target == null) return "";
            var stack = new Stack<string>();
            var current = target;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", stack.ToArray());
        }

        private static string SafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Avatar";
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }
    }

    public static class LastOpOutfitConflictChecker
    {
        public static void ShowReport(LastOpOutfitComponent config)
        {
            var report = GenerateReport(config);
            Debug.Log(report);

            var dialogText = report.Length > 6500
                ? report.Substring(0, 6500) + "\n\n……内容较长，完整报告已输出到 Console。"
                : report;

            EditorUtility.DisplayDialog("Paulxstx Outfit - Conflict Detection", dialogText, "确定");
        }

        private static string GenerateReport(LastOpOutfitComponent config)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var infos = new List<string>();

            if (config == null)
            {
                errors.Add("Config component not found.");
                return BuildReport(errors, warnings, infos);
            }

            if (config.avatarRoot == null)
                errors.Add("Avatar root is empty.");

            var parameterOwners = new Dictionary<string, List<string>>();
            CollectParameterIssues(config, parameterOwners, errors, warnings);
            CollectValueIssues(config, warnings);
            CollectDefaultMixIssues(config, warnings);
            CollectCustomCloseButtonIssues(config, warnings);
            CollectObjectIssues(config, warnings, infos);
            CollectExternalObjectToggleIssues(config, warnings);

            if (errors.Count == 0 && warnings.Count == 0)
                infos.Add("No obvious conflicts found. Safe to build.");

            return BuildReport(errors, warnings, infos);
        }

        private static void CollectParameterIssues(
            LastOpOutfitComponent config,
            Dictionary<string, List<string>> parameterOwners,
            List<string> errors,
            List<string> warnings)
        {
            foreach (var group in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (group == null) continue;

                if (string.IsNullOrWhiteSpace(group.parameterName))
                    errors.Add("套装项目切换「" + group.displayName + "」Parameter name is empty.");
                else
                    AddParameter(parameterOwners, group.parameterName, "套装项目切换：" + group.displayName);

                var lastApplied = string.IsNullOrWhiteSpace(group.lastAppliedParameterName)
                    ? "LW_LastApplied_" + SanitizeParameterName(group.parameterName)
                    : group.lastAppliedParameterName;
                AddParameter(parameterOwners, lastApplied, "内部已应用参数：" + group.displayName);
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null) continue;

                if (string.IsNullOrWhiteSpace(group.parameterName))
                    errors.Add("混搭项目「" + group.displayName + "」Parameter name is empty.");
                else
                {
                    AddParameter(parameterOwners, group.parameterName, "混搭项目：" + group.displayName);
                    AddParameter(parameterOwners, "LW_LastMix_" + SanitizeParameterName(group.parameterName), "内部上次穿着：" + group.displayName);
                }
            }

            foreach (var dup in parameterOwners.Where(kv => kv.Value.Count > 1))
            {
                warnings.Add("参数名重复：「" + dup.Key + "」被这些项目共用：\n  - " + string.Join("\n  - ", dup.Value));
            }
        }

        private static void CollectValueIssues(LastOpOutfitComponent config, List<string> warnings)
        {
            foreach (var group in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (group == null || group.items == null) continue;

                foreach (var dup in group.items
                             .Where(i => i != null)
                             .GroupBy(i => i.value)
                             .Where(g => g.Key > 0 && g.Count() > 1))
                {
                    warnings.Add("套装项目切换「" + group.displayName + "」里参数值重复：" + dup.Key + "，涉及：" +
                                 string.Join("、", dup.Select(i => i.displayName)));
                }

                foreach (var item in group.items.Where(i => i != null))
                {
                    if (item.objects == null || item.objects.All(o => o == null))
                        warnings.Add("套装选项「" + group.displayName + " / " + item.displayName + "」没有拖入任何主体物体。");
                }
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null || group.items == null) continue;

                foreach (var dup in group.items
                             .Where(i => i != null)
                             .GroupBy(i => i.value)
                             .Where(g => g.Key > 0 && g.Count() > 1))
                {
                    warnings.Add("混搭项目「" + group.displayName + "」里参数值重复：" + dup.Key + "，涉及：" +
                                 string.Join("、", dup.Select(i => i.displayName)));
                }

                foreach (var item in group.items.Where(i => i != null))
                {
                    if (item.objects == null || item.objects.All(o => o == null))
                        warnings.Add("混搭选项「" + group.displayName + " / " + item.displayName + "」没有拖入任何主体物体。");
                }
            }
        }

        private static void CollectDefaultMixIssues(LastOpOutfitComponent config, List<string> warnings)
        {
            var mixGroups = (config.mixGroups ?? new List<MixGroupOption>()).Where(g => g != null).ToList();

            foreach (var super in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (super == null || super.items == null) continue;

                foreach (var item in super.items.Where(i => i != null))
                {
                    foreach (var setting in item.defaultMixValues ?? new List<MixDefaultSetting>())
                    {
                        if (setting == null || !setting.enabled) continue;
                        var matched = mixGroups.Any(g => g.displayName == setting.mixGroupName || g.parameterName == setting.parameterName);
                        if (!matched)
                        {
                            warnings.Add("默认混搭未匹配：套装「" + super.displayName + " / " + item.displayName +
                                         "」里的「" + setting.mixGroupName + " / " + setting.parameterName + "」没有找到对应混搭项目。");
                        }
                    }
                }
            }
        }

        private static void CollectCustomCloseButtonIssues(LastOpOutfitComponent config, List<string> warnings)
        {
            var mixGroupNames = new HashSet<string>((config.mixGroups ?? new List<MixGroupOption>())
                .Where(g => g != null)
                .Select(g => g.displayName));

            foreach (var button in config.customCloseButtons ?? new List<CustomCloseButtonOption>())
            {
                if (button == null || !button.enabled) continue;

                if (button.includedMixGroupNames == null || button.includedMixGroupNames.Count == 0)
                    warnings.Add("自定义一键全关按钮「" + button.displayName + "」没有勾选任何混搭项目。");

                foreach (var name in button.includedMixGroupNames ?? new List<string>())
                {
                    if (!mixGroupNames.Contains(name))
                        warnings.Add("自定义一键全关按钮「" + button.displayName + "」包含了不存在的混搭项目：「" + name + "」。");
                }
            }
        }

        private static void CollectObjectIssues(LastOpOutfitComponent config, List<string> warnings, List<string> infos)
        {
            var owners = new Dictionary<GameObject, List<string>>();
            var bodyOwners = new Dictionary<GameObject, List<string>>();
            var closeOwners = new Dictionary<GameObject, List<string>>();

            foreach (var group in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (group == null || group.items == null) continue;

                foreach (var item in group.items.Where(i => i != null))
                {
                    AddObjectOwners(owners, item.objects, "套装主体：" + group.displayName + " / " + item.displayName);
                    AddObjectOwners(bodyOwners, item.objects, "套装主体：" + group.displayName + " / " + item.displayName);

                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close == null) continue;
                        if (close.objects == null || close.objects.All(o => o == null))
                            warnings.Add("关闭部件「" + group.displayName + " / " + item.displayName + " / " + close.displayName + "」没有拖入要关闭的部件。");

                        AddObjectOwners(owners, close.objects, "套装关闭项：" + group.displayName + " / " + item.displayName + " / " + close.displayName);
                        AddObjectOwners(closeOwners, close.objects, "套装关闭项：" + group.displayName + " / " + item.displayName + " / " + close.displayName);
                    }
                }
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null || group.items == null) continue;

                foreach (var item in group.items.Where(i => i != null))
                {
                    AddObjectOwners(owners, item.objects, "混搭主体：" + group.displayName + " / " + item.displayName);
                    AddObjectOwners(bodyOwners, item.objects, "混搭主体：" + group.displayName + " / " + item.displayName);

                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close == null) continue;
                        if (close.objects == null || close.objects.All(o => o == null))
                            warnings.Add("关闭部件「" + group.displayName + " / " + item.displayName + " / " + close.displayName + "」没有拖入要关闭的部件。");

                        AddObjectOwners(owners, close.objects, "混搭关闭项：" + group.displayName + " / " + item.displayName + " / " + close.displayName);
                        AddObjectOwners(closeOwners, close.objects, "混搭关闭项：" + group.displayName + " / " + item.displayName + " / " + close.displayName);
                    }
                }
            }

            foreach (var pair in owners.Where(kv => kv.Value.Count > 1))
            {
                var inBody = bodyOwners.ContainsKey(pair.Key);
                var inClose = closeOwners.ContainsKey(pair.Key);

                if (inBody && inClose)
                {
                    warnings.Add("物体既被“主体切换”控制，又被“关闭部件”控制，可能出现开关优先级冲突：\n" +
                                 GetObjectPath(config.avatarRoot, pair.Key) + "\n  - " + string.Join("\n  - ", pair.Value));
                }
                else if (inClose)
                {
                    warnings.Add("Same object controlled by multiple close toggles - may conflict: \n" +
                                 GetObjectPath(config.avatarRoot, pair.Key) + "\n  - " + string.Join("\n  - ", pair.Value));
                }
                else
                {
                    infos.Add("Same object in multiple main options. Ignore if shared: \n" +
                              GetObjectPath(config.avatarRoot, pair.Key) + "\n  - " + string.Join("\n  - ", pair.Value));
                }
            }
        }

        private static void CollectExternalObjectToggleIssues(LastOpOutfitComponent config, List<string> warnings)
        {
            if (config == null || config.avatarRoot == null) return;

            var pluginObjects = new HashSet<GameObject>();
            foreach (var group in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (group == null) continue;
                foreach (var item in group.items ?? new List<SuperSwitchItemOption>())
                {
                    if (item == null) continue;
                    AddObjectsToSet(pluginObjects, item.objects);
                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close != null) AddObjectsToSet(pluginObjects, close.objects);
                    }
                }
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null) continue;
                foreach (var item in group.items ?? new List<PartOption>())
                {
                    if (item == null) continue;
                    AddObjectsToSet(pluginObjects, item.objects);
                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close != null) AddObjectsToSet(pluginObjects, close.objects);
                    }
                }
            }

            if (pluginObjects.Count == 0) return;

            var manager = config.avatarRoot.transform.Find(config.managerObjectName);
            var components = config.avatarRoot.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null) continue;
                if (!component.GetType().Name.Contains("ObjectToggle")) continue;
                if (manager != null && component.transform.IsChildOf(manager)) continue;

                var targets = ExtractGameObjectReferences(component).Where(o => o != null && pluginObjects.Contains(o)).Distinct().ToList();
                if (targets.Count == 0) continue;

                warnings.Add("检测到已有 MA Object Toggle 也控制了本插件中的物体：\n" +
                             component.gameObject.name + "\n  - " +
                             string.Join("\n  - ", targets.Select(t => GetObjectPath(config.avatarRoot, t))) + "\n" +
                             "如果你就是想让已有开关优先，请保持“让已有 MA 开关优先于本插件”开启，并确保已有开关在 Hierarchy 中位于本插件管理器后面。");
            }
        }

        private static void AddParameter(Dictionary<string, List<string>> dict, string name, string owner)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (!dict.TryGetValue(name, out var owners))
            {
                owners = new List<string>();
                dict[name] = owners;
            }
            owners.Add(owner);
        }

        private static void AddObjectOwners(Dictionary<GameObject, List<string>> dict, List<GameObject> objects, string owner)
        {
            foreach (var obj in objects ?? new List<GameObject>())
            {
                if (obj == null) continue;
                if (!dict.TryGetValue(obj, out var owners))
                {
                    owners = new List<string>();
                    dict[obj] = owners;
                }
                owners.Add(owner);
            }
        }

        private static void AddObjectsToSet(HashSet<GameObject> set, List<GameObject> objects)
        {
            foreach (var obj in objects ?? new List<GameObject>())
            {
                if (obj != null) set.Add(obj);
            }
        }

        private static IEnumerable<GameObject> ExtractGameObjectReferences(Component component)
        {
            var result = new List<GameObject>();
            try
            {
                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                var enter = true;
                while (prop.NextVisible(enter))
                {
                    enter = false;
                    if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue is GameObject go)
                        result.Add(go);
                }
            }
            catch
            {
                // Skipping unreadable third-party fields; main detection unaffected.
            }
            return result;
        }

        private static string BuildReport(List<string> errors, List<string> warnings, List<string> infos)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Paulxstx Outfit - Conflict Detection Report");
            sb.AppendLine("================================");
            sb.AppendLine("严重问题：" + errors.Count);
            sb.AppendLine("风险提示：" + warnings.Count);
            sb.AppendLine("普通信息：" + infos.Count);
            sb.AppendLine();

            AppendSection(sb, "严重问题，需要先修", errors);
            AppendSection(sb, "风险提示，建议检查", warnings);
            AppendSection(sb, "普通信息，可按需忽略", infos);

            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, List<string> items)
        {
            if (items == null || items.Count == 0) return;
            sb.AppendLine("【" + title + "】");
            for (var i = 0; i < items.Count; i++)
            {
                sb.AppendLine((i + 1) + ". " + items[i]);
                sb.AppendLine();
            }
        }

        private static string GetObjectPath(GameObject root, GameObject obj)
        {
            if (obj == null) return "<空>";
            if (root == null) return obj.name;
            if (obj == root) return "$$$AVATAR_ROOT$$$";
            if (!obj.transform.IsChildOf(root.transform)) return obj.name;

            var stack = new Stack<string>();
            var current = obj.transform;
            while (current != null && current != root.transform)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack.ToArray());
        }

        private static string SanitizeParameterName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "LW_Param";
            var chars = value.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
            return new string(chars);
        }
    }

    public class LastOpOutfitUploadProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -1024;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            foreach (var builder in avatarGameObject.GetComponentsInChildren<LastOpOutfitComponent>(true))
            {
                try
                {
                    if (builder.buildOnUpload)
                        LastOpOutfitGenerator.Build(builder, false);

                    if (builder.stripBuilderComponentOnUpload)
                        Object.DestroyImmediate(builder);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return false;
                }
            }
            return true;
        }
    }

    public static class LastOpOutfitGenerator
    {
        private const int NeutralValue = 255;

        public static void Build(LastOpOutfitComponent config, bool showDialog)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Building, please wait...", 0.02f);

                if (config != null && config.autoGenerateCloseOptionsFromFirstLevelChildren)
                {
                    EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Auto-filling Mix sub-level part toggles...", 0.08f);
                    AutoGenerateCloseOptionsFromFirstLevelChildren(config, false);
                }

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Checking configuration...", 0.14f);
                EnsureObjectSlots(config);
                ValidateConfig(config);
                EnsureFolder(GetActualOutputFolder(config));

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Detecting  Modular Avatar...", 0.20f);
                var maMenuItemType = FindType("nadena.dev.modular_avatar.core.ModularAvatarMenuItem", "ModularAvatarMenuItem");
                var maMenuInstallerType = FindType("nadena.dev.modular_avatar.core.ModularAvatarMenuInstaller", "ModularAvatarMenuInstaller");
                var maParametersType = FindType("nadena.dev.modular_avatar.core.ModularAvatarParameters", "ModularAvatarParameters");
                var maObjectToggleType = FindType("nadena.dev.modular_avatar.core.ModularAvatarObjectToggle", "ModularAvatarObjectToggle");
                var maMergeAnimatorType = FindType("nadena.dev.modular_avatar.core.ModularAvatarMergeAnimator", "ModularAvatarMergeAnimator");

                if (maMenuItemType == null || maMenuInstallerType == null || maParametersType == null || maObjectToggleType == null || maMergeAnimatorType == null)
                    throw new Exception("没有检测到完整 Modular Avatar。请确认 VCC 已安装 Modular Avatar 后再生成。");

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Creating manager and parameters...", 0.28f);
                var manager = PrepareManagerObject(config);

                ConfigureMenuItem(GetOrAddComponent(manager, maMenuItemType), VRCExpressionsMenu.Control.ControlType.SubMenu, "", 0, config.rootMenuName, true, false, false);
                GetOrAddComponent(manager, maMenuInstallerType);
                ConfigureMAParameters(GetOrAddComponent(manager, maParametersType), config);

                var superGroups = GetSuperGroups(config);
                var mixGroups = GetMixGroups(config);
                var totalMenuGroups = Mathf.Max(1, superGroups.Count + mixGroups.Count);
                var doneMenuGroups = 0;

                foreach (var super in superGroups)
                {
                    var progress = 0.32f + 0.30f * (doneMenuGroups / (float)totalMenuGroups);
                    EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Generating Super Switch menu: " + super.displayName, progress);

                    var menuName = string.IsNullOrWhiteSpace(super.menuName) ? super.displayName : super.menuName;
                    var menu = CreateChild(manager.transform, menuName);
                    ConfigureMenuItem(GetOrAddComponent(menu, maMenuItemType), VRCExpressionsMenu.Control.ControlType.SubMenu, "", 0, menuName, true, false, false);
                    BuildSuperSwitchSlots(config, super, menu.transform, maMenuItemType, maObjectToggleType);

                    doneMenuGroups++;
                }

                foreach (var group in mixGroups)
                {
                    var progress = 0.32f + 0.30f * (doneMenuGroups / (float)totalMenuGroups);
                    EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Generating Mix menu: " + group.displayName, progress);

                    var menuName = string.IsNullOrWhiteSpace(group.menuName) ? group.displayName : group.menuName;
                    var menu = CreateChild(manager.transform, menuName);
                    ConfigureMenuItem(GetOrAddComponent(menu, maMenuItemType), VRCExpressionsMenu.Control.ControlType.SubMenu, "", 0, menuName, true, false, false);
                    BuildPartSlots(config, menu.transform, maMenuItemType, maObjectToggleType, group.parameterName, group.items, group.generateNone);

                    doneMenuGroups++;
                }

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Generating  FX 动画控制器...", 0.70f);
                var fx = CreateGenericMixFx(config);

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Integrating  MA Merge Animator...", 0.86f);
                ConfigureMergeAnimator(GetOrAddComponent(manager, maMergeAnimatorType), fx);

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Saving assets...", 0.94f);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayProgressBar("Paulxstx Outfit", "Build Complete", 1.0f);

                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Paulxstx Outfit",
                        "Build complete.\n\n" +
                        "Currently: Universal Super Switch + Mix + 通用混搭版。\n" +
                        "你可以新增多个类似“衣服”的套装项目切换，也可以新增任意混搭项目。\n\n" +
                        "No longer auto-generates Super Switch part toggles by default; manual ones preserved.\n" +
                        "若开启“让已有 MA 开关优先于本插件”，管理器会放在角色根节点最前面。\n\n" +
                        "Output: \n" +
                        config.avatarRoot.name + "/" + config.managerObjectName,
                        "确定");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static string GetActualOutputFolder(LastOpOutfitComponent config)
        {
            if (config == null)
            {
                return "Assets/Paulxstx Outfit生成";
            }

            var baseFolder = string.IsNullOrWhiteSpace(config.outputFolder)
                ? "Assets/Paulxstx Outfit生成"
                : config.outputFolder.Replace("\\", "/").TrimEnd('/');

            if (!config.useAvatarNameSubFolder || config.avatarRoot == null)
            {
                return baseFolder;
            }

            var avatarName = SafeName(config.avatarRoot.name);
            if (string.IsNullOrWhiteSpace(avatarName))
            {
                avatarName = "未命名角色";
            }

            return baseFolder + "/" + avatarName;
        }

        private static void ValidateConfig(LastOpOutfitComponent config)
        {
            if (config == null) throw new Exception("Config is empty.");
            if (config.avatarRoot == null) config.avatarRoot = config.gameObject;
            if (config.avatarRoot == null) throw new Exception("Avatar root is empty.");

            var parameterNames = new List<string>();

            foreach (var super in GetSuperGroups(config))
            {
                if (string.IsNullOrWhiteSpace(super.displayName)) throw new Exception("Super Switch group name cannot be empty.");
                if (string.IsNullOrWhiteSpace(super.parameterName)) throw new Exception("套装项目切换「" + super.displayName + "」Parameter name cannot be empty.");
                if (string.IsNullOrWhiteSpace(GetLastAppliedParameterName(super))) throw new Exception("套装项目切换「" + super.displayName + "」Internal applied param name cannot be empty.");

                parameterNames.Add(super.parameterName);
                parameterNames.Add(GetLastAppliedParameterName(super));
                ValidatePartValues("套装项目切换「" + super.displayName + "」", super.items.Select(i => i.value).ToList());

                foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                {
                    if (item == null) continue;
                    ValidateObjectsUnderAvatar(config, super.displayName, item.displayName, item.objects);

                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close == null) continue;
                        ValidateObjectsUnderAvatar(config, super.displayName + "关闭部件", item.displayName + "/" + close.displayName, close.objects);
                    }
                }
            }

            foreach (var group in GetMixGroups(config))
            {
                if (string.IsNullOrWhiteSpace(group.displayName)) throw new Exception("Mix Group name cannot be empty.");
                if (string.IsNullOrWhiteSpace(group.parameterName)) throw new Exception("混搭项目「" + group.displayName + "」Parameter name cannot be empty.");
                parameterNames.Add(group.parameterName);
                ValidatePartValues("混搭项目「" + group.displayName + "」", group.items.Select(i => i.value).ToList());

                foreach (var item in group.items ?? new List<PartOption>())
                {
                    if (item == null) continue;
                    ValidateObjectsUnderAvatar(config, group.displayName, item.displayName, item.objects);
                }
            }

            var dupParam = parameterNames.GroupBy(p => p).FirstOrDefault(g => g.Count() > 1);
            if (dupParam != null) throw new Exception("参数名重复：" + dupParam.Key);
        }

        private static void ValidatePartValues(string label, List<int> values)
        {
            if (values.Any(v => v <= 0 || v >= 255))
                throw new Exception(label + "Parameter value can only use  1-254。0 是“不穿/关闭”，255 是“保持当前状态”。");

            var dup = values.GroupBy(v => v).FirstOrDefault(g => g.Count() > 1);
            if (dup != null) throw new Exception(label + "参数值重复：" + dup.Key);
        }

        private static void ValidateObjectsUnderAvatar(LastOpOutfitComponent config, string group, string item, List<GameObject> objects)
        {
            if (objects == null) return;
            foreach (var obj in objects)
            {
                if (obj == null) continue;
                if (!obj.transform.IsChildOf(config.avatarRoot.transform))
                    throw new Exception(group + "「" + item + "」里的物体不在角色根节点下：" + obj.name);
            }
        }

        public static void EnsureObjectSlots(LastOpOutfitComponent config)
        {
            if (config == null || !config.keepOneObjectSlotForEveryObjectList) return;

            foreach (var super in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
            {
                if (super == null || super.items == null) continue;
                foreach (var item in super.items)
                {
                    if (item == null) continue;
                    if (item.objects == null) item.objects = new List<GameObject>();
                    if (item.objects.Count == 0) item.objects.Add(null);

                    foreach (var close in item.closeOptions ?? new List<ClothesCloseOption>())
                    {
                        if (close == null) continue;

                        close.saved = true;
                        close.synced = true;

                        if (close.objects == null) close.objects = new List<GameObject>();
                        if (close.objects.Count == 0) close.objects.Add(null);
                    }
                }
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null) continue;
                if (group.items == null) group.items = new List<PartOption>();

                foreach (var item in group.items)
                {
                    if (item == null) continue;
                    if (item.objects == null) item.objects = new List<GameObject>();
                    if (item.objects.Count == 0) item.objects.Add(null);

                    if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();

                    foreach (var close in item.closeOptions)
                    {
                        if (close == null) continue;

                        // 混搭选项内部关闭菜单参数默认保存并同步。
                        close.saved = true;
                        close.synced = true;

                        if (close.objects == null) close.objects = new List<GameObject>();
                        if (close.objects.Count == 0) close.objects.Add(null);
                    }
                }
            }

            EditorUtility.SetDirty(config);
        }

        public static int AutoGenerateCloseOptionsFromFirstLevelChildren(LastOpOutfitComponent config, bool forceSupplement, bool includeSuperSwitchGroups = false)
        {
            if (config == null) return 0;

            Undo.RecordObject(config, "Auto-fill Mix sub-level close toggles");
            var added = 0;

            // 默认不再自动补充“套装项目切换”的部件开关，避免重新生成或更新后塞入大量部件开关。
            // 如确实需要，只能先在 Inspector 勾选“允许手动补充套装项目切换的部件开关”，再点击手动补充按钮。
            if (includeSuperSwitchGroups && forceSupplement && config.allowManualGenerateSuperSwitchCloseOptions)
            {
                foreach (var super in config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
                {
                    if (super == null || super.items == null) continue;

                    foreach (var item in super.items)
                    {
                        if (item == null) continue;
                        if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();

                        var existingTargets = new HashSet<GameObject>(
                            item.closeOptions
                                .Where(option => option != null && option.objects != null)
                                .SelectMany(option => option.objects)
                                .Where(obj => obj != null));

                        var firstLevelChildren = (item.objects ?? new List<GameObject>())
                            .Where(root => root != null)
                            .SelectMany(root =>
                            {
                                var list = new List<GameObject>();
                                foreach (Transform child in root.transform)
                                {
                                    if (child != null) list.Add(child.gameObject);
                                }
                                return list;
                            })
                            .Where(obj => obj != null)
                            .Where(obj => IsAutoGeneratedCloseOptionCandidate(config, obj))
                            .Distinct()
                            .ToList();

                        foreach (var child in firstLevelChildren)
                        {
                            if (existingTargets.Contains(child)) continue;

                            item.closeOptions.Add(new ClothesCloseOption
                            {
                                displayName = child.name,
                                objects = new List<GameObject> { child },
                                saved = true,
                                synced = true
                            });

                            existingTargets.Add(child);
                            added++;
                        }
                    }
                }
            }

            foreach (var group in config.mixGroups ?? new List<MixGroupOption>())
            {
                if (group == null || group.items == null) continue;

                foreach (var item in group.items)
                {
                    if (item == null) continue;
                    if (item.closeOptions == null) item.closeOptions = new List<ClothesCloseOption>();

                    if (!forceSupplement && config.onlyAutoGenerateCloseOptionsWhenEmpty && item.closeOptions.Count > 0)
                        continue;

                    var existingTargets = new HashSet<GameObject>(
                        item.closeOptions
                            .Where(option => option != null && option.objects != null)
                            .SelectMany(option => option.objects)
                            .Where(obj => obj != null));

                    var firstLevelChildren = (item.objects ?? new List<GameObject>())
                        .Where(root => root != null)
                        .SelectMany(root =>
                        {
                            var list = new List<GameObject>();
                            foreach (Transform child in root.transform)
                            {
                                if (child != null) list.Add(child.gameObject);
                            }
                            return list;
                        })
                        .Where(obj => obj != null)
                        .Where(obj => IsAutoGeneratedCloseOptionCandidate(config, obj))
                        .Distinct()
                        .ToList();

                    foreach (var child in firstLevelChildren)
                    {
                        if (existingTargets.Contains(child)) continue;

                        item.closeOptions.Add(new ClothesCloseOption
                        {
                            displayName = child.name,
                            objects = new List<GameObject> { child },
                            saved = true,
                            synced = true
                        });

                        existingTargets.Add(child);
                        added++;
                    }
                }
            }

            EnsureObjectSlots(config);
            EditorUtility.SetDirty(config);
            return added;
        }

        private static bool IsAutoGeneratedCloseOptionCandidate(LastOpOutfitComponent config, GameObject obj)
        {
            if (obj == null) return false;
            if (config == null || !config.skipBonesWhenAutoGenerateCloseOptions) return true;
            if (IsLikelyBoneObject(obj)) return false;
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            return renderers != null && renderers.Length > 0;
        }

        private static bool IsLikelyBoneObject(GameObject obj)
        {
            if (obj == null) return false;

            var name = obj.name == null ? "" : obj.name.ToLowerInvariant();
            var compact = name.Replace("_", "").Replace("-", "").Replace(" ", "");

            var exactNames = new HashSet<string>
            {
                "armature", "root", "hips", "spine", "chest", "upperchest",
                "neck", "head", "jaw", "eye", "eyes",
                "leftshoulder", "rightshoulder", "leftupperarm", "rightupperarm",
                "leftlowerarm", "rightlowerarm", "lefthand", "righthand",
                "leftupperleg", "rightupperleg", "leftlowerleg", "rightlowerleg",
                "leftfoot", "rightfoot", "lefttoes", "righttoes"
            };

            if (exactNames.Contains(compact)) return true;

            if (name.Contains("armature") ||
                name.Contains("bone") ||
                name.Contains("bip") ||
                name.Contains("mixamorig") ||
                name.Contains("j_bip") ||
                name.Contains("skel") ||
                name.Contains("skeleton"))
            {
                return true;
            }

            var components = obj.GetComponents<Component>();
            var hasOnlyTransform = components.All(c => c == null || c is Transform);
            if (hasOnlyTransform)
            {
                var childRenderers = obj.GetComponentsInChildren<Renderer>(true);
                return childRenderers == null || childRenderers.Length == 0;
            }

            return false;
        }

        private static GameObject PrepareManagerObject(LastOpOutfitComponent config)
        {
            if (config.replaceOldManagerObject)
            {
                var old = config.avatarRoot.transform.Find(config.managerObjectName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var existing = config.avatarRoot.transform.Find(config.managerObjectName);
            if (existing != null)
            {
                if (config.putGeneratedManagerFirst) existing.SetAsFirstSibling();
                return existing.gameObject;
            }

            var go = new GameObject(config.managerObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create Paulxstx Outfit Manager");
            go.transform.SetParent(config.avatarRoot.transform, false);
            if (config.putGeneratedManagerFirst) go.transform.SetAsFirstSibling();
            return go;
        }

        private static void BuildSuperSwitchSlots(LastOpOutfitComponent config, SuperSwitchGroupOption super, Transform parent, Type menuItemType, Type objectToggleType)
        {
            var allObjects = (super.items ?? new List<SuperSwitchItemOption>())
                .SelectMany(i => i.objects ?? new List<GameObject>())
                .Where(o => o != null)
                .Distinct()
                .ToList();

            if (super.generateNone && !super.sameItemClickResetsDefaultMix)
            {
                var none = CreateChild(parent, "不穿_关闭");
                ConfigureMenuItem(GetOrAddComponent(none, menuItemType), VRCExpressionsMenu.Control.ControlType.Toggle, super.parameterName, 0, "不穿/关闭", false, true, true);
                ConfigureObjectToggle(GetOrAddComponent(none, objectToggleType), allObjects.Select(o => new TargetState { target = o, active = false }).ToList(), config.avatarRoot);
            }

            foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
            {
                if (item == null) continue;

                var wearSlot = CreateChild(parent, SafeName(item.displayName));
                ConfigureMenuItem(
                    GetOrAddComponent(wearSlot, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    super.parameterName,
                    item.value,
                    item.displayName,
                    false,
                    true,
                    true);

                var selected = new HashSet<GameObject>((item.objects ?? new List<GameObject>()).Where(o => o != null));
                ConfigureObjectToggle(
                    GetOrAddComponent(wearSlot, objectToggleType),
                    allObjects.Select(o => new TargetState { target = o, active = selected.Contains(o) }).ToList(),
                    config.avatarRoot);

                var hasCloseOptions = item.closeOptions != null && item.closeOptions.Any(close => close != null);
                var needsPerItemCloseButtons = !config.closeAllButtonsOnlyOnceInSuperMenu &&
                    (config.generateCloseAllPartsButton || GetCustomCloseButtons(config).Any(button => button != null && button.enabled));

                if (!hasCloseOptions && !needsPerItemCloseButtons)
                {
                    continue;
                }

                var partsMenuName = item.displayName + " " + super.partsMenuSuffix;
                var closeMenu = CreateChild(parent, SafeName(partsMenuName));
                ConfigureMenuItem(
                    GetOrAddComponent(closeMenu, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.SubMenu,
                    "",
                    0,
                    partsMenuName,
                    true,
                    false,
                    false);

                if (needsPerItemCloseButtons)
                    BuildCloseButtons(config, super, item, closeMenu.transform, menuItemType, objectToggleType);

                if (!hasCloseOptions) continue;

                for (var i = 0; i < item.closeOptions.Count; i++)
                {
                    var close = item.closeOptions[i];
                    if (close == null) continue;

                    var closeSlot = CreateChild(closeMenu.transform, SafeName(close.displayName));
                    var parameterName = GetItemCloseParameterName(super, item, i);

                    ConfigureMenuItem(
                        GetOrAddComponent(closeSlot, menuItemType),
                        VRCExpressionsMenu.Control.ControlType.Toggle,
                        parameterName,
                        1,
                        close.displayName,
                        false,
                        close.saved,
                        close.synced);

                    var closeTargets = (close.objects ?? new List<GameObject>())
                        .Where(o => o != null)
                        .Distinct()
                        .Select(o => new TargetState { target = o, active = false })
                        .ToList();

                    ConfigureObjectToggle(GetOrAddComponent(closeSlot, objectToggleType), closeTargets, config.avatarRoot);
                }
            }

            if (config.closeAllButtonsOnlyOnceInSuperMenu)
                BuildCloseButtons(config, super, null, parent, menuItemType, objectToggleType);
        }

        private static void BuildCloseButtons(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item, Transform parent, Type menuItemType, Type objectToggleType)
        {
            var closeTargets = item == null
                ? GetAllCloseOptionObjectsForSuper(config, super).Select(o => new TargetState { target = o, active = false }).ToList()
                : GetAllCloseOptionObjects(item).Select(o => new TargetState { target = o, active = false }).ToList();

            if (config.generateCloseAllPartsButton)
            {
                var closeAllSlot = CreateChild(parent, SafeName(config.closeAllPartsButtonName));
                ConfigureMenuItem(
                    GetOrAddComponent(closeAllSlot, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    GetEffectiveBaseCloseAllParameterName(config, super, item),
                    1,
                    config.closeAllPartsButtonName,
                    false,
                    true,
                    true);

                ConfigureObjectToggle(GetOrAddComponent(closeAllSlot, objectToggleType), closeTargets, config.avatarRoot);
            }

            var buttons = GetCustomCloseButtons(config);
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.enabled) continue;

                var slot = CreateChild(parent, SafeName(button.displayName));
                ConfigureMenuItem(
                    GetOrAddComponent(slot, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    GetEffectiveCustomCloseButtonParameterName(config, super, item, button, i),
                    1,
                    button.displayName,
                    false,
                    true,
                    true);

                ConfigureObjectToggle(GetOrAddComponent(slot, objectToggleType), closeTargets, config.avatarRoot);
            }
        }

        private static void BuildPartSlots(
            LastOpOutfitComponent config,
            Transform parent,
            Type menuItemType,
            Type objectToggleType,
            string parameterName,
            List<PartOption> items,
            bool generateNone)
        {
            var allObjects = (items ?? new List<PartOption>())
                .SelectMany(i => i.objects ?? new List<GameObject>())
                .Where(o => o != null)
                .Distinct()
                .ToList();

            if (generateNone)
            {
                var none = CreateChild(parent, "不穿_关闭");
                ConfigureMenuItem(GetOrAddComponent(none, menuItemType), VRCExpressionsMenu.Control.ControlType.Toggle, parameterName, 0, "不穿/关闭", false, true, true);
                ConfigureObjectToggle(GetOrAddComponent(none, objectToggleType), allObjects.Select(o => new TargetState { target = o, active = false }).ToList(), config.avatarRoot);
            }

            foreach (var item in items ?? new List<PartOption>())
            {
                if (item == null) continue;

                var slot = CreateChild(parent, SafeName(item.displayName));
                ConfigureMenuItem(GetOrAddComponent(slot, menuItemType), VRCExpressionsMenu.Control.ControlType.Toggle, parameterName, item.value, item.displayName, false, true, true);

                var selected = new HashSet<GameObject>((item.objects ?? new List<GameObject>()).Where(o => o != null));
                ConfigureObjectToggle(
                    GetOrAddComponent(slot, objectToggleType),
                    allObjects.Select(o => new TargetState { target = o, active = selected.Contains(o) }).ToList(),
                    config.avatarRoot);

                BuildMixPartCloseMenu(config, parent, menuItemType, objectToggleType, parameterName, item);
            }
        }

        private static void BuildMixPartCloseMenu(
            LastOpOutfitComponent config,
            Transform parent,
            Type menuItemType,
            Type objectToggleType,
            string mixParameterName,
            PartOption item)
        {
            if (item == null || item.closeOptions == null || item.closeOptions.Count == 0)
            {
                return;
            }

            var partsMenuName = item.displayName + " 部件";
            var closeMenu = CreateChild(parent, SafeName(partsMenuName));
            ConfigureMenuItem(
                GetOrAddComponent(closeMenu, menuItemType),
                VRCExpressionsMenu.Control.ControlType.SubMenu,
                "",
                0,
                partsMenuName,
                true,
                false,
                false);

            if (config.generateCloseAllPartsButton)
            {
                var closeAllSlot = CreateChild(closeMenu.transform, SafeName(config.closeAllPartsButtonName));
                ConfigureMenuItem(
                    GetOrAddComponent(closeAllSlot, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    GetMixItemCloseAllParameterName(mixParameterName, item),
                    1,
                    config.closeAllPartsButtonName,
                    false,
                    true,
                    true);

                ConfigureObjectToggle(
                    GetOrAddComponent(closeAllSlot, objectToggleType),
                    GetAllMixCloseOptionObjects(item).Select(o => new TargetState { target = o, active = false }).ToList(),
                    config.avatarRoot);
            }

            for (var i = 0; i < item.closeOptions.Count; i++)
            {
                var close = item.closeOptions[i];
                if (close == null) continue;

                var closeSlot = CreateChild(closeMenu.transform, SafeName(close.displayName));
                var parameterName = GetMixItemCloseParameterName(mixParameterName, item, i);

                ConfigureMenuItem(
                    GetOrAddComponent(closeSlot, menuItemType),
                    VRCExpressionsMenu.Control.ControlType.Toggle,
                    parameterName,
                    1,
                    close.displayName,
                    false,
                    true,
                    true);

                var closeTargets = (close.objects ?? new List<GameObject>())
                    .Where(o => o != null)
                    .Distinct()
                    .Select(o => new TargetState { target = o, active = false })
                    .ToList();

                ConfigureObjectToggle(GetOrAddComponent(closeSlot, objectToggleType), closeTargets, config.avatarRoot);
            }
        }

        private static RuntimeAnimatorController CreateGenericMixFx(LastOpOutfitComponent config)
        {
            var path = GetActualOutputFolder(config).TrimEnd('/') + "/" + SafeName(config.fxAnimatorAssetName) + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            foreach (var super in GetSuperGroups(config))
            {
                AddIntParameter(controller, super.parameterName);
                AddIntParameter(controller, GetLastAppliedParameterName(super));
            }

            foreach (var group in GetMixGroups(config))
            {
                AddIntParameter(controller, group.parameterName);
                AddIntParameter(controller, GetLastMixParameterName(group));
            }

            foreach (var closeAllParam in GetAllBaseCloseAllParameters(config))
                AddBoolParameter(controller, closeAllParam.name);

            foreach (var closeButtonParam in GetAllCustomCloseButtonParameters(config))
                AddBoolParameter(controller, closeButtonParam.name);

            foreach (var appliedParam in GetAllCustomCloseButtonAppliedParameters(config))
                AddBoolParameter(controller, appliedParam.name);

            foreach (var closeParam in GetAllItemCloseParameters(config))
                AddBoolParameter(controller, closeParam.name);

            foreach (var closeParam in GetAllMixItemCloseParameters(config))
                AddBoolParameter(controller, closeParam.name);

            var layer = controller.layers.Length > 0 ? controller.layers[0] : new AnimatorControllerLayer
            {
                name = "最终操作_通用混搭",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };

            layer.name = "最终操作_通用混搭";
            if (layer.stateMachine == null) layer.stateMachine = new AnimatorStateMachine();
            ClearStateMachine(layer.stateMachine);
            var sm = layer.stateMachine;

            var idle = sm.AddState("待机");
            idle.writeDefaultValues = false;
            sm.defaultState = idle;

            AddLastMixRecorderStates(config, sm, idle);

            foreach (var super in GetSuperGroups(config))
            {
                if (super.generateNone && !super.sameItemClickResetsDefaultMix)
                {
                    var st = sm.AddState(super.displayName + "_关闭_应用默认混搭");
                    st.writeDefaultValues = false;

                    var ops = new List<(string name, int value)> { (GetLastAppliedParameterName(super), 0) };
                    foreach (var group in GetMixGroups(config).Where(g => g.superNoneAlsoTurnsOff))
                        ops.Add((group.parameterName, 0));

                    AddAvatarParameterDriver(st, ops);

                    var tr = sm.AddAnyStateTransition(st);
                    tr.hasExitTime = false; tr.duration = 0f; tr.canTransitionToSelf = false;
                    tr.AddCondition(AnimatorConditionMode.Equals, 0, super.parameterName);

                    var back = st.AddTransition(idle);
                    back.hasExitTime = false; back.duration = 0f;
                }

                foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                {
                    var st = sm.AddState(super.displayName + "_" + item.value + "_默认混搭");
                    st.writeDefaultValues = false;

                    var ops = GetDefaultMixOps(config, item);
                    ops.Add((GetLastAppliedParameterName(super), item.value));
                    AddResetClosePartParameters(config, super, item, ops);
                    AddAvatarParameterDriver(st, ops);

                    var tr = sm.AddAnyStateTransition(st);
                    tr.hasExitTime = false; tr.duration = 0f; tr.canTransitionToSelf = false;
                    tr.AddCondition(AnimatorConditionMode.Equals, item.value, super.parameterName);
                    tr.AddCondition(AnimatorConditionMode.NotEqual, item.value, GetLastAppliedParameterName(super));

                    var back = st.AddTransition(idle);
                    back.hasExitTime = false; back.duration = 0f;

                    if (super.sameItemClickResetsDefaultMix)
                    {
                        var restore = sm.AddState("再次点击_" + super.displayName + "_" + item.value + "_恢复默认混搭");
                        restore.writeDefaultValues = false;

                        var restoreOps = GetDefaultMixOps(config, item);
                        restoreOps.Add((super.parameterName, item.value));
                        restoreOps.Add((GetLastAppliedParameterName(super), item.value));
                        AddResetClosePartParameters(config, super, item, restoreOps);
                        AddAvatarParameterDriver(restore, restoreOps);

                        var restoreTr = sm.AddAnyStateTransition(restore);
                        restoreTr.hasExitTime = false; restoreTr.duration = 0f; restoreTr.canTransitionToSelf = false;
                        restoreTr.AddCondition(AnimatorConditionMode.Equals, 0, super.parameterName);
                        restoreTr.AddCondition(AnimatorConditionMode.Equals, item.value, GetLastAppliedParameterName(super));

                        var restoreBack = restore.AddTransition(idle);
                        restoreBack.hasExitTime = false; restoreBack.duration = 0f;
                    }

                    AddCustomCloseButtonStates(config, sm, idle, super, item);
                }
            }

            controller.layers = new[] { layer };
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AddCustomCloseButtonStates(LastOpOutfitComponent config, AnimatorStateMachine sm, AnimatorState idle, SuperSwitchGroupOption super, SuperSwitchItemOption item)
        {
            var buttons = GetCustomCloseButtons(config);

            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.enabled) continue;

                var closeParam = GetEffectiveCustomCloseButtonParameterName(config, super, item, button, i);
                var appliedParam = GetEffectiveCustomCloseButtonAppliedParameterName(config, super, item, button, i);
                var includedGroups = GetIncludedMixGroups(config, button).ToList();

                if (includedGroups.Count == 0) continue;

                var fullClose = sm.AddState(super.displayName + "_一键全关_" + i + "_" + item.value);
                fullClose.writeDefaultValues = false;

                var closeOps = new List<(string name, int value)>();

                foreach (var group in includedGroups)
                {
                    closeOps.Add((group.parameterName, 0));
                }

                closeOps.Add((appliedParam, 1));

                // 最后一次操作优先：打开一个自定义全关按钮时，关闭同菜单里的其他自定义全关按钮状态。
                for (var otherIndex = 0; otherIndex < buttons.Count; otherIndex++)
                {
                    if (otherIndex == i) continue;
                    var other = buttons[otherIndex];
                    if (other == null || !other.enabled) continue;
                    closeOps.Add((GetEffectiveCustomCloseButtonParameterName(config, super, item, other, otherIndex), 0));
                    closeOps.Add((GetEffectiveCustomCloseButtonAppliedParameterName(config, super, item, other, otherIndex), 0));
                }

                AddAvatarParameterDriver(fullClose, closeOps);

                // 只要按钮打开且尚未应用，就执行关闭。
                // 不再要求当前混搭值 != 0，因为用户可能在点击前已经临时关闭了鞋/袜；
                // 取消时仍希望恢复“上次穿过”的鞋/袜，而不是继续保持 0。
                var tr = sm.AddAnyStateTransition(fullClose);
                tr.hasExitTime = false; tr.duration = 0f; tr.canTransitionToSelf = false;
                tr.AddCondition(AnimatorConditionMode.If, 0, closeParam);
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, appliedParam);

                var back = fullClose.AddTransition(idle);
                back.hasExitTime = false; back.duration = 0f;

                AddRestoreLastWornStatesForCustomClose(config, sm, idle, super, item, button, i, includedGroups, closeParam, appliedParam);

                if (config.generateCloseAllPartsButton)
                {
                    var switchToNormal = sm.AddState("切换普通全关_清理自定义全关_" + super.displayName + "_" + i + "_" + item.value);
                    switchToNormal.writeDefaultValues = false;

                    var switchOps = new List<(string name, int value)>
                    {
                        (closeParam, 0),
                        (appliedParam, 0)
                    };

                    AddAvatarParameterDriver(switchToNormal, switchOps);

                    var switchTr = sm.AddAnyStateTransition(switchToNormal);
                    switchTr.hasExitTime = false; switchTr.duration = 0f; switchTr.canTransitionToSelf = false;
                    switchTr.AddCondition(AnimatorConditionMode.If, 0, GetEffectiveBaseCloseAllParameterName(config, super, item));
                    switchTr.AddCondition(AnimatorConditionMode.If, 0, closeParam);
                    switchTr.AddCondition(AnimatorConditionMode.Equals, item.value, GetLastAppliedParameterName(super));

                    var switchBack = switchToNormal.AddTransition(idle);
                    switchBack.hasExitTime = false; switchBack.duration = 0f;
                }
            }
        }

        private static void AddRestoreLastWornStatesForCustomClose(
            LastOpOutfitComponent config,
            AnimatorStateMachine sm,
            AnimatorState idle,
            SuperSwitchGroupOption super,
            SuperSwitchItemOption item,
            CustomCloseButtonOption button,
            int buttonIndex,
            List<MixGroupOption> includedGroups,
            string closeParam,
            string appliedParam)
        {
            var restore = sm.AddState("关闭一键全关_" + super.displayName + "_" + buttonIndex + "_恢复上次穿着_" + item.value);
            restore.writeDefaultValues = false;

            var restoreOps = new List<DriverOp>();

            foreach (var group in includedGroups)
            {
                // 直接把“上次穿过的混搭值”复制回当前混搭参数。
                // 这样不再生成大量组合状态，生成速度会快很多。
                restoreOps.Add(DriverOp.Copy(GetLastMixParameterName(group), group.parameterName));
            }

            restoreOps.Add(DriverOp.Set(appliedParam, 0));
            AddAvatarParameterDriverOps(restore, restoreOps);

            var restoreTr = sm.AddAnyStateTransition(restore);
            restoreTr.hasExitTime = false;
            restoreTr.duration = 0f;
            restoreTr.canTransitionToSelf = false;
            restoreTr.AddCondition(AnimatorConditionMode.IfNot, 0, closeParam);
            restoreTr.AddCondition(AnimatorConditionMode.If, 0, appliedParam);
            restoreTr.AddCondition(AnimatorConditionMode.Equals, item.value, GetLastAppliedParameterName(super));

            var restoreBack = restore.AddTransition(idle);
            restoreBack.hasExitTime = false;
            restoreBack.duration = 0f;
        }

        private static void AddLastMixRecorderStates(LastOpOutfitComponent config, AnimatorStateMachine sm, AnimatorState idle)
        {
            foreach (var group in GetMixGroups(config))
            {
                foreach (var item in group.items ?? new List<PartOption>())
                {
                    if (item == null || item.value <= 0 || item.value >= 255) continue;

                    var state = sm.AddState("记录上次混搭_" + group.displayName + "_" + item.value);
                    state.writeDefaultValues = false;
                    AddAvatarParameterDriver(state, new List<(string name, int value)>
                    {
                        (GetLastMixParameterName(group), item.value)
                    });

                    var tr = sm.AddAnyStateTransition(state);
                    tr.hasExitTime = false;
                    tr.duration = 0f;
                    tr.canTransitionToSelf = false;
                    tr.AddCondition(AnimatorConditionMode.Equals, item.value, group.parameterName);
                    tr.AddCondition(AnimatorConditionMode.NotEqual, item.value, GetLastMixParameterName(group));

                    var back = state.AddTransition(idle);
                    back.hasExitTime = false;
                    back.duration = 0f;
                }
            }
        }

        private static List<(string name, int value)> GetDefaultMixOps(LastOpOutfitComponent config, SuperSwitchItemOption item, IEnumerable<MixGroupOption> onlyGroups = null)
        {
            var result = new List<(string name, int value)>();
            var groups = onlyGroups != null ? onlyGroups.ToList() : GetMixGroups(config);

            foreach (var group in groups)
            {
                var value = GetDefaultMixValue(item, group);
                if (value >= 0)
                    result.Add((group.parameterName, value));
            }

            return result;
        }

        private static int GetDefaultMixValue(SuperSwitchItemOption item, MixGroupOption group)
        {
            if (item == null || group == null) return -1;

            if (item.defaultMixValues == null || item.defaultMixValues.Count == 0)
            {
                return Mathf.Clamp(item.value, 1, 254);
            }

            var match = item.defaultMixValues.FirstOrDefault(v =>
                v != null &&
                (v.mixGroupName == group.displayName || v.parameterName == group.parameterName));

            // 兜底：如果默认混搭列表没有对上这个混搭项目，就按当前套装项目切换项的序号写入。
            // 这样衣服A会默认带混搭A，衣服B会默认带混搭B，避免因为改名/旧配置导致“衣服切换不带混搭切换”。
            if (match == null)
            {
                return Mathf.Clamp(item.value, 1, 254);
            }

            if (!match.enabled) return -1;
            return match.value;
        }

        private static void AddIntParameter(AnimatorController controller, string parameterName)
        {
            if (!controller.parameters.Any(p => p.name == parameterName))
                controller.AddParameter(parameterName, AnimatorControllerParameterType.Int);
        }

        private static void AddBoolParameter(AnimatorController controller, string parameterName)
        {
            if (!controller.parameters.Any(p => p.name == parameterName))
                controller.AddParameter(parameterName, AnimatorControllerParameterType.Bool);
        }

        private static void ClearStateMachine(AnimatorStateMachine sm)
        {
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
        }

        private static void ConfigureMenuItem(Component component, VRCExpressionsMenu.Control.ControlType type, string parameter, float value, string label, bool submenuChildren, bool saved, bool synced)
        {
            // 所有带参数的外显菜单项都会导致模型外观或可见状态变化，默认保存并同步。
            if (!string.IsNullOrWhiteSpace(parameter))
            {
                saved = true;
                synced = true;
            }

            var compType = component.GetType();

            var control = new VRCExpressionsMenu.Control
            {
                name = label,
                type = type,
                value = value,
                parameter = new VRCExpressionsMenu.Control.Parameter { name = parameter ?? "" }
            };

            var controlField = compType.GetField("Control");
            if (controlField != null) controlField.SetValue(component, control);

            SetField(component, "label", label);
            SetField(component, "isSaved", saved);
            SetField(component, "isSynced", synced);
            SetField(component, "isDefault", false);
            SetField(component, "automaticValue", false);

            if (submenuChildren)
            {
                var menuSourceField = compType.GetField("MenuSource");
                if (menuSourceField != null && menuSourceField.FieldType.IsEnum)
                {
                    var children = Enum.Parse(menuSourceField.FieldType, "Children");
                    menuSourceField.SetValue(component, children);
                }
            }

            EditorUtility.SetDirty(component);
        }

        private static void SetField(Component component, string fieldName, object value)
        {
            var f = component.GetType().GetField(fieldName);
            if (f != null) f.SetValue(component, value);
        }

        private class CloseParameterInfo
        {
            public string name;
            public bool saved;
            public bool synced;
        }

        private static string GetLastMixParameterName(MixGroupOption group)
        {
            if (group == null) return "LW_LastMix";
            return SanitizeParameterName("LW_LastMix_" + group.parameterName);
        }

        private static string GetLastAppliedParameterName(SuperSwitchGroupOption super)
        {
            if (super == null) return "LW_LastApplied";
            if (!string.IsNullOrWhiteSpace(super.lastAppliedParameterName)) return super.lastAppliedParameterName;
            return "LW_LastApplied_" + SanitizeParameterName(super.parameterName);
        }

        private static string GetGlobalBaseCloseAllParameterName(SuperSwitchGroupOption super)
        {
            return SanitizeParameterName("LW_" + super.parameterName + "_Global_CloseAll");
        }

        private static string GetPerItemBaseCloseAllParameterName(SuperSwitchGroupOption super, SuperSwitchItemOption item)
        {
            var value = item != null ? item.value : 0;
            return SanitizeParameterName("LW_" + super.parameterName + "_I" + value + "_CloseAll");
        }

        private static string GetEffectiveBaseCloseAllParameterName(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item)
        {
            return config.closeAllButtonsOnlyOnceInSuperMenu || item == null
                ? GetGlobalBaseCloseAllParameterName(super)
                : GetPerItemBaseCloseAllParameterName(super, item);
        }

        private static string GetGlobalCustomCloseButtonParameterName(SuperSwitchGroupOption super, int index)
        {
            return SanitizeParameterName("LW_" + super.parameterName + "_Global_CustomClose_" + (index + 1));
        }

        private static string GetGlobalCustomCloseButtonAppliedParameterName(SuperSwitchGroupOption super, int index)
        {
            return SanitizeParameterName("LW_" + super.parameterName + "_Global_CustomClose_" + (index + 1) + "_Applied");
        }

        private static string GetPerItemCustomCloseButtonParameterName(SuperSwitchGroupOption super, SuperSwitchItemOption item, int index)
        {
            var value = item != null ? item.value : 0;
            return SanitizeParameterName("LW_" + super.parameterName + "_I" + value + "_CustomClose_" + (index + 1));
        }

        private static string GetPerItemCustomCloseButtonAppliedParameterName(SuperSwitchGroupOption super, SuperSwitchItemOption item, int index)
        {
            var value = item != null ? item.value : 0;
            return SanitizeParameterName("LW_" + super.parameterName + "_I" + value + "_CustomClose_" + (index + 1) + "_Applied");
        }

        private static string GetEffectiveCustomCloseButtonParameterName(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item, CustomCloseButtonOption button, int index)
        {
            return config.closeAllButtonsOnlyOnceInSuperMenu || item == null
                ? GetGlobalCustomCloseButtonParameterName(super, index)
                : GetPerItemCustomCloseButtonParameterName(super, item, index);
        }

        private static string GetEffectiveCustomCloseButtonAppliedParameterName(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item, CustomCloseButtonOption button, int index)
        {
            return config.closeAllButtonsOnlyOnceInSuperMenu || item == null
                ? GetGlobalCustomCloseButtonAppliedParameterName(super, index)
                : GetPerItemCustomCloseButtonAppliedParameterName(super, item, index);
        }

        private static string GetGlobalCustomCloseButtonBackupParameterName(SuperSwitchGroupOption super, int index, MixGroupOption mixGroup)
        {
            return SanitizeParameterName("LW_" + super.parameterName + "_Global_CustomClose_" + (index + 1) + "_Backup_" + mixGroup.parameterName);
        }

        private static string GetPerItemCustomCloseButtonBackupParameterName(SuperSwitchGroupOption super, SuperSwitchItemOption item, int index, MixGroupOption mixGroup)
        {
            var value = item != null ? item.value : 0;
            return SanitizeParameterName("LW_" + super.parameterName + "_I" + value + "_CustomClose_" + (index + 1) + "_Backup_" + mixGroup.parameterName);
        }

        private static string GetEffectiveCustomCloseButtonBackupParameterName(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item, CustomCloseButtonOption button, int index, MixGroupOption mixGroup)
        {
            return config.closeAllButtonsOnlyOnceInSuperMenu || item == null
                ? GetGlobalCustomCloseButtonBackupParameterName(super, index, mixGroup)
                : GetPerItemCustomCloseButtonBackupParameterName(super, item, index, mixGroup);
        }

        private static List<CloseParameterInfo> GetAllBaseCloseAllParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();
            if (config == null || !config.generateCloseAllPartsButton) return result;

            foreach (var super in GetSuperGroups(config))
            {
                if (config.closeAllButtonsOnlyOnceInSuperMenu)
                {
                    result.Add(new CloseParameterInfo
                    {
                        name = GetGlobalBaseCloseAllParameterName(super),
                        saved = true,
                        synced = true
                    });
                }
                else
                {
                    foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                    {
                        if (item == null) continue;
                        result.Add(new CloseParameterInfo
                        {
                            name = GetPerItemBaseCloseAllParameterName(super, item),
                            saved = true,
                            synced = true
                        });
                    }
                }
            }

            return result;
        }

        private static List<CloseParameterInfo> GetAllCustomCloseButtonParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();
            var buttons = GetCustomCloseButtons(config);

            foreach (var super in GetSuperGroups(config))
            {
                for (var i = 0; i < buttons.Count; i++)
                {
                    var button = buttons[i];
                    if (button == null || !button.enabled) continue;

                    if (config.closeAllButtonsOnlyOnceInSuperMenu)
                    {
                        result.Add(new CloseParameterInfo { name = GetGlobalCustomCloseButtonParameterName(super, i), saved = true, synced = true });
                    }
                    else
                    {
                        foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                        {
                            if (item == null) continue;
                            result.Add(new CloseParameterInfo { name = GetPerItemCustomCloseButtonParameterName(super, item, i), saved = true, synced = true });
                        }
                    }
                }
            }

            return result;
        }

        private static List<CloseParameterInfo> GetAllCustomCloseButtonAppliedParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();
            var buttons = GetCustomCloseButtons(config);

            foreach (var super in GetSuperGroups(config))
            {
                for (var i = 0; i < buttons.Count; i++)
                {
                    var button = buttons[i];
                    if (button == null || !button.enabled) continue;

                    if (config.closeAllButtonsOnlyOnceInSuperMenu)
                    {
                        result.Add(new CloseParameterInfo { name = GetGlobalCustomCloseButtonAppliedParameterName(super, i), saved = false, synced = false });
                    }
                    else
                    {
                        foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                        {
                            if (item == null) continue;
                            result.Add(new CloseParameterInfo { name = GetPerItemCustomCloseButtonAppliedParameterName(super, item, i), saved = false, synced = false });
                        }
                    }
                }
            }

            return result;
        }

        private static List<CloseParameterInfo> GetAllCustomCloseButtonBackupParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();
            var buttons = GetCustomCloseButtons(config);

            foreach (var super in GetSuperGroups(config))
            {
                for (var i = 0; i < buttons.Count; i++)
                {
                    var button = buttons[i];
                    if (button == null || !button.enabled) continue;

                    var includedGroups = GetIncludedMixGroups(config, button).ToList();
                    if (includedGroups.Count == 0) continue;

                    if (config.closeAllButtonsOnlyOnceInSuperMenu)
                    {
                        foreach (var mixGroup in includedGroups)
                        {
                            result.Add(new CloseParameterInfo
                            {
                                name = GetGlobalCustomCloseButtonBackupParameterName(super, i, mixGroup),
                                saved = false,
                                synced = false
                            });
                        }
                    }
                    else
                    {
                        foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                        {
                            if (item == null) continue;

                            foreach (var mixGroup in includedGroups)
                            {
                                result.Add(new CloseParameterInfo
                                {
                                    name = GetPerItemCustomCloseButtonBackupParameterName(super, item, i, mixGroup),
                                    saved = false,
                                    synced = false
                                });
                            }
                        }
                    }
                }
            }

            return result
                .GroupBy(p => p.name)
                .Select(g => g.First())
                .ToList();
        }

        private static List<CloseParameterInfo> GetAllItemCloseParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();
            foreach (var super in GetSuperGroups(config))
            {
                foreach (var item in super.items ?? new List<SuperSwitchItemOption>())
                {
                    if (item == null || item.closeOptions == null) continue;
                    for (var i = 0; i < item.closeOptions.Count; i++)
                    {
                        var close = item.closeOptions[i];
                        if (close == null) continue;
                        result.Add(new CloseParameterInfo
                        {
                            name = GetItemCloseParameterName(super, item, i),
                            saved = true,
                            synced = true
                        });
                    }
                }
            }
            return result;
        }

        private static string GetItemCloseParameterName(SuperSwitchGroupOption super, SuperSwitchItemOption item, int index)
        {
            return SanitizeParameterName("LW_" + super.parameterName + "_I" + item.value + "_Close_" + (index + 1));
        }

        private static void AddResetClosePartParameters(LastOpOutfitComponent config, SuperSwitchGroupOption super, SuperSwitchItemOption item, List<(string name, int value)> ops)
        {
            if (config.generateCloseAllPartsButton)
                ops.Add((GetEffectiveBaseCloseAllParameterName(config, super, item), 0));

            var buttons = GetCustomCloseButtons(config);
            for (var i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (button == null || !button.enabled) continue;
                ops.Add((GetEffectiveCustomCloseButtonParameterName(config, super, item, button, i), 0));
                ops.Add((GetEffectiveCustomCloseButtonAppliedParameterName(config, super, item, button, i), 0));
            }

            if (item.closeOptions == null) return;
            for (var i = 0; i < item.closeOptions.Count; i++)
            {
                var close = item.closeOptions[i];
                if (close == null) continue;
                ops.Add((GetItemCloseParameterName(super, item, i), 0));
            }
        }

        private static List<CloseParameterInfo> GetAllMixItemCloseParameters(LastOpOutfitComponent config)
        {
            var result = new List<CloseParameterInfo>();

            foreach (var group in GetMixGroups(config))
            {
                foreach (var item in group.items ?? new List<PartOption>())
                {
                    if (item == null || item.closeOptions == null) continue;

                    if (config.generateCloseAllPartsButton && item.closeOptions.Count > 0)
                    {
                        result.Add(new CloseParameterInfo
                        {
                            name = GetMixItemCloseAllParameterName(group.parameterName, item),
                            saved = true,
                            synced = true
                        });
                    }

                    for (var i = 0; i < item.closeOptions.Count; i++)
                    {
                        var close = item.closeOptions[i];
                        if (close == null) continue;

                        result.Add(new CloseParameterInfo
                        {
                            name = GetMixItemCloseParameterName(group.parameterName, item, i),
                            saved = true,
                            synced = true
                        });
                    }
                }
            }

            return result
                .GroupBy(p => p.name)
                .Select(g => g.First())
                .ToList();
        }

        private static string GetMixItemCloseAllParameterName(string mixParameterName, PartOption item)
        {
            return SanitizeParameterName("LW_Mix_" + mixParameterName + "_I" + item.value + "_CloseAll");
        }

        private static string GetMixItemCloseParameterName(string mixParameterName, PartOption item, int index)
        {
            return SanitizeParameterName("LW_Mix_" + mixParameterName + "_I" + item.value + "_Close_" + (index + 1));
        }

        private static List<GameObject> GetAllMixCloseOptionObjects(PartOption item)
        {
            if (item == null || item.closeOptions == null)
            {
                return new List<GameObject>();
            }

            return item.closeOptions
                .Where(close => close != null && close.objects != null)
                .SelectMany(close => close.objects)
                .Where(obj => obj != null)
                .Distinct()
                .ToList();
        }

        private static List<GameObject> GetAllCloseOptionObjectsForSuper(LastOpOutfitComponent config, SuperSwitchGroupOption super)
        {
            if (super == null || super.items == null) return new List<GameObject>();

            return super.items
                .Where(item => item != null)
                .SelectMany(GetAllCloseOptionObjects)
                .Where(obj => obj != null)
                .Distinct()
                .ToList();
        }

        private static List<GameObject> GetAllCloseOptionObjects(SuperSwitchItemOption item)
        {
            if (item == null) return new List<GameObject>();

            var objectsFromCloseOptions = new List<GameObject>();

            if (item.closeOptions != null)
            {
                objectsFromCloseOptions = item.closeOptions
                    .Where(close => close != null && close.objects != null)
                    .SelectMany(close => close.objects)
                    .Where(obj => obj != null)
                    .Distinct()
                    .ToList();
            }

            if (objectsFromCloseOptions.Count > 0)
                return objectsFromCloseOptions;

            return (item.objects ?? new List<GameObject>())
                .Where(obj => obj != null)
                .Distinct()
                .ToList();
        }

        private static List<SuperSwitchGroupOption> GetSuperGroups(LastOpOutfitComponent config)
        {
            return (config.superSwitchGroups ?? new List<SuperSwitchGroupOption>())
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.displayName) && !string.IsNullOrWhiteSpace(g.parameterName))
                .ToList();
        }

        private static List<MixGroupOption> GetMixGroups(LastOpOutfitComponent config)
        {
            return (config.mixGroups ?? new List<MixGroupOption>())
                .Where(g => g != null && !string.IsNullOrWhiteSpace(g.displayName) && !string.IsNullOrWhiteSpace(g.parameterName))
                .ToList();
        }

        private static List<CustomCloseButtonOption> GetCustomCloseButtons(LastOpOutfitComponent config)
        {
            return (config.customCloseButtons ?? new List<CustomCloseButtonOption>())
                .Where(b => b != null && b.enabled)
                .ToList();
        }

        private static IEnumerable<MixGroupOption> GetIncludedMixGroups(LastOpOutfitComponent config, CustomCloseButtonOption button)
        {
            if (button == null || button.includedMixGroupNames == null) yield break;

            foreach (var group in GetMixGroups(config))
                if (button.includedMixGroupNames.Contains(group.displayName))
                    yield return group;
        }

        private static void ConfigureMAParameters(Component component, LastOpOutfitComponent config)
        {
            var so = new SerializedObject(component);
            var array = FindArrayProperty(so, "parameters", "m_parameters", "parameterConfig", "parameterConfigs");
            if (array != null)
            {
                array.ClearArray();
                var defaultValue = config.keepCurrentOutfitByDefault ? NeutralValue : 0;

                foreach (var super in GetSuperGroups(config))
                {
                    AddMaParameter(array, super.parameterName, defaultValue, true, true);
                    AddMaParameter(array, GetLastAppliedParameterName(super), defaultValue, false, false);
                }

                foreach (var group in GetMixGroups(config))
                {
                    AddMaParameter(array, group.parameterName, defaultValue, true, true);
                    AddMaParameter(array, GetLastMixParameterName(group), 1, false, false, "Int");
                }

                foreach (var closeAllParam in GetAllBaseCloseAllParameters(config))
                    AddMaParameter(array, closeAllParam.name, 0, closeAllParam.saved, closeAllParam.synced, "Bool");

                foreach (var closeButtonParam in GetAllCustomCloseButtonParameters(config))
                    AddMaParameter(array, closeButtonParam.name, 0, closeButtonParam.saved, closeButtonParam.synced, "Bool");

                foreach (var appliedParam in GetAllCustomCloseButtonAppliedParameters(config))
                    AddMaParameter(array, appliedParam.name, 0, false, false, "Bool");

                foreach (var closeParam in GetAllItemCloseParameters(config))
                {
                    var saved = closeParam.saved || config.closePartSavedByDefault;
                    var synced = closeParam.synced || config.closePartSyncedByDefault;
                    AddMaParameter(array, closeParam.name, 0, saved, synced, "Bool");
                }

                foreach (var closeParam in GetAllMixItemCloseParameters(config))
                {
                    var saved = closeParam.saved || config.closePartSavedByDefault;
                    var synced = closeParam.synced || config.closePartSyncedByDefault;
                    AddMaParameter(array, closeParam.name, 0, saved, synced, "Bool");
                }
            }
            else
            {
                Debug.LogWarning("[Paulxstx Outfit] 未能自动写入 MA Parameters，请手动确认参数。");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static void AddMaParameter(SerializedProperty array, string name, int defaultValue, bool saved, bool synced, string valueType = "Int")
        {
            var index = array.arraySize;
            array.InsertArrayElementAtIndex(index);
            var e = array.GetArrayElementAtIndex(index);

            SetString(e, "nameOrPrefix", name);
            SetString(e, "remapTo", "");

            SetBool(e, "isPrefix", false);
            SetBool(e, "saved", saved);
            SetBool(e, "hasExplicitDefaultValue", true);
            SetNumber(e, "defaultValue", defaultValue);

            if (synced)
            {
                SetBool(e, "internalParameter", false);
                SetBool(e, "localOnly", false);
                SetEnum(e, "syncType", valueType);
            }
            else
            {
                SetBool(e, "internalParameter", true);
                SetBool(e, "localOnly", true);
                SetEnum(e, "syncType", "NotSynced");
            }

            SetString(e, "name", name);
            SetString(e, "parameterName", name);
            SetString(e, "syncParameterName", name);
            SetEnum(e, "type", valueType);
            SetEnum(e, "valueType", valueType);
            SetEnum(e, "parameterType", valueType);
            SetNumber(e, "defaultValueInt", defaultValue);
            SetBool(e, "isSaved", saved);
            SetBool(e, "save", saved);
            SetBool(e, "isSave", saved);
            SetBool(e, "synced", synced);
            SetBool(e, "isSynced", synced);
            SetBool(e, "networkSynced", synced);
            SetBool(e, "sync", synced);
            SetBoolDeepByName(e, saved, "saved", "isSaved", "save", "isSave");
            SetBoolDeepByName(e, synced, "synced", "isSynced", "networkSynced", "sync");
        }

        private static bool SetBoolDeepByName(SerializedProperty element, bool value, params string[] names)
        {
            var nameSet = new HashSet<string>(names.Select(n => n.ToLowerInvariant()));
            var changed = false;
            var copy = element.Copy();
            var end = copy.GetEndProperty();
            var enterChildren = true;

            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                enterChildren = false;
                if (copy.propertyType != SerializedPropertyType.Boolean) continue;
                var lower = copy.name.ToLowerInvariant();
                if (nameSet.Contains(lower))
                {
                    copy.boolValue = value;
                    changed = true;
                }
            }

            return changed;
        }

        private class TargetState
        {
            public GameObject target;
            public bool active;
        }

        private static void ConfigureObjectToggle(Component component, List<TargetState> targets, GameObject avatarRoot)
        {
            var so = new SerializedObject(component);
            var array = so.FindProperty("m_objects");
            if (array == null || !array.isArray)
                array = FindArrayProperty(so, "objects", "targetObjects", "targets", "m_targets");

            if (array == null)
            {
                Debug.LogWarning("[Paulxstx Outfit] 未能识别 MA Object Toggle 目标列表字段：" + component.name);
                return;
            }

            array.ClearArray();

            foreach (var t in targets)
            {
                var index = array.arraySize;
                array.InsertArrayElementAtIndex(index);
                var e = array.GetArrayElementAtIndex(index);

                var objRef = e.FindPropertyRelative("Object");
                if (objRef != null)
                {
                    var targetObject = objRef.FindPropertyRelative("targetObject");
                    if (targetObject != null && targetObject.propertyType == SerializedPropertyType.ObjectReference)
                        targetObject.objectReferenceValue = t.target;

                    var referencePath = objRef.FindPropertyRelative("referencePath");
                    if (referencePath != null && referencePath.propertyType == SerializedPropertyType.String)
                        referencePath.stringValue = MakeReferencePath(avatarRoot, t.target);
                }
                else
                {
                    SetObjectDeep(e, t.target);
                }

                var active = e.FindPropertyRelative("Active");
                if (active != null && active.propertyType == SerializedPropertyType.Boolean)
                    active.boolValue = t.active;
                else
                    SetBoolDeep(e, t.active);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private static string MakeReferencePath(GameObject avatarRoot, GameObject target)
        {
            if (avatarRoot == null || target == null) return "";
            if (target == avatarRoot) return "$$$AVATAR_ROOT$$$";
            if (!target.transform.IsChildOf(avatarRoot.transform)) return "";
            return GetRelativePath(avatarRoot.transform, target.transform);
        }

        private static void ConfigureMergeAnimator(Component component, RuntimeAnimatorController controller)
        {
            var so = new SerializedObject(component);
            SetObjectAny(so, controller, "animator", "animatorController", "controller", "animatorToMerge");
            SetEnumAny(so, "FX", "layerType", "layer", "animLayerType", "targetLayerType");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
        }

        private class DriverOp
        {
            public string type;
            public string name;
            public int value;
            public string source;
            public string destination;

            public static DriverOp Set(string name, int value)
            {
                return new DriverOp
                {
                    type = "Set",
                    name = name,
                    destination = name,
                    value = value
                };
            }

            public static DriverOp Copy(string source, string destination)
            {
                return new DriverOp
                {
                    type = "Copy",
                    name = destination,
                    source = source,
                    destination = destination,
                    value = 0
                };
            }
        }

        private static void AddAvatarParameterDriver(AnimatorState state, List<(string name, int value)> ops)
        {
            var type = FindType("VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver", "VRC.SDK3.Avatars.Components.VRC_AvatarParameterDriver", "VRCAvatarParameterDriver", "VRC_AvatarParameterDriver");
            if (type == null) throw new Exception("找不到 VRChat Avatar Parameter Driver。");

            var behaviour = state.AddStateMachineBehaviour(type);
            var so = new SerializedObject(behaviour);
            var parameters = so.FindProperty("parameters");
            if (parameters == null || !parameters.isArray) throw new Exception("Avatar Parameter Driver 上找不到 parameters。");

            foreach (var op in ops)
            {
                var index = parameters.arraySize;
                parameters.InsertArrayElementAtIndex(index);
                var e = parameters.GetArrayElementAtIndex(index);
                SetEnum(e, "type", "Set");
                SetString(e, "name", op.name);
                SetString(e, "destination", op.name);
                SetNumber(e, "value", op.value);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(behaviour);
        }

        private static void AddAvatarParameterDriverOps(AnimatorState state, List<DriverOp> ops)
        {
            var type = FindType("VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver", "VRC.SDK3.Avatars.Components.VRC_AvatarParameterDriver", "VRCAvatarParameterDriver", "VRC_AvatarParameterDriver");
            if (type == null) throw new Exception("找不到 VRChat Avatar Parameter Driver。");

            var behaviour = state.AddStateMachineBehaviour(type);
            var so = new SerializedObject(behaviour);
            var parameters = so.FindProperty("parameters");
            if (parameters == null || !parameters.isArray) throw new Exception("Avatar Parameter Driver 上找不到 parameters。");

            foreach (var op in ops)
            {
                var index = parameters.arraySize;
                parameters.InsertArrayElementAtIndex(index);
                var e = parameters.GetArrayElementAtIndex(index);

                SetEnum(e, "type", op.type);

                // Set 类型主要使用 name/value。
                // Copy 类型在不同 SDK 版本里字段名可能是 source/dest 或 source/destination。
                // 这里同时写入常见字段，保证兼容。
                SetString(e, "name", op.name);
                SetString(e, "source", op.source);
                SetString(e, "dest", op.destination);
                SetString(e, "destination", op.destination);
                SetString(e, "parameter", op.name);
                SetString(e, "sourceParameter", op.source);
                SetString(e, "destinationParameter", op.destination);

                SetNumber(e, "value", op.value);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(behaviour);
        }

        private static Type FindType(params string[] names)
        {
            foreach (var name in names)
            {
                var t = Type.GetType(name);
                if (t != null) return t;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                    if (names.Any(n => t.FullName == n || t.Name == n))
                        return t;
            }

            return null;
        }

        private static Component GetOrAddComponent(GameObject go, Type type)
        {
            var c = go.GetComponent(type) as Component;
            return c != null ? c : Undo.AddComponent(go, type) as Component;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create outfit menu slot");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static SerializedProperty FindArrayProperty(SerializedObject so, params string[] names)
        {
            foreach (var name in names)
            {
                var p = so.FindProperty(name);
                if (p != null && p.isArray && p.propertyType != SerializedPropertyType.String) return p;
            }

            var it = so.GetIterator();
            var enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.isArray && it.propertyType != SerializedPropertyType.String)
                    return it.Copy();
            }
            return null;
        }

        private static void SetObjectAny(SerializedObject so, Object value, params string[] names)
        {
            foreach (var name in names)
            {
                var p = so.FindProperty(name);
                if (p != null && p.propertyType == SerializedPropertyType.ObjectReference)
                {
                    p.objectReferenceValue = value;
                    return;
                }
            }
        }

        private static void SetEnumAny(SerializedObject so, string enumName, params string[] names)
        {
            foreach (var name in names)
            {
                var p = so.FindProperty(name);
                if (p != null && p.propertyType == SerializedPropertyType.Enum)
                {
                    var idx = Array.FindIndex(p.enumNames, n => string.Equals(n, enumName, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) { p.enumValueIndex = idx; return; }
                }
            }
        }

        private static string GetString(SerializedProperty e, string name, string fallback)
        {
            var p = e != null ? e.FindPropertyRelative(name) : null;
            if (p != null && p.propertyType == SerializedPropertyType.String && !string.IsNullOrWhiteSpace(p.stringValue))
                return p.stringValue;
            return fallback;
        }

        private static void SetString(SerializedProperty e, string name, string value)
        {
            var p = e.FindPropertyRelative(name);
            if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = value;
        }

        private static void SetBool(SerializedProperty e, string name, bool value)
        {
            var p = e.FindPropertyRelative(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value;
        }

        private static void SetNumber(SerializedProperty e, string name, int value)
        {
            var p = e.FindPropertyRelative(name);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
            else if (p.propertyType == SerializedPropertyType.Float) p.floatValue = value;
        }

        private static void SetEnum(SerializedProperty e, string name, string enumName)
        {
            var p = e.FindPropertyRelative(name);
            if (p == null || p.propertyType != SerializedPropertyType.Enum) return;
            var idx = Array.FindIndex(p.enumNames, n => string.Equals(n, enumName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) p.enumValueIndex = idx;
        }

        private static bool SetObjectDeep(SerializedProperty e, Object value)
        {
            var copy = e.Copy();
            var end = copy.GetEndProperty();
            var enter = true;
            while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
            {
                enter = false;
                if (copy.propertyType == SerializedPropertyType.ObjectReference)
                {
                    copy.objectReferenceValue = value;
                    return true;
                }
            }
            return false;
        }

        private static bool SetBoolDeep(SerializedProperty e, bool value)
        {
            var copy = e.Copy();
            var end = copy.GetEndProperty();
            var enter = true;
            while (copy.NextVisible(enter) && !SerializedProperty.EqualContents(copy, end))
            {
                enter = false;
                if (copy.propertyType == SerializedPropertyType.Boolean)
                {
                    copy.boolValue = value;
                    return true;
                }
            }
            return false;
        }

        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folder)) return;

            var parts = folder.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new Exception("Output folder must start with  Assets 开头。");

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            var stack = new Stack<string>();
            var current = target;
            while (current != null && current != root)
            {
                stack.Push(current.name);
                current = current.parent;
            }
            return string.Join("/", stack.ToArray());
        }

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "空";
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray())
                .Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace(" ", "_");
        }

        private static string SanitizeParameterName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "LW_Param";
            var chars = value.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_').ToArray();
            return new string(chars);
        }
    }
}

#endif
