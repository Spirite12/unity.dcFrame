using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace DCFrame {
    public class ScientificToLongConvert : DefaultTypeConverter {
        /// <summary>
        /// 进行 long 的科学计数转换识别
        /// </summary>
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) {
                return (long)d;
            }
            return base.ConvertFromString(text, row, memberMapData);
        }
    }
}
