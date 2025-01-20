using System;
using UnityEngine;

namespace DCFrame {
    public class UIMono : MonoBehaviour {
        public static Action onClickMonoFile;
        public static Action onClickCtrlFile;
        public static Action onClickBtnMethods;

        [ContextMenu("定位Mono脚本", false)]
        private void FocusMonoFile() {
            onClickMonoFile?.Invoke();
        }

        [ContextMenu("定位Ctrl脚本", false)]
        private void FocusCtrlFile() {
            onClickCtrlFile?.Invoke();
        }

        [ContextMenu("生成按钮函数", false)]
        private void InstanceBtnMethods() {
            onClickBtnMethods?.Invoke();
        }

        [ContextMenu("定位Mono脚本", true)]
        private bool FocusMonoFileValidate() {
            return IsShowTool();
        }

        [ContextMenu("定位Ctrl脚本", true)]
        private bool FocusCtrlFileValidate() {
            return IsShowTool();
        }

        [ContextMenu("生成按钮函数", true)]
        private bool InstanceBtnMethodsValidate() {
            return IsShowTool();
        }

        /// <summary>
        /// 是否显示工具
        /// </summary>
        protected virtual bool IsShowTool() {
            return true;
        }
    }
}

