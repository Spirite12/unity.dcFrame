using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    /// <summary>
    /// 通用对象池管理器。
    /// 按池名统一管理运行时对象复用。
    /// </summary>
    public class PoolMgr : MonoSingleton<PoolMgr> {
        protected override void OnDestroy() {
            ClearAll(true);
            base.OnDestroy();
        }
        
        /// <summary>
        /// 注册一个对象池，并按需预热。
        /// </summary>
        public void Register(GameObject prefab, string poolName = "DefaultPool", int prewarmCount = 0, int maxCacheCount = -1) {
            if (!prefab) {
                Debug.LogError("注册对象池失败，prefab 不能为空");
                return;
            }

            GameObjectPool pool = GetOrCreatePool(poolName, prefab, maxCacheCount);
            if (prewarmCount <= 0) {
                return;
            }

            pool.Prewarm(prewarmCount);
        }

        /// <summary>
        /// 从指定对象池中取出对象。
        /// </summary>
        public GameObject Get(string poolName, GameObject prefab, Transform parent = null, bool worldPositionStays = false) {
            GameObjectPool pool = GetOrCreatePool(poolName, prefab);
            if (pool == null) {
                return null;
            }

            GameObject poolObject = pool.Get(parent, worldPositionStays);
            if (!poolObject) {
                return null;
            }

            activePoolDic[poolObject] = pool;
            return poolObject;
        }

        /// <summary>
        /// 回收对象到原来的对象池。
        /// </summary>
        public bool Recycle(GameObject poolObject) {
            if (!poolObject) {
                return false;
            }

            CleanupNullActiveObjects();
            if (!activePoolDic.Remove(poolObject, out GameObjectPool pool)) {
                Debug.LogWarning($"回收对象失败，未找到对象 [{poolObject.name}] 对应的对象池");
                return false;
            }

            bool recycleSuccess = pool.Recycle(poolObject);
            TryRemovePool(pool);
            return recycleSuccess;
        }

        /// <summary>
        /// 判断对象池是否已存在。
        /// </summary>
        public bool ContainsPool(string poolName) {
            return poolDic.ContainsKey(poolName);
        }

        /// <summary>
        /// 获取当前缓存中的对象数量。
        /// </summary>
        public int GetCacheCount(string poolName) {
            return !poolDic.TryGetValue(poolName, out GameObjectPool pool) ? 0 : pool.CacheCount;
        }

        /// <summary>
        /// 获取当前激活中的对象数量。
        /// </summary>
        public int GetActiveCount(string poolName) {
            return !poolDic.TryGetValue(poolName, out GameObjectPool pool) ? 0 : pool.ActiveCount;
        }

        /// <summary>
        /// 清空指定对象池。
        /// </summary>
        public void Clear(string poolName, bool includeActive = false) {
            if (!poolDic.TryGetValue(poolName, out GameObjectPool pool)) {
                return;
            }

            CleanupNullActiveObjects();
            ClearPool(pool, includeActive);
            TryRemovePool(pool);
        }

        /// <summary>
        /// 清空所有对象池。
        /// </summary>
        private void ClearAll(bool includeActive = false) {
            CleanupNullActiveObjects();
            tempPoolList.Clear();
            foreach (GameObjectPool pool in poolDic.Values) {
                tempPoolList.Add(pool);
            }

            for (int i = 0; i < tempPoolList.Count; i++) {
                GameObjectPool pool = tempPoolList[i];
                ClearPool(pool, includeActive);
                TryRemovePool(pool);
            }

            tempPoolList.Clear();
        }

        /// <summary>
        /// 获取或创建对象池。
        /// </summary>
        private GameObjectPool GetOrCreatePool(string poolName, GameObject prefab, int? maxCacheCount = null) {
            if (!prefab) {
                Debug.LogError("创建对象池失败，prefab 不能为空");
                return null;
            }

            if (!poolDic.TryGetValue(poolName, out GameObjectPool pool)) {
                GameObject root = new GameObject(poolName);
                root.transform.SetParent(gameObject.transform, false);
                pool = new GameObjectPool(poolName, prefab, root.transform, maxCacheCount ?? -1);
                poolDic.Add(poolName, pool);
                return pool;
            }

            pool.UpdateConfig(prefab, maxCacheCount);
            return pool;
        }

        /// <summary>
        /// 清理激活集合中已被销毁的对象。
        /// </summary>
        private void CleanupNullActiveObjects() {
            tempNullObjectList.Clear();
            foreach (var kv in activePoolDic) {
                if (!kv.Key) {
                    tempNullObjectList.Add(kv.Key);
                }
            }

            for (int i = 0; i < tempNullObjectList.Count; i++) {
                GameObject poolObject = tempNullObjectList[i];
                if (!activePoolDic.TryGetValue(poolObject, out GameObjectPool pool)) {
                    continue;
                }

                activePoolDic.Remove(poolObject);
                pool.RemoveDestroyedActiveObject(poolObject);
            }
            tempNullObjectList.Clear();
        }

        /// <summary>
        /// 清空一个对象池。
        /// </summary>
        private void ClearPool(GameObjectPool pool, bool includeActive) {
            if (includeActive) {
                tempObjectList.Clear();
                pool.CollectActiveObjects(tempObjectList);
                for (int i = 0; i < tempObjectList.Count; i++) {
                    activePoolDic.Remove(tempObjectList[i]);
                }
                tempObjectList.Clear();
            }

            pool.Clear(includeActive);
        }

        /// <summary>
        /// 尝试移除空对象池。
        /// </summary>
        private void TryRemovePool(GameObjectPool pool) {
            if (!pool.IsEmpty) {
                return;
            }
            if (pool.Root) {
                Destroy(pool.Root.gameObject);
            }
            poolDic.Remove(pool.PoolName);
        }

        private readonly Dictionary<string, GameObjectPool> poolDic = new();
        private readonly Dictionary<GameObject, GameObjectPool> activePoolDic = new();
        private readonly List<GameObjectPool> tempPoolList = new();
        private readonly List<GameObject> tempObjectList = new();
        private readonly List<GameObject> tempNullObjectList = new();
    }
}
