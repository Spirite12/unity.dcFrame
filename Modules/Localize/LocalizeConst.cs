using System.Collections.Generic;

namespace DCFrame {
    public class LocalizeConst {
        
        /// <summary>
        /// 文本忽略标识
        /// </summary>
        public const string TxtIgnoreSign = "`";
        /// <summary>
        /// 正常字体库的key
        /// </summary>
        public const string KeyTxtFontNormal = "";
        /// <summary>
        /// 正常TMP字体库的key
        /// </summary>
        public const string KeyTMPTxtFontNormal = "";
        /// <summary>
        /// 表内的收集名称
        /// </summary>
        public const string TableCollectionName = "TableTable";
        
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
        private static readonly Dictionary<EnumLocaleCode, string> LocaleCodeDic = new() {
            { EnumLocaleCode.ZhCN, "zh-CN" },
            { EnumLocaleCode.EN, "en" },
        };
    }
}

