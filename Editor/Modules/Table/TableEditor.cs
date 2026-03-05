using System;
using System.IO;
using DCFrame;
using UnityEditor;
using UnityEngine;

public class TableEditor : Editor {
    [MenuItem("Tools/资源项/一键导表")]
    public static void PackageConfig() {
        PackageConfig("");
    }
    
    public static void PackageConfig(string tableName) {
        tableRules = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.EnumPrefixPath.Settings));
        AnalyzeTableData(tableName);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 分析表数据
    /// </summary>
    private static void AnalyzeTableData(string tableName = "") {
        var files = Directory.GetFiles(TableUtil.TableDataPath);
        if (files.Length <= 0) {
            Debug.LogWarning("暂无配表数据");
            return;
        }
        int totalFiles = files.Length;
        int currentIndex = 0;
        try {
            foreach (var file in files) {
                currentIndex++;
                if (file.EndsWith(".meta")) {
                    continue;
                }
                var fileName = Path.GetFileNameWithoutExtension(file);
                // 进度条显示
                float progress = (float)currentIndex / totalFiles;
                EditorUtility.DisplayProgressBar("分析配表数据", $"正在处理文件: {fileName} ({currentIndex}/{totalFiles})", progress);
                if (tableName.Length > 0 && tableName != fileName) {
                    continue;
                }
                tableRule = tableRules.tableRuleList.Find((x) => x.name == fileName);
                ITableType tableType = tableRule.enumTableType switch {
                    TableUtil.EnumTableType.Default => new TableRulesTypeCommon(),
                    TableUtil.EnumTableType.Const => new TableRulesTypeConst(),
                    TableUtil.EnumTableType.Enum => new TableRulesTypeEnum(),
                    TableUtil.EnumTableType.String => new TableRulesTypeString(),
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (tableType.Init(tableRule)) {
                    tableType.OnDealWithData();
                }
                if (tableName == fileName) {
                    break;
                }
            }
        }
        finally {
            EditorUtility.ClearProgressBar();
        }
    }
    
    private static TableRules.TableRule tableRule;
    private static TableRules tableRules;
}
