using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace DCFrame {
    public class UITool :Singleton<UITool> {
        /// <summary>
        /// 队列打开界面
        /// </summary>
        /// <param name="uiBase">类实例</param>
        /// <param name="openAction">打开回调</param>
        /// <param name="closeAction">关闭回调</param>
        /// <param name="strDicKey">排序key,无需求则默认不写</param>
        public void ShowQueuePanel(UIBase uiBase, Action openAction = null, Action closeAction = null, string strDicKey = "default") {
            if (!uiQueueDic.ContainsKey(strDicKey)) {
                uiQueueDic.Add(strDicKey, new List<QueueClass>());
            }
            uiQueueDic[strDicKey].Add(new QueueClass(uiBase, openAction, closeAction));
            if (uiQueueDic[strDicKey].Count == 1) {
                _ = OpenFirstPanel(strDicKey);
            }
        }

        /// <summary>
        /// 打开下一个面板
        /// </summary>
        private void ShowQueueNextPanel(string strDicKey) {
            uiQueueDic[strDicKey].RemoveAt(0);
            if (uiQueueDic[strDicKey].Count == 0) {
                return;
            }
            _ = OpenFirstPanel(strDicKey);
        }

        /// <summary>
        /// 打开第一个队列
        /// </summary>
        private async UniTask OpenFirstPanel(string strDicKey) {
            var uiQueue = uiQueueDic[strDicKey][0];
            await uiQueue.uiBase.Open(() => {
                uiQueue.closeAction?.Invoke();
                ShowQueueNextPanel(strDicKey);
            });
            uiQueue.openAction?.Invoke();
        }

        /// <summary>
        /// 队列打开类
        /// </summary>
        private class QueueClass {
            public UIBase uiBase;
            public Action openAction;
            public Action closeAction;

            public QueueClass(UIBase uiBase, Action openAction, Action closeAction) {
                this.uiBase = uiBase;
                this.openAction = openAction;
                this.closeAction = closeAction;
            }
        }

        /// <summary>
        /// 队列打开数据
        /// </summary>
        private readonly Dictionary<string,List<QueueClass>> uiQueueDic = new Dictionary<string, List<QueueClass>>();
    }
}
