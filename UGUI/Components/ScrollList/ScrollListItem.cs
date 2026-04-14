using UnityEngine;

namespace DCFrame.UGUI {
    /// <summary>
    /// 列表项基类，业务层可继承并覆写刷新逻辑。
    /// </summary>
    public class ScrollListItem : MonoBehaviour {
        /// <summary>
        /// 当前数据索引。
        /// </summary>
        public int Index => index;

        /// <summary>
        /// 当前绑定的数据对象。
        /// </summary>
        public object Data => data;

        /// <summary>
        /// 当前节点的 RectTransform。
        /// </summary>
        public RectTransform RectTransform {
            get {
                if (!rectTransform) {
                    rectTransform = transform as RectTransform;
                }
                return rectTransform;
            }
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
        /// 清空当前绑定状态，供虚拟列表回收时复位。
        /// </summary>
        public void ResetState() {
            index = -1;
            data = null;
        }

        /// <summary>
        /// 回收前的清理入口。
        /// </summary>
        public void Recycle() {
            ResetState();
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
