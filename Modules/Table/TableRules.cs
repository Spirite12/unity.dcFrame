using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    public class TableRules : ScriptableObject {
        [HideInInspector]
        public List<TableRule> tableRuleList = new List<TableRule>();
        
        [System.Serializable]
        public class TableRule {
            /// <summary>
            /// 表名
            /// </summary>
            public string name;
            /// <summary>
            /// 主键名称
            /// </summary>
            public string mainKey;
            /// <summary>
            /// 表类型
            /// </summary>
            public TableUtil.EnumTableType enumTableType = TableUtil.EnumTableType.Default;
            /// <summary>
            /// 默认表数据
            /// </summary>
            public TableTypeDefault defaultData = new();
            /// <summary>
            /// 枚举表数据
            /// </summary>
            public List<TableTypeEnum> enumList = new();
        }
        
        [System.Serializable]
        public class TableTypeEnum {
            /// <summary>
            /// 枚举标识
            /// </summary>
            public string sign;
            /// <summary>
            /// 是否多语言
            /// </summary>
            public bool isLocalize = false;
        }

        [System.Serializable]
        public class TableTypeDefault {
            /// <summary>
            /// 副Key类型
            /// </summary>
            public TableUtil.EnumViceKey enumViceKey = TableUtil.EnumViceKey.None;
            /// <summary>
            /// 最大值类型
            /// </summary>
            public TableUtil.EnumConfigMax enumConfigMax = TableUtil.EnumConfigMax.None;
            /// <summary>
            /// 字段数据
            /// </summary>
            public List<TableField> fieldList = new();
        }

        [System.Serializable]
        public class TableField {
            /// <summary>
            /// 字段名称
            /// </summary>
            public string fieldName = "";
            /// <summary>
            /// 字段类型
            /// </summary>
            public TableUtil.EnumFieldType enumField = TableUtil.EnumFieldType.Int;
            /// <summary>
            /// 是否多语言
            /// </summary>
            public bool isLocalize = false;
            /// <summary>
            /// 副Key的索引值
            /// </summary>
            public int viceKeyValue = 0;
            /// <summary>
            /// 最大值索引值
            /// </summary>
            public int configMaxValue = 0;
        }
    }
}
