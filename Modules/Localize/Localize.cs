using TMPro;
using UnityEngine;
using DCFrame.Utility;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DCFrame {
    public class Localize {

        /// <summary>
        /// 加载 文本 本地化
        /// </summary>
        public static string GetText(string key, object args = null, Locale locale = null, bool allowFallback = true) {
            var tableName = LocalizeUtil.GetTableCollectionName(key);
            var stringTable = LocalizationSettings.StringDatabase.GetTable(tableName, locale);
            if (!stringTable) {
                Debug.LogError($"StringTable 未加载：{tableName}");
                return key;
            }
            var entryKey = LocalizeUtil.GetTableCollectionKey(key);
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
        /// 资源加载的映射
        /// </summary>
        public static readonly Dictionary<System.Type, string> AssetTableNameDic = new() {
            { typeof(GameObject), "Prefab"},
            { typeof(RawImage), "RawImage"},
            { typeof(Sprite), "Sprite" },
            { typeof(Texture), "Texture" },
            { typeof(AudioClip), "AudioClip" },
            { typeof(Font), "Font" },
            { typeof(TMP_FontAsset), "TMPFont" },
        };
        
        /// <summary>
        /// 加载 资源 本地化
        /// </summary>
        public static async UniTask<T> LoadAsset<T>(string key) where T : Object {
            if (!AssetTableNameDic.TryGetValue(typeof(T), out var tableName)) {
                Debug.LogError($"未为类型 {typeof(T)} 配置本地化表名");
                return null;
            }
            var handle = LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<T>(tableName, key);
            await handle.Task;
            if (handle.Status == AsyncOperationStatus.Succeeded) {
                return handle.Result;
            }
            Debug.LogError($"加载本地化资源失败: {key} ({typeof(T)})");
            return null;
        }
    }
}

