using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 列表项基类，业务层可继承并覆写刷新逻辑。
    /// </summary>
    public class ScrollListItemMono : MonoBehaviour {
        /// <summary>
        /// 当前数据索引。
        /// </summary>
        public int Index => index;
        /// <summary>
        /// 当前绑定的数据对象。
        /// </summary>
        public object Data => data;
        public RectTransform RectTransform => rectTransform;

        void Awake() {
            rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 刷新列表项数据。
        /// </summary>
        public void Refresh(int itemIndex, object itemData) {
            index = itemIndex;
            data = itemData;
            OnRefresh(itemIndex, itemData);
        }

        /// <summary>
        /// 回收前的清理入口。
        /// </summary>
        public void Recycle() {
            index = -1;
            data = null;
            OnRecycle();
        }

        /// <summary>
        /// 供子类覆写的数据刷新回调。
        /// </summary>
        protected virtual void OnRefresh(int itemIndex, object itemData) {
        }

        /// <summary>
        /// 供子类覆写的回收回调。
        /// </summary>
        protected virtual void OnRecycle() {
        }

        private int index = -1;
        private object data;
        private RectTransform rectTransform;
    }
}
