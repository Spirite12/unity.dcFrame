using System;
using System.Collections.Generic;

namespace DCFrame {
    public class EventMgr {

        /// <summary>
        /// 监听不需要参数传递的事件
        /// </summary>
        public static void AddListener(string eventName, Action callback) {
            AddListenerBase(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（1个参数）
        /// </summary>
        public static void AddListener<T>(string eventName, Action<T> callback) {
            AddListenerBase(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（2个参数）
        /// </summary>
        public static void AddListener<T1, T2>(string eventName, Action<T1, T2> callback) {
            AddListenerBase(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（3个参数）
        /// </summary>
        public static void AddListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) {
            AddListenerBase(eventName, callback);
        }
        /// <summary>
        /// 添加事件监听（4个参数）
        /// </summary>
        public static void AddListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) {
            AddListenerBase(eventName, callback);
        }

        /// <summary>
        /// 移除不需要参数的事件
        /// </summary>
        public static void RemoveListener(string eventName, Action callback) {
            RemoveListenerBase(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（1个参数）
        /// </summary>
        public static void RemoveListener<T>(string eventName, Action<T> callback) {
            RemoveListenerBase(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（2个参数）
        /// </summary>
        public static void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback) {
            RemoveListenerBase(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（3个参数）
        /// </summary>
        public static void RemoveListener<T1, T2, T3>(string eventName, Action<T1, T2, T3> callback) {
            RemoveListenerBase(eventName, callback);
        }
        /// <summary>
        /// 移除对应的事件监听（4个参数）
        /// </summary>
        public static void RemoveListener<T1, T2, T3, T4>(string eventName, Action<T1, T2, T3, T4> callback) {
            RemoveListenerBase(eventName, callback);
        }

        /// <summary>
        /// 事件触发（不需要参数的）
        /// </summary>
        public static void DispatchEvent(string eventName) {
            List<Delegate> callbacks = SnapshotCallbacks(eventName);
            if (callbacks == null) {
                return;
            }
            try {
                for (int i = 0; i < callbacks.Count; i++) {
                    Delegate callback = callbacks[i];
                    (callback as Action)?.Invoke();
                }
            } finally {
                ReleaseSnapshotList(callbacks);
            }
        }
        /// <summary>
        /// 事件触发（1个参数）
        /// </summary>
        public static void DispatchEvent<T>(string eventName, T info) {
            List<Delegate> callbacks = SnapshotCallbacks(eventName);
            if (callbacks == null) {
                return;
            }
            try {
                for (int i = 0; i < callbacks.Count; i++) {
                    Delegate callback = callbacks[i];
                    (callback as Action<T>)?.Invoke(info);
                }
            } finally {
                ReleaseSnapshotList(callbacks);
            }
        }
        /// <summary>
        /// 事件触发（2个参数）
        /// </summary>
        public static void DispatchEvent<T1, T2>(string eventName, T1 info1, T2 info2) {
            List<Delegate> callbacks = SnapshotCallbacks(eventName);
            if (callbacks == null) {
                return;
            }
            try {
                for (int i = 0; i < callbacks.Count; i++) {
                    Delegate callback = callbacks[i];
                    (callback as Action<T1, T2>)?.Invoke(info1, info2);
                }
            } finally {
                ReleaseSnapshotList(callbacks);
            }
        }
        /// <summary>
        /// 事件触发（3个参数）
        /// </summary>
        public static void DispatchEvent<T1, T2, T3>(string eventName, T1 info1, T2 info2, T3 info3) {
            List<Delegate> callbacks = SnapshotCallbacks(eventName);
            if (callbacks == null) {
                return;
            }
            try {
                for (int i = 0; i < callbacks.Count; i++) {
                    Delegate callback = callbacks[i];
                    (callback as Action<T1, T2, T3>)?.Invoke(info1, info2, info3);
                }
            } finally {
                ReleaseSnapshotList(callbacks);
            }
        }
        /// <summary>
        /// 事件触发（4个参数）
        /// </summary>
        public static void DispatchEvent<T1, T2, T3, T4>(string eventName, T1 info1, T2 info2, T3 info3, T4 info4) {
            List<Delegate> callbacks = SnapshotCallbacks(eventName);
            if (callbacks == null) {
                return;
            }
            try {
                for (int i = 0; i < callbacks.Count; i++) {
                    Delegate callback = callbacks[i];
                    (callback as Action<T1, T2, T3, T4>)?.Invoke(info1, info2, info3, info4);
                }
            } finally {
                ReleaseSnapshotList(callbacks);
            }
        }

        /// <summary>
        /// 添加事件
        /// </summary>
        private static void AddListenerBase(string eventName, Delegate callBack) {
            if (eventDic.TryGetValue(eventName, out var value)) {
                value.Add(callBack);
            }else {
                eventDic.Add(eventName, new List<Delegate>(){ callBack });
            }
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        private static void RemoveListenerBase(string eventName, Delegate callBack) {
            if (eventDic.TryGetValue(eventName, out List<Delegate> eventList)) {
                eventList.Remove(callBack);
                if (eventList.Count == 0) {
                    eventDic.Remove(eventName);
                }
            }
        }

        /// <summary>
        /// 清空事件
        /// </summary>
        public static void Clear() {
            eventDic.Clear();
        }
        
        private static List<Delegate> SnapshotCallbacks(string eventName) {
            if (!eventDic.TryGetValue(eventName, out List<Delegate> value) || value.Count == 0) {
                return null;
            }
            List<Delegate> tempList = GetSnapshotList();
            tempList.AddRange(value);
            return tempList;
        }

        private static List<Delegate> GetSnapshotList() {
            if (dispatchSnapshotPool.Count > 0) {
                List<Delegate> list = dispatchSnapshotPool.Pop();
                list.Clear();
                return list;
            }
            return new List<Delegate>(SnapshotInitCapacity);
        }

        private static void ReleaseSnapshotList(List<Delegate> list) {
            if (list.Capacity > MaxSnapshotCapacity) {
                list = new List<Delegate>(SnapshotInitCapacity);
            } else {
                list.Clear();
            }

            dispatchSnapshotPool.Push(list);
        }

        private static readonly Dictionary<string, List<Delegate>> eventDic = new();
        /// <summary>
        /// 快照池
        /// </summary>
        private static readonly Stack<List<Delegate>> dispatchSnapshotPool = new();
        /// <summary>
        /// 快照初始容量
        /// </summary>
        private const int SnapshotInitCapacity = 64;
        /// <summary>
        /// 最大快照容量
        /// </summary>
        private const int MaxSnapshotCapacity = 256;
    }
}
