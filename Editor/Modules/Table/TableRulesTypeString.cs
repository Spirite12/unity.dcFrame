using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DCFrame;
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

    public void OnInspectorGUI() {}

#region 创建脚本

    /// <summary>
    /// 创建并设置脚本
    /// </summary>
    public void AnalyzeAndCreateScripts() {
        if (tableList.Count <= 0) {
            return;
        }
        string path = Asset.GetTxtPath(TableUtil.TableClassTpString, Asset.EnumPrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", tableRule.name);
        var filePath = TableUtil.GetScriptPath(tableRule.name);
        DealWithField();
        File.WriteAllText(filePath, fileContent);
    }

    private void DealWithField() {
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

    private string fileContent;
    private TableRules.TableRule tableRule;
    private List<TableStringClass> tableList = new();

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
