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

public class TableRulesTypeEnum : ITableType {
    public void Init(TableRules.TableRule tableRule) {
        this.tableRule = tableRule;
        try {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var reader = new StreamReader(TableUtil.GetFilePath(tableRule.name), Encoding.UTF8);
            var csv = new CsvReader(reader, config);
            var tableList = csv.GetRecords<TableEnumClass>().ToList();
            foreach (var table in tableList) {
                if (!tableDic.ContainsKey(table.EnumSign)) {
                    tableDic[table.EnumSign] = new List<TableEnumClass>();
                }
                tableDic[table.EnumSign].Add(table);
            }
        }
        catch (Exception ex) {
            tableDic.Clear();
            Debug.LogError($"CSV 解析枚举表失败：{ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Destroy() {
        tableDic.Clear();
    }

    public void OnInspectorGUI() {
        GUILayout.Space(12);
        GUILayout.BeginHorizontal();
        findEnumSign = GUILayout.TextField(findEnumSign);
        if (GUILayout.Button("查询枚举", GUILayout.Width(70))) {
            RenderFindEnumIndex();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("枚举列表: ", GUILayout.Width(120));
        List<string> nameList = new List<string>();
        foreach (var table in tableDic) {
            nameList.Add(table.Key);
        }
        selectIndex = EditorGUILayout.Popup(selectIndex, nameList.ToArray());
        var data = tableDic.ElementAt(selectIndex);
        tableTypeEnum = tableRule.enumList.Find(x => x.sign == data.Key);
        EditorGUILayout.EndHorizontal();
        
        // 枚举字段 + 枚举本地化
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("字段名", GUILayout.Width(70));
        EditorGUILayout.LabelField("本地化", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        foreach (var table in data.Value) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(table.ValueSign, GUILayout.Width(70));
            // 是否本地化
            GUILayout.Space(10);
            var isLocalize = tableTypeEnum != null && tableTypeEnum.valueSignList.Contains(table.ValueSign);
            var isToggle = EditorGUILayout.Toggle(isLocalize , GUILayout.Width(30));
            if (isToggle && !isLocalize) {
                if (tableTypeEnum == null) {
                    tableRule.enumList.Add(new TableRules.TableTypeEnum() {
                        sign = table.EnumSign,
                        valueSignList = new List<string>() {
                            table.ValueSign
                        }
                    });
                    
                }else {
                    tableTypeEnum.valueSignList.Add(table.ValueSign);
                }
            }else if (!isToggle && isLocalize) {
                tableTypeEnum.valueSignList.Remove(table.ValueSign);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 获取查找的枚举索引
    /// </summary>
    private void RenderFindEnumIndex() {
        var index = 0;
        bool isFind = false;
        foreach (var table in tableDic) {
            if (string.Equals(table.Key, findEnumSign, StringComparison.CurrentCultureIgnoreCase)) {
                selectIndex = index;
                isFind = true;
                break;
            }
            index += 1;
        }
        if (!isFind) {
            Debug.LogError("查询枚举标识失败：" + findEnumSign);
        }
    }


    #region 创建脚本

    public void AnalyzeAndCreateScripts() {
        if (tableDic.Count <= 0) {
            return;
        }
        string path = Asset.GetTxtPath(TableUtil.TableClassTpEnum, Asset.EnumPrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", tableRule.name);
        var filePath = TableUtil.GetScriptPath(tableRule.name);
        DealWithField();
        File.WriteAllText(filePath, fileContent);
    }
    
    private void DealWithField() {
        fileContent = fileContent.Replace("#CONFIGINFO#", "");
    }

    #endregion
    
    private int selectIndex;
    private string findEnumSign;
    private string fileContent;
    private TableRules.TableTypeEnum tableTypeEnum;
    private TableRules.TableRule tableRule;
    private readonly Dictionary<string, List<TableEnumClass>> tableDic = new();

    /// <summary>
    /// 枚举类
    /// </summary>
    private class TableEnumClass {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 枚举标识
        /// </summary>
        public string EnumSign { get; set; }
        /// <summary>
        /// 枚举名称
        /// </summary>
        public string EnumName { get; set; }
        /// <summary>
        /// 枚举值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 枚举值标识
        /// </summary>
        public string ValueSign { get; set; }
        /// <summary>
        /// 枚举值名称
        /// </summary>
        public string ValueName { get; set; }
    }
}
