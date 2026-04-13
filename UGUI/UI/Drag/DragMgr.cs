using System.Collections.Generic;

namespace DCFrame.UGUI {
    /// <summary>
    /// 拖拽管理器，负责统一注册、查询与解绑拖拽控制器。
    /// </summary>
    public static class DragMgr {
        /// <summary>
        /// 绑定拖拽监听器。
        /// 若同一个唯一标识已经存在，会先解绑旧实例再注册新实例。
        /// </summary>
        public static Drag<TTemplate, TData> AddListener<TTemplate, TData>(DragArgs<TTemplate, TData> args) {
            RemoveListener(args.UniqueId);

            Drag<TTemplate, TData> drag = new Drag<TTemplate, TData>(args);
            drag.Init();
            DragDict[args.UniqueId] = drag;
            return drag;
        }

        /// <summary>
        /// 查询指定唯一标识对应的拖拽控制器。
        /// </summary>
        public static IDrag GetDragAddListener(int uniqueId) {
            DragDict.TryGetValue(uniqueId, out var dragUI);
            return dragUI;
        }

        /// <summary>
        /// 解绑指定唯一标识对应的拖拽控制器。
        /// </summary>
        public static void RemoveListener(int uniqueId) {
            if (!DragDict.TryGetValue(uniqueId, out var dragUI)) {
                return;
            }

            dragUI.Destroy();
            DragDict.Remove(uniqueId);
        }

        private static readonly Dictionary<int, IDrag> DragDict = new();
    }
}
