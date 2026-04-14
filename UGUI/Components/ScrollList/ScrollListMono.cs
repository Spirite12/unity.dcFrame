using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// ScrollList 组件挂载入口，负责暴露配置与运行时调用方法。
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollListMono : MonoBehaviour {
        [Tooltip("模板预制件")]
        public GameObject goItem;
        [Tooltip("四周内边距")]
        public RectOffset padding = new RectOffset();
        [Tooltip("元素间距")]
        public Vector2 spacing;
        [Tooltip("布局类型")]
        public LayoutType layoutType = LayoutType.Grid;
        [Tooltip("垂直排列方向")]
        public bool isTopToBottom = true;
        
        #region LayoutType.Grid

        [Tooltip("约束方式")]
        public GridLayoutGroup.Constraint constraint = GridLayoutGroup.Constraint.Flexible;
        [Tooltip("固定行列数量，只有固定约束时生效")]
        public int constraintCount = 1;
        [Tooltip("起始排列方向")]
        public GridLayoutGroup.Axis axis = GridLayoutGroup.Axis.Horizontal;

        #endregion

        #region LayoutType.Horizontal and Vertical

        [Tooltip("是否使用子预制件的宽高")]
        public bool useItemSizeDelta;
        
        #endregion
        
        
        [HideInInspector]
        [Tooltip("滚动定位时每滑动多少距离耗时 0.1 秒")]
        public float tweenDistanceStep = 100f;
        [HideInInspector]
        [Tooltip("在可视区域基础上额外缓存的 Item 数量")]
        public int extraItemCount = 4;

        /// <summary>
        /// 支持的布局类型。
        /// </summary>
        public enum LayoutType {
            /// <summary>
            /// 垂直
            /// </summary>
            Vertical,
            /// <summary>
            /// 水平
            /// </summary>
            Horizontal,
            /// <summary>
            /// 网格
            /// </summary>
            Grid
        }

        /// <summary>
        /// 当前 ScrollRect 组件。
        /// </summary>
        public ScrollRect ScrollRect => scrollRect;

        /// <summary>
        /// 当前列表控制器。
        /// </summary>
        public ScrollList List => scrollList;
        
        /// <summary>
        /// 初始化组件并建立列表控制器。
        /// </summary>
        private void Awake() {
            scrollRect = GetComponent<ScrollRect>();
            scrollList = new ScrollList(this);
        }
        
        /// <summary>
        /// 销毁时移除运行时监听。
        /// </summary>
        private void OnDestroy() {
            scrollList?.Dispose();
        }

        /// <summary>
        /// 设置列表数据，并可选传入绑定回调。
        /// </summary>
        public void InitData(IList dataList) {
            scrollList.InitData(dataList);
        }

        /// <summary>
        /// 仅刷新当前列表显示，不替换数据源。
        /// </summary>
        public void Refresh() {
            scrollList.Refresh();
        }

        /// <summary>
        /// 清空当前列表显示。
        /// </summary>
        public void Clear() {
            scrollList.Clear();
        }

        /// <summary>
        /// 滚动到指定索引位置。
        /// </summary>
        public void ScrollToIndex(int index, bool useTween = false) {
            scrollList.ScrollToIndex(index, useTween);
        }

        private ScrollRect scrollRect;
        private ScrollList scrollList;
    }
}
