using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DCFrame;
using UnityEditor;
using UnityEngine;

public class TableEditor : Editor {
    [MenuItem("Tools/资源项/导表")]
    public static void PackageConfig() {
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
            Dictionary<string, string> tableDataDic = new Dictionary<string, string>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                HasHeaderRecord = true,  // 如果有表头，设为 true
                IgnoreBlankLines = true,  // 忽略空行
            };
            using var reader = new StreamReader(file, Encoding.UTF8);
            using var csv = new CsvReader(reader, config);
            // 读取 CSV 并解析成动态对象
            var records = csv.GetRecords<dynamic>();
            // 遍历所有行
            foreach (var record in records) {
                // 每行数据,只获取第一行数据
                foreach (var kvp in (IDictionary<string, object>)record) {
                    if (!tableDataDic.ContainsKey(kvp.Key)) {
                        tableDataDic.Add(kvp.Key, kvp.Value.ToString());
                    }
                }
                break;
            }
            CreateTableScript(Path.GetFileNameWithoutExtension(file), tableDataDic);
        }
    }

    /// <summary>
    /// 创建脚本并写入数据
    /// </summary>
    private static void CreateTableScript(string fileName, Dictionary<string, string> tableDataDic) {
        string path = Asset.GetTxtPath(TableClassFileName, Asset.EnumPrefixPath.ScriptTemplates);
        string fileContent = File.ReadAllText(path);
        fileContent = fileContent.Replace("#SCRIPTNAME#", fileName);
        string fieldContent = "";
        fileContent = fileContent.Replace("#SCRIPTFIELD#", fieldContent);
        File.WriteAllText(TableScriptPath + "\\Table" + fileName + ".cs", fileContent);
    }
    
    /// <summary>
    /// 表类的模板文件
    /// </summary>
    private const string TableClassFileName = "TableClassTp";
    /// <summary>
    /// 表脚本路径
    /// </summary>
    private const string TableScriptPath = "Assets/Game/Scripts/Table";

}
