using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DCFrame {
    public class AAHotUpdate {
        /// <summary>
        /// 加载本地热更启动配置。
        /// </summary>
        public static async UniTask<bool> LoadSettings() {
            TextAsset settingsAsset = await Asset.LoadAsset<TextAsset>(AAConst.AAHotUpdateSettingsPath);
            try {
                if (settingsAsset == null || string.IsNullOrWhiteSpace(settingsAsset.text)) {
                    enableHotUpdate = false;
                    Debug.LogWarning("本地热更启动配置为空，已关闭热更。");
                    return false;
                }

                HotUpdateSettingsData settingsData = JsonUtility.FromJson<HotUpdateSettingsData>(settingsAsset.text);
                enableHotUpdate = settingsData != null && settingsData.enableHotUpdate;
                return enableHotUpdate;
            }
            finally {
                if (settingsAsset != null) {
                    Asset.Release(AAConst.AAHotUpdateSettingsPath);
                }
            }
        }

        /// <summary>
        /// 获取热更是否开启。
        /// </summary>
        public static bool IsEnabled() {
            return enableHotUpdate;
        }

        /// <summary>
        /// 获取启动前热更资源需要下载的字节数。
        /// </summary>
        public static async UniTask<long> GetStartupDownloadSize() {
            return await GetDownloadSize(AAConst.AAGameStartUpLabel);
        }

        /// <summary>
        /// 下载启动前热更资源。
        /// </summary>
        public static async UniTask<bool> DownloadStartup() {
            return await DownloadDependencies(AAConst.AAGameStartUpLabel);
        }

        /// <summary>
        /// 获取指定标签或地址需要下载的字节数。
        /// </summary>
        public static async UniTask<long> GetDownloadSize(string key) {
            if (!enableHotUpdate || string.IsNullOrEmpty(key)) {
                return 0;
            }

            if (!await HasResourceLocation(key)) {
                return 0;
            }

            var handle = Addressables.GetDownloadSizeAsync(key);
            await handle.Task;
            long size = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : 0;
            Addressables.Release(handle);
            return size;
        }

        /// <summary>
        /// 判断指定标签或地址是否需要下载。
        /// </summary>
        public static async UniTask<bool> NeedDownload(string key) {
            return await GetDownloadSize(key) > 0;
        }

        /// <summary>
        /// 下载指定标签或地址的依赖资源。
        /// </summary>
        public static async UniTask<bool> DownloadDependencies(string key) {
            if (!enableHotUpdate || string.IsNullOrEmpty(key)) {
                return true;
            }

            if (!await HasResourceLocation(key)) {
                return true;
            }

            var handle = Addressables.DownloadDependenciesAsync(key);
            await handle.Task;
            bool isSuccess = handle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(handle);
            return isSuccess;
        }

        /// <summary>
        /// 判断指定标签或地址是否存在可下载定位，避免空标签触发 Addressables InvalidKeyException。
        /// </summary>
        private static async UniTask<bool> HasResourceLocation(string key) {
            var handle = Addressables.LoadResourceLocationsAsync(key);
            await handle.Task;
            bool hasLocation = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0;
            Addressables.Release(handle);
            return hasLocation;
        }

        private static bool enableHotUpdate;

        /// <summary>
        /// 本地热更启动配置数据。
        /// </summary>
        [Serializable]
        private class HotUpdateSettingsData {
            public bool enableHotUpdate;
        }
    }
}
