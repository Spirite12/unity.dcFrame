using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DCFrame.UGUI {
    /// <summary>
    /// 拖拽控制器，负责按需创建拖拽中的 UI 实例并驱动位置更新。
    /// </summary>
    public sealed class Drag<TTemplate, TData> : IDrag {
        /// <summary>
        /// 当前拖拽中的 UI 节点。
        /// </summary>
        public RectTransform DragRectTransform => dragRectTransform;

        /// <summary>
        /// 当前是否处于拖拽中。
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// 当前拖拽中的业务数据。
        /// </summary>
        public TData DragData => dragData;

        /// <summary>
        /// 构造拖拽控制器。
        /// </summary>
        public Drag(DragArgs<TTemplate, TData> args) {
            this.args = args ?? throw new ArgumentNullException(nameof(args));
        }

        /// <summary>
        /// 绑定拖拽事件。
        /// </summary>
        public void Init() {
            if (isInitialized) {
                return;
            }

            args.EventListener.BeginDragEvent += OnBeginDrag;
            args.EventListener.DragEvent += OnDrag;
            args.EventListener.EndDragEvent += OnEndDrag;
            isInitialized = true;
        }

        /// <summary>
        /// 主动销毁拖拽实例并解除事件绑定。
        /// </summary>
        public void Destroy() {
            if (isInitialized) {
                args.EventListener.BeginDragEvent -= OnBeginDrag;
                args.EventListener.DragEvent -= OnDrag;
                args.EventListener.EndDragEvent -= OnEndDrag;
                isInitialized = false;
            }

            EndDragInternal();
        }

        /// <summary>
        /// 处理开始拖拽回调，在允许开始时创建拖拽实例并同步初始位置。
        /// </summary>
        private void OnBeginDrag(PointerEventData eventData) {
            if (isDragging) {
                return;
            }

            TTemplate dragTemplate = CheckStartDragInternal(eventData);
            if (ReferenceEquals(dragTemplate, null)) {
                return;
            }

            CreateDragObjectInternal(dragTemplate);
            ProcessDragging(eventData);
        }

        /// <summary>
        /// 处理拖拽中回调，只负责更新拖拽实例位置。
        /// </summary>
        private void OnDrag(PointerEventData eventData) {
            if (!isDragging) {
                return;
            }

            ProcessDragging(eventData);
        }

        /// <summary>
        /// 处理结束拖拽回调。
        /// </summary>
        private void OnEndDrag(PointerEventData eventData) {
            args.EndDrag?.Invoke(dragRectTransform, dragData);

            EndDragInternal();
        }

        /// <summary>
        /// 把屏幕坐标转换到 Canvas 局部坐标，并判断能否开始拖拽。
        /// </summary>
        private TTemplate CheckStartDragInternal(PointerEventData eventData) {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(args.CanvasRoot, eventData.position, args.UICamera, out var localPosition)) {
                return default;
            }

            return args.CheckStartDrag.Invoke(localPosition);
        }

        /// <summary>
        /// 更新拖拽实例位置并回调外部逻辑。
        /// </summary>
        private void ProcessDragging(PointerEventData eventData) {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    args.CanvasRoot,
                    eventData.position,
                    args.UICamera,
                    out var localPosition)) {
                return;
            }

            dragRectTransform.anchoredPosition = localPosition;
            args.OnDrag?.Invoke(dragRectTransform, dragData, eventData.position);
        }

        /// <summary>
        /// 创建拖拽实例与拖拽数据。
        /// </summary>
        private void CreateDragObjectInternal(TTemplate dragTemplate) {
            if (dragRectTransform) {
                throw new InvalidOperationException("重复创建拖拽实例，请先结束当前拖拽。");
            }

            GameObject templateObject = args.CreateDragObject.Invoke(dragTemplate);
            if (!templateObject) {
                throw new InvalidOperationException("CreateDragObject 返回了空对象，无法创建拖拽实例。");
            }

            GameObject instance = UnityEngine.Object.Instantiate(templateObject, args.CanvasRoot, false);
            dragRectTransform = instance.GetComponent<RectTransform>();
            if (!dragRectTransform) {
                throw new InvalidOperationException("拖拽预制体缺少 RectTransform，无法作为 UGUI 拖拽节点。");
            }

            dragData = args.CreateDragData.Invoke(dragTemplate);
            isDragging = true;

            args.StartDrag?.Invoke(dragData, dragRectTransform);
        }

        /// <summary>
        /// 结束当前拖拽并清理运行时状态。
        /// </summary>
        private void EndDragInternal() {
            DestroyDragObject();
            isDragging = false;
            dragData = default;
        }

        /// <summary>
        /// 销毁拖拽实例。
        /// </summary>
        private void DestroyDragObject() {
            if (dragRectTransform) {
                UnityEngine.Object.Destroy(dragRectTransform.gameObject);
                dragRectTransform = null;
            }
        }

        private readonly DragArgs<TTemplate, TData> args;

        private RectTransform dragRectTransform;
        private TData dragData;
        private bool isDragging;
        private bool isInitialized;
    }
}
