using System;
using System.Collections.Generic;

namespace DCFrame {
    public class UITool :Singleton<UITool> {
        /// <summary>
        /// 队列打开界面
        /// </summary>
        /// <param name="uiBase">类实例</param>
        /// <param name="openAction">打开回调</param>
        /// <param name="closeAction">关闭回调</param>
        /// <param name="strDicKey">排序key,无需求则默认不写</param>
        /// <param name="sort">队列排序ID越大越后面出现</param>
        public void ShowQueuePanel(UIBase uiBase, Action<Exception> openAction = null, Action closeAction = null, string strDicKey = "default") {
            if (!uiQueueDic.ContainsKey(strDicKey)) {
                uiQueueDic.Add(strDicKey, new List<QueueClass>());
            }
            uiQueueDic[strDicKey].Add(new QueueClass(uiBase, openAction, closeAction));
            if (uiQueueDic[strDicKey].Count == 1) {
                OpenFirstPanel(strDicKey);
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
            OpenFirstPanel(strDicKey);
        }

        /// <summary>
        /// 打开第一个队列
        /// </summary>
        private void OpenFirstPanel(string strDicKey) {
            var uiQueue = uiQueueDic[strDicKey][0];
            uiQueue.uiBase.Open(uiQueue.openAction, () => {
                uiQueue.closeAction?.Invoke();
                ShowQueueNextPanel(strDicKey);
            });
        }

        /// <summary>
        /// 队列打开类
        /// </summary>
        public class QueueClass {
            public UIBase uiBase;
            public Action<Exception> openAction;
            public Action closeAction;

            public QueueClass(UIBase uiBase, Action<Exception> openAction, Action closeAction) {
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
