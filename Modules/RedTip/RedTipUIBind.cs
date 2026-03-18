using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DCFrame {
    /// <summary>
    /// 创建红点，且监听红点数据变化
    /// </summary>
    public class RedTipUIBind : MonoBehaviour {
        public string RedTipName => redTipName;
        public int RedTipId => redTipId;

        public void InitRedTipNameAndId(string redTipName, int redTipId = 0) {
            this.redTipName = redTipName;
            this.redTipId = redTipId;
            RenderActiveStatus();
            _ = CreateRedTipGameObject();
        }

        void Awake() {
            EventMgr.AddListener<string, bool, int>(EventBase.Frame.RedTipRefreshActive, RedTipRefreshActive);
        }

        void Start() {
            RenderActiveStatus();
            _ = CreateRedTipGameObject();
        }

        void OnDestroy() {
            EventMgr.RemoveListener<string, bool, int>(EventBase.Frame.RedTipRefreshActive, RedTipRefreshActive);
            if (redTipUI) {
                DestroyImmediate(redTipUI.gameObject);
            }
        }

        /// <summary>
        /// 红点状态监听
        /// </summary>
        private void RedTipRefreshActive(string _redTipName, bool isActive, int id) {
            if (redTipName == "" || redTipName != _redTipName || redTipId != id) {
                return;
            }
            gameObject.SetActive(isActive);
            if (isActive && redTipUI) {
                redTipUI.RenderRedTipStatus(redTipName, redTipId);
            }
        }

        /// <summary>
        /// 渲染激活状态
        /// </summary>
        private void RenderActiveStatus() {
            if (redTipName == "") {
                return;
            }
            bool active = RedTipMgr.IsActive(redTipName, redTipId);
            gameObject.SetActive(active);
        }

        /// <summary>
        /// 创建UI红点的逻辑
        /// </summary>
        private async UniTask CreateRedTipGameObject() {
            if (redTipName == "") {
                return;
            }
            GameObject prefab = await Asset.LoadAsset<GameObject>(Asset.GetPrefabPath("Frame/RedTip"));
            var goNew = Instantiate(prefab, transform);
            redTipUI = goNew.GetComponent<RedTipUI>();
            redTipUI.RenderRedTipStatus(redTipName, redTipId);
        }

        private RedTipUI redTipUI;
        [SerializeField]
        private string redTipName = "";
        [SerializeField]
        private int redTipId = 0;
    }
}

