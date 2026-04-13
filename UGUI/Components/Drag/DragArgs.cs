using System;
using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 拖拽配置参数。
    /// </summary>
    [Serializable]
    public sealed class DragArgs<TTemplate, TData> {
        /// <summary>
        /// 拖拽实例挂载的 Canvas 根节点。
        /// </summary>
        public RectTransform CanvasRoot;

        /// <summary>
        /// 参与屏幕坐标换算的 UI 相机。
        /// </summary>
        public Camera UICamera;

        /// <summary>
        /// 负责转发拖拽事件的监听组件。
        /// </summary>
        public DragEventListener EventListener;

        /// <summary>
        /// 当前实例数据中的唯一标识。
        /// </summary>
        public int UniqueId;

        /// <summary>
        /// 根据当前拖拽位置判断是否允许开始拖拽，并返回拖拽模板数据。
        /// 返回 null 表示本次不能开始拖拽。
        /// </summary>
        public Func<Vector2, TTemplate> CheckStartDrag;

        /// <summary>
        /// 根据模板数据解析拖拽预制体。
        /// </summary>
        public Func<TTemplate, GameObject> CreateDragObject;

        /// <summary>
        /// 根据模板数据构造拖拽数据。
        /// </summary>
        public Func<TTemplate, TData> CreateDragData;

        /// <summary>
        /// 创建出拖拽实例后触发。
        /// </summary>
        public Action<TData, RectTransform> StartDrag;

        /// <summary>
        /// 拖拽过程中持续回调。
        /// </summary>
        public Action<RectTransform, TData, Vector2> OnDrag;

        /// <summary>
        /// 拖拽结束时回调。
        /// </summary>
        public Action<RectTransform, TData> EndDrag;
    }
}
