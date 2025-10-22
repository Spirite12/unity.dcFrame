using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DCFrame;
using DCFrame.Utility;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using FileUtil = DCFrame.Utility.FileUtil;

[CustomEditor(typeof(TableRules))]
public class TableRulesEditor : Editor {
    private void OnEnable() {
        tableRules = AssetDatabase.LoadAssetAtPath<TableRules>(Asset.GetAssetPath("Table/TableRules", Asset.EnumPrefixPath.Settings));
        if (tableRules && tableRules.tableRuleList.Count > 0) {
            tableRules.tableRuleList.Sort((x, y) => string.Compare(x.name, y.name, StringComparison.OrdinalIgnoreCase));
        }
    }

    private void OnDisable() {
        if (tableRules) {
            SaveRuleData();
            tableRules = null;
            if (tableType != null) {
                tableType.Destroy();
                tableType = null;
            }
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
        RenderTableToolkit();
        InitTableType();
        var tableName = tableRules.tableRuleList[selectIndex].name;
        if (tableType == null || FileUtil.IsFileLocked(TableUtil.GetFilePath(tableName))) return;
        if (AnalyzeTableData()) {
            CreateFileData();
        }
    }

    private void InitTableType() {
        if (tableRules.tableRuleList.Count <= selectIndex) {
            return;
        }
        var tableRule = tableRules.tableRuleList[selectIndex];
        
        if (lastSelectIndex == selectIndex && lastEnumTableType == tableRule.enumTableType) {
            return;
        }
        lastSelectIndex = selectIndex;
        lastEnumTableType = tableRule.enumTableType;
        tableType?.Destroy();
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
            default:
                tableType = null;
                break;
        }
        tableType?.Init(tableRules.tableRuleList[selectIndex]);
    }

    /// <summary>
    /// 分析表格数据
    /// </summary>
    private bool AnalyzeTableData() {
        var tableName = tableRules.tableRuleList[selectIndex].name;
        // 表数据不存
        if (!File.Exists(TableUtil.GetFilePath(tableName))) {
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
            return false;
        }
        return true;
    }

    /// <summary>
    /// 创建表格内容数据
    /// </summary>
    private void CreateFileData() {
        tableType.OnInspectorGUI();
    }

    /// <summary>
    /// 渲染表工具信息
    /// </summary>
    private void RenderTableToolkit() {
        GUILayout.Space(5);
        if (GUILayout.Button("一键导表")) {
            TableEditor.PackageConfig();
        }
        
        GUILayout.Space(16);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("表列表: ", GUILayout.Width(120));
        List<string> nameList = new List<string>();
        foreach (var rule in tableRules.tableRuleList) {
            nameList.Add(rule.name.Replace("Table", ""));
        }
        selectIndex = EditorGUILayout.Popup(selectIndex, nameList.ToArray());
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("表类型: ", GUILayout.Width(120));
        var tableRule = tableRules.tableRuleList[selectIndex];
        var typeList = CommonUtil.GetEnumDescriptions<TableUtil.EnumTableType>();
        tableRule.enumTableType = (TableUtil.EnumTableType)EditorGUILayout.Popup((int)tableRule.enumTableType, typeList.ToArray());
        EditorGUILayout.EndHorizontal();
    }

    private void OpenCsvFile() {
        var tableName = tableRules.tableRuleList[selectIndex].name;
        string filePath = TableUtil.GetFilePath(tableName);
        Process.Start(new ProcessStartInfo {
            FileName = filePath,
            UseShellExecute = true // 必须为 true 才能用默认程序打开
        });
    }
    
    private void OpenScriptFile() {
        var tableName = tableRules.tableRuleList[selectIndex].name;
        string filePath = TableUtil.GetScriptPath(tableName);
        CommonUtil.OpenScript(filePath);
    }

    /// <summary>
    /// 保存配置
    /// </summary>
    private void SaveRuleData() {
        EditorUtility.SetDirty(tableRules);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    #endregion

    #region Toolkit

    /// <summary>
    /// 渲染工具内容
    /// </summary>
    private void RenderToolkitInfo() {
        EditorGUILayout.LabelField("工具：");
        RenderBtnTips();
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

        // 删除
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("删除无用表")) {
            RenderDelUnUseConfig();
        }
        if (GUILayout.Button("删除表配置")) {
            RenderDelCurConfig();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("打开表代码")) {
            OpenScriptFile();
        }
        if (GUILayout.Button("打开表CSV")) {
            OpenCsvFile();
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("保存表配置")) {
            SaveRuleData();
        }
        if (GUILayout.Button("刷新表配置")) {
            RenderRefreshCurConfig();
        }
        EditorGUILayout.EndHorizontal();
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

        if (!File.Exists(TableUtil.GetFilePath(newTableName))) {
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
        string content = "";
        List<TableRules.TableRule> tableRuleList = new List<TableRules.TableRule>();
        List<int> ruleIndexList = new List<int>();
        for (var i = 0; i < tableRules.tableRuleList.Count; i++) {
            var tableRule = tableRules.tableRuleList[i];
            if (!File.Exists(TableUtil.GetFilePath(tableRule.name))) {
                tableRuleList.Add(tableRule);
                ruleIndexList.Add(i);
            }
        }
        if (tableRuleList.Count <= 0) {
            EditorUtility.DisplayDialog("删除表配置名", "查询不到无用表配置", "确定");
            return;
        }
        foreach (var rule in tableRuleList) {
            content += rule.name + "\n";
        }
        if (EditorUtility.DisplayDialog("删除表配置名", content, "确定")) {
            for (int i = ruleIndexList.Count - 1; i >= 0; i--) {
                var index = ruleIndexList[i];
                tableRules.tableRuleList.RemoveAt(index);
            }
            SaveRuleData();
        }
    }

    /// <summary>
    /// 刷新表配置
    /// </summary>
    private void RenderRefreshCurConfig() {
        if (tableType == null) {
            return;
        }
        tableType.Destroy();
        tableType.Init(tableRules.tableRuleList[selectIndex]);
    }
    
    /// <summary>
    /// 渲染提示说明
    /// </summary>
    private void RenderBtnTips() {
        if (GUILayout.Button("提示说明")) {
            string str = "";
            str += String.Format($"CSV配表不允许使用科学计数法，若要使用大数字，则在前方新增 {TableUtil.ScientificSign} 字符\n\n");
            str += "删除无用表配置：\n依次查找配置对应的表文件，若查询无果则删除\n\n";
            str += String.Format($"副Key：\n{nameof(TableUtil.EnumViceKey.Vice)}：生成由主Key和副key的相关表代码\n{nameof(TableUtil.EnumViceKey.ViceWithList)}：递增生成由主Key到多副key的相关表代码\n\n");
            str += String.Format($"获取最大值：\n{nameof(TableUtil.EnumConfigMax.Single)}：获取当前表字段数据内最大值并构造字段\n\n");
            EditorUtility.DisplayDialog("说明介绍", str, "关闭");
        }
        GUILayout.Space(5);
    }

    #endregion

    private TableRules tableRules;
    private ITableType tableType;
    private int lastSelectIndex = -1;
    private TableUtil.EnumTableType lastEnumTableType;
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
}
