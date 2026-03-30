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
            // 获取是否已经加载到
            if (AssetDic.TryGetValue(address, out var assetRef)) {
                assetRef.count = Mathf.Max(0, assetRef.count) + 1;
                assetRef.releaseTime = -1f;
                return assetRef.handle.Result as T;
            }
            // 获取是否正在加载中
            if (LoadingDic.TryGetValue(address, out var loadingRef)) {
                loadingRef.count = Mathf.Max(0, loadingRef.count) + 1;
                await loadingRef.handle.Task;
                if (loadingRef.handle.Status != AsyncOperationStatus.Succeeded) {
                    Debug.LogError($"加载资源失败，地址是: {address}");
                    return null;
                }
                if (!AssetDic.TryGetValue(address, out var loadedAssetRef)) {
                    GetAssetRef(address, loadingRef.handle, loadingRef.count);
                    LoadingDic.Remove(address);
                    loadedAssetRef = AssetDic[address];
                }
                return loadedAssetRef.handle.Result as T;
            }
            // 加载一个新的
            var handle = Addressables.LoadAssetAsync<T>(address);
            var pendingRef = new LoadingRef {
                handle = handle,
                count = 1
            };
            LoadingDic[address] = pendingRef;
            await handle.Task;
            LoadingDic.Remove(address);
            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"加载资源失败，地址是: {address}");
                return null;
            }
            GetAssetRef(address, handle, pendingRef.count);
            return handle.Result;
        }

        /// <summary>
        /// 获取一个实例
        /// </summary>
        private static void GetAssetRef(string address, AsyncOperationHandle handle, int count) {
            AssetDic[address] = new AssetRef {
                handle = handle,
                count = count,
                releaseTime = -1f
            };
        }
        
        /// <summary>
        /// 根据地址卸载资源
        /// </summary>
        public static void Release(string address) {
            if (!AssetDic.TryGetValue(address, out var assetRef)) {
                Debug.LogWarning($"未找到资源: {address}");
                return;
            }
            assetRef.count--;
            if (assetRef.count > 0) {
                return;
            }
            assetRef.count = 0;
            assetRef.releaseTime = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// 释放计时处理
        /// </summary>
        public static void FixedUpdate() {
            if (AssetDic.Count == 0) {
                return;
            }
            var now = Time.realtimeSinceStartup;
            removeList.Clear();
            foreach (var kv in AssetDic) {
                var assetRef = kv.Value;
                if (assetRef.count != 0 || assetRef.releaseTime < 0f) {
                    continue;
                }
                if (now - assetRef.releaseTime >= ReleaseDelay) {
                    Addressables.Release(assetRef.handle);
                    removeList.Add(kv.Key);
                }
            }
            for (int i = 0; i < removeList.Count; i++) {
                AssetDic.Remove(removeList[i]);
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
        private static readonly Dictionary<string, AssetRef> AssetDic = new();
        private static readonly Dictionary<string, LoadingRef> LoadingDic = new();
        private static readonly List<string> removeList = new();
        /// <summary>
        /// 资源释放时间
        /// </summary>
        private const float ReleaseDelay = 60f;

        /// <summary>
        /// 资源引用类
        /// </summary>
        private class AssetRef {
            /// <summary>
            /// 资源句柄
            /// </summary>
            public AsyncOperationHandle handle;
            /// <summary>
            /// 引用次数
            /// </summary>
            public int count;
            /// <summary>
            /// 释放时间
            /// </summary>
            public float releaseTime;
        }

        /// <summary>
        /// 正在加载中的资源引用
        /// </summary>
        private class LoadingRef {
            public AsyncOperationHandle handle;
            public int count;
        }

    }
}
