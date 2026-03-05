using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace DCFrame.Utility {
    public class LocalizeUtil {
        /// <summary>
        /// 获取默认的语言
        /// </summary>
        /// <returns></returns>
        public static Locale GetDefaultLocale() {
            var selectors = LocalizationSettings.Instance.GetStartupLocaleSelectors();
            var specific = selectors.OfType<SpecificLocaleSelector>().FirstOrDefault();
            if (specific == null) return null;
            var locales = LocalizationSettings.AvailableLocales.Locales;
            return locales.Find(x => x.Identifier.Code == specific.LocaleId.Code);
        }
        
        /// <summary>
        /// 清除表的所有数据
        /// </summary>
        public static void ClearCollection(StringTableCollection collection) {
            collection.SharedData.Clear();
            foreach (var table in collection.StringTables) {
                table.Clear();
            }
        }

        /// <summary>
        /// 收集指定表内所有中文文本作为 Key 的缓存字典
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> GetCollectionCnDic(StringTableCollection collection) {
            var result = new Dictionary<string, Dictionary<string, string>>();
            if (!collection) {
                Debug.LogError($"未找到 Localization 表: {collection.name}");
                return result;
            }
            
            StringTable zhTable = null;
            var cnCode = LocalizeConst.LocaleCodeDic[LocalizeConst.EnumLocaleCode.ZhCN];
            foreach (var table in collection.StringTables) {
                if (table.LocaleIdentifier.Code == cnCode) {
                    zhTable = table;
                    break;
                }
            }
            if (!zhTable) {
                Debug.LogError($"未找到中文表: {collection.name}");
                return result;
            }
            foreach (var entry in zhTable.Values) {
                if (string.IsNullOrEmpty(entry.Value))
                    continue;
                // 遍历所有语言表
                foreach (var table in collection.StringTables) {
                    if (table.LocaleIdentifier.Code == cnCode) {
                        continue;
                    }
                    if (!result.ContainsKey(entry.Value)) {
                        result.Add(entry.Value, new Dictionary<string, string>());
                    }
                    var value = table.GetEntry(entry.Key);
                    if (value != null) {
                        result[entry.Value].Add(table.LocaleIdentifier.Code, value.Value);
                    }
                }
            }
            return result;
        }
        
        /// <summary>
        /// 获取收集表，如果没有获取到则创建
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static StringTableCollection GetOrCreateCollection(string tableName) {
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection) {
                return collection;
            }
            // 创建 Collection
            var rootPath = LocalizeConst.LocalizeTableRootPath + "/Text";
            collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, rootPath + "/Collection");
            if (!collection) {
                Debug.LogError($"创建 Localization 表失败: {tableName}");
                return null;
            }
            foreach (var locale in LocalizationEditorSettings.GetLocales()) {
                LocalizationEditorSettings.AddLocale(locale);
                var table = collection.GetTable(locale.Identifier) as StringTable;
                if (!table) {
                    collection.AddNewTable(locale.Identifier);
                    table = collection.GetTable(locale.Identifier) as StringTable;
                }
                // 将每种语言的 StringTable 放到指定语言子文件夹
                var localePathName = StringUtil.ToUpFirstChar(locale.Identifier.Code);
                string langFolder = $"{rootPath}/{localePathName}";
                if (!AssetDatabase.IsValidFolder(langFolder)) {
                    AssetDatabase.CreateFolder(rootPath, localePathName);
                }
                string tablePath = $"{langFolder}/{tableName}_{locale.Identifier.Code}.asset";
                AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(table), tablePath);
            }
            return collection;
        }
    }
}