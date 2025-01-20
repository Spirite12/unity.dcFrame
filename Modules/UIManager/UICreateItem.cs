using UnityEditor;
using UnityEngine;

namespace DCFrame {
    [ExecuteInEditMode]
    public class UICreateItem : UIMono {
        [HideInInspector]
        public UIMono uiMono;
        [HideInInspector]
        public GameObject goInstance;
        /// <summary>
        /// 预制体引用
        /// </summary>
        #pragma warning disable
        public GameObject goItem;

        /// <summary>
        /// 传递父节点的排序值
        /// </summary>
        /// <param name="orderValue"></param>
        public void SetParentOrder(int orderValue) {
            if (uiOrder == null) {
                InstanceAndSetMono();
            }
            uiOrder.SetOrder(orderValue);
        }

        /// <summary>
        /// 获取子预制体实例名称
        /// </summary>
        /// <returns></returns>
        public string GetGoItemName() {
            return goItem.name;
        }

        public void Awake() {
            // 是预制体编辑模式下
#if UNITY_EDITOR
            if (UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(this)) {
                return;
            }
#endif

            InstanceAndSetMono();
        }

        public void OnDestroy() {
            if (goInstance) {
                GameObject.DestroyImmediate(goInstance);
            }
        }

        /// <summary>
        /// 设置实例化并赋值UIMono
        /// </summary>
        private void InstanceAndSetMono() {
#if UNITY_EDITOR
            if (Application.isPlaying && !isPlayModeCreate) {
                return;
            }
            goInstance = PrefabUtility.InstantiatePrefab(goItem, this.transform) as GameObject;
#endif


            if (goInstance != null) {
                uiMono = goInstance.GetComponent<UIMono>();
                uiOrder = goInstance.GetComponent<UIOrder>();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 是否是运行模式就创建
        /// </summary>
        [SerializeField]
        private bool isPlayModeCreate = true;
#endif

        protected override bool IsShowTool() {
            return false;
        }

        private UIOrder uiOrder;
    }
}

