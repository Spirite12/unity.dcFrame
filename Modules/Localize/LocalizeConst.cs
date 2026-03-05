using System.Collections.Generic;

namespace DCFrame {
    public class LocalizeConst {

        /// <summary>
        /// 本地化的跟路径
        /// </summary>
        public const string LocalizeTableRootPath = "Assets/Game/Localize";
        
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

