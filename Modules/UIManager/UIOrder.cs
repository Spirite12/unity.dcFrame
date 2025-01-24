using UnityEngine;

namespace DCFrame {
	public class UIOrder : MonoBehaviour {
        /// <summary>
        /// 获取当前界面的层级值
        /// </summary>
        public int GetOrderValue() {
            return orderValue;
        }

        /// <summary>
        /// 设置层级
        /// </summary>
        public void SetOrder(int addValue) {
            Awake();
            foreach (var canvas in canvasArray) {
                canvas.overrideSorting = true;
                canvas.sortingOrder = canvas.sortingOrder + addValue - orderValue;
            }

            foreach (var renderer in renderArray) {
                renderer.sortingOrder = renderer.sortingOrder + addValue - orderValue;
            }

            foreach (var uiItem in uiItemArray) {
                uiItem.SetParentOrder(addValue);
            }

            orderValue = addValue;
        }

        public void Awake() {
            if (canvasArray == null) {
                canvasArray = GetComponentsInChildren<Canvas>(true);
            }
            if (renderArray == null) {
                renderArray = GetComponentsInChildren<Renderer>(true);
            }
            if (uiItemArray == null) {
                uiItemArray = GetComponentsInChildren<UICreateItem>(true);
            }
        }

        public void OnDestroy() {
            orderValue = 0;
            canvasArray = null;
            renderArray = null;
        }

        private Canvas[] canvasArray;
        private Renderer[] renderArray;
        private UICreateItem[] uiItemArray;
        /// <summary>
        /// 当前界面的Order层级值
        /// </summary>
        private int orderValue = 0;
    }
}
