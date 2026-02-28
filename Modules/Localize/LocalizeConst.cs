using System.Collections.Generic;

namespace DCFrame {
    public class LocalizeConst {
        /// <summary>
        /// 语言枚举表
        /// </summary>
        public enum EnumLocaleCode {
            /// <summary>
            /// 简体
            /// </summary>
            ZhCN,
            /// <summary>
            /// 英文
            /// </summary>
            EN,
        }
        
        /// <summary>
        /// 枚举对应的本地化Code
        /// </summary>
        public static readonly Dictionary<EnumLocaleCode, string> LocaleCodeDic = new() {
            { EnumLocaleCode.ZhCN, "zh-CN" },
            { EnumLocaleCode.EN, "en" },
        };
    }
}

