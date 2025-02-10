using System;
using System.Collections.Generic;

namespace DCFrame {
    public class RedTipTree {
        /// <summary>
        /// 获取红点实例类
        /// </summary>
        public static RedTipBase GetRedTipClass(string redTipName) {
            RedTipBase redTip = redTipBaseDic[redTipName].Invoke() ?? new RedTipBase();
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

