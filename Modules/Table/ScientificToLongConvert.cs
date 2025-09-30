using System;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DCFrame.Utility;

namespace DCFrame {
    public class ScientificToLongConvert : DefaultTypeConverter {
        /// <summary>
        /// 进行 long 的科学计数转换识别
        /// </summary>
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
            if (string.IsNullOrWhiteSpace(text)) {
                return 0;
            }
            
            // 在前缀新增 ` 来避免科学计数，以至于识别成字符串
            if (text.StartsWith(TableConst.ScientificSign)) {
                text = text.Substring(1);
            }
            
            // 判断是否是科学计数法
            if (StringUtil.IsScientificNotation(text)) {
                throw new FormatException($"检测到科学计数法格式，数值: {text}，请检查 CSV 文件。");
            }

            // 先尝试用 decimal 解析，decimal 支持科学计数，精度更高
            if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var dec)) {
                // 检查是否超出 long 范围
                if (dec > long.MaxValue || dec < long.MinValue) {
                    throw new OverflowException($"数值 {dec} 超出了 long 的范围。");
                }
                return (long)dec;
            }

            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}
