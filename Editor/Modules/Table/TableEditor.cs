using DCFrame;
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
        tableRules = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.PrefixPath.Settings));
        foreach (var rule in tableRules.tableRuleList) {
            tableRuleDic[rule.name] = rule;
        }
        AnalyzeTableData(tableName);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 处理表数据
    /// </summary>
    private static void AnalyzeTableData(string tableName = "") {
        var files = Directory.GetFiles(TableUtil.TableDataPath, "*.csv", SearchOption.TopDirectoryOnly);
        if (files.Length <= 0) {
            Debug.LogWarning("暂无配表数据");
            return;
        }
        HashSet<string> tableNames = new HashSet<string>(files.Length);
        int totalFiles = files.Length + 1;
        int currentIndex = 0;
        string strNoneConfig = "";

        try {
            foreach (var file in files) {
                currentIndex++;
                if (file.EndsWith(".meta")) {
                    continue;
                }
                var fileName = Path.GetFileNameWithoutExtension(file);
                tableNames.Add(fileName);
                
                // 进度条显示
                float progress = (float)currentIndex / totalFiles;
                EditorUtility.DisplayProgressBar("分析配表数据", $"正在处理文件: {fileName} ({currentIndex}/{totalFiles})", progress);
                if (tableName.Length > 0 && tableName != fileName) {
                    continue;
                }

                tableRuleDic.TryGetValue(fileName, out var tableRule);
                if (tableRule == null){
                    strNoneConfig += fileName + " ,";
                    continue;
                }
                ITableType tableType = tableRule.enumTableType switch {
                    TableUtil.TableType.Default => new TableRulesTypeCommon(),
                    TableUtil.TableType.Const => new TableRulesTypeConst(),
                    TableUtil.TableType.Enum => new TableRulesTypeEnum(),
                    TableUtil.TableType.String => new TableRulesTypeString(),
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
            if (strNoneConfig.Length > 0){
                Debug.LogWarning($"以下表没有找到相对应的配置数据：\n<color=#FF0000>{strNoneConfig}</color>");
            }
            if (tableNames.Count > 0) {
                EditorUtility.DisplayProgressBar("分析配表数据", $"正在删除多余的本地化数据", 1);
                CheckDeletedTables(tableNames);
            }
            EditorUtility.ClearProgressBar();
        }
    }
    
    /// <summary>
    /// 检测已删除表但仍残留的数据
    /// </summary>
    private static void CheckDeletedTables(HashSet<string> tableNames) {
        List<string> deletedPathList = new List<string>();
        
        // 检测多余的表代码
        if (Directory.Exists(TableUtil.TableScriptPath)) {
            foreach (var file in Directory.EnumerateFiles(TableUtil.TableScriptPath)) {
                if (file.EndsWith(".meta") || file.EndsWith(".asmdef")) continue;
                var name = Path.GetFileNameWithoutExtension(file);
                if (tableNames.Contains(name)) continue;
                var assetPath = file.Replace("\\", "/");
                deletedPathList.Add(assetPath);
            }
        }

        // 检测多余的本地化数据
        if (Directory.Exists(LocalizeConst.LocalizeTableRootPath)) {
            var paths = Directory.GetDirectories(LocalizeConst.LocalizeTableRootPath + "/" + LocalizeConst.LocalizeStringTableName, "*", SearchOption.TopDirectoryOnly);
            foreach (var dir in paths) {
                var folderName = Path.GetFileNameWithoutExtension(dir);
                if (folderName == LocalizeConst.LocalizeCollectionTableName) {
                    // 数据表
                    foreach (var file in Directory.EnumerateFiles(dir)) {
                        if (Path.GetExtension(file) == ".meta")
                            continue;
                        var name = Path.GetFileNameWithoutExtension(file);
                        var tableName = name.Replace(" Shared Data", "");
                        if (tableNames.Contains(tableName))
                            continue;
                        deletedPathList.Add(file.Replace("\\", "/"));
                    }
                }else {
                    // 语言表
                    foreach (var file in Directory.EnumerateFiles(dir)) {
                        if (Path.GetExtension(file) == ".meta")
                            continue;
                        var name = Path.GetFileNameWithoutExtension(file);
                        var index = name.IndexOf('_');
                        if (index <= 0)
                            continue;
                        var tableName = name.Substring(0, index);
                        if (tableNames.Contains(tableName))
                            continue;
                        deletedPathList.Add(file.Replace("\\", "/"));
                    }
                }
            }
        }
        
        // 输出并删除多余的资源
        if (deletedPathList.Count > 0) {
            foreach (var path in deletedPathList) {
                AssetDatabase.DeleteAsset(path);
            }
            Debug.LogWarning($"以下表已不存在，因此删除相关资源：\n<color=#FF0000>{string.Join("\n", deletedPathList)}</color>\n");
        }
    }
   
    private static Dictionary<string, TableRules.TableRule> tableRuleDic = new();
    private static TableRules tableRules;
}
