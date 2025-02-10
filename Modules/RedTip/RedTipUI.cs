using UnityEngine;
using UnityEngine.UI;

namespace DCFrame {
    public class RedTipUI : MonoBehaviour {
        public Text TxtNumber;

        /// <summary>
        /// 渲染红点的显示状态
        /// </summary>
        public void RenderRedTipStatus(string redTipName, int redTipId) {
            RedTipBase redTip = RedTipMgr.GetRedTip(redTipName, redTipId);
            bool isTypeNumber = redTip.type == RedTipBase.RedTipType.Number;
            TxtNumber.gameObject.SetActive(isTypeNumber);
            if (!isTypeNumber) {
                return;
            }
            TxtNumber.text = redTip.GetChildNum(true).ToString();
        }
    }
}

