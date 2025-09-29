using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace DCFrame {
    public abstract class TableBase {
        
        /// <summary>
        /// 获取字典类型的数据
        /// </summary>
        protected Dictionary<TKey, TValue> LoadTableDic<TKey, TValue>(Func<TValue, TKey> keySelector) {
            return GetRecords<TValue>().ToDictionary(keySelector);
        }
        
        /// <summary>
        /// 获取字典类型的组List
        /// </summary>
        protected Dictionary<TKey, List<TValue>> LoadTableDicList<TKey, TValue>(Func<TValue, TKey> keySelector) {
            return GetRecords<TValue>().GroupBy(keySelector)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        /// <summary>
        /// 加载读表配置
        /// </summary>
        private IEnumerable<T> GetRecords<T>(){
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
                HasHeaderRecord = true,
                IgnoreBlankLines = true,
            };

            using var reader = new StreamReader(CsvPath, Encoding.UTF8);
            var csv = new CsvReader(reader, config);
            csv.Context.TypeConverterCache.AddConverter<long>(new ScientificToLongConvert());
            return csv.GetRecords<T>().ToList();
        }

        protected abstract string CsvPath { get; }
    }
}
