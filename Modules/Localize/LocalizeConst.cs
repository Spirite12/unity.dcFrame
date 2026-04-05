using System.Collections.Generic;

namespace DCFrame {
    public class LocalizeConst {

        /// <summary>
        /// 本地化的跟路径
        /// </summary>
        public const string LocalizeTableRootPath = "Assets/Game/Localize";
        /// <summary>
        /// 本地化文本Table名称
        /// </summary>
        public const string LocalizeStringTableName = "Text";
        /// <summary>
        /// 本地化文本收集表通用表名称
        /// </summary>
        public const string LocalizeCollectionTableName = "Table";
        
        /// <summary>
        /// 语言枚举表
        /// </summary>
        public enum LocaleCode {
            /// <summary>
            /// 简体
            /// </summary>
            ZhCN,
        }
        
        /// <summary>
        /// 枚举对应的本地化Code
        /// </summary>
        public static readonly Dictionary<LocaleCode, string> LocaleCodeDic = new() {
            { LocaleCode.ZhCN, "zh-CN" },
        };
    }
}

