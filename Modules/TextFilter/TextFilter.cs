using System.Collections.Generic;
using System.IO;
using System.Text;
using DCFrame.Utility;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace DCFrame {
    public class TextFilter {
        /// <summary>
        /// 初始化敏感词库，固定加载全局词库，并尝试加载当前本地化地区词库。
        /// </summary>
        public static void Init() {
            Destroy();
            var filterPaths = BuildFilterPaths();
            foreach (var path in filterPaths) {
                LoadFilterFile(path);
            }
        }

        /// <summary>
        /// 清空当前已加载的词库。
        /// </summary>
        public static void Destroy() {
            Root.IsWord = false;
            Root.Children.Clear();
        }

        /// <summary>
        /// 添加屏蔽词。
        /// </summary>
        /// <param name="word">待添加的词条。</param>
        public static void AddWord(string word) {
            var normalizedWord = NormalizeWord(word);
            if (string.IsNullOrWhiteSpace(normalizedWord)) {
                return;
            }
            TextTrieNode current = Root;
            foreach (char c in normalizedWord) {
                if (!current.Children.ContainsKey(c)) {
                    current.Children.Add(c, new TextTrieNode());
                }
                current = current.Children[c];
            }
            current.IsWord = true;
        }

        /// <summary>
        /// 判断词是否存在。
        /// </summary>
        /// <param name="word">待判断词条。</param>
        /// <returns>存在返回 true，否则返回 false。</returns>
        public static bool ContainsWord(string word) {
            var normalizedWord = NormalizeWord(word);
            if (string.IsNullOrWhiteSpace(normalizedWord)) {
                return false;
            }
            TextTrieNode current = Root;
            foreach (char c in normalizedWord) {
                if (!current.Children.ContainsKey(c)) {
                    return false;
                }
                current = current.Children[c];
            }
            return current.IsWord;
        }

        /// <summary>
        /// 判断给定消息是否包含屏蔽词。
        /// </summary>
        /// <param name="message">待检测消息。</param>
        /// <returns>包含返回 true，否则返回 false。</returns>
        public static bool ContainsFilterWords(string message) {
            if (string.IsNullOrEmpty(message)) {
                return false;
            }
            int index = 0;
            while (index < message.Length) {
                int start = index;
                TextTrieNode current = Root;
                while (index < message.Length && current.Children.ContainsKey(message[index])) {
                    current = current.Children[message[index++]];
                    if (current.IsWord) {
                        return true;
                    }
                }
                index = ++start;
            }
            return false;
        }

        /// <summary>
        /// 将消息中的屏蔽词替换为 *。
        /// </summary>
        /// <param name="message">原始消息。</param>
        /// <returns>替换后的消息。</returns>
        public static string ReplaceFilterWords(string message) {
            if (string.IsNullOrEmpty(message)) {
                return message ?? string.Empty;
            }
            int index = 0;
            StringBuilder sb = new StringBuilder();
            while (index < message.Length) {
                int start = index;
                int end = -1;
                TextTrieNode current = Root;
                while (index < message.Length && current.Children.ContainsKey(message[index])) {
                    current = current.Children[message[index++]];
                    if (current.IsWord) {
                        end = index - 1;
                    }
                }

                if (end != -1) {
                    while (start <= end) {
                        start++;
                        sb.Append('*');
                    }
                    index = start;
                } else {
                    sb.Append(message[start++]);
                    index = start;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构建本次需要加载的词库路径：全局 + 当前本地化地区。
        /// </summary>
        /// <returns>按顺序去重后的词库路径列表。</returns>
        private static List<string> BuildFilterPaths() {
            var pathSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();

            AddFilterPath(GlobalFilterPath, pathSet, result);

            var localeCode = GetCurrentLocaleCode();
            if (string.IsNullOrWhiteSpace(localeCode)) {
                return result;
            }

            var localePath = GetFilterFilePath(localeCode);
            if (!File.Exists(localePath)) {
                Debug.LogWarning($"TextFilter 未找到当前地区词库文件：{localePath}，locale={localeCode}");
                return result;
            }

            AddFilterPath(localePath, pathSet, result);
            return result;
        }

        /// <summary>
        /// 获取当前本地化地区编码，优先取 SelectedLocale，其次取默认地区。
        /// </summary>
        /// <returns>当前地区编码。</returns>
        private static string GetCurrentLocaleCode() {
            var selectedCode = LocalizationSettings.SelectedLocale?.Identifier.Code;
            if (!string.IsNullOrWhiteSpace(selectedCode)) {
                return selectedCode.Trim();
            }

            var defaultCode = LocalizeUtil.GetDefaultLocale()?.Identifier.Code;
            return defaultCode?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 获取指定地区码对应的词库文件路径，命名格式为 TextFilter_XXX.txt。
        /// </summary>
        /// <param name="localeCode">地区编码。</param>
        /// <returns>词库文件路径。</returns>
        private static string GetFilterFilePath(string localeCode) {
            return Path.Combine(FilterRootPath, $"{FilterFileNamePrefix}{localeCode}{FilterFileExtension}");
        }

        /// <summary>
        /// 加载单个词库文件并返回成功加载的词条数量。
        /// </summary>
        /// <param name="filterPath">词库路径。</param>
        /// <returns>成功加载的词条数量。</returns>
        private static void LoadFilterFile(string filterPath) {
            if (string.IsNullOrWhiteSpace(filterPath) || !File.Exists(filterPath)) {
                return;
            }
            var lines = File.ReadAllLines(filterPath, Encoding.UTF8);
            foreach (var line in lines) {
                var word = NormalizeWord(line);
                if (string.IsNullOrWhiteSpace(word) || word.StartsWith("#")) {
                    continue;
                }
                AddWord(word);
            }
        }

        /// <summary>
        /// 对词条做统一清洗。
        /// </summary>
        /// <param name="word">词条文本。</param>
        /// <returns>清洗后的文本。</returns>
        private static string NormalizeWord(string word) {
            return word?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// 追加词库路径并去重。
        /// </summary>
        /// <param name="path">词库路径。</param>
        /// <param name="pathSet">去重集合。</param>
        /// <param name="result">结果列表。</param>
        private static void AddFilterPath(string path, HashSet<string> pathSet, List<string> result) {
            if (string.IsNullOrWhiteSpace(path)) {
                return;
            }
            if (pathSet.Add(path)) {
                result.Add(path);
            }
        }

        private static readonly TextTrieNode Root = new();

        /// <summary>
        /// 屏蔽词库根路径。
        /// </summary>
        private const string FilterRootPath = "Assets/Game/Settings/TextFilter";

        /// <summary>
        /// 词库文件名前缀。
        /// </summary>
        private const string FilterFileNamePrefix = "TextFilter_";

        /// <summary>
        /// 词库文件扩展名。
        /// </summary>
        private const string FilterFileExtension = ".txt";

        /// <summary>
        /// 全局词库路径。
        /// </summary>
        private const string GlobalFilterPath = FilterRootPath + "/TextFilter_global.txt";
    }
}
