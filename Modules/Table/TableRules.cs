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
            public TableConst.EnumConfigMax enumConfigMax = TableConst.EnumConfigMax.None;
            /// <summary>
            /// 字段数据
            /// </summary>
            public List<TableField> fieldList = new List<TableField>();
        }

        [System.Serializable]
        public class TableField {
            /// <summary>
            /// 字段名称
            /// </summary>
            public string fileldName = "";
            /// <summary>
            /// 字段类型
            /// </summary>
            public TableConst.EnumFieldType enumField = TableConst.EnumFieldType.Int;
            /// <summary>
            /// 是否多语言
            /// </summary>
            public bool isLocalize = false;
            /// <summary>
            /// 是否副Key
            /// </summary>
            public bool isViceKey = false;
            /// <summary>
            /// 是否最大值
            /// </summary>
            public bool isConfigMax = false;
        }
    }
}
