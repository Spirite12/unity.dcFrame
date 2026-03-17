using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using DCFrame.Utility;
using UnityEngine;

namespace DCFrame {
    public class TableUtil : MonoBehaviour {
        /// <summary>
        /// 科学计数法的标识
        /// </summary>
        public const string ScientificSign = "`";
        /// <summary>
        /// 表类的模板文件
        /// </summary>
        public const string TableClassTpNormal = nameof(TableClassTpNormal);
        /// <summary>
        /// 表类的模板文件
        /// </summary>
        public const string TableClassTpConst = nameof(TableClassTpConst);
        /// <summary>
        /// 表类的模板文件
        /// </summary>
        public const string TableClassTpEnum = nameof(TableClassTpEnum);
        /// <summary>
        /// 表类的模板文件
        /// </summary>
        public const string TableClassTpString = nameof(TableClassTpString);
        /// <summary>
        /// 表数据路径
        /// </summary>
        public const string TableDataPath = "Assets/Game/Table";
        /// <summary>
        /// 表脚本路径
        /// </summary>
        public const string TableScriptPath = "Assets/Game/Scripts/Table";
        
        /// <summary>
        /// 表类型
        /// </summary>
        public enum EnumTableType {
            [Description("默认表")]
            Default = 0,
            [Description("常量表")]
            Const = 1,
            [Description("枚举表")]
            Enum = 2,
            [Description("文本表")]
            String = 3,
        }
        
        /// <summary>
        /// 表key类型
        /// </summary>
        public enum EnumKeyType {
            [Description("无")]
            None = 0,
            [Description("单key")]
            Single = 1,
            [Description("多Key")]
            Multi = 2,
            [Description("多Key列表")]
            MultiWithList = 3,
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
        
        /// <summary>
        /// 获取文件路径
        /// </summary>
        public static string GetFilePath(string tableName) {
            var filePath = Path.Combine(TableDataPath, tableName + ".csv");
            return filePath;
        }

        /// <summary>
        /// 获取脚本路径
        /// </summary>
        public static string GetScriptPath(string tableName) {
            var filePath = Path.Combine(TableScriptPath, tableName + ".cs");
            return filePath;
        }
    }
}
