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

public class TableRulesTypeConst : ITableType {
    /// <summary>
    /// 分析表数据
    /// </summary>
    public void Init(TableRules.TableRule tableRule) {
        this.tableRule = tableRule;
        try {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture);
            using var reader = new StreamReader(TableUtil.GetFilePath(tableRule.name), Encoding.UTF8);
            var csv = new CsvReader(reader, config);
            tableList = csv.GetRecords<TableConstClass>().ToList();
        }
        catch (Exception ex) {
            tableList.Clear();
            Debug.LogError($"CSV 解析常量表失败：{ex.Message}\n{ex.StackTrace}");
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
        string path = Asset.GetTxtPath(TableUtil.TableClassTpConst, Asset.EnumPrefixPath.ScriptTemplates);
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
            configFieldTp = configFieldTp.Replace("#ID#", tableData.Id.ToString());
            configFieldTp = configFieldTp.Replace("#DESC#", tableData.Desc);
            configFieldTp = configFieldTp.Replace("#SIGN#", tableData.Sign);
            configFieldTp = configFieldTp.Replace("#VALUE#", tableData.Value.ToString());
            configField += configFieldTp;
        }
        fileContent = fileContent.Replace("#CONFIGFIELD#", configField);
    }
    
#endregion

    private string fileContent;
    private TableRules.TableRule tableRule;
    private List<TableConstClass> tableList = new();

    /// <summary>
    /// 常量类
    /// </summary>
    private class TableConstClass {
        /// <summary>
        /// ID
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 标识
        /// </summary>
        public string Sign { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public int Value { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        public string Desc { get; set; }
    }
    
#region 模板
    
    private const string ConfigField =
        "\t\t/// <summary>\r\n" +
        "\t\t/// #ID# #DESC#\r\n" +
        "\t\t/// </summary>\r\n" +
        "\t\tpublic const int #SIGN# = #VALUE#;\r\n";
    
#endregion
}
