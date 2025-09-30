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
        var files = Directory.GetFiles(TableConst.TableDataPath);
        if (files.Length <= 0) {
            Debug.LogWarning("暂无配表数据");
            return;
        }
        foreach (var file in files) {
            if (file.EndsWith(".meta")) {
                continue;
            }
            Dictionary<string, List<string>> tableDataDic = new Dictionary<string, List<string>>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                HasHeaderRecord = true,  // 如果有表头，设为 true
                IgnoreBlankLines = true,  // 忽略空行
            };
            using var reader = new StreamReader(file, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);
            // 读取 CSV 并解析成动态对象
            var records = csv.GetRecords<dynamic>();
            var fileName = Path.GetFileNameWithoutExtension(file);
            tableRule = tableRules.tableRuleList.Find((x) => x.name == fileName);
            // 提前判断最大值获取
            var fieldDic = tableRule != null ? tableRule.fieldList.FindAll((x)=> x.configMaxValue > 0).ToDictionary((x)=>x.fieldName) : new Dictionary<string, TableRules.TableField>();
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
            CreateTableScript(fileName, tableDataDic);
        }
    }

    /// <summary>
    /// 创建脚本并写入数据
    /// </summary>
    private static void CreateTableScript(string fileName, Dictionary<string, List<string>> tableDataDic) {
        string path = Asset.GetTxtPath(TableClassFileName, Asset.EnumPrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", fileName);
        fileContent = fileContent.Replace("#SCRIPTFIELD#", WriteTableClassField(tableDataDic));
        DealWithConfigDic();
        DealWithConfigMax(tableDataDic);
        var filePath = TableScriptPath + "\\Table" + fileName + ".cs";
        DealWithCustomSave(filePath);
        File.WriteAllText(filePath, fileContent);
    }

    #region 写入表数据

    /// <summary>
    /// 处理表的类字段
    /// </summary>
    /// <returns></returns>
    private static string WriteTableClassField(Dictionary<string, List<string>> tableDataDic) {
        string fieldContent = "";
        if (tableRule != null) {
            var existList = new List<TableRules.TableField>();
            foreach (var field in tableRule.fieldList) {
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
        }
        return fieldContent;
    }

    #region ConfigDic

    /// <summary>
    /// 处理字典数据
    /// </summary>
    private static void DealWithConfigDic(){
        if (tableRule == null) {
            return;
        }
        switch (tableRule.enumViceKey) {
            case TableConst.EnumViceKey.None:
                DealWithConfigDicNone();
                break;
            case TableConst.EnumViceKey.Vice:
                DealWithConfigDicVice();
                break;
            case TableConst.EnumViceKey.ViceWithList:
                DealWithConfigDicViceList();
                break;
        }
    }

    // 处理 主Key
    private static void DealWithConfigDicNone() {
        var mainKeyFieldType = tableRule.fieldList.Find((x)=> x.fieldName == tableRule.mainKey).enumField;
        var key = mainKeyFieldType.ToString().ToLower();
        var fieldName = tableRule.mainKey.ToLower();
        var value = "Table" + tableRule.name + "Class";
        // 字段
        string configDic = ConfigDicTp;
        configDic = configDic.Replace("#KEY#", key);
        configDic = configDic.Replace("#VALUE#", value);
        configDic = configDic.Replace("#NUM#", "");
        fileContent = fileContent.Replace("#CONFIGDIC#", configDic);
        // 初始化
        string configInit = ConfigInitDic;
        configInit = configInit.Replace("#KEY#", key);
        configInit = configInit.Replace("#VALUE#", value);
        configInit = configInit.Replace("#MATCH#", "x." + tableRule.mainKey);
        configInit = configInit.Replace("#NUM#", "");
        fileContent = fileContent.Replace("#CONFIGDICINIT#", configInit);
        // 函数
        string configMethodKey = ConfigMethodsKey;
        configMethodKey = configMethodKey.Replace("#RETURN#", value);
        configMethodKey = configMethodKey.Replace("#NUM#", "");
        configMethodKey = configMethodKey.Replace("#KEY#", fieldName);
        configMethodKey = configMethodKey.Replace("#PARAM#", key + " " + fieldName);
        configMethodKey = configMethodKey.Replace("#ERRER#", fieldName + "：{" + fieldName + "}");
        fileContent = fileContent.Replace("#CONFIGMETHODSKEY#", configMethodKey);
    }

    // 处理 主副Key
    private static void DealWithConfigDicVice() {
        List<TableRules.TableField> fieldList = new List<TableRules.TableField>();
        fieldList.Add(tableRule.fieldList.Find((x) => x.fieldName == tableRule.mainKey));
        var fields = tableRule.fieldList.FindAll((x) => x.viceKeyValue > 0);
        fields.Sort((x, y)=>x.viceKeyValue > y.viceKeyValue ? 1 : -1);
        for (int i = 0; i < fields.Count; i++) {
            fieldList.Add(fields[i]);
        }
        var strKey = "";
        var strFieldName = "";
        var strMatch = "";
        var strParam = "";
        var strError = "";
        var addCount = 0;
        foreach (var field in fieldList) {
            var fileName = StringUtil.ToLowerFirstChar(field.fieldName);
            var enumField = field.enumField.ToString().ToLower();
            strFieldName += fileName;
            strKey += enumField;
            strMatch += "x." + field.fieldName;
            strParam += enumField + " " + fileName;
            strError += field.fieldName + "：{" + fileName + "}";
            addCount += 1;
            if (addCount < fieldList.Count) {
                strKey += ", ";
                strMatch += ", ";
                strFieldName += ", ";
                strParam += ", ";
                strError += ", ";
            }
        }
        var key = String.Format($"({strKey})");
        var value = "Table" + tableRule.name + "Class";
        // 字段
        var configDic = ConfigDicTp;
        configDic = configDic.Replace("#KEY#", key);
        configDic = configDic.Replace("#VALUE#", value);
        configDic = configDic.Replace("#NUM#", "");
        fileContent = fileContent.Replace("#CONFIGDIC#", configDic);
        // 初始化 
        var configInit = ConfigInitDic;
        configInit = configInit.Replace("#KEY#", key);
        configInit = configInit.Replace("#VALUE#", value);
        configInit = configInit.Replace("#MATCH#", strMatch);
        fileContent = fileContent.Replace("#CONFIGDICINIT#", configInit);
        // 函数
        string configMethodKey = ConfigMethodsKey;
        configMethodKey = configMethodKey.Replace("#RETURN#", value);
        configMethodKey = configMethodKey.Replace("#NUM#", "");
        configMethodKey = configMethodKey.Replace("#KEY#", String.Format($"({strFieldName})"));
        configMethodKey = configMethodKey.Replace("#PARAM#", strParam);
        configMethodKey = configMethodKey.Replace("#ERRER#", strError);
        fileContent = fileContent.Replace("#CONFIGMETHODSKEY#", configMethodKey);
    }
    
    // 处理 主副KeyList
    private static void DealWithConfigDicViceList() {
        List<TableRules.TableField> fieldList = new List<TableRules.TableField>();
        fieldList.Add(tableRule.fieldList.Find((x) => x.fieldName == tableRule.mainKey));
        var fields = tableRule.fieldList.FindAll((x) => x.viceKeyValue > 0);
        fields.Sort((x, y)=>x.viceKeyValue < y.viceKeyValue ? 1 : -1);
        for (int i = 0; i < fields.Count; i++) {
            fieldList.Add(fields[i]);
        }
        var configDic = "";
        var configInit = "";
        var configMethodKey = "";
        var strKey = "";
        var strMatch = "";
        var strFieldKey = "";
        var strParam = "";
        var strError = "";
        var value = "Table" + tableRule.name + "Class";
        for (int i = 0; i < fieldList.Count; i++) {
            var isEnd = i == fieldList.Count - 1;
            var fileName = StringUtil.ToLowerFirstChar(fieldList[i].fieldName);
            var enumField = fieldList[i].enumField.ToString().ToLower();
            strKey += enumField;
            var key = i == 0 ? strKey : String.Format($"({strKey})");
            var strNum = isEnd ? "" : (i + 1).ToString();
            var strReturn = isEnd ? value : String.Format($"List<{value}>");
            strMatch += "x." + fieldList[i].fieldName;
            strFieldKey += fileName;
            strParam += enumField + " " + fileName;
            strError += fieldList[i].fieldName + "：{" + fileName + "}";
            // 字段
            var configDicTp = ConfigDicTp;
            configDicTp = configDicTp.Replace("#KEY#", key);
            configDicTp = configDicTp.Replace("#VALUE#", strReturn);
            configDicTp = configDicTp.Replace("#NUM#", strNum);
            configDic += configDicTp;
            // 初始化 
            var configInitTp = isEnd ? ConfigInitDic : ConfigInitDicList;
            configInitTp = configInitTp.Replace("#KEY#", key);
            configInitTp = configInitTp.Replace("#VALUE#", value);
            configInitTp = configInitTp.Replace("#NUM#", strNum);
            configInitTp = configInitTp.Replace("#MATCH#", strMatch);
            configInit += configInitTp;
            // 函数
            var configMethodKeyTp = ConfigMethodsKey;
            configMethodKeyTp = configMethodKeyTp.Replace("#RETURN#", strReturn);
            configMethodKeyTp = configMethodKeyTp.Replace("#NUM#", strNum);
            configMethodKeyTp = configMethodKeyTp.Replace("#KEY#", i == 0 ? strFieldKey : String.Format($"({strFieldKey})"));
            configMethodKeyTp = configMethodKeyTp.Replace("#PARAM#", strParam);
            configMethodKeyTp = configMethodKeyTp.Replace("#ERRER#", strError);
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
        fileContent = fileContent.Replace("#CONFIGDIC#", configDic);
        fileContent = fileContent.Replace("#CONFIGDICINIT#", configInit);
        fileContent = fileContent.Replace("#CONFIGMETHODSKEY#", configMethodKey);
    }
    
    #endregion
    
    #region ConfigMax
    
    /// <summary>
    /// 处理最大值数据
    /// </summary>
    private static void DealWithConfigMax(Dictionary<string, List<string>> tableDataDic){
        if (tableRule == null) {
            return;
        }

        switch (tableRule.enumConfigMax) {
            case TableConst.EnumConfigMax.None:
                fileContent = fileContent.Replace("#CONFIGMAX#", "");
                break;
            case TableConst.EnumConfigMax.Single:
                DealWithConfigMaxSingle(tableDataDic);
                break;
        }
    }
    
    // 生成最大值变量
    private static void DealWithConfigMaxSingle(Dictionary<string, List<string>> tableDataDic) {
        var fieldList = tableRule.fieldList.FindAll((x)=> x.configMaxValue > 0);
        if (fieldList.Count == 0) {
            fileContent = fileContent.Replace("#CONFIGMAX#", "");
            return;
        }
        var configMax = "";
        var addCount = 0;
        foreach (var field in fieldList) {
            if (field.enumField is TableConst.EnumFieldType.Bool or TableConst.EnumFieldType.String) {
                continue;
            }
            var configMaxTp = ConfigMaxTp;
            configMaxTp = configMaxTp.Replace("#FIELDTYPE#", field.enumField.ToString().ToLower());
            configMaxTp = configMaxTp.Replace("#FIELDNAME#", field.fieldName);
            decimal maxValue = 0;
            foreach (var value in tableDataDic[field.fieldName]) {
                var valueTp = decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                if (maxValue < valueTp) {
                    maxValue = valueTp;
                }
            }
            var strValue = maxValue.ToString(CultureInfo.InvariantCulture);
            if (field.enumField == TableConst.EnumFieldType.Float) {
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
            fileContent = fileContent.Replace("#CONFIGMAX#", "");
            return;
        }
        configMax = "\r\n\t\t" + configMax + "\r\n";
        fileContent = fileContent.Replace("#CONFIGMAX#", configMax);
    }

    #endregion

    /// <summary>
    /// 处理自定义的代码保存
    /// </summary>
    private static void DealWithCustomSave(string filePath) {
        var csFile = File.ReadAllText(filePath);
        var strSave = StringUtil.StringGetMiddle(csFile, "#region 自定义内容\r\n", "\r\n        #endregion");
        fileContent = fileContent.Replace("#CONFIGCUSTOM#", strSave);
    }

    #endregion

    private static string fileContent;
    private static TableRules.TableRule tableRule;
    private static TableRules tableRules;
    /// <summary>
    /// 表类的模板文件
    /// </summary>
    private const string TableClassFileName = "TableClassTp";
    /// <summary>
    /// 表脚本路径
    /// </summary>
    private const string TableScriptPath = "Assets/Game/Scripts/Table";

#region 模板
    private const string ConfigMaxTp = "public const #FIELDTYPE# Max#FIELDNAME# = #VALUE#;";
    private const string ConfigDicTp = "private readonly Dictionary<#KEY#, #VALUE#> keyDic#NUM#;";
    private const string ConfigInitDic = "keyDic#NUM# = LoadTableDic<#KEY#, #VALUE#>(x => (#MATCH#));";
    private const string ConfigInitDicList = "keyDic#NUM# = LoadTableDicList<#KEY#, #VALUE#>(x => (#MATCH#));";
    private const string ConfigMethodsKey = 
        "\t\t/// <summary>\r\n" +
        "\t\t/// 找表数据\r\n" +
        "\t\t/// </summary>\r\n" +
        "\t\tpublic #RETURN# GetConfigDataByKey(#PARAM#, bool showTips = true) {\r\n" +
        "\t\t\tif (keyDic#NUM#.ContainsKey(#KEY#)) {\r\n" +
        "\t\t\t\treturn keyDic#NUM#[#KEY#];\r\n" +
        "\t\t\t}\r\n" +
        "\t\t\tif (showTips) {\r\n" +
        "\t\t\t\tDebug.LogError(String.Format($\"查找表：{CsvPath} 失败, #ERRER#\"));\r\n" +
        "\t\t\t}\r\n" +
        "\t\t\treturn null;\r\n" +
        "\t\t}";
#endregion
}
