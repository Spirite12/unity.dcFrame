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
using UnityEngine;

public class TableRulesTypeCommon : ITableType {
    public bool Init(TableRules.TableRule tableRule) {
        Destroy();
        this.tableRule = tableRule;
        return true;
    }
    
    public void Destroy() {
        fieldDic.Clear();
        popupDic.Clear();
        fileContent = "";
    }
    
#region Editor面板显示
    
    /// <summary>
    /// 显示 Editor 的面板UI
    /// </summary>
    public void OnInspectorGUI() {
        AnalyzeTableDataGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableFieldDataGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableKeyDataGUI();
    }

    /// <summary>
    /// 分析数据
    /// </summary>
    private void AnalyzeTableDataGUI() {
        if (fieldDic.Count > 0) {
            return;
        }
        // 解析表数据
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        using var reader = new StreamReader(TableUtil.GetFilePath(tableRule.name), Encoding.UTF8);
        using var csv = new CsvReader(reader, config);
        // 读取 CSV 并解析成动态对象
        var records = csv.GetRecords<dynamic>();
        // 遍历所有行
        foreach (var record in records) {
            // 每行数据,只获取第一行数据
            foreach (var kvp in (IDictionary<string, object>)record) {
                if (!fieldDic.ContainsKey(kvp.Key)) {
                    var enumFieldType = TableUtil.GetEnumFieldType(kvp.Value.ToString());
                    fieldDic.Add(kvp.Key, enumFieldType);
                }
            }
            break;
        }
    }
    
    /// <summary>
    /// 字段的特有数据渲染
    /// </summary>
    private void TableFieldDataGUI() {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("字段名", GUILayout.Width(70));
        EditorGUILayout.LabelField("数据类型", GUILayout.Width(60));
        EditorGUILayout.LabelField("本地化", GUILayout.Width(40));
        EditorGUILayout.LabelField("最大值", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        List<string> fileList = new List<string>(Enum.GetNames(typeof(TableUtil.EnumFieldType)));
        foreach (var field in fieldDic) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Key, GUILayout.Width(70));
            // 数据类型
            var enumFieldTp = fieldDic[field.Key];
            var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == field.Key);
            if (fieldData != null) {
                enumFieldTp = fieldData.enumField;
            }else {
                fieldData = new TableRules.TableField() {
                    fieldName = field.Key,
                    enumField = fieldDic[field.Key]
                };
                tableRule.defaultData.fieldList.Add(fieldData);
            }
            enumFieldTp = (TableUtil.EnumFieldType)EditorGUILayout.Popup("",(int)enumFieldTp, fileList.ToArray(), GUILayout.Width(60));
            fieldData.enumField = enumFieldTp;
            // 是否本地化
            GUILayout.Space(10);
            fieldData.isLocalize = EditorGUILayout.Toggle(fieldData.isLocalize, GUILayout.Width(30));
            // 最大值
            var isHide = fieldData.enumField is TableUtil.EnumFieldType.Bool or TableUtil.EnumFieldType.String;
            if (!isHide) {
                GUILayout.Space(10);
                fieldData.isMaxValue = EditorGUILayout.Toggle(fieldData.isMaxValue, GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();
        }
    }
    
    /// <summary>
    /// 字段的查找函数生成
    /// </summary>
    private void TableKeyDataGUI() {
        EditorGUILayout.LabelField("生成查找数据函数");
        GUILayout.Space(5);
        int curCount = 0;
        var keyArray = fieldDic.Keys.ToArray();
        var noneList = new List<string>();
        var tableFieldList = new List<TableRules.TableField>();
        for (int i = 0; i < keyArray.Length; i++) {
            var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == keyArray[i]);
            if (fieldData != null) {
                if (fieldData.enumMainViceKey != TableUtil.EnumKeyType.None) {
                    curCount += 1;
                    tableFieldList.Add(fieldData);
                }else {
                    noneList.Add(fieldData.fieldName);
                }
            }
        }
        // 显示列表数据
        var typeList = CommonUtil.GetEnumDescriptions<TableUtil.EnumKeyType>();
        foreach (var field in tableFieldList) {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.fieldName, GUILayout.Width(70));
            field.enumMainViceKey = (TableUtil.EnumKeyType)EditorGUILayout.Popup("",(int)field.enumMainViceKey, typeList.ToArray(), GUILayout.Width(100));
            if (field.enumMainViceKey is TableUtil.EnumKeyType.None or TableUtil.EnumKeyType.Single) {
                field.fieldKeyList.Clear();
                EditorGUILayout.EndHorizontal();
            }else if (field.enumMainViceKey is TableUtil.EnumKeyType.Multi or TableUtil.EnumKeyType.MultiWithList) {
                List<string> viceList = new List<string> { "None" };
                for (int i = 0; i < keyArray.Length; i++) {
                    if (keyArray[i] != field.fieldName && !field.fieldKeyList.Contains(keyArray[i])) {
                        viceList.Add(keyArray[i]);
                    }
                }
                if (!popupDic.ContainsKey(field.fieldName) || viceList.Count < popupDic[field.fieldName]) {
                    popupDic[field.fieldName] = 0;
                }
                var viceArray = viceList.ToArray();
                popupDic[field.fieldName] = EditorGUILayout.Popup("", popupDic[field.fieldName], viceArray, GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (popupDic[field.fieldName] != 0) {
                    OnClickAddViceKey(field, viceArray[popupDic[field.fieldName]]);
                    popupDic[field.fieldName] = 0;
                }
                var fieldArray = field.fieldKeyList.ToArray();
                foreach (var name in fieldArray) {
                    Vector2 size = EditorStyles.popup.CalcSize(new GUIContent(name));
                    if (GUILayout.Button(name, GUILayout.Width(size.x))) {
                        OnClickRemoveViceKey(field, name);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
        GUILayout.Space(5);
        // 显示添加数据
        if (noneList.Count > 0) {
            EditorGUILayout.LabelField("添加：");
            EditorGUILayout.BeginHorizontal();
            if (!popupDic.ContainsKey("addKeyIndex") || noneList.Count < popupDic["addKeyIndex"]) {
                popupDic["addKeyIndex"] = 0;
            }
            popupDic["addKeyIndex"] = EditorGUILayout.Popup("", popupDic["addKeyIndex"], noneList.ToArray(), GUILayout.Width(100));
            if (curCount < keyArray.Length) {
                GUILayout.Space(5);
                if (GUILayout.Button("+", GUILayout.Width(30))) {
                    OnClickAddKey(noneList[popupDic["addKeyIndex"]]);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void OnClickAddKey(string filedName) {
        var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == filedName);
        if (fieldData == null) {
            return;
        }
        popupDic["addKeyIndex"] = 0;
        fieldData.enumMainViceKey = TableUtil.EnumKeyType.Single;
        fieldData.fieldKeyList.Clear();
    }
    
    private void OnClickAddViceKey(TableRules.TableField field, string filedName) {
        field.fieldKeyList.Add(filedName);
    }

    private void OnClickRemoveViceKey(TableRules.TableField field, string filedName) {
        foreach (var nameTp in field.fieldKeyList.ToList()) {
            if (nameTp == filedName) {
                field.fieldKeyList.Remove(filedName);
                break;
            }
        }
    }
    
#endregion

#region 创建脚本

        /// <summary>
        /// 分析并创建脚本
        /// </summary>
        public void AnalyzeAndCreateScripts() {
            var filePath = TableUtil.GetFilePath(tableRule.name);
            Dictionary<string, List<string>> tableDataDic = new Dictionary<string, List<string>>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);
            // 读取 CSV 并解析成动态对象
            var records = csv.GetRecords<dynamic>();
            // 提前判断最大值获取
            var fieldDic = tableRule != null ? tableRule.defaultData.fieldList.FindAll((x)=> x.isMaxValue).ToDictionary((x)=>x.fieldName) : new Dictionary<string, TableRules.TableField>();
            // 遍历所有行
            foreach (var record in records) {
                // 每行数据,只获取第一行数据
                foreach (var kvp in (IDictionary<string, object>)record) {
                    if (!tableDataDic.ContainsKey(kvp.Key)) {
                        tableDataDic.Add(kvp.Key, new List<string>());
                    }
                    if (!fieldDic.ContainsKey(kvp.Key)) {
                        continue;
                    }
                    tableDataDic[kvp.Key].Add(kvp.Value.ToString());
                }
                if (fieldDic.Count == 0) {
                    break;
                }
            }
            CreateTableScript(tableDataDic);
        }

    /// <summary>
    /// 创建脚本并写入数据
    /// </summary>
    private void CreateTableScript(Dictionary<string, List<string>> tableDataDic) {
        string path = Asset.GetTxtPath(TableUtil.TableClassTpNormal, Asset.EnumPrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        keyReplaceDic.Add("#SCRIPTNAME#", tableRule.name);
        WriteTableClassField(tableDataDic);
        DealWithConfigDic();
        DealWithConfigMaxSingle(tableDataDic);
        var filePath = TableUtil.GetScriptPath(tableRule.name);
        DealWithCustomSave(filePath);
        foreach (var keyValue in keyReplaceDic) {
            fileContent = fileContent.Replace(keyValue.Key, keyValue.Value);
        }
        File.WriteAllText(filePath, fileContent);
    }

    #region 写入表数据

    /// <summary>
    /// 处理表的类字段
    /// </summary>
    /// <returns></returns>
    private void WriteTableClassField(Dictionary<string, List<string>> tableDataDic) {
        string fieldContent = "";
        var existList = new List<TableRules.TableField>();
        foreach (var field in tableRule.defaultData.fieldList) {
            if (tableDataDic.ContainsKey(field.fieldName)) {
                existList.Add(field);
            }
        }
        int addCount = 0;
        foreach (var field in existList) {
            var fileType = field.enumField.ToString().ToLower();
            string strField = string.Format($"public {fileType} {field.fieldName} {{{{ get; set; }}}}");
            fieldContent += strField;
            addCount += 1;
            if (addCount < existList.Count) {
                fieldContent += "\r\n\t\t";
            }
        }
        keyReplaceDic.Add("#SCRIPTFIELD#", fieldContent);
    }

    #region ConfigDic

    /// <summary>
    /// 处理字典数据
    /// </summary>
    private void DealWithConfigDic() {
        bool hasReplace = false;
        foreach (var field in tableRule.defaultData.fieldList) {
            switch (field.enumMainViceKey) {
                case TableUtil.EnumKeyType.None:
                    break;
                case TableUtil.EnumKeyType.Single:
                    DealWithConfigDicMain(field);
                    hasReplace = true;
                    break;
                case TableUtil.EnumKeyType.Multi:
                    DealWithConfigDicVice(field);
                    hasReplace = true;
                    break;
                case TableUtil.EnumKeyType.MultiWithList:
                    DealWithConfigDicViceList(field);
                    hasReplace = true;
                    break;
            }
        }
        if (!hasReplace) {
            AddKeyReplace("#CONFIGDIC#", "");
            AddKeyReplace("#CONFIGDICINIT#", "");
            AddKeyReplace("#CONFIGMETHODSKEY#", "");
        }
    }

    // 处理 主Key
    private void DealWithConfigDicMain(TableRules.TableField tableField) {
        var mainKeyFieldType = tableField.enumField;
        var key = mainKeyFieldType.ToString().ToLower();
        var fieldName = char.ToLower(tableField.fieldName[0]) + tableField.fieldName.Substring(1);
        var value = tableRule.name + "Class";
        // 字段
        string configDic = ConfigDicTp;
        configDic = configDic.Replace("#KEY#", key);
        configDic = configDic.Replace("#VALUE#", value);
        configDic = configDic.Replace("#NUM#", "");
        configDic = configDic.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGDIC#", configDic);
        // 初始化
        string configInit = ConfigInitDic;
        configInit = configInit.Replace("#KEY#", key);
        configInit = configInit.Replace("#VALUE#", value);
        configInit = configInit.Replace("#MATCH#", "x." + tableField.fieldName);
        configInit = configInit.Replace("#NUM#", "");
        configInit = configInit.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGDICINIT#", configInit);
        // 函数
        string configMethodKey = ConfigMethodsKey;
        configMethodKey = configMethodKey.Replace("#RETURN#", value);
        configMethodKey = configMethodKey.Replace("#NUM#", "");
        configMethodKey = configMethodKey.Replace("#KEY#", fieldName);
        configMethodKey = configMethodKey.Replace("#PARAM#", key + " " + fieldName);
        configMethodKey = configMethodKey.Replace("#ERRER#", fieldName + "：{" + fieldName + "}");
        configMethodKey = configMethodKey.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGMETHODSKEY#", configMethodKey);
    }

    // 处理 主副Key
    private void DealWithConfigDicVice(TableRules.TableField tableField) {
        var nameList = new List<string>() { tableField.fieldName };
        foreach (var field in tableField.fieldKeyList) {
            nameList.Add(field);
        }
        var strKey = "";
        var strFieldName = "";
        var strMatch = "";
        var strParam = "";
        var strError = "";
        var addCount = 0;
        foreach (var name in nameList) {
            var fileName = StringUtil.ToLowerFirstChar(name);
            var enumField = tableRule.defaultData.fieldList.Find((x)=> x.fieldName == name).enumField.ToString().ToLower();
            strFieldName += fileName;
            strKey += enumField;
            strMatch += "x." + name;
            strParam += enumField + " " + fileName;
            strError += name + "：{" + fileName + "}";
            addCount += 1;
            if (addCount < nameList.Count) {
                strKey += ", ";
                strMatch += ", ";
                strFieldName += ", ";
                strParam += ", ";
                strError += ", ";
            }
        }
        var key = String.Format($"({strKey})");
        var value = tableRule.name + "Class";
        // 字段
        var configDic = ConfigDicTp;
        configDic = configDic.Replace("#KEY#", key);
        configDic = configDic.Replace("#VALUE#", value);
        configDic = configDic.Replace("#NUM#", "");
        configDic = configDic.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGDIC#", configDic);
        // 初始化 
        var configInit = ConfigInitDic;
        configInit = configInit.Replace("#KEY#", key);
        configInit = configInit.Replace("#VALUE#", value);
        configInit = configInit.Replace("#MATCH#", strMatch);
        configInit = configInit.Replace("#NUM#", "");
        configInit = configInit.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGDICINIT#", configInit);
        // 函数
        string configMethodKey = ConfigMethodsKey;
        configMethodKey = configMethodKey.Replace("#RETURN#", value);
        configMethodKey = configMethodKey.Replace("#NUM#", "");
        configMethodKey = configMethodKey.Replace("#KEY#", String.Format($"({strFieldName})"));
        configMethodKey = configMethodKey.Replace("#PARAM#", strParam);
        configMethodKey = configMethodKey.Replace("#ERRER#", strError);
        configMethodKey = configMethodKey.Replace("#FIELDNAME#", tableField.fieldName);
        AddKeyReplace("#CONFIGMETHODSKEY#", configMethodKey);
    }
    
    // 处理 主副KeyList
    private void DealWithConfigDicViceList(TableRules.TableField tableField) {
        var nameList = new List<string>() { tableField.fieldName };
        foreach (var field in tableField.fieldKeyList) {
            nameList.Add(field);
        }
        var configDic = "";
        var configInit = "";
        var configMethodKey = "";
        var strKey = "";
        var strMatch = "";
        var strFieldKey = "";
        var strParam = "";
        var strError = "";
        var value = tableRule.name + "Class";
        for (int i = 0; i < nameList.Count; i++) {
            var isEnd = i == nameList.Count - 1;
            var fileName = StringUtil.ToLowerFirstChar(nameList[i]);
            var enumField = tableRule.defaultData.fieldList.Find((x)=> x.fieldName == nameList[i]).enumField.ToString().ToLower();
            strKey += enumField;
            var key = i == 0 ? strKey : String.Format($"({strKey})");
            var strNum = isEnd ? "" : (i + 1).ToString();
            var strReturn = isEnd ? value : String.Format($"List<{value}>");
            strMatch += "x." + nameList[i];
            strFieldKey += fileName;
            strParam += enumField + " " + fileName;
            strError += nameList[i] + "：{" + fileName + "}";
            // 字段
            var configDicTp = ConfigDicTp;
            configDicTp = configDicTp.Replace("#KEY#", key);
            configDicTp = configDicTp.Replace("#VALUE#", strReturn);
            configDicTp = configDicTp.Replace("#NUM#", strNum);
            configDicTp = configDicTp.Replace("#FIELDNAME#", tableField.fieldName);
            configDic += configDicTp;
            // 初始化 
            var configInitTp = isEnd ? ConfigInitDic : ConfigInitDicList;
            configInitTp = configInitTp.Replace("#KEY#", key);
            configInitTp = configInitTp.Replace("#VALUE#", value);
            configInitTp = configInitTp.Replace("#NUM#", strNum);
            configInitTp = configInitTp.Replace("#MATCH#", strMatch);
            configInitTp = configInitTp.Replace("#FIELDNAME#", tableField.fieldName);
            configInit += configInitTp;
            // 函数
            var configMethodKeyTp = ConfigMethodsKey;
            configMethodKeyTp = configMethodKeyTp.Replace("#RETURN#", strReturn);
            configMethodKeyTp = configMethodKeyTp.Replace("#NUM#", strNum);
            configMethodKeyTp = configMethodKeyTp.Replace("#KEY#", i == 0 ? strFieldKey : String.Format($"({strFieldKey})"));
            configMethodKeyTp = configMethodKeyTp.Replace("#PARAM#", strParam);
            configMethodKeyTp = configMethodKeyTp.Replace("#ERRER#", strError);
            configMethodKeyTp = configMethodKeyTp.Replace("#FIELDNAME#", tableField.fieldName);
            configMethodKey += configMethodKeyTp;
            if (!isEnd) {
                configDic += "\r\n\t\t";
                configInit += "\r\n\t\t\t";
                configMethodKey += "\r\n\r\n";
                strKey += ", ";
                strMatch += ", ";
                strFieldKey += ", ";
                strParam += ", ";
                strError += ", ";
            }
        }
        AddKeyReplace("#CONFIGDIC#", configDic);
        AddKeyReplace("#CONFIGDICINIT#", configInit);
        AddKeyReplace("#CONFIGMETHODSKEY#", configMethodKey);
    }
    
    #endregion
    
    #region ConfigMax
    
    // 生成最大值变量
    private void DealWithConfigMaxSingle(Dictionary<string, List<string>> tableDataDic) {
        var strKey = "#CONFIGMAX#";
        var fieldList = tableRule.defaultData.fieldList.FindAll((x)=> x.isMaxValue);
        if (fieldList.Count == 0) {
            keyReplaceDic.Add(strKey, "");
            return;
        }
        var configMax = "";
        var addCount = 0;
        foreach (var field in fieldList) {
            if (field.enumField is TableUtil.EnumFieldType.Bool or TableUtil.EnumFieldType.String) {
                continue;
            }
            var configMaxTp = ConfigMaxTp;
            configMaxTp = configMaxTp.Replace("#FIELDTYPE#", field.enumField.ToString().ToLower());
            configMaxTp = configMaxTp.Replace("#FIELDNAME#", field.fieldName);
            decimal maxValue = 0;
            foreach (var value in tableDataDic[field.fieldName]) {
                var strValueTp = value;
                if (strValueTp.StartsWith(TableUtil.ScientificSign)) {
                    strValueTp = strValueTp.Replace(TableUtil.ScientificSign, "");
                }
                var valueTp = decimal.Parse(strValueTp, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (maxValue < valueTp) {
                    maxValue = valueTp;
                }
            }
            var strValue = maxValue.ToString(CultureInfo.InvariantCulture);
            if (field.enumField == TableUtil.EnumFieldType.Float) {
                strValue += "f";
            }
            configMaxTp = configMaxTp.Replace("#VALUE#", strValue);
            configMax += configMaxTp;
            addCount += 1;
            if (addCount < fieldList.Count) {
                configMax += "\r\n\t\t";
            }
        }
        if (configMax == "") {
            keyReplaceDic.Add(strKey, "");
            return;
        }
        configMax = "\r\n\t\t" + configMax + "\r\n";
        keyReplaceDic.Add(strKey, configMax);
    }

    #endregion

    #region CustomSave
    
    /// <summary>
    /// 处理自定义的代码保存
    /// </summary>
    private void DealWithCustomSave(string filePath) {
        var strKey = "#CONFIGCUSTOM#";
        if (!File.Exists(filePath)) {
            keyReplaceDic.Add(strKey, tableRule.name);
            return;
        }
        var csFile = File.ReadAllText(filePath);
        var strSave = StringUtil.StringGetMiddle(csFile, "#region 自定义内容\r\n", "\r\n        #endregion");
        keyReplaceDic.Add(strKey, strSave);
    }
    
    #endregion

    private void AddKeyReplace(string strKey, string strValue) {
        if (!keyReplaceDic.TryAdd(strKey, strValue)) {
            switch (strKey) {
                case "#CONFIGDIC#":
                    keyReplaceDic[strKey] += "\r\n\t\t";
                    break;
                case "#CONFIGDICINIT#":
                    keyReplaceDic[strKey] += "\r\n\t\t\t";
                    break;
                case "#CONFIGMETHODSKEY#":
                    keyReplaceDic[strKey] += "\r\n\r\n";
                    break;
            }
            keyReplaceDic[strKey] += strValue;
        }
    }
    
    #endregion
    
#endregion

    private static string fileContent;
    private TableRules.TableRule tableRule;
    private readonly Dictionary<string, TableUtil.EnumFieldType> fieldDic = new();
    /// <summary>
    /// 替换的字典
    /// </summary>
    private readonly Dictionary<string, string> keyReplaceDic = new Dictionary<string, string>();
    /// <summary>
    /// 记录多健查找的index值
    /// </summary>
    private readonly Dictionary<string, int> popupDic = new Dictionary<string, int>();
    
#region 模板
    private const string ConfigMaxTp = "public const #FIELDTYPE# Max#FIELDNAME# = #VALUE#;";
    private const string ConfigDicTp = "private readonly Dictionary<#KEY#, #VALUE#> keyDic#FIELDNAME##NUM#;";
    private const string ConfigInitDic = "keyDic#FIELDNAME##NUM# = LoadTableDic<#KEY#, #VALUE#>(x => (#MATCH#));";
    private const string ConfigInitDicList = "keyDic#FIELDNAME##NUM# = LoadTableDicList<#KEY#, #VALUE#>(x => (#MATCH#));";
    private const string ConfigMethodsKey = 
        "\t\tpublic #RETURN# GetConfigBy#FIELDNAME#(#PARAM#, bool showTips = true) {\r\n" +
        "\t\t\tif (keyDic#FIELDNAME##NUM#.ContainsKey(#KEY#)) {\r\n" +
        "\t\t\t\treturn keyDic#FIELDNAME##NUM#[#KEY#];\r\n" +
        "\t\t\t}\r\n" +
        "\t\t\tif (showTips) {\r\n" +
        "\t\t\t\tDebug.LogError(String.Format($\"查找表：{CsvPath} 失败, #ERRER#\"));\r\n" +
        "\t\t\t}\r\n" +
        "\t\t\treturn null;\r\n" +
        "\t\t}";
#endregion
}
