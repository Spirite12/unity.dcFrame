using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    public abstract class CacheMgr {

        /// <summary>
        /// 本地记录类型
        /// </summary>
        public enum SaveType {
            /// <summary>
            /// 玩家ID
            /// </summary>
            PlayerId = 1,
            /// <summary>
            /// 区服ID
            /// </summary>
            Server = 2,
            /// <summary>
            /// 账号ID
            /// </summary>
            Account = 3
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init() {
            lastSaveTime = Time.time;
        }

        /// <summary>
        /// 游戏层初始化
        /// </summary>
        public static void GameInit(Func<string> _funcPlayer, Func<string> _funcServer, Func<string> _funcAccount) {
            funcPlayer = _funcPlayer;
            funcServer = _funcServer;
            funcAccount = _funcAccount;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public static void Destroy() {
            SaveAllCacheBase();
            ClearAllCacheBase();
        }

        /// <summary>
        /// 定时保存机制
        /// </summary>
        public static void FixedUpdate() {
            if (Time.time - lastSaveTime < SaveDuration) {
                return;
            }
            lastSaveTime = Time.time;
            SaveAllCacheBase();
        }

        /// <summary>
        /// 获取项目组本地记录类型值
        /// </summary>
        public static string GetSaveTypeValue(SaveType enumSaveType) {
            string value = "";
            switch (enumSaveType) {
                case SaveType.PlayerId:
                    value = funcPlayer?.Invoke();
                    break;
                case SaveType.Account:
                    value = funcAccount?.Invoke();
                    break;
                case SaveType.Server:
                    value = funcServer?.Invoke();
                    break;
            }
            if (value == null || string.IsNullOrEmpty(value)) {
                Debug.LogError($"enumSaveType 为：{enumSaveType} 的对应回调返回错误");
                return "";
            }
            return value;
        }

        /// <summary>
        /// 保存所有的缓存本地数据
        /// 调用时机：退游、切换账号、或定时保存
        /// 后续如果数据量大，记得分帧处理
        /// </summary>
        public static void SaveAllCacheBase() {
            foreach (var cacheBase in CacheBaseList) {
                cacheBase.SaveFileData();
            }
        }

        /// <summary>
        /// 清除当前的缓存数据
        /// 调用时机：退游、切换账号
        /// </summary>
        public static void ClearAllCacheBase() {
            CacheBaseList.Clear();
        }

        /// <summary>
        /// 添加缓存数据
        /// </summary>
        /// <param name="cache"></param>
        public static void AddCacheBase(CacheBase cache) {
            CacheBaseList.Add(cache);
        }

        private static readonly List<CacheBase> CacheBaseList = new List<CacheBase>();
        /// <summary>
        /// 定时保存机制，单位秒
        /// </summary>
        private const float SaveDuration = 5f;
        /// <summary>
        /// 上一次回收时刻
        /// </summary>
        private static float lastSaveTime = 0;
        private static Func<string> funcPlayer;
        private static Func<string> funcServer;
        private static Func<string> funcAccount;
    }
}