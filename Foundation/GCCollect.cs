using System;
using UnityEngine;

namespace DCFrame.Foundation {
    public class GCCollect {
        public static void Init() {
            lastCollectTime = Time.time;
            AddListeners();
        }

        public static void Destroy() {
            unhandledLowMemoryWarning = false;
            RemoveListener();
        }
        
        /// <summary>
        /// 回收接口
        /// </summary>
        public static void GcCollect() {
            if (!unhandledLowMemoryWarning || Time.time - lastCollectTime < CollectDuration) {
                return;
            }

            Resources.UnloadUnusedAssets();
            GC.Collect();
            lastCollectTime = Time.time;
            unhandledLowMemoryWarning = false;
        }
        
        private static void AddListeners() {
            Application.lowMemory += OnLowMemory;
            UIBase.OnUnLoadUI += GcCollect;
        }

        private static void RemoveListener() {
            Application.lowMemory -= OnLowMemory;
            UIBase.OnUnLoadUI -= GcCollect;
        }
        
        /// <summary>
        /// 接收到了低内存的事件
        /// </summary>
        private static void OnLowMemory() {
            unhandledLowMemoryWarning = true;
            GcCollect();
        }

        /// <summary>
        /// 接收UI的GC事件
        /// </summary>
        private static void GcCollect(UIBase uiBase) {
            GcCollect();
        }
        
        /// <summary>
        /// 回收周期，单位秒
        /// </summary>
        private const float CollectDuration = 10f;
        
        /// <summary>
        /// 上一次回收时刻
        /// </summary>
        private static float lastCollectTime = 0;
        
        /// <summary>
        /// 是否有未处理的低内存警告
        /// </summary>
        private static bool unhandledLowMemoryWarning = false;
    }
}

