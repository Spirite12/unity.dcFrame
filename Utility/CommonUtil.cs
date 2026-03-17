using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace DCFrame.Utility {
    public abstract class CommonUtil {
        
        /// <summary>
        /// 获取枚举内的描述List
        /// </summary>
        public static List<string> GetEnumDescriptions<T>() where T : Enum {
            var result = new List<string>();
            foreach (var value in Enum.GetValues(typeof(T))) {
                var field = typeof(T).GetField(value.ToString());
                var desc = field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
                result.Add(desc);
            }
            return result;
        }
        
        /// <summary>
        /// 获取枚举内的单个描述
        /// </summary>
        public static string GetDescription(Enum value) {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attr = field.GetCustomAttribute<DescriptionAttribute>();
            return attr != null ? attr.Description : value.ToString();
        }
    }
}
