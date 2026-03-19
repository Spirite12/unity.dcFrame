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
            /// 表类型
            /// </summary>
            public TableUtil.TableType enumTableType = TableUtil.TableType.Default;
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
            /// 是否本地化
            /// </summary>
            public bool isLocalize = false;
        }

        [System.Serializable]
        public class TableTypeDefault {
            /// <summary>
            /// 字段数据
            /// </summary>
            public List<TableField> fieldList = new();
            /// <summary>
            /// 本地化的Key
            /// </summary>
            public string localizeKey = "";
            /// <summary>
            /// 标注
            /// </summary>
            public string remark = "";
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
            public TableUtil.FieldType enumField = TableUtil.FieldType.Int;
            /// <summary>
            /// 是否本地化
            /// </summary>
            public bool isLocalize = false;
            /// <summary>
            /// 是否获取最大值
            /// </summary>
            public bool isMaxValue = false;
            /// <summary>
            /// 多键查询枚举
            /// </summary>
            public TableUtil.KeyType enumMainViceKey = TableUtil.KeyType.None;
            /// <summary>
            /// 多键字段列表
            /// </summary>
            public List<string> fieldKeyList = new List<string>();
            /// <summary>
            /// 表关联列表
            /// </summary>
            public List<TableFieldRelate> fieldRelateList = new List<TableFieldRelate>();
        }
        
        [System.Serializable]
        public class TableFieldRelate {
            /// <summary>
            /// 表关联名
            /// </summary>
            public string tableName = "";
            /// <summary>
            /// 表关联字段
            /// </summary>
            public string fieldName = "";
        }
    }
}
