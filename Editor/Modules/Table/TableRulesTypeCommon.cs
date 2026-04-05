using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DCFrame;
using DCFrame.Utility;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;

public class TableRulesTypeCommon : ITableType {
    public bool Init(TableRules.TableRule tableRule) {
        Destroy();
        this.tableRule = tableRule;
        remarkInput = tableRule?.defaultData?.remark ?? "";
        return true;
    }

    public void InitEditor(TableRulesEditor rulesEditor) {
        this.rulesEditor = rulesEditor;
    }

    public void Destroy() {
        CleanInvalidRelateData();
        fieldDic.Clear();
        fieldNameDisplayDic.Clear();
        relateTableList.Clear();
        popupDic.Clear();
        tableDataDic.Clear();
        tableLocalizeDic.Clear();
        fileContent = "";
        remarkInput = "";
    }
    
#region Editor面板显示
    
    /// <summary>
    /// 显示 Editor 的面板UI
    /// </summary>
    public void OnInspectorGUI() {
        AnalyzeTableDataGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableFieldDataGUI();
        TableOtherDataGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableKeyDataGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableRemarkGUI();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        TableFieldRelateGUI();
    }

    /// <summary>
    /// 分析数据
    /// </summary>
    private void AnalyzeTableDataGUI() {
        if (fieldDic.Count > 0) {
            return;
        }
        using var csv = TableCsvEditorUtil.CreateCsvReader(TableUtil.GetFilePath(tableRule.name));
        var headerData = TableCsvEditorUtil.ReadHeaderData(csv);
        fieldNameDisplayDic.Clear();
        foreach (var header in headerData.HeaderList) {
            fieldNameDisplayDic[header] = headerData.GetDisplayName(header);
        }

        if (!csv.Read()) {
            return;
        }

        foreach (var header in headerData.HeaderList) {
            if (!fieldDic.ContainsKey(header)) {
                var enumFieldType = TableUtil.GetEnumFieldType(csv.GetField(header) ?? string.Empty);
                fieldDic.Add(header, enumFieldType);
            }
        }
    }
    
    /// <summary>
    /// 字段的特有数据渲染
    /// </summary>
    private void TableFieldDataGUI() {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("字段名", GUILayout.Width(70));
        EditorGUILayout.LabelField("中文名", GUILayout.Width(70));
        EditorGUILayout.LabelField("数据类型", GUILayout.Width(60));
        EditorGUILayout.LabelField("本地化", GUILayout.Width(40));
        EditorGUILayout.LabelField("最大值", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        List<string> fileList = new List<string>(Enum.GetNames(typeof(TableUtil.FieldType)));
        foreach (var field in fieldDic) {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Key, GUILayout.Width(70));
            EditorGUILayout.LabelField(GetFieldDisplayName(field.Key), GUILayout.Width(70));
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
            enumFieldTp = (TableUtil.FieldType)EditorGUILayout.Popup("",(int)enumFieldTp, fileList.ToArray(), GUILayout.Width(60));
            fieldData.enumField = enumFieldTp;
            // 是否本地化
            GUILayout.Space(10);
            fieldData.isLocalize = EditorGUILayout.Toggle(fieldData.isLocalize, GUILayout.Width(30));
            // 最大值
            var isHide = fieldData.enumField is TableUtil.FieldType.Bool or TableUtil.FieldType.String;
            if (!isHide) {
                GUILayout.Space(10);
                fieldData.isMaxValue = EditorGUILayout.Toggle(fieldData.isMaxValue, GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 其他数据的渲染
    /// </summary>
    private void TableOtherDataGUI() {
        var hasLocalize = false;
        List<string> fileList = new List<string>();
        foreach (var field in fieldDic) {
            fileList.Add(field.Key);
            if (!hasLocalize) {
                var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == field.Key);
                if (fieldData.isLocalize) {
                    hasLocalize = true;
                }
            }
        }
        if (hasLocalize) {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("本地化的Key：", GUILayout.Width(80));
            var idx = fileList.IndexOf(tableRule.defaultData.localizeKey);
            if (idx < 0) {
                idx = 0;
            }
            var selectIdx = EditorGUILayout.Popup("", idx, fileList.ToArray(), GUILayout.Width(60));
            tableRule.defaultData.localizeKey = fileList[selectIdx];
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 备注的功能
    /// </summary>
    private void TableRemarkGUI() {
        if (tableRule?.defaultData == null) {
            return;
        }
        remarkFoldout = EditorGUILayout.Foldout(remarkFoldout, "备注功能", true);
        if (!remarkFoldout) {
            return;
        }
        GUILayout.Space(5);
        if (remarkInput == null) {
            remarkInput = tableRule.defaultData.remark ?? "";
        }
        if (remarkTextStyle == null) {
            remarkTextStyle = new GUIStyle(EditorStyles.textArea) {
                wordWrap = true
            };
        }
        var viewWidth = Mathf.Max(100f, EditorGUIUtility.currentViewWidth - 40f);
        var contentHeight = remarkTextStyle.CalcHeight(new GUIContent(remarkInput), viewWidth);
        var remarkHeight = Mathf.Max(50f, contentHeight);
        remarkInput = EditorGUILayout.TextArea(remarkInput, remarkTextStyle, GUILayout.Height(remarkHeight));
        var savedRemark = tableRule.defaultData.remark ?? "";
        var inputRemark = remarkInput ?? "";
        var isSameRemark = string.Equals(inputRemark, savedRemark, StringComparison.Ordinal);
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(isSameRemark)) {
            if (GUILayout.Button("重置", GUILayout.Width(60))) {
                remarkInput = savedRemark;
                GUI.FocusControl(null);
            }
        }
        using (new EditorGUI.DisabledScope(isSameRemark)) {
            if (GUILayout.Button("保存", GUILayout.Width(60))) {
                tableRule.defaultData.remark = inputRemark;
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    
    /// <summary>
    /// 字段的查找函数生成
    /// </summary>
    private void TableKeyDataGUI() {
        keyFoldout = EditorGUILayout.Foldout(keyFoldout, "生成查找数据函数", true);
        if (!keyFoldout) {
            return;
        }
        GUILayout.Space(5);
        int curCount = 0;
        var keyArray = fieldDic.Keys.ToArray();
        var noneList = new List<string>();
        var tableFieldList = new List<TableRules.TableField>();
        for (int i = 0; i < keyArray.Length; i++) {
            var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == keyArray[i]);
            if (fieldData != null) {
                if (fieldData.enumMainViceKey != TableUtil.KeyType.None) {
                    curCount += 1;
                    tableFieldList.Add(fieldData);
                }else {
                    noneList.Add(fieldData.fieldName);
                }
            }
        }
        // 显示添加数据
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("添加：", GUILayout.Width(74));
        if (noneList.Count > 0) {
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
        }else {
            List<string> noneListTp = new List<string>() { "None" };
            EditorGUILayout.Popup("", 0, noneListTp.ToArray(), GUILayout.Width(100));
        }
        EditorGUILayout.EndHorizontal();
        // 显示列表数据
        var typeList = CommonUtil.GetEnumDescriptions<TableUtil.KeyType>();
        foreach (var field in tableFieldList) {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.fieldName, GUILayout.Width(70));
            field.enumMainViceKey = (TableUtil.KeyType)EditorGUILayout.Popup("",(int)field.enumMainViceKey, typeList.ToArray(), GUILayout.Width(100));
            if (field.enumMainViceKey is TableUtil.KeyType.None or TableUtil.KeyType.Single) {
                field.fieldKeyList.Clear();
                EditorGUILayout.EndHorizontal();
            }else if (field.enumMainViceKey is TableUtil.KeyType.Multi or TableUtil.KeyType.MultiWithList) {
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
    }

    private void OnClickAddKey(string filedName) {
        var fieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == filedName);
        if (fieldData == null) {
            return;
        }
        popupDic["addKeyIndex"] = 0;
        fieldData.enumMainViceKey = TableUtil.KeyType.Single;
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
    
    #region 字段关联
    
    /// <summary>
    /// 字段关联的渲染
    /// </summary>
    private void TableFieldRelateGUI() {
        if (tableRule?.defaultData == null) {
            return;
        }
        relateFoldout = EditorGUILayout.Foldout(relateFoldout, "字段关联", true);
        if (!relateFoldout) {
            return;
        }
        // 添加逻辑
        var fieldNameList = fieldDic.Keys.ToList();
        if (fieldNameList.Count > 0) {
            EditorGUILayout.LabelField("添加：");
            EditorGUILayout.BeginHorizontal();
                if (!popupDic.ContainsKey("addRelateFieldIndex") || fieldNameList.Count <= popupDic["addRelateFieldIndex"]) {
                popupDic["addRelateFieldIndex"] = 0;
            }
            popupDic["addRelateFieldIndex"] = EditorGUILayout.Popup("", popupDic["addRelateFieldIndex"], fieldNameList.ToArray(), GUILayout.Width(120));
            var selectedFieldName = fieldNameList[popupDic["addRelateFieldIndex"]];
            var selectedFieldData = tableRule.defaultData.fieldList.Find((x) => x.fieldName == selectedFieldName);
            var usedTables = new HashSet<string>();
            if (selectedFieldData != null && selectedFieldData.fieldRelateList != null) {
                foreach (var relate in selectedFieldData.fieldRelateList) {
                    if (!string.IsNullOrEmpty(relate.tableName)) {
                        usedTables.Add(relate.tableName);
                    }
                }
            }
            var tableList = GetAllTableNames(tableRule.name);
            tableList.RemoveAll((x) => usedTables.Contains(x));
            var tableDisplayList = tableList.Select(GetTableDisplayName).ToList();
            if (tableList.Count > 0) {
                if (!popupDic.ContainsKey("addRelateTableIndex") || tableList.Count <= popupDic["addRelateTableIndex"]) {
                    popupDic["addRelateTableIndex"] = 0;
                }
                popupDic["addRelateTableIndex"] = EditorGUILayout.Popup("", popupDic["addRelateTableIndex"], tableDisplayList.ToArray(), GUILayout.Width(120));
                if (GUILayout.Button("+", GUILayout.Width(30))) {
                    if (selectedFieldData != null) {
                        var relate = new TableRules.TableFieldRelate {
                            tableName = tableList[popupDic["addRelateTableIndex"]],
                            fieldName = ""
                        };
                        selectedFieldData.fieldRelateList?.Add(relate);
                    }
                    popupDic["addRelateFieldIndex"] = popupDic["addRelateFieldIndex"];
                    popupDic["addRelateTableIndex"] = 0;
                }
            } else {
                EditorGUILayout.LabelField("无表可选", GUILayout.Width(120));
                using (new EditorGUI.DisabledScope(true)) {
                    GUILayout.Button("+", GUILayout.Width(30));
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.Space(5);
        // 渲染逻辑
        var hasRelateList = new List<TableRules.TableField>();
        foreach (var field in tableRule.defaultData.fieldList) {
            if (field == null) {
                continue;
            }
            if (field.fieldRelateList != null && field.fieldRelateList.Count > 0) {
                hasRelateList.Add(field);
            }
        }
        int addCount = 0;
        foreach (var field in hasRelateList) {
            if (field == null) {
                continue;
            }
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(++addCount + ".", GUILayout.Width(14));
            var fieldLabel = field.fieldName;
            var fieldLabelWidth = EditorStyles.miniButton.CalcSize(new GUIContent(fieldLabel)).x + 6f;
            if (GUILayout.Button(fieldLabel, GUILayout.Width(fieldLabelWidth))) {
                field.fieldRelateList.Clear();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();
            field.fieldRelateList ??= new List<TableRules.TableFieldRelate>();
            var selectedMap = new Dictionary<string, List<string>>();
            var tableOrder = new List<string>();
            foreach (var item in field.fieldRelateList) {
                if (item == null || string.IsNullOrEmpty(item.tableName)) {
                    continue;
                }
                if (!selectedMap.TryGetValue(item.tableName, out var list)) {
                    list = new List<string>();
                    selectedMap[item.tableName] = list;
                    tableOrder.Add(item.tableName);
                }
                if (!string.IsNullOrEmpty(item.fieldName) && !list.Contains(item.fieldName)) {
                    list.Add(item.fieldName);
                }
            }
            foreach (var tableName in tableOrder) {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                var fieldList = selectedMap[tableName];
                var relateFields = GetTableFieldList(tableName);
                relateFields.RemoveAll((x) => fieldList.Contains(x));
                relateFields.Insert(0, "None");
                var popupKey = $"{field.fieldName}_relateTable_{tableName}";
                if (!popupDic.ContainsKey(popupKey) || relateFields.Count <= popupDic[popupKey]) {
                    popupDic[popupKey] = 0;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(GetTableDisplayName(tableName), GUILayout.Width(120));
                var selectIdx = EditorGUILayout.Popup("", popupDic[popupKey], relateFields.ToArray(), GUILayout.Width(120));
                popupDic[popupKey] = selectIdx;
                if (selectIdx != 0) {
                    field.fieldRelateList.Add(new TableRules.TableFieldRelate {
                        tableName = tableName,
                        fieldName = relateFields[selectIdx]
                    });
                    popupDic[popupKey] = 0;
                }
                if (GUILayout.Button("-", GUILayout.Width(30))) {
                    RemoveRelateTable(field, tableName);
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("跳转", GUILayout.Width(50))) {
                    rulesEditor.JumpConfig(tableName);
                }
                EditorGUILayout.EndHorizontal();
                if (fieldList.Count > 0) {
                    EditorGUILayout.BeginHorizontal();
                    foreach (var name in fieldList) {
                        if (GUILayout.Button(name, GUILayout.Width(EditorStyles.popup.CalcSize(new GUIContent(name)).x))) {
                            ClearRelateFieldName(field, tableName, name);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.Space(4);
                EditorGUILayout.EndVertical();
            }
            GUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 字段关联的名称
    /// </summary>
    private static string GetTableDisplayName(string tableName) {
        if (string.IsNullOrEmpty(tableName)) {
            return "";
        }
        return tableName.StartsWith("Table", StringComparison.Ordinal) ? tableName.Substring(5) : tableName;
    }

    
    /// <summary>
    /// 清除关联字段名
    /// </summary>
    private static void ClearRelateFieldName(TableRules.TableField field, string tableName, string fieldName) {
        if (field?.fieldRelateList == null) {
            return;
        }
        foreach (var relate in field.fieldRelateList) {
            if (relate == null) {
                continue;
            }
            if (string.Equals(relate.tableName, tableName, StringComparison.Ordinal) &&
                string.Equals(relate.fieldName, fieldName, StringComparison.Ordinal)) {
                relate.fieldName = "";
                break;
            }
        }
    }

    /// <summary>
    /// 移除关联表名
    /// </summary>
    private static void RemoveRelateTable(TableRules.TableField field, string tableName) {
        if (field?.fieldRelateList == null) {
            return;
        }
        for (int i = field.fieldRelateList.Count - 1; i >= 0; i--) {
            var relate = field.fieldRelateList[i];
            if (relate != null && string.Equals(relate.tableName, tableName, StringComparison.Ordinal)) {
                field.fieldRelateList.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 获取表名列表
    /// </summary>
    private List<string> GetAllTableNames(string curTableName) {
        if (relateTableList.Count > 0) {
            return relateTableList;
        }
        foreach (var rule in rulesEditor.tableRules.tableRuleList) {
            if (rule == null || string.IsNullOrEmpty(rule.name)) {
                continue;
            }
            bool condition1 = string.Equals(rule.name, curTableName, StringComparison.OrdinalIgnoreCase);
            bool condition2 = rule.enumTableType != TableUtil.TableType.Default;
            if (condition1 || condition2) {
                continue;
            }
            relateTableList.Add(rule.name);
        }
        return relateTableList;
    }

    /// <summary>
    /// 获取表字段列表
    /// </summary>
    private static List<string> GetTableFieldList(string tableName) {
        if (string.IsNullOrEmpty(tableName)) {
            return new List<string>();
        }
        if (fieldRelateTableDic.TryGetValue(tableName, out var cached)) {
            return new List<string>(cached);
        }
        var list = new List<string>();
        var filePath = TableUtil.GetFilePath(tableName);
        if (!File.Exists(filePath)) {
            fieldRelateTableDic[tableName] = list;
            return new List<string>(list);
        }
        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<dynamic>();
        foreach (var record in records) {
            foreach (var kvp in (IDictionary<string, object>)record) {
                if (!list.Contains(kvp.Key)) {
                    list.Add(kvp.Key);
                }
            }
            break;
        }
        fieldRelateTableDic[tableName] = list;
        return new List<string>(list);
    }
    
    /// <summary>
    /// 清除表关联
    /// </summary>
    private void CleanInvalidRelateData() {
        var fieldList = tableRule?.defaultData?.fieldList;
        if (fieldList == null) {
            return;
        }
        foreach (var field in fieldList) {
            if (field?.fieldRelateList == null) {
                continue;
            }
            for (int i = field.fieldRelateList.Count - 1; i >= 0; i--) {
                var relate = field.fieldRelateList[i];
                if (relate == null ||
                    string.IsNullOrEmpty(relate.tableName) ||
                    string.IsNullOrEmpty(relate.fieldName)) {
                    field.fieldRelateList.RemoveAt(i);
                }
            }
        }
    }
    
    #endregion
    
#endregion

    /// <summary>
    /// 处理数据
    /// </summary>
    public void OnDealWithData() {
        AnalyzeyData();
        collection = LocalizeUtilEditor.GetOrCreateStringCollection(tableRule.name);
        OnDealWithFile();
        OnDealWithLocalize();
    }

    private void AnalyzeyData() {
        var filePath = TableUtil.GetFilePath(tableRule.name);
        using var csv = TableCsvEditorUtil.CreateCsvReader(filePath);
        var headerData = TableCsvEditorUtil.ReadHeaderData(csv);
        fieldNameDisplayDic.Clear();
        foreach (var header in headerData.HeaderList) {
            fieldNameDisplayDic[header] = headerData.GetDisplayName(header);
        }
        // 提前判断最大值获取
        var fieldDicTp = tableRule != null ? tableRule.defaultData.fieldList.FindAll((x)=> x.isMaxValue).ToDictionary((x)=>x.fieldName) : new Dictionary<string, TableRules.TableField>();
        // 提前判断本地化数据获取
        var localizeKey = tableRule != null ? tableRule.defaultData.fieldList.FindAll((x)=> x.isLocalize).ToDictionary((x)=>x.fieldName) : new Dictionary<string, TableRules.TableField>();
        tableDataDic.Clear();
        tableLocalizeDic.Clear();
        // 遍历所有行
        string rowId = "";
        while (csv.Read()) {
            foreach (var header in headerData.HeaderList) {
                var fieldValue = csv.GetField(header) ?? string.Empty;
                if (header == "Id") {
                    rowId = fieldValue;
                }
                if (!tableDataDic.ContainsKey(header)) {
                    tableDataDic.Add(header, new List<string>());
                }
                if (fieldDicTp.ContainsKey(header)) {
                    tableDataDic[header].Add(fieldValue);
                }
                if (localizeKey.ContainsKey(header)) {
                    if (!tableLocalizeDic.ContainsKey(header)) {
                        tableLocalizeDic[header] = new List<tableLocalizeValue>();
                    }
                    tableLocalizeDic[header].Add(new tableLocalizeValue() {
                        Id = rowId,
                        Text = fieldValue
                    });
                }
            }
            if (fieldDicTp.Count == 0 && localizeKey.Count == 0) {
                break;
            }
        }
    }

#region 创建脚本

    public void OnDealWithFile() {
        string path = Asset.GetTxtPath(TableUtil.TableClassTpNormal, Asset.PrefixPath.ScriptTemplates);
        fileContent = File.ReadAllText(path);
        keyReplaceDic.Add("#SCRIPTNAME#", tableRule.name);
        keyReplaceDic.Add("#CLASSENDSIGN#", ClassEndSign);
        WriteTableClassField();
        DealWithConfigDic();
        DealWithConfigMaxSingle();
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
    private void WriteTableClassField() {
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
            string strField;
            if (field.isLocalize) {
                var key = string.Format($"{tableRule.name}.{field.fieldName}.");
                strField = string.Format($"public {fileType} {field.fieldName} => Localize.GetText(\"{key}\" + {tableRule.defaultData.localizeKey});");
            }else {
                strField = string.Format($"public {fileType} {field.fieldName} {{{{ get; set; }}}}");
            }
            var fieldDisplayName = SecurityElement.Escape(GetFieldDisplayName(field.fieldName)) ?? field.fieldName;
            fieldContent += $"/// <summary>\r\n\t\t/// {fieldDisplayName}\r\n\t\t/// </summary>\r\n\t\t{strField}";
            addCount += 1;
            if (addCount < existList.Count) {
                fieldContent += "\r\n\t\t";
            }
        }
        keyReplaceDic.Add("#SCRIPTFIELD#", fieldContent);
    }

    /// <summary>
    /// 获取字段名称
    /// </summary>
    private string GetFieldDisplayName(string fieldName) {
        if (fieldNameDisplayDic.TryGetValue(fieldName, out var displayName) && !string.IsNullOrWhiteSpace(displayName)) {
            return displayName;
        }
        return fieldName;
    }

    #region ConfigDic

    /// <summary>
    /// 处理字典数据
    /// </summary>
    private void DealWithConfigDic() {
        bool hasReplace = false;
        foreach (var field in tableRule.defaultData.fieldList) {
            switch (field.enumMainViceKey) {
                case TableUtil.KeyType.None:
                    break;
                case TableUtil.KeyType.Single:
                    DealWithConfigDicMain(field);
                    hasReplace = true;
                    break;
                case TableUtil.KeyType.Multi:
                    DealWithConfigDicVice(field);
                    hasReplace = true;
                    break;
                case TableUtil.KeyType.MultiWithList:
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
        var value = tableRule.name + ClassEndSign;
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
        var value = tableRule.name + ClassEndSign;
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
        var value = tableRule.name + ClassEndSign;
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
    private void DealWithConfigMaxSingle() {
        var strKey = "#CONFIGMAX#";
        var fieldList = tableRule.defaultData.fieldList.FindAll((x)=> x.isMaxValue);
        if (fieldList.Count == 0) {
            keyReplaceDic.Add(strKey, "");
            return;
        }
        var configMax = "";
        var addCount = 0;
        foreach (var field in fieldList) {
            if (field.enumField is TableUtil.FieldType.Bool or TableUtil.FieldType.String) {
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
            if (field.enumField == TableUtil.FieldType.Float) {
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
            keyReplaceDic.Add(strKey, "");
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

#region 处理本地化数据

    private void OnDealWithLocalize() {
        if (!collection) {
            return;
        }
        var cnDic = LocalizeUtilEditor.GetCollectionCnDic(collection);
        LocalizeUtilEditor.ClearCollection(collection);
        var cnCode = LocalizeConst.LocaleCodeDic[LocalizeConst.LocaleCode.ZhCN];
        foreach (var field in tableRule.defaultData.fieldList) {
            if (field.isLocalize && tableLocalizeDic.TryGetValue(field.fieldName, out var value1)) {
                foreach (var tableLocalize in value1) {
                    var cnText = tableLocalize.Text;
                    var key = string.Format($"{field.fieldName}.{tableLocalize.Id}");
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

    private static string fileContent;
    private TableRulesEditor rulesEditor;
    private TableRules.TableRule tableRule;
    private StringTableCollection collection;
    private string remarkInput;
    private GUIStyle remarkTextStyle;
    private bool remarkFoldout = true;
    private bool keyFoldout = true;
    private bool relateFoldout = true;
    private readonly Dictionary<string, TableUtil.FieldType> fieldDic = new();
    private readonly Dictionary<string, string> fieldNameDisplayDic = new();
    private readonly Dictionary<string, List<string>> tableDataDic = new();
    private readonly Dictionary<string, List<tableLocalizeValue>> tableLocalizeDic = new();
    private List<string> relateTableList = new();
    private static readonly Dictionary<string, List<string>> fieldRelateTableDic = new();
    private class tableLocalizeValue {
        public string Id;
        public string Text;
    }
    
    /// <summary>
    /// 类的尾部标识
    /// </summary>
    private const string ClassEndSign = "Field";
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
