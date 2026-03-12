using System.Linq;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DCFrame.Utility {
    public static class LocalizeUtil {
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
        /// 获取表收集名称
        /// </summary>
        public static string GetTableCollectionName(string key) {
            return !key.Contains(".") ? key : key.Split(".")[0];
        }
        
        /// <summary>
        /// 获取表收集的Key
        /// </summary>
        public static string GetTableCollectionKey(string key) {
            var tableName = GetTableCollectionName(key);
            return key.Contains(".") ? key[(tableName.Length + 1)..] : key;
        }
    }
}