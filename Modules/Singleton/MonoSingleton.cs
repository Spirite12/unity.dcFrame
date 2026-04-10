using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DCFrame {
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> {

        private static T mInstance = null;
        public static T Instance {
            get {
                if (mInstance == null) {
                    mInstance = FindObjectOfType<T>();
                    if (FindObjectsOfType<T>().Length > 1) {
                        Debug.LogWarning("More than 1");
                        return mInstance;
                    }
                    if (mInstance == null) {
                        var instanceName = typeof(T).Name;
                        var instanceObj = GameObject.Find(instanceName);
                        if (!instanceObj) {
                            instanceObj = new GameObject(instanceName);
                        }
                        mInstance = instanceObj.GetComponent<T>();
                        if (!mInstance) {
                            mInstance = instanceObj.AddComponent<T>();
                        }
                        DontDestroyOnLoad(instanceObj);
                    } else {
                        Debug.LogFormat("Already exist: {0}", mInstance.name);
                    }
                }
                return mInstance;
            }
        }
        protected virtual void OnDestroy() {
            ReleaseAssetAll();
            mInstance = null;
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="address"></param> 地址
        /// <typeparam name="T1"></typeparam> 类型
        protected async UniTask<T1> LoadAsset<T1>(string address) where T1 : Object {
            var asset = await Asset.LoadAsset<T1>(address);
            if (!asset) {
                return null;
            }
            if (addressRefDic.TryGetValue(address, out int count)) {
                addressRefDic[address] = count + 1;
            } else {
                addressRefDic[address] = 1;
            }
            return asset;
        }
        
        /// <summary>
        /// 释放资源
        /// </summary>
        protected void ReleaseAsset(string address) {
            if (!addressRefDic.ContainsKey(address)) {
                return;
            }
            addressRefDic[address] -= 1;
            if (addressRefDic[address] <= 0) {
                addressRefDic.Remove(address);
            }
            Asset.Release(address);
        }

        /// <summary>
        /// 释放全部资源
        /// </summary>
        private void ReleaseAssetAll() {
            foreach (var kv in addressRefDic) {
                for (int i = 0; i < kv.Value; i++) {
                    Asset.Release(kv.Key);
                }
            }
            addressRefDic.Clear();
        }

        /// <summary>
        /// 资源加载地址列表
        /// </summary>
        private readonly Dictionary<string, int> addressRefDic = new();
    }
}
