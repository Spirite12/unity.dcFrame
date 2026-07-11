using System;
using System.Collections.Generic;

namespace DCFrame {
    public class RedTipTree {
        /// <summary>
        /// 获取红点实例类
        /// </summary>
        public static RedTipBase GetRedTipClass(string redTipName) {
            if (!redTipBaseDic.TryGetValue(redTipName, out Func<RedTipBase> redTipFactory) || redTipFactory == null) {
                UnityEngine.Debug.LogError($"未配置红点实例：{redTipName}");
                return null;
            }
            RedTipBase redTip = redTipFactory.Invoke();
            if (redTip == null) {
                UnityEngine.Debug.LogError($"红点实例创建失败：{redTipName}");
            }
            return redTip;
        }

        /// <summary>
        /// 节点树
        /// </summary>
        public static readonly Dictionary<string, List<string>> redTipTreeDic = new Dictionary<string, List<string>>();

        /// <summary>
        /// 节点实例
        /// </summary>
        public static readonly Dictionary<string, Func<RedTipBase>> redTipBaseDic = new Dictionary<string, Func<RedTipBase>>();
    }
}

