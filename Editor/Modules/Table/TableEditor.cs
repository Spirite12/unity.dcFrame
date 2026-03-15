using DCFrame;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
        string strNoneConfig = "";
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
                if (tableRule == null){
                    strNoneConfig += fileName + " ,";
                    continue;
                }
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
            if (strNoneConfig.Length > 0){
                Debug.LogWarning($"以下表没有找到相对应的配置数据：\n<color=#FF0000>{strNoneConfig}</color>");
            }
        }
    }
    
    private static TableRules.TableRule tableRule;
    private static TableRules tableRules;
}
