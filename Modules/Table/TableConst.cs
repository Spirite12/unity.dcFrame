using System;
using System.Globalization;
using DCFrame.Utility;
using UnityEngine;

namespace DCFrame {
    public class TableConst : MonoBehaviour {
        /// <summary>
        /// 表数据路径
        /// </summary>
        public const string TableDataPath = "Assets/Game/Table";
        /// <summary>
        /// 科学计数法的标识
        /// </summary>
        public const string ScientificSign = "`";
        
        /// <summary>
        /// 表最大值枚举
        /// </summary>
        public enum EnumConfigMax {
            None = 0,
            Single = 1,
        }
        
        /// <summary>
        /// 表副Key
        /// </summary>
        public enum EnumViceKey {
            None = 0,
            Vice = 1,
            ViceWithList = 2,
        }
        
        /// <summary>
        /// 表字段数据
        /// 新增枚举需要处理：EnumFieldTypeParsers
        /// </summary>
        public enum EnumFieldType {
            Int = 0,
            Long = 1,
            Float = 2,
            Double = 3,
            String = 4,
            Bool = 5,
        }

        /// <summary>
        /// 获取表字段数据转换
        /// </summary>
        /// <returns></returns>
        public static EnumFieldType GetEnumFieldType(string s) {
            if (string.IsNullOrWhiteSpace(s)) return EnumFieldType.String;
            s = s.Trim();

            if (bool.TryParse(s, out _)) return EnumFieldType.Bool;
            if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return EnumFieldType.Int;
            if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)) return EnumFieldType.Long;

            // 浮点数 & 科学计数法处理
            if (decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decVal)) {
                // 判断是否可以安全转为 long（整数且在范围内）
                if (decVal == Math.Floor(decVal)) {
                    if (decVal is >= int.MinValue and <= int.MaxValue) return EnumFieldType.Int;
                    if (decVal is >= long.MinValue and <= long.MaxValue) return EnumFieldType.Long;
                }
                if (StringUtil.CanBeFloat(decVal)) return EnumFieldType.Float;
                if (StringUtil.CanBeDouble(decVal)) return EnumFieldType.Double;
            }

            return EnumFieldType.String;
        }
    }
}
