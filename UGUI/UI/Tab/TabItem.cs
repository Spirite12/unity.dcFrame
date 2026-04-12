using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DCFrame.UGUI {
    /// <summary>
    /// 单个一级 Tab 需要的节点引用。
    /// </summary>
    [Serializable]
    public sealed class TabItem {
        /// <summary>
        /// 绑定点击回调。
        /// </summary>
        public void BindClick(Action onClick) {
            if (Root == null || onClick == null) {
                return;
            }
            UnbindClick();
            ResolveStateNodes();
            button = button != null ? button : Root.GetComponent<Button>();
            if (button == null){
                button = Root.AddComponent<Button>();
            }
            buttonHandler = delegate { onClick.Invoke(); };
            button.onClick.AddListener(buttonHandler);
        }

        /// <summary>
        /// 移除当前 Tab 的点击回调。
        /// </summary>
        public void UnbindClick() {
            if (button != null && buttonHandler != null) {
                button.onClick.RemoveListener(buttonHandler);
            }
            buttonHandler = null;
        }

        /// <summary>
        /// 根据选中态和禁用态刷新显示节点。
        /// </summary>
        public void SetState(bool isSelected, bool isDisabled) {
            ResolveStateNodes();
            if (PressObject != null) {
                PressObject.SetActive(isSelected && !isDisabled);
            }

            if (NormalObject != null) {
                NormalObject.SetActive(!isSelected && !isDisabled);
            }

            if (DisabledObject != null) {
                DisabledObject.SetActive(isDisabled);
            }
        }

        /// <summary>
        /// 根节点，点击事件和状态节点查找都以它为入口。
        /// </summary>
        public GameObject Root;

        /// <summary>
        /// 可选的 Button 组件。
        /// 若为空，会自动回退到 PointerClick 监听。
        /// </summary>
        public Button button;

        /// <summary>
        /// 选中态节点，默认自动查找名为 press 的子节点。
        /// </summary>
        public GameObject PressObject;

        /// <summary>
        /// 普通态节点，默认自动查找名为 normal 的子节点。
        /// </summary>
        public GameObject NormalObject;

        /// <summary>
        /// 禁用态节点，默认自动查找名为 none 的子节点。
        /// </summary>
        public GameObject DisabledObject;

        /// <summary>
        /// 延迟解析默认命名的状态节点，减少外部手工配置。
        /// </summary>
        private void ResolveStateNodes() {
            if (Root == null) {
                return;
            }

            if (PressObject == null) {
                Transform press = Root.transform.Find("GoPress");
                PressObject = press != null ? press.gameObject : null;
            }

            if (NormalObject == null) {
                Transform normal = Root.transform.Find("GoNormal");
                NormalObject = normal != null ? normal.gameObject : null;
            }

            if (DisabledObject == null) {
                Transform none = Root.transform.Find("GoNone");
                DisabledObject = none != null ? none.gameObject : null;
            }
        }

        private UnityAction buttonHandler;
    }
}
