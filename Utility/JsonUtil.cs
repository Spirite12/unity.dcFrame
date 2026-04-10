using System;
using System.Collections;
using UnityEngine;

namespace DCFrame.Utility {
    /// <summary>
    /// JsonUtility 的扩展工具，补充顶层数组和列表的序列化能力
    /// </summary>
    public static class JsonUtil {
        /// <summary>
        /// 包装字段名
        /// </summary>
        private const string WrapFieldName = "data";

        /// <summary>
        /// Json 空值文本
        /// </summary>
        private const string NullJson = "null";

        /// <summary>
        /// 把对象转换为 Json 字符串
        /// </summary>
        /// <param name="obj">对象</param>
        public static string ToJson<T>(T obj) {
            if (ReferenceEquals(obj, null)) {
                return NullJson;
            }

            if (IsListType(typeof(T))) {
                string wrapJson = JsonUtility.ToJson(new Pack<T> {
                    data = obj
                });
                return UnwrapJson(wrapJson);
            }

            return JsonUtility.ToJson(obj);
        }

        /// <summary>
        /// 解析 Json 字符串
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">Json 字符串</param>
        public static T FromJson<T>(string json) {
            if (string.IsNullOrWhiteSpace(json)) {
                return default(T);
            }

            string trimJson = json.Trim();
            if (trimJson == NullJson && CanReturnNull(typeof(T))) {
                return default(T);
            }

            if (IsListType(typeof(T))) {
                Pack<T> pack = JsonUtility.FromJson<Pack<T>>(WrapJson(trimJson));
                return pack == null ? default(T) : pack.data;
            }

            return JsonUtility.FromJson<T>(trimJson);
        }

        /// <summary>
        /// 判断目标类型是否是数组或列表
        /// </summary>
        /// <param name="type">目标类型</param>
        private static bool IsListType(Type type) {
            return type.IsArray || typeof(IList).IsAssignableFrom(type);
        }

        /// <summary>
        /// 判断目标类型是否可以返回 null
        /// </summary>
        /// <param name="type">目标类型</param>
        private static bool CanReturnNull(Type type) {
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        /// <summary>
        /// 为顶层数组或列表补上包装对象
        /// </summary>
        /// <param name="json">原始 Json 字符串</param>
        private static string WrapJson(string json) {
            return "{\"" + WrapFieldName + "\":" + json + "}";
        }

        /// <summary>
        /// 从包装对象中拆出顶层数组或列表的 Json
        /// </summary>
        /// <param name="json">包装后的 Json 字符串</param>
        private static string UnwrapJson(string json) {
            string prefix = "{\"" + WrapFieldName + "\":";
            if (json.StartsWith(prefix, StringComparison.Ordinal) && json.EndsWith("}", StringComparison.Ordinal)) {
                return json.Substring(prefix.Length, json.Length - prefix.Length - 1);
            }

            return json;
        }

        /// <summary>
        /// JsonUtility 解析数组或列表时使用的内部包装类
        /// </summary>
        [Serializable]
        private class Pack<TData> {
            public TData data;
        }
    }
}
