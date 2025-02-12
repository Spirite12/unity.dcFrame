using System.IO;
using System.Text;
using UnityEngine;

namespace DCFrame {
    public class TextFilter {
        /// <summary>
        /// 初始化屏蔽文件内的敏感词库文本
        /// </summary>
        public static void InitFilterFile() {
            if (FilterPath.Length <= 0) {
                Debug.LogError("屏蔽词库路径配置错误");
                return;
            }
            var lines = File.ReadAllLines(FilterPath);
            foreach (var line in lines) {
                AddWord(line);
            }
        }

        /// <summary>
        /// 添加屏蔽词
        /// </summary>
        public static void AddWord(string word) {
            TextTrieNode current = Root;
            foreach (char c in word) {
                if (!current.Children.ContainsKey(c)) {
                    current.Children.Add(c, new TextTrieNode());
                }

                current = current.Children[c];
            }

            current.IsWord = true;
        }

        /// <summary>
        /// 判断词是否存在
        /// </summary>
        public static bool ContainsWord(string word) {
            TextTrieNode current = Root;
            foreach (char c in word) {
                if (!current.Children.ContainsKey(c)) {
                    return false;
                }

                current = current.Children[c];
            }

            return current.IsWord;
        }

        /// <summary>
        /// 判断给定消息是否包含屏蔽词
        /// </summary>
        public static bool ContainsFilterWords(string message) {
            int index = 0;
            while (index < message.Length) {
                int start = index;
                TextTrieNode current = Root;
                while (index < message.Length && current.Children.ContainsKey(message[index])) {
                    current = current.Children[message[index++]];
                    // 只要匹配到一个就返回
                    if (current.IsWord) {
                        return true;
                    }
                }

                index = ++start;
            }

            return false;
        }

        /// <summary>
        ///  将给定的消息其中的屏蔽词替换为 *
        /// </summary>
        public static string ReplaceFilterWords(string message) {
            int index = 0;
            StringBuilder sb = new StringBuilder();
            while (index < message.Length) {
                int start = index;
                int end = -1;
                TextTrieNode current = Root;
                while (index < message.Length && current.Children.ContainsKey(message[index])) {
                    current = current.Children[message[index++]];
                    // 找到最长的连续屏蔽词
                    if (current.IsWord) {
                        end = index - 1;
                    }
                }

                // 有找到屏蔽词
                if (end != -1) {
                    while (start <= end) {
                        start++;
                        sb.Append("*");
                    }

                    index = start;
                } else {
                    sb.Append(message[start++]);
                    index = start;
                }
            }

            return sb.ToString();
        }

        private static readonly TextTrieNode Root = new TextTrieNode();
        /// <summary>
        /// 屏蔽词库路径
        /// </summary>
        private const string FilterPath = "Assets/Game/Settings/TextFilter/TextFilter.txt";
    }
}
