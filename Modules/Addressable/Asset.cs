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
            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"加载资源失败，地址是: {address}");
                return null;
            }
            return handle.Result;
        }

        #region 加载前缀和函数
        
        /// <summary>
        /// 项目前缀地址
        /// </summary>
        public enum EnumPrefixPath {
            Single = 0,
            GamePrefab = 1,
            Settings = 2,
            ScriptTemplates = 3,
        }
        
        /// <summary>
        /// 前缀与地址的映射
        /// </summary>
        private static readonly Dictionary<EnumPrefixPath, string> PrefixPathDic = new() {
            { EnumPrefixPath.Single, "Assets/Simple/"},
            { EnumPrefixPath.GamePrefab, "Assets/Game/Prefabs/"},
            { EnumPrefixPath.Settings, "Assets/Game/Settings/"},
            { EnumPrefixPath.ScriptTemplates, "Tools/ScriptTemplates/"}
        };
        
        /// <summary>
        /// 获取资源加载地址
        /// </summary>
        public static string GetPath(string path, EnumPrefixPath enumPrefix = EnumPrefixPath.GamePrefab) {
            if (!PrefixPathDic.TryGetValue(enumPrefix, out string prefixPath)) {
                Debug.LogError($"暂未支持当前前缀枚举： {enumPrefix}");
                return "";
            }
            return Path.Combine(prefixPath, path);
        }
        
        /// <summary>
        /// 获取贴图加载地址
        /// </summary>
        public static string GetSpritePath(string path, EnumPrefixPath enumPrefix = EnumPrefixPath.GamePrefab) {
            return GetPath(Path.Combine(path, "png"), enumPrefix);
        }
        
        /// <summary>
        /// 获取文本加载地址
        /// </summary>
        public static string GetTxtPath(string path, EnumPrefixPath enumPrefix = EnumPrefixPath.GamePrefab) {
            return GetPath(Path.Combine(path, "txt"), enumPrefix);
        }
        
        /// <summary>
        /// 获取预制件加载地址
        /// </summary>
        public static string GetPrefabPath(string path, EnumPrefixPath enumPrefix = EnumPrefixPath.GamePrefab) {
            return GetPath(Path.Combine(path, "prefab"), enumPrefix);
        }
        
        /// <summary>
        /// 获取资源加载地址
        /// </summary>
        public static string GetAssetPath(string path, EnumPrefixPath enumPrefix = EnumPrefixPath.GamePrefab) {
            return GetPath(Path.Combine(path, "asset"), enumPrefix);
        }

        #endregion
    }
}
