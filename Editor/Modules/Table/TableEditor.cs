using System.IO;
using DCFrame;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEditor.Localization;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization.Tables;

public class TableEditor : Editor {
    [MenuItem("Tools/资源项/导表")]
    public static void PackageConfig() {
        tableRules = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.EnumPrefixPath.Settings));
        AnalyzeTableData();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 分析表数据
    /// </summary>
    private static void AnalyzeTableData() {
        var files = Directory.GetFiles(TableUtil.TableDataPath);
        if (files.Length <= 0) {
            Debug.LogWarning("暂无配表数据");
            return;
        }
        foreach (var file in files) {
            if (file.EndsWith(".meta")) {
                continue;
            }
            var fileName = Path.GetFileNameWithoutExtension(file);
            tableRule = tableRules.tableRuleList.Find((x) => x.name == fileName);
            ITableType tableType = new TableRulesTypeCommon();
            switch (tableRule.enumTableType) {
                case TableUtil.EnumTableType.Default:
                    tableType = new TableRulesTypeCommon();
                    break;
                case TableUtil.EnumTableType.Const:
                    tableType = new TableRulesTypeConst();
                    break;
                case TableUtil.EnumTableType.Enum:
                    tableType = new TableRulesTypeEnum();
                    break;
                case TableUtil.EnumTableType.String:
                    tableType = new TableRulesTypeString();
                    break;
            }

            if (tableType.Init(tableRule)) {
                tableType.AnalyzeAndCreateScripts();
            }
        }
    }
    
    #region 右键功能

    [MenuItem("CONTEXT/Text/Add Localize", false, 2000)]
    static void LocalizeUIText(MenuCommand command) {
        var target = command.context as Text;
        SetupForLocalization(target);
    }
    
    private static MonoBehaviour SetupForLocalization(Text target) {
        // 存在组件则删除
        var oldComp = target.GetComponent<LocalizeStringEvent>();
        if (oldComp) {
            Undo.DestroyObjectImmediate(oldComp);
        }
        // 添加组件
        var comp = Undo.AddComponent(target.gameObject, typeof(LocalizeStringEvent)) as LocalizeStringEvent;
        if (!comp) {
            return null;
        }
        // 赋值
        var tableRule = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.EnumPrefixPath.Settings));
        var config = tableRule.tableRuleList.Find((x)=> x.enumTableType == TableUtil.EnumTableType.String);
        comp.StringReference.TableReference = config.name;
        // 绑定事件
        var setStringMethod = target.GetType().GetProperty("text")?.GetSetMethod();
        if (setStringMethod != null) {
            var methodDelegate = System.Delegate.CreateDelegate(
                typeof(UnityAction<string>),
                target,
                setStringMethod
            ) as UnityAction<string>;
            UnityEditor.Events.UnityEventTools.AddPersistentListener(comp.OnUpdateString, methodDelegate);
        }
        comp.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
        comp.OnUpdateString.Invoke(target.text);
        // 读取文本是否存在Key
        var table = LocalizationEditorSettings.GetStringTableCollection(config.name);
        if (table) {
            var currentText = target.text;
            var sharedData = table.SharedData;
            var entry = sharedData.GetEntry(currentText);
            if (entry != null) {
                comp.StringReference.TableEntryReference = currentText;
                var chineseLocale = LocalizationSettings.AvailableLocales.GetLocale(LocalizeConst.LocaleCodeDic[LocalizeConst.EnumLocaleCode.ZhCN]);
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
        return comp;
    }
    
    #endregion
    
    private static TableRules.TableRule tableRule;
    private static TableRules tableRules;
}
