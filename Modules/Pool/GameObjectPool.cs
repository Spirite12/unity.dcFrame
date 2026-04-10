using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    /// <summary>
    /// 单个 GameObject 对象池。
    /// 负责 Unity 对象的创建、激活、回收和销毁。
    /// </summary>
    public class GameObjectPool {
        /// <summary>
        /// 创建一个 GameObject 对象池。
        /// </summary>
        public GameObjectPool(string poolName, GameObject prefab, Transform root, int maxCacheCount = -1) {
            PoolName = poolName;
            Prefab = prefab;
            Root = root;
            MaxCacheCount = maxCacheCount;
            cachePool = new Pool<GameObject>(CreatePoolObject, null, OnRecycleObject, DestroyPoolObject);
        }

        /// <summary>
        /// 更新对象池配置。
        /// </summary>
        public void UpdateConfig(GameObject prefab, int? maxCacheCount = null) {
            if (!Prefab) {
                Prefab = prefab;
            }
            else if (prefab && Prefab != prefab) {
                Debug.LogWarning($"对象池 [{PoolName}] 已绑定其他 prefab，将继续使用首次注册的 prefab：{Prefab.name}");
            }

            if (maxCacheCount.HasValue) {
                MaxCacheCount = maxCacheCount.Value;
            }
        }

        /// <summary>
        /// 预热对象池。
        /// </summary>
        public void Prewarm(int count) {
            if (count <= 0) {
                return;
            }

            CleanupNullCacheObjects();
            if (MaxCacheCount >= 0) {
                count = Mathf.Min(count, Mathf.Max(0, MaxCacheCount - cachePool.InactiveCount));
            }

            for (int i = 0; i < count; i++) {
                GameObject poolObject = cachePool.Create();
                if (!poolObject) {
                    return;
                }
                cachePool.Recycle(poolObject);
            }
        }

        /// <summary>
        /// 取出一个对象。
        /// </summary>
        public GameObject Get(Transform parent = null, bool worldPositionStays = false) {
            CleanupNullCacheObjects();
            CleanupNullActiveObjects();

            GameObject poolObject = cachePool.Get();
            if (!poolObject) {
                return null;
            }

            ActivateObject(poolObject, parent, worldPositionStays);
            activeSet.Add(poolObject);
            return poolObject;
        }

        /// <summary>
        /// 回收一个对象。
        /// </summary>
        public bool Recycle(GameObject poolObject) {
            if (!poolObject || !activeSet.Remove(poolObject)) {
                return false;
            }

            CleanupNullCacheObjects();
            if (MaxCacheCount >= 0 && cachePool.InactiveCount >= MaxCacheCount) {
                cachePool.Destroy(poolObject);
                return true;
            }

            cachePool.Recycle(poolObject);
            return true;
        }

        /// <summary>
        /// 移除一个已经被外部销毁的激活对象。
        /// </summary>
        public void RemoveDestroyedActiveObject(GameObject poolObject) {
            if (!activeSet.Remove(poolObject)) {
                return;
            }

            cachePool.Destroy(poolObject);
        }

        /// <summary>
        /// 收集当前仍处于激活状态的对象。
        /// </summary>
        public void CollectActiveObjects(List<GameObject> result) {
            CleanupNullActiveObjects();
            foreach (GameObject poolObject in activeSet) {
                result.Add(poolObject);
            }
        }

        /// <summary>
        /// 清空当前对象池。
        /// </summary>
        public void Clear(bool includeActive) {
            CleanupNullCacheObjects();
            cachePool.Clear();

            if (!includeActive) {
                return;
            }

            CleanupNullActiveObjects();
            tempObjectList.Clear();
            foreach (GameObject poolObject in activeSet) {
                tempObjectList.Add(poolObject);
            }

            for (int i = 0; i < tempObjectList.Count; i++) {
                cachePool.Destroy(tempObjectList[i]);
            }

            tempObjectList.Clear();
            activeSet.Clear();
        }

        /// <summary>
        /// 当前缓存中的对象数量。
        /// </summary>
        public int CacheCount {
            get {
                CleanupNullCacheObjects();
                return cachePool.InactiveCount;
            }
        }

        /// <summary>
        /// 当前激活中的对象数量。
        /// </summary>
        public int ActiveCount {
            get {
                CleanupNullActiveObjects();
                return activeSet.Count;
            }
        }

        /// <summary>
        /// 当前对象池是否为空。
        /// </summary>
        public bool IsEmpty => CacheCount == 0 && ActiveCount == 0;

        /// <summary>
        /// 池名。
        /// </summary>
        public string PoolName { get; }

        /// <summary>
        /// 预制体引用。
        /// </summary>
        public GameObject Prefab { get; private set; }

        /// <summary>
        /// 池根节点。
        /// </summary>
        public Transform Root { get; }

        /// <summary>
        /// 最大缓存数量，-1 表示不限制。
        /// </summary>
        public int MaxCacheCount { get; private set; }

        /// <summary>
        /// 激活对象并设置父节点。
        /// </summary>
        private static void ActivateObject(GameObject poolObject, Transform parent, bool worldPositionStays) {
            if (parent) {
                poolObject.transform.SetParent(parent, worldPositionStays);
            }
            else {
                poolObject.transform.SetParent(null, false);
            }

            poolObject.SetActive(true);
        }

        /// <summary>
        /// 创建一个新的池对象。
        /// </summary>
        private GameObject CreatePoolObject() {
            if (!Prefab) {
                Debug.LogError($"对象池 [{PoolName}] 未绑定 prefab");
                return null;
            }

            GameObject poolObject = Object.Instantiate(Prefab, Root);
            poolObject.name = Prefab.name;
            poolObject.SetActive(false);
            return poolObject;
        }

        /// <summary>
        /// 对象回收时重置父节点和基础变换。
        /// </summary>
        private void OnRecycleObject(GameObject poolObject) {
            if (!poolObject) {
                return;
            }

            poolObject.SetActive(false);
            Transform target = poolObject.transform;
            target.SetParent(Root, false);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        /// <summary>
        /// 销毁一个池对象。
        /// </summary>
        private static void DestroyPoolObject(GameObject poolObject) {
            if (poolObject) {
                Object.Destroy(poolObject);
            }
        }

        /// <summary>
        /// 清理缓存中已被外部销毁的对象。
        /// </summary>
        private void CleanupNullCacheObjects() {
            cachePool.RemoveInvalid((poolObject) => !poolObject);
        }

        /// <summary>
        /// 清理激活集合中已被外部销毁的对象。
        /// </summary>
        private void CleanupNullActiveObjects() {
            tempObjectList.Clear();
            foreach (GameObject poolObject in activeSet) {
                if (!poolObject) {
                    tempObjectList.Add(poolObject);
                }
            }
            foreach (var t in tempObjectList) {
                RemoveDestroyedActiveObject(t);
            }

            tempObjectList.Clear();
        }
        
        private readonly Pool<GameObject> cachePool;
        private readonly HashSet<GameObject> activeSet = new();
        private readonly List<GameObject> tempObjectList = new();
    }
}
