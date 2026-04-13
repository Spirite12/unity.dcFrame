using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DCFrame.UGUI {
    /// <summary>
    /// 拖拽事件监听组件，统一把 UGUI 的拖拽回调转成可订阅事件。
    /// </summary>
    public sealed class DragEventListener : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
        /// <summary>
        /// 开始拖拽时触发。
        /// </summary>
        public event Action<PointerEventData> BeginDragEvent;

        /// <summary>
        /// 拖拽过程中持续触发。
        /// </summary>
        public event Action<PointerEventData> DragEvent;

        /// <summary>
        /// 结束拖拽时触发。
        /// </summary>
        public event Action<PointerEventData> EndDragEvent;

        /// <summary>
        /// UGUI 开始拖拽回调。
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData) {
            BeginDragEvent?.Invoke(eventData);
        }

        /// <summary>
        /// UGUI 拖拽中回调。
        /// </summary>
        public void OnDrag(PointerEventData eventData) {
            DragEvent?.Invoke(eventData);
        }

        /// <summary>
        /// UGUI 结束拖拽回调。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData) {
            EndDragEvent?.Invoke(eventData);
        }
    }
}

