using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DCFrame;
using DCFrame.Utility;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

public class TableRulesTypeEnum : ITableType {
    public bool Init(TableRules.TableRule tableRule) {
        this.tableRule = tableRule;
        try {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var reader = new StreamReader(TableUtil.GetFilePath(tableRule.name), Encoding.UTF8);
            var csv = new CsvReader(reader, config);
            var tableList = csv.GetRecords<TableEnumClass>().ToList();
            foreach (var table in tableList) {
                if (!tableDic.ContainsKey(table.EnumSign)) {
                    tableDic[table.EnumSign] = new TableDicValue() {
                        tableEnumList = new List<TableEnumClass>(),
                        tableTypeEnum = tableRule.enumList.Find(x => x.sign == table.EnumSign),
                    };
                }
                tableDic[table.EnumSign].tableEnumList.Add(table);
            }
            return true;
        }
        catch (Exception ex) {
            tableDic.Clear();
            Debug.LogError($"CSV 解析枚举表失败：{ex.Message}\n{ex.StackTrace}");
            return false;
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
        EditorGUILayout.EndHorizontal();
        
        // 枚举字段 + 枚举本地化
        GUILayout.Space(15);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("本地化", GUILayout.Width(40));
        EditorGUILayout.BeginHorizontal();
        // 是否本地化
        GUILayout.Space(10);
        var data = tableDic.ElementAt(selectIndex);
        var tableTypeEnum = data.Value.tableTypeEnum;
        var isLocalize = tableTypeEnum is { isLocalize: true };
        var isToggle = EditorGUILayout.Toggle(isLocalize , GUILayout.Width(30));
        if (isToggle && !isLocalize) {
            if (tableTypeEnum == null) {
                tableRule.enumList.Add(new TableRules.TableTypeEnum() {
                    sign = data.Key,
                    isLocalize = true
                });
            }else {
                tableTypeEnum.isLocalize = true;
            }
        }else if (!isToggle && isLocalize) {
            tableTypeEnum.isLocalize = false;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndHorizontal();
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
    
    /// <summary>
    /// 处理数据
    /// </summary>
    public void OnDealWithData() {
        if (tableDic.Count <= 0) {
            return;
        }
        collection = LocalizeUtilEditor.GetOrCreateStringCollection(tableRule.name);
        OnDealWithFile();
        OnDealWithLocalize();
    }

#region 创建脚本

    public void OnDealWithFile() {
        string path = Asset.GetTxtPath(TableUtil.TableClassTpEnum, Asset.PrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", tableRule.name);
        var filePath = TableUtil.GetScriptPath(tableRule.name);
        OnDealWithFileField();
        File.WriteAllText(filePath, fileContent);
    }

    private void OnDealWithFileField() {
        string contentInfo = "";
        foreach (var dic in tableDic) {
            // 处理枚举
            string enumInfo = ConfigEnum;
            var tableEnumList = dic.Value.tableEnumList;
            string enumSign = tableEnumList[0].EnumSign.Replace("Enum", "");
            enumInfo = enumInfo.Replace("#SIGNNAME#", tableEnumList[0].EnumName);
            enumInfo = enumInfo.Replace("#ENUMSIGN#", enumSign);
            string enumField = "";
            var addCount = 0;
            foreach (var tableClass in tableEnumList) {
                string enumFieldTp = ConfigEnumValue;
                enumFieldTp = enumFieldTp.Replace("#VALUENAME#", tableClass.ValueName);
                enumFieldTp = enumFieldTp.Replace("#VALUESIGN#", tableClass.ValueSign);
                enumFieldTp = enumFieldTp.Replace("#VALUE#", tableClass.Value);
                addCount += 1;
                enumFieldTp = enumFieldTp.Replace("#DOT#", addCount < tableEnumList.Count ? ",\r\n" : "");
                enumField += enumFieldTp;
            }
            enumInfo = enumInfo.Replace("#ENUM#", enumField);
            enumInfo += "\r\n";
            contentInfo += enumInfo;
            // 处理字典
            if (dic.Value.tableTypeEnum is { isLocalize: true }) {
                string dicInfo = ConfigDic;
                dicInfo = dicInfo.Replace("#ENUMSIGN#", enumSign);
                string dicField = "";
                addCount = 0;
                foreach (var tableClass in tableEnumList) {
                    string dicFieldTp = ConfigDicValue;
                    dicFieldTp = dicFieldTp.Replace("#ENUMSIGN#", enumSign);
                    dicFieldTp = dicFieldTp.Replace("#VALUESIGN#", tableClass.ValueSign);
                    dicFieldTp = dicFieldTp.Replace("#VALUENAME#", $"Localize.GetText(\"{tableRule.name}.{enumSign}.{tableClass.ValueSign}\")");
                    addCount += 1;
                    dicFieldTp = dicFieldTp.Replace("#DOT#", addCount < tableEnumList.Count ? ",\r\n" : "");
                    dicField += dicFieldTp;
                }
                dicInfo = dicInfo.Replace("#DIC#", dicField);
                contentInfo += dicInfo;
                contentInfo += "\r\n";
            }
        }
        fileContent = fileContent.Replace("#CONFIGINFO#", contentInfo);
    }

#endregion

#region 处理本地化数据

    public void OnDealWithLocalize() {
        if (!collection) {
            return;
        }
        var cnDic = LocalizeUtilEditor.GetCollectionCnDic(collection);
        LocalizeUtilEditor.ClearCollection(collection);
        var cnCode = LocalizeConst.LocaleCodeDic[LocalizeConst.LocaleCode.ZhCN];
        foreach (var dic in tableDic) {
            if (dic.Value.tableTypeEnum is { isLocalize: true }) {
                foreach (var tableClass in dic.Value.tableEnumList) {
                    var cnText = tableClass.ValueName;
                    string enumSign = dic.Value.tableEnumList[0].EnumSign.Replace("Enum", "");
                    var key = $"{enumSign}.{tableClass.ValueSign}";
                    foreach (var table in collection.StringTables) {
                        var localeCode = table.LocaleIdentifier.Code;
                        string value = null;
                        if (localeCode == cnCode) {
                            // 中文直接用 Excel
                            value = cnText;
                        }else if (cnDic.TryGetValue(cnText, out var localeDic)) {
                            localeDic.TryGetValue(localeCode, out value);
                        }
                        table.AddEntry(key, value ?? "");
                    }
                }
            }
        }
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
    }

#endregion
    
    private int selectIndex;
    private string findEnumSign;
    private string fileContent;
    private TableRules.TableRule tableRule;
    private StringTableCollection collection;
    private readonly Dictionary<string, TableDicValue> tableDic = new();


    /// <summary>
    /// 字典的key类型值
    /// </summary>
    private class TableDicValue {
        public List<TableEnumClass> tableEnumList;
        public TableRules.TableTypeEnum tableTypeEnum;
    }
    
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
    
#region 模板

private const string ConfigEnum =
    "\t\t/// <summary>\r\n" +
    "\t\t/// #SIGNNAME#\r\n" +
    "\t\t/// </summary>\r\n" +
    "\t\tpublic enum #ENUMSIGN# {\r\n" +
    "#ENUM#\r\n" +
    "\t\t}\r\n";

private const string ConfigEnumValue =
    "\t\t\t/// <summary>\r\n" +
    "\t\t\t/// #VALUENAME#\r\n" +
    "\t\t\t/// </summary>\r\n" +
    "\t\t\t#VALUESIGN# = #VALUE##DOT#";

private const string ConfigDic =
    "\t\tpublic readonly Dictionary<#ENUMSIGN#, Func<string>> #ENUMSIGN#Dic = new() {\r\n" +
    "#DIC#\r\n" +
    "\t\t};\r\n";

private const string ConfigDicValue =
    "\t\t\t[#ENUMSIGN#.#VALUESIGN#] = () => #VALUENAME##DOT#";

#endregion
}
