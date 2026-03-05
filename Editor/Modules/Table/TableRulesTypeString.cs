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

public class TableRulesTypeString : ITableType {
    /// <summary>
    /// 分析表数据
    /// </summary>
    public bool Init(TableRules.TableRule tableRule) {
        this.tableRule = tableRule;
        try {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var reader = new StreamReader(TableUtil.GetFilePath(tableRule.name), Encoding.UTF8);
            var csv = new CsvReader(reader, config);
            tableList = csv.GetRecords<TableStringClass>().ToList();
            return true;
        }
        catch (Exception ex) {
            tableList.Clear();
            Debug.LogError($"CSV 解析文本表失败：{ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public void Destroy() {
        tableList.Clear();
    }

    public void OnInspectorGUI() {
        GUILayout.Space(10);
        GUILayout.Label("Sign 的命名规范是：功能名.标识名，如：Main.Title");
    }
    
    /// <summary>
    /// 处理数据
    /// </summary>
    public void OnDealWithData() {
        if (tableList.Count <= 0) {
            return;
        }
        collection = LocalizeUtil.GetOrCreateCollection(tableRule.name);
        OnDealWithFile();
        OnDealWithLocalize();
    }

#region 创建脚本

    public void OnDealWithFile() {
        string path = Asset.GetTxtPath(TableUtil.TableClassTpString, Asset.EnumPrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", tableRule.name);
        var filePath = TableUtil.GetScriptPath(tableRule.name);
        OnDealWithFileField();
        File.WriteAllText(filePath, fileContent);
    }

    private void OnDealWithFileField() {
        string configField = "";
        foreach (var tableData in tableList) {
            string configFieldTp = ConfigField;
            configFieldTp = configFieldTp.Replace("#STRING#", tableData.String);
            configFieldTp = configFieldTp.Replace("#SIGN#", tableData.Sign);
            configFieldTp = configFieldTp.Replace("#TABLENAME#", tableRule.name);
            configField += configFieldTp;
        }
        fileContent = fileContent.Replace("#CONFIGFIELD#", configField);
    }
    
#endregion

#region 处理本地化数据

    public void OnDealWithLocalize() {
        if (!collection) {
            return;
        }
        var cnDic = LocalizeUtil.GetCollectionCnDic(collection);
        LocalizeUtil.ClearCollection(collection);
        var cnCode = LocalizeConst.LocaleCodeDic[LocalizeConst.EnumLocaleCode.ZhCN];
        foreach (var tableData in tableList) {
            var cnText = tableData.String;
            var key = $"{tableData.Sign}";
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
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssets();
    }

#endregion

    private string fileContent;
    private TableRules.TableRule tableRule;
    private List<TableStringClass> tableList = new();
    private StringTableCollection collection;

    /// <summary>
    /// 文本类
    /// </summary>
    private class TableStringClass {
        /// <summary>
        /// 标识
        /// </summary>
        public string Sign { get; set; }
        /// <summary>
        /// 字符串值
        /// </summary>
        public string String { get; set; }
    }
    
#region 模板
    
    private const string ConfigField =
        "\t\t/// <summary>\r\n" +
        "\t\t/// #STRING#\r\n" +
        "\t\t/// </summary>\r\n" +
        "\t\tpublic string #SIGN# = Localize.GetText(\"#TABLENAME#.#SIGN#\");\r\n";
    
#endregion
}
