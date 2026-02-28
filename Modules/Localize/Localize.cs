using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace DCFrame {
    public class Localize {

        /// <summary>
        /// 加载 文本 本地化
        /// </summary>
        public static string GetText(string key, object args = null, Locale locale = null, bool allowFallback = true) {
            var tableName = GetTableCollectionName(key);
            var stringTable = LocalizationSettings.StringDatabase.GetTable(tableName, locale);
            if (!stringTable) {
                Debug.LogError($"StringTable 未加载：{tableName}");
                return key;
            }
            var entryKey = GetTableCollectionKey(key);
            var entry = stringTable.GetEntry(entryKey);
            if (entry == null) {
                Debug.LogError($"{stringTable.TableCollectionName}:包内没有当前 Key：{entryKey}");
                return key;
            }
            string result = entry.GetLocalizedString(args);
            if (result != null) {
                return result;
            }
            Debug.LogWarning($"{stringTable.TableCollectionName}：内没设置值，Key：{entryKey}，语言是：{LocalizationSettings.SelectedLocale.Identifier.Code}");
            if (!allowFallback) {
                return key;
            }
            var localeTp = GetDefaultLocale();
            if (!localeTp) {
                Debug.LogError($"找不到默认语言设置");
                return key;
            }
            return GetText(key, args, localeTp, false);
        }
        
        /// <summary>
        /// 加载 预制体 本地化
        /// </summary>
        public static async UniTask<GameObject> GetPrefab(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as GameObject;
        }
        
        /// <summary>
        /// 加载 贴图 本地化
        /// </summary>
        public static async UniTask<Sprite> GetSprite(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Sprite;
        }
        
        /// <summary>
        /// 加载 贴图 本地化
        /// </summary>
        public static async UniTask<Texture> GetTexture(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Texture;
        }
        
        /// <summary>
        /// 加载 音效 本地化
        /// </summary>
        public static async UniTask<AudioClip> GetAudioClip(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as AudioClip;
        }
        
        /// <summary>
        /// 加载 字体 本地化
        /// </summary>
        public static async UniTask<Font> GetFont(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Font;
        }
        
        /// <summary>
        /// 加载 TMP字体 本地化
        /// </summary>
        public static async UniTask<TMP_FontAsset> GetTMPFont(string key) {
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as TMP_FontAsset;
        }

        /// <summary>
        /// 获取资源表的GUID
        /// </summary>
        private static string GetTableAssetPath(string key, Locale locale = null, bool allowFallback = true){
            var tableName = GetTableCollectionName(key);
            var assetTable = LocalizationSettings.AssetDatabase.GetTable(tableName, locale);
            if (!assetTable) {
                Debug.LogError($"AssetTable 未加载：{tableName}");
                return "";
            }
            var entry = assetTable.GetEntry(key);
            if (entry != null) {
                var path = AssetDatabase.GUIDToAssetPath(entry.Guid);
                return path;
            }
            Debug.LogWarning($"{assetTable.TableCollectionName}:内没有当前，Key：{key}，语言是：{LocalizationSettings.SelectedLocale.Identifier.Code}");
            if (!allowFallback) {
                return "";
            }
            var localeTp = GetDefaultLocale();
            if (!localeTp) {
                Debug.LogError($"找不到默认语言设置");
                return "";
            }
            return GetTableAssetPath(key, localeTp, false);
        }
        
        /// <summary>
        /// 获取默认的语言
        /// </summary>
        /// <returns></returns>
        private static Locale GetDefaultLocale() {
            var selectors = LocalizationSettings.Instance.GetStartupLocaleSelectors();
            var specific = selectors.OfType<SpecificLocaleSelector>().FirstOrDefault();
            if (specific == null) return null;
            var locales = LocalizationSettings.AvailableLocales.Locales;
            return locales.Find(x => x.Identifier.Code == specific.LocaleId.Code);
        }
        
        /// <summary>
        /// 获取表收集名称
        /// </summary>
        private static string GetTableCollectionName(string key) {
            return !key.Contains(".") ? key : key.Split(".")[0];
        }
        
        /// <summary>
        /// 获取表收集的Key
        /// </summary>
        private static string GetTableCollectionKey(string key) {
            var tableName = GetTableCollectionName(key);
            return key.Contains(".") ? key[(tableName.Length + 1)..] : key;
        }
    }
}

