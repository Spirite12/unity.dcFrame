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
            if (!string.IsNullOrEmpty(redTipPrefabAddress)) {
                Asset.Release(redTipPrefabAddress);
                redTipPrefabAddress = "";
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
            var address = Asset.GetPrefabPath("Frame/RedTip");
            GameObject prefab = await Asset.LoadAsset<GameObject>(address);
            if (!prefab) {
                return;
            }
            redTipPrefabAddress = address;
            var goNew = Instantiate(prefab, transform);
            redTipUI = goNew.GetComponent<RedTipUI>();
            redTipUI.RenderRedTipStatus(redTipName, redTipId);
        }

        private RedTipUI redTipUI;
        /// <summary>
        /// 资源加载地址
        /// </summary>
        private string redTipPrefabAddress = "";
        [SerializeField]
        private string redTipName = "";
        [SerializeField]
        private int redTipId = 0;
    }
}

