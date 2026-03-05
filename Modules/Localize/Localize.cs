using Cysharp.Threading.Tasks;
using DCFrame.Utility;
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
            var localeTp = LocalizeUtil.GetDefaultLocale();
            if (!localeTp) {
                Debug.LogError($"找不到默认语言设置");
                return key;
            }
            return GetText(key, args, localeTp, false);
        }
        
        /// <summary>
        /// 加载 预制体 本地化
        /// </summary>
        public static async UniTask<GameObject> GetPrefab(string key, string tableName = "Prefab.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as GameObject;
        }
        
        /// <summary>
        /// 加载 精灵 本地化
        /// </summary>
        public static async UniTask<Sprite> GetSprite(string key, string tableName = "Sprite.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Sprite;
        }
        
        /// <summary>
        /// 加载 贴图 本地化
        /// </summary>
        public static async UniTask<Texture> GetTexture(string key, string tableName = "Texture.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Texture;
        }
        
        /// <summary>
        /// 加载 音效 本地化
        /// </summary>
        public static async UniTask<AudioClip> GetAudioClip(string key, string tableName = "AudioClip.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as AudioClip;
        }
        
        /// <summary>
        /// 加载 字体 本地化
        /// </summary>
        public static async UniTask<Font> GetFont(string key, string tableName = "Font.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as Font;
        }
        
        /// <summary>
        /// 加载 TMP字体 本地化
        /// </summary>
        public static async UniTask<TMP_FontAsset> GetTMPFont(string key, string tableName = "TMPFont.") {
            if (tableName.Length > 0) {
                key = tableName + key;
            }
            var path = GetTableAssetPath(key);
            return await Asset.LoadAsset(path) as TMP_FontAsset;
        }

        /// <summary>
        /// 获取资源表的GUID
        /// </summary>
        public static string GetTableAssetPath(string key, Locale locale = null, bool allowFallback = true){
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
            var localeTp = LocalizeUtil.GetDefaultLocale();
            if (!localeTp) {
                Debug.LogError($"找不到默认语言设置");
                return "";
            }
            return GetTableAssetPath(key, localeTp, false);
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

