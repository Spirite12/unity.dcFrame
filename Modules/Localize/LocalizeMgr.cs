using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DCFrame {
    public class LocalizeMgr {
        /// <summary>
        /// 对预制体上的多语言进行本地化处理
        /// </summary>
        public static async UniTask LocalizePrefab(GameObject go) {
            var txtArray = go.GetComponentsInChildren<Text>();
            foreach (var text in txtArray) {
                if (!IsNeedLocalizeTxt(text.text)) {
                    continue;
                }
                text.text = Localize.GetText(text.text);
                text.font = await Localize.GetFont(LocalizeConst.KeyTxtFontNormal);
            }
            var txtMeshProArray = go.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var txtMeshPro in txtMeshProArray) {
                if (!IsNeedLocalizeTxt(txtMeshPro.text)) {
                    continue;
                }
                txtMeshPro.text = Localize.GetText(txtMeshPro.text);
                txtMeshPro.font = await Localize.GetTMPFont(LocalizeConst.KeyTMPTxtFontNormal);
            }
        }

        /// <summary>
        /// 判断是否需要翻译文本
        /// </summary>
        private static bool IsNeedLocalizeTxt(string txt) {
            return !txt.StartsWith(LocalizeConst.TxtIgnoreSign);
        }
    }
}
