using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace DCFrame {
    public class Asset {
        /// <summary>
        /// 根据地址加载资源
        /// </summary>
        public static async UniTask<T> LoadAsset<T>(string address) where T : Object {
            if (assetDic.TryGetValue(address, out var assetRef)) {
                assetRef.count = Mathf.Max(0, assetRef.count) + 1;
                assetRef.zeroTime = -1f;
                return assetRef.handle.Result as T;
            }
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"加载资源失败，地址是: {address}");
                return null;
            }
            assetDic[address] = new AssetAddRef {
                handle = handle,
                count = 1,
                zeroTime = -1f
            };
            return handle.Result;
        }
        
        /// <summary>
        /// 根据地址卸载资源
        /// </summary>
        public static void Release(string address) {
            if (!assetDic.TryGetValue(address, out var assetRef)) {
                Debug.LogWarning($"未找到资源: {address}");
                return;
            }
            assetRef.count--;
            if (assetRef.count > 0) {
                return;
            }
            assetRef.count = 0;
            assetRef.zeroTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// 释放计时处理
        /// </summary>
        public static void Update() {
            if (assetDic.Count == 0) {
                return;
            }
            var now = Time.realtimeSinceStartup;
            removeList.Clear();
            foreach (var kv in assetDic) {
                var assetRef = kv.Value;
                if (assetRef.count != 0 || assetRef.zeroTime < 0f) {
                    continue;
                }
                if (now - assetRef.zeroTime >= ReleaseDelay) {
                    Addressables.Release(assetRef.handle);
                    removeList.Add(kv.Key);
                }
            }
            for (int i = 0; i < removeList.Count; i++) {
                assetDic.Remove(removeList[i]);
            }
        }

        #region 加载前缀和函数
        
        /// <summary>
        /// 项目前缀地址
        /// </summary>
        public enum PrefixPath {
            Single = 0,
            GamePrefab = 1,
            Settings = 2,
            ScriptTemplates = 3,
        }
        
        /// <summary>
        /// 前缀与地址的映射
        /// </summary>
        private static readonly Dictionary<PrefixPath, string> PrefixPathDic = new() {
            { PrefixPath.Single, "Assets/Simple/"},
            { PrefixPath.GamePrefab, "Assets/Game/Prefabs/"},
            { PrefixPath.Settings, "Assets/Game/Settings/"},
            { PrefixPath.ScriptTemplates, "Tools/ScriptTemplates/"}
        };
        
        /// <summary>
        /// 获取资源加载地址
        /// </summary>
        public static string GetPath(string path, PrefixPath enumPrefix = PrefixPath.GamePrefab) {
            if (!PrefixPathDic.TryGetValue(enumPrefix, out string prefixPath)) {
                Debug.LogError($"暂未支持当前前缀枚举： {enumPrefix}");
                return "";
            }
            return Path.Combine(prefixPath, path).Replace("\\", "/");
        }
        
        /// <summary>
        /// 获取贴图加载地址
        /// </summary>
        public static string GetSpritePath(string path, PrefixPath enumPrefix = PrefixPath.GamePrefab) {
            return GetPath(path + ".png", enumPrefix);
        }
        
        /// <summary>
        /// 获取文本加载地址
        /// </summary>
        public static string GetTxtPath(string path, PrefixPath enumPrefix = PrefixPath.GamePrefab) {
            return GetPath(path + ".txt", enumPrefix);
        }
        
        /// <summary>
        /// 获取预制件加载地址
        /// </summary>
        public static string GetPrefabPath(string path, PrefixPath enumPrefix = PrefixPath.GamePrefab) {
            return GetPath(path + ".prefab", enumPrefix);
        }
        
        /// <summary>
        /// 获取资源加载地址
        /// </summary>
        public static string GetAssetPath(string path, PrefixPath enumPrefix = PrefixPath.GamePrefab) {
            return GetPath(path + ".asset", enumPrefix);
        }

        #endregion
        
        
        /// <summary>
        /// 资源计数
        /// </summary>
        private static readonly Dictionary<string, AssetAddRef> assetDic = new();
        private static readonly List<string> removeList = new();
        private const float ReleaseDelay = 60f;

        private class AssetAddRef {
            public AsyncOperationHandle handle;
            public int count;
            public float zeroTime;
        }

    }
}
