using System.IO;
using DCFrame;
using DCFrame.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEditor.Localization;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization.Tables;
using StringUtil = DCFrame.Utility.StringUtil;

public class LocalizeEditor : Editor {
    [MenuItem("Tools/资源项/本地化资源生成")]
    public static void CreateLocalizeAsset() {
        if (!Directory.Exists(LocalizeConst.LocalizeTableRootPath)) {
            Debug.LogError($"目录不存在: {LocalizeConst.LocalizeTableRootPath}");
            return;
        }
        AssetDatabase.StartAssetEditing();
        var tableFolders = Directory.GetDirectories(LocalizeConst.LocalizeTableRootPath);
        int totalFiles = tableFolders.Length;
        int currentIndex = 0;
        try {
            foreach (var tableFolder in tableFolders) {
                currentIndex++;
                string tableName = Path.GetFileName(tableFolder);
                if (tableName == LocalizeConst.LocalizeStringTableName) {
                    continue;
                }
                // 进度条显示
                float progress = (float)currentIndex / totalFiles;
                EditorUtility.DisplayProgressBar("分析配表数据", $"正在处理文件: {tableName} ({currentIndex}/{totalFiles})", progress);
                // 获取或创建表数据
                var collection = LocalizationEditorSettings.GetAssetTableCollection(tableName);
                if (!collection) {
                    collection = LocalizationEditorSettings.CreateAssetTableCollection(tableName, $"{tableFolder}/{LocalizeConst.LocalizeCollectionTableName}");
                }else {
                    LocalizeUtilEditor.ClearCollection(collection);
                }
                var langFolders = Directory.GetDirectories(tableFolder);
                foreach (var langFolder in langFolders) {
                    // 遍历本地化文件夹
                    string langName = Path.GetFileName(langFolder);
                    if (langName == LocalizeConst.LocalizeCollectionTableName) {
                        continue;
                    }
                    var locale = LocalizationEditorSettings.GetLocale(StringUtil.ToLowerFirstChar(langName));
                    if (!locale) {
                        Debug.LogWarning($"Locale 不存在: {langName}");
                        continue;
                    }
                    
                    var assetTable = collection.GetTable(locale.Identifier) as AssetTable;
                    if (!assetTable) {
                        // 创建本地化的localization表
                        collection.AddNewTable(locale.Identifier);
                        assetTable = collection.GetTable(locale.Identifier) as AssetTable;
                    }
                    // 移动资源
                    string assetPath = AssetDatabase.GetAssetPath(assetTable);
                    string newPath = $"{tableFolder}/{LocalizeConst.LocalizeCollectionTableName}/{tableName}_{langName}.asset";
                    if (!string.Equals(assetPath, newPath, System.StringComparison.Ordinal)) {
                        string moveError = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (!string.IsNullOrEmpty(moveError)) {
                            throw new System.InvalidOperationException($"移动本地化资源表失败：{moveError}");
                        }
                    }
                    // 遍历资源
                    var assets = Directory.GetFiles(langFolder);
                    foreach (var file in assets) {
                        if (file.EndsWith(".meta")) {
                            continue;
                        }
                        string resName = Path.GetFileNameWithoutExtension(file);
                        string key = $"{resName}";
                        string assetPathTp = file.Replace(Application.dataPath, "Assets");
                        var guid = AssetDatabase.GUIDFromAssetPath(assetPathTp);
                        var entry = assetTable.GetEntry(key);
                        if (entry == null) {
                            assetTable.AddEntry(key, guid.ToString());
                        }else {
                            entry.Guid = guid.ToString();
                        }
                    }
                }
                EditorUtility.SetDirty(collection);
            }
        }finally {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        Debug.Log("执行成功");
    }
    
    #region 右键功能

    [MenuItem("CONTEXT/Text/Add Localize", false, 2000)]
    static void LocalizeUIText(MenuCommand command) {
        var target = command.context as Text;
        SetupForLocalization(target);
    }
    
    [MenuItem("CONTEXT/Image/Add Localize", false, 2000)]
    static void LocalizeUIImage(MenuCommand command) {
        var target = command.context as Image;
        SetupForLocalization(target);
    }
    
    [MenuItem("CONTEXT/RawImage/Add Localize", false, 2000)]
    static void LocalizeUIRawImage(MenuCommand command) {
        var target = command.context as RawImage;
        SetupForLocalization(target);
    }

    private static void SetupForLocalization(Image target) {
        // 存在组件则删除
        var oldComp = target.GetComponent<LocalizeSpriteEvent>();
        if (oldComp) {
            Undo.DestroyObjectImmediate(oldComp);
        }
        // 添加组件
        var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeSpriteEvent)) as LocalizeSpriteEvent;
        if (!comp) {
            return;
        }
        // 设置表
        var tableName = Localize.AssetTableNameDic[typeof(Sprite)];
        comp.AssetReference.TableReference = tableName;
        // 绑定事件：Sprite → Image.sprite
        var setTextureMethod = target.GetType().GetProperty("sprite")?.GetSetMethod();
        if (setTextureMethod != null) {
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<Sprite>), target, setTextureMethod) as UnityAction<Sprite>;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateAsset, methodDelegate);
        }
        comp.OnUpdateAsset.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        var sprite = target.sprite;
        if (!sprite) return;
        var key = sprite.name;
        // 查找 Localization 表
        var table = LocalizationEditorSettings.GetAssetTableCollection(tableName);
        if (!table) return;
        var sharedData = table.SharedData;
        var entry = sharedData.GetEntry(key);
        if (entry == null) return;
        comp.AssetReference.TableEntryReference = key;
    }
    
    private static void SetupForLocalization(RawImage target) {
        // 存在组件则删除
        var oldComp = target.GetComponent<LocalizeTextureEvent>();
        if (oldComp) {
            Undo.DestroyObjectImmediate(oldComp);
        }
        // 添加组件
        var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeTextureEvent)) as LocalizeTextureEvent;
        if (!comp) {
            return;
        }
        // 设置表
        var tableName = Localize.AssetTableNameDic[typeof(RawImage)];
        comp.AssetReference.TableReference = tableName;
        // 绑定事件：Sprite → Image.sprite
        var setTextureMethod = target.GetType().GetProperty("texture")?.GetSetMethod();
        if (setTextureMethod != null) {
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<Texture>), target, setTextureMethod) as UnityAction<Texture>;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateAsset, methodDelegate);
        }
        comp.OnUpdateAsset.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        var rawImage = target.texture;
        if (!rawImage) return;
        var key = rawImage.name;
        // 查找 Localization 表
        var table = LocalizationEditorSettings.GetAssetTableCollection(tableName);
        if (!table) return;
        var sharedData = table.SharedData;
        var entry = sharedData.GetEntry(key);
        if (entry == null) return;
        comp.AssetReference.TableEntryReference = key;
    }

    private static void SetupForLocalization(Text target) {
        // 存在组件则删除
        var oldComp = target.GetComponent<LocalizeStringEvent>();
        if (oldComp) {
            Undo.DestroyObjectImmediate(oldComp);
        }
        // 添加组件
        var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeStringEvent)) as LocalizeStringEvent;
        if (!comp) {
            return;
        }
        var currentText = target.text;
        StringTableCollection table = null;
        string entryKey = null;
        string tableName = null;

        // 查找 table 数据
        var split = currentText.Split('.');
        if (split.Length > 1) {
            tableName = split[0];
            table = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (table) {
                entryKey = LocalizeUtil.GetTableCollectionKey(currentText);
            }
        }
        if (!table) {
            var tableRule = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.PrefixPath.Settings));
            var config = tableRule.tableRuleList.Find(x => x.enumTableType == TableUtil.TableType.String);
            if (config == null) {
                Debug.LogWarning("请先创建文本表");
                return;
            }
            tableName = config.name;
            table = LocalizationEditorSettings.GetStringTableCollection(tableName);
            entryKey = currentText;
        }
        comp.StringReference.TableReference = tableName;

        // 绑定事件
        var setStringMethod = target.GetType().GetProperty("text")?.GetSetMethod();
        if (setStringMethod != null) {
            var methodDelegate = System.Delegate.CreateDelegate(typeof(UnityAction<string>), target, setStringMethod) as UnityAction<string>;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
        }
        comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);

        // 设置并赋值
        if (table && !string.IsNullOrEmpty(entryKey)) {
            var sharedData = table.SharedData;
            var entry = sharedData.GetEntry(entryKey);

            if (entry != null) {
                comp.StringReference.TableEntryReference = entryKey;

                var chineseLocale = LocalizationSettings.AvailableLocales.GetLocale(
                    LocalizeConst.LocaleCodeDic[LocalizeConst.LocaleCode.ZhCN]);

                if (chineseLocale) {
                    var chineseTable = table.GetTable(chineseLocale.Identifier) as StringTable;
                    if (chineseTable) {
                        var stringTable = chineseTable.GetEntry(entry.Key);
                        if (stringTable != null) {
                            target.text = stringTable.Value;
                        }
                    }
                }
            }
        }
        comp.OnUpdateString.Invoke(target.text);
    }
    
    #endregion
}
