using System;
using System.Text;
using UnityEngine;

namespace DCFrame.Utility {
	public abstract class StringUtil {
		
		/// <summary>
		/// 首字母小写
		/// </summary>
		public static string ToLowerFirstChar(string str) {
			if (string.IsNullOrEmpty(str)) return str;
			return char.ToLower(str[0]) + str.Substring(1);
		}

		/// <summary>
		/// 移除字符串中匹配的字符串之前的字符
		/// </summary>
		public static string StringRemoveBeforeByStr(string str, string matchStr) {
			var index = str.IndexOf(matchStr, StringComparison.Ordinal);
			if(index != -1) {
				str = str.Substring(index, str.Length - index);
			}
			return str;
		}

		/// <summary>
		/// 移除字符串中匹配的字符串之后的字符
		/// </summary>
		public static string StringRemoveAfterByStr(string str, string matchStr) {
            var index = str.IndexOf(matchStr, StringComparison.Ordinal);
            if (index != -1) {
                str = str.Substring(index + matchStr.Length);
            }
            return str;
        }

		/// <summary>
		/// 判断字符中是否有中文
		/// </summary>
		public static bool CheckCharIsChinese(char c) {
			return c >= 0x4E00 && c <= 0x9FA5;
		}

		/// <summary>
		/// 判断字符串中是否有中文
		/// </summary>
		public static bool CheckStringHasChinese(string str) {
			char[] ch = str.ToCharArray();
			if(str != "") {
				for(int i = 0; i < ch.Length; i++) {
					if(CheckCharIsChinese(ch[i])) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// 连接字符串
		/// </summary>
		public static string ConnectString(params string[] args) {
			builder.Remove(0, builder.Length);
			for (int i = 0;i < args.Length;i++) {
				if (string.IsNullOrEmpty(args[i])) { continue; }
				builder.Append(args[i]);
			}
			return builder.ToString();
		}

		/// <summary>
		/// 从字符串中移除颜色富文本标志,如：<color></color>>
		/// </summary>
		public static string StringRemoveColorMark(string source) {
			source = StringRemoveByStr(source, "<color", ">", true);
			source = source.Replace("</color>", Empty);
			return source;
		}

		/// <summary>
		/// 对字符串中有特殊标识的字符串进行删除标识中间字符串或者删除标识
		/// </summary>
		/// <param name="source">需要处理的字符串</param>
		/// <param name="startStr">开始的字符串标识</param>
		/// <param name="endStr">结束的字符串标识</param>
		/// <param name="isRemove">是否直接移除标识内的字符串</param>
		public static string StringRemoveByStr(string source, string startStr, string endStr, bool isRemove = false) {
			if (isRemove) {
				source = StringMidException(source, startStr, endStr);
			} else {
				source = source.Replace(startStr, Empty);
				source = source.Replace(endStr, Empty);
			}
			return source;
		}

        /// <summary>
        /// 去除字符串指定的字符标识中的中间字符串
        /// </summary>
        /// <param name="source">原字符串</param>
        /// <param name="startStr">开始的字符串标识</param>
        /// <param name="endStr">结束的字符串标识</param>
        /// <param name="isRemove">是否移除前后标识</param>
        public static string StringMidException(string source, string startStr, string endStr, bool isRemove = true) {
			string result = source;
			try {
				var startIndex = source.IndexOf(startStr, StringComparison.Ordinal);
                if (startIndex == -1) {
                    return result;
                }
                var endIndex = source.IndexOf(endStr, startIndex + startStr.Length, StringComparison.Ordinal);
                if (endIndex == -1) {
                    return result;
                }
                if (isRemove) {
                    var endIdx = endIndex + endStr.Length;
                    result = source.Substring(0, startIndex) + source.Substring(endIdx, source.Length - endIdx);
                } else {
                    result = source.Substring(0, startIndex + startStr.Length) + source.Substring(endIndex, source.Length - endIndex);
                }
            } catch (Exception ex) {
				Debug.LogError("MidStrEx Err:" + ex.Message);
			}

			return result;
		}
        
		/// <summary>
		/// 提取字符串指定的字符标识中的中间字符串
		/// </summary>
		/// <param name="source">原字符串</param>
		/// <param name="startStr">开始的字符串标识</param>
		/// <param name="endStr">结束的字符串标识</param>
		public static string StringGetMiddle(string source, string startStr, string endStr) {
			if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(startStr) || string.IsNullOrEmpty(endStr)) {
				return string.Empty;
			}

			int startIndex = source.IndexOf(startStr, StringComparison.Ordinal);
			if (startIndex == -1) {
				return string.Empty;
			}
			startIndex += startStr.Length;
			
			int endIndex = source.IndexOf(endStr, startIndex, StringComparison.Ordinal);
			if (endIndex == -1) {
				return string.Empty;
			}

			return source.Substring(startIndex, endIndex - startIndex);
		}
        
        /// <summary>
        /// 获取是否可以转换成 float，精度约 7 位
        /// </summary>
		public static bool CanBeFloat(decimal decVal) {
			float f = (float)decVal;
			return Math.Abs((decimal)f - decVal) < 1e-7m;
		}

        /// <summary>
        /// 获取是否可以转换成 float，精度约 15 位
        /// </summary>
		public static bool CanBeDouble(decimal decVal) {
			double d = (double)decVal;
			return Math.Abs((decimal)d - decVal) < 1e-15m;
		}

        /// <summary>
        /// 空字符串
        /// </summary>
        private const string Empty = "";
        private static StringBuilder builder { get; } = new StringBuilder();
	}
}

