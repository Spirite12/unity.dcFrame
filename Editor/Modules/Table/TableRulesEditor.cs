using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DCFrame;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TableRules))]
public class TableRulesEditor : Editor {
    private void OnEnable() {
        tableRules = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.EnumPrefixPath.Settings));
        tableRules.tableRuleList.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
    }

    private void OnDisable() {
        if (tableRules) {
            EditorUtility.SetDirty(tableRules);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            tableRules = null;
        }
    }
    
    public override void OnInspectorGUI() {
        if (!tableRules) {
            return;
        }
        RenderToolkitInfo();
        DrawDefaultInspector();
        RenderTableInfo();
    }

    #region ConfigInfo

    /// <summary>
    /// 渲染表信息
    /// </summary>
    private void RenderTableInfo() {
        if (tableRules.tableRuleList.Count <= selectIndex) {
            return;
        }
        AnalyzeTableData();
        CreateFileData();
        
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("表列表: ");
        List<string> nameList = new List<string>();
        foreach (var rule in tableRules.tableRuleList) {
            nameList.Add(rule.name);
        }
        selectIndex = EditorGUILayout.Popup(selectIndex, nameList.ToArray());
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        if (GUILayout.Button("保存表数据")) {
            EditorUtility.SetDirty(tableRules);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// 创建表字段
    /// </summary>
    private void CreateFileData() {
        var tableRule = tableRules.tableRuleList[selectIndex];
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("字段名", GUILayout.Width(70));
        EditorGUILayout.LabelField("数据类型", GUILayout.Width(60));
        EditorGUILayout.LabelField("本地化", GUILayout.Width(40));
        if (isOpenViceKey) {
            EditorGUILayout.LabelField("副Key", GUILayout.Width(40));
        }
        var isOpenMax = tableRule.enumConfigMax != TableConst.EnumConfigMax.None;
        if (isOpenMax) {
            EditorGUILayout.LabelField("最大值", GUILayout.Width(40));
        }
        EditorGUILayout.EndHorizontal();
        List<string> fileList = new List<string>(Enum.GetNames(typeof(TableConst.EnumFieldType)));
        foreach (var field in fieldDic) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Key, GUILayout.Width(70));
            // 数据类型
            var enumFieldTp = fieldDic[field.Key];
            var fieldData = tableRule.fieldList.Find((x) => x.fileldName == field.Key);
            if (fieldData != null) {
                enumFieldTp = fieldData.enumField;
            }else {
                fieldData = new TableRules.TableField() {
                    fileldName = field.Key,
                    enumField = fieldDic[field.Key]
                };
                tableRule.fieldList.Add(fieldData);
            }
            enumFieldTp = (TableConst.EnumFieldType)EditorGUILayout.Popup("",(int)enumFieldTp, fileList.ToArray(), GUILayout.Width(60));
            fieldData.enumField = enumFieldTp;
            // 是否本地化
            GUILayout.Space(10);
            fieldData.isLocalize = EditorGUILayout.Toggle(fieldData.isLocalize, GUILayout.Width(30));
            // 是否副Key
            if (isOpenViceKey) {
                GUILayout.Space(10);
            }
            fieldData.isViceKey = isOpenViceKey && EditorGUILayout.Toggle(fieldData.isViceKey, GUILayout.Width(30));
            // 最大值
            if (isOpenMax) {
                GUILayout.Space(10);
            }
            
            fieldData.isConfigMax = isOpenMax && EditorGUILayout.Toggle(fieldData.isConfigMax, GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Space(15);
        // 主Key
        var keyArray = fieldDic.Keys.ToArray();
        var keyIndex = 0;
        for (int i = 0; i < keyArray.Length; i++) {
            if (keyArray[i] == tableRule.mainKey) {
                keyIndex = i;
                break;
            }
        }
        keyIndex = EditorGUILayout.Popup("主Key：", keyIndex, keyArray);
        if (keyArray[keyIndex] != null) {
            tableRule.mainKey = keyArray[keyIndex];
        }
        // 开启副Key
        isOpenViceKey = EditorGUILayout.Toggle("开启副Key：", isOpenViceKey, GUILayout.Width(30));
        // 最大值
        List<string> maxList = new List<string>(Enum.GetNames(typeof(TableConst.EnumConfigMax)));
        tableRule.enumConfigMax = (TableConst.EnumConfigMax)EditorGUILayout.Popup("获取最大值：",(int)tableRule.enumConfigMax, maxList.ToArray());
    }
    
    /// <summary>
    /// 分析表数据
    /// </summary>
    private void AnalyzeTableData() {
        if (tableRules.tableRuleList.Count <= selectIndex) {
            return;
        }
        if (lastSelectIndex == selectIndex) {
            return;
        }
        lastSelectIndex = selectIndex;
        var tableName = tableRules.tableRuleList[selectIndex].name;
        // 表数据不存
        if (!GetHasFile(tableName)) {
            var message = String.Format($"当前配表{tableName}不存在，是否删除当前表配置数据");
            var isOk = EditorUtility.DisplayDialog("说明介绍", message, "删除", "取消");
            if (isOk) {
                tableRules.tableRuleList.RemoveAt(selectIndex);
                selectIndex = 0;
                EditorUtility.SetDirty(tableRules);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }else {
                selectIndex = 0;
            }
            return;
        }
        // 解析表数据
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = true,  // 如果有表头，设为 true
            IgnoreBlankLines = true,  // 忽略空行
        };
        var filePath = GetFilePath(tableName);
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);
        // 读取 CSV 并解析成动态对象
        var records = csv.GetRecords<dynamic>();
        // 遍历所有行
        foreach (var record in records) {
            // 每行数据,只获取第一行数据
            foreach (var kvp in (IDictionary<string, object>)record) {
                if (!fieldDic.ContainsKey(kvp.Key)) {
                    var enumFieldType = TableConst.EnumFieldType.String;
                    if (int.TryParse(kvp.Value.ToString(), out int _)) {
                        enumFieldType = TableConst.EnumFieldType.Int;
                    }else if (bool.TryParse(kvp.Value.ToString(), out bool _)) {
                        enumFieldType = TableConst.EnumFieldType.Bool;
                    }
                    fieldDic.Add(kvp.Key, enumFieldType);
                }
            }
            break;
        }
    }

    /// <summary>
    /// 判断是否有文件
    /// </summary>
    private bool GetHasFile(string tableName) {
        return File.Exists(GetFilePath(tableName));
    }

    private string GetFilePath(string tableName) {
        var filePath = Path.Combine(TableConst.TableDataPath, tableName + ".csv");
        return filePath;
    }

    #endregion

    #region Toolkit

    /// <summary>
    /// 渲染工具内容
    /// </summary>
    private void RenderToolkitInfo() {
        EditorGUILayout.LabelField("工具：");
        // 查询
        GUILayout.BeginHorizontal();
        findTableName = GUILayout.TextField(findTableName);
        if (GUILayout.Button("查询表", GUILayout.Width(70))) {
            RenderFindConfig();
        }
        GUILayout.EndHorizontal();
        
        // 新增
        GUILayout.BeginHorizontal();
        newTableName = GUILayout.TextField(newTableName);
        if (GUILayout.Button("新增表", GUILayout.Width(70))) {
            RenderNewConfig();
        }
        GUILayout.EndHorizontal();
        
        // 删除
        GUILayout.BeginHorizontal();
        delTableName = GUILayout.TextField(delTableName);
        if (GUILayout.Button("删除表", GUILayout.Width(70))) {
            RenderDelConfig();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
        // 删除
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("删除无用表配置")) {
            RenderDelUnUseConfig();
        }
        if (GUILayout.Button("删除当前表配置")) {
            RenderDelCurConfig();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(6);
    }

    /// <summary>
    /// 查询表
    /// </summary>
    private void RenderFindConfig() {
        if (findTableName.Length <= 0) {
            return;
        }
        var findIdx = tableRules.tableRuleList.FindIndex((x)=> x.name == findTableName);
        if (findIdx < 0) {
            Debug.LogError("查询不到此配表");
            return;
        }
        selectIndex = findIdx;
        findTableName = "";
    }
    
    /// <summary>
    /// 新增表
    /// </summary>
    private void RenderNewConfig() {
        if (newTableName.Length <= 0) {
            return;
        }
        var findData = tableRules.tableRuleList.Find((x) => x.name == newTableName);
        if (findData != null) {
            EditorUtility.DisplayDialog("说明介绍", "当前配表数据已存在", "关闭");
            return;
        }

        if (!GetHasFile(newTableName)) {
            EditorUtility.DisplayDialog("说明介绍", "无法创建，当前配表数据不存在", "关闭");
            return;
        }
        TableRules.TableRule rules = new TableRules.TableRule {
            name = newTableName,
        };
        tableRules.tableRuleList.Add(rules);
        selectIndex = tableRules.tableRuleList.Count - 1;
        newTableName = "";
    }

    /// <summary>
    /// 删除表
    /// </summary>
    private void RenderDelConfig() {
        if (delTableName.Length <= 0) {
            return;
        }
        var findIdx = tableRules.tableRuleList.FindIndex((x) => x.name == delTableName);
        if (findIdx < 0) {
            Debug.LogError("查询不到此配表");
            return;
        }
        tableRules.tableRuleList.RemoveAt(findIdx);
        if (findIdx == selectIndex) {
            selectIndex = 0;
        }
        delTableName = "";
        EditorUtility.SetDirty(tableRules);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("说明介绍", "删除成功", "关闭");
    }

    /// <summary>
    /// 删除当前表信息
    /// </summary>
    private void RenderDelCurConfig() {
        tableRules.tableRuleList.RemoveAt(selectIndex);
        selectIndex = 0;
        EditorUtility.SetDirty(tableRules);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("说明介绍", "删除成功", "关闭");
    }

    /// <summary>
    /// 删除无用表信息
    /// </summary>
    private void RenderDelUnUseConfig() {
        Debug.LogError("写代码");
        EditorUtility.SetDirty(tableRules);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion

    private TableRules tableRules;
    private readonly Dictionary<string, TableConst.EnumFieldType> fieldDic = new();
    private int lastSelectIndex = -1;
    /// <summary>
    /// 所选择的列表
    /// </summary>
    private int selectIndex = 0;
    /// <summary>
    /// 查询的表名称
    /// </summary>
    private string findTableName = "";
    /// <summary>
    /// 新的表名称
    /// </summary>
    private string newTableName = "";
    /// <summary>
    /// 删除的表名称
    /// </summary>
    private string delTableName = "";
    /// <summary>
    /// 是否开启副key
    /// </summary>
    private bool isOpenViceKey = false;
}
