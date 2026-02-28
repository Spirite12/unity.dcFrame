using System.IO;
using DCFrame;
using UnityEditor;
using UnityEngine;

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
    
    private static TableRules.TableRule tableRule;
    private static TableRules tableRules;
}
