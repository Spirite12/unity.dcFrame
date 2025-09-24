using UnityEngine;

namespace DCFrame {
    public class TableConst : MonoBehaviour {
        /// <summary>
        /// 表数据路径
        /// </summary>
        public const string TableDataPath = "Assets/Game/Table";
        
        /// <summary>
        /// 表字段数据
        /// </summary>
        public enum EnumFieldType {
            Int = 0,
            String = 1,
            Bool = 2,
            Json = 3,
        }
        
        /// <summary>
        /// 表最大值枚举
        /// </summary>
        public enum EnumConfigMax {
            None = 0,
            Single = 1,
            Multiple = 2,
        }
    }
}
