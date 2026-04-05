using System;
using System.Collections.Generic;
using System.IO;
using DCFrame;
using UnityEngine;

public class TableRulesTypeConst : ITableType {
    /// <summary>
    /// 分析表数据
    /// </summary>
    public bool Init(TableRules.TableRule tableRule) {
        this.tableRule = tableRule;
        tableList.Clear();
        try {
            using var csv = TableCsvEditorUtil.CreateCsvReader(TableUtil.GetFilePath(tableRule.name));
            TableCsvEditorUtil.ReadHeaderData(csv);
            tableList = new List<TableConstClass>();
            while (csv.Read()) {
                tableList.Add(csv.GetRecord<TableConstClass>());
            }
            return true;
        }
        catch (Exception ex) {
            tableList.Clear();
            Debug.LogError($"CSV 解析常量表失败：{ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }

    public void InitEditor(TableRulesEditor rulesEditor) {
    }

    public void Destroy() {
        tableList.Clear();
    }

    public void OnInspectorGUI() {}
    
    /// <summary>
    /// 处理数据
    /// </summary>
    public void OnDealWithData() {
        if (tableList.Count <= 0) {
            return;
        }
        OnDealWithFile();
    }

#region 创建脚本

    public void OnDealWithFile() {
        string path = Asset.GetTxtPath(TableUtil.TableClassTpConst, Asset.PrefixPath.ScriptTemplates);
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
