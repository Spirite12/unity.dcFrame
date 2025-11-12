using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Debug = UnityEngine.Debug;

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
        
        /// <summary>
        /// 打开脚本到当前 Unity IDE
        /// </summary>
        public static void OpenScript(string filePath, int line = 0) {
            if (!System.IO.File.Exists(filePath)) {
                Debug.LogError($"文件不存在: {filePath}");
                return;
            }

            // 获取 Unity 当前使用的 IDE
            var editor = UnityEditorInternal.ScriptEditorUtility.GetExternalScriptEditor();
            if (string.IsNullOrEmpty(editor)) {
                Debug.LogWarning("未设置外部脚本编辑器。请在 Preferences > External Tools 中设置。");
                return;
            }

            // 不同 IDE 的参数略有不同
            string args = "";
            
            if (editor.ToLower().Contains("code")){ // VSCode
                args = line > 0 ? $"-g \"{filePath}:{line}\"" : $"\"{filePath}\"";
            }else if (editor.ToLower().Contains("rider")) {
                args = line > 0 ? $"--line {line} \"{filePath}\"" : $"\"{filePath}\"";
            }else if (editor.ToLower().Contains("devenv")){ // Visual Studio
                args = $"\"{filePath}\"";
            }
            // 启动 IDE 打开文件
            Process.Start(new ProcessStartInfo
            {
                FileName = editor,
                Arguments = args,
                UseShellExecute = false
            });
        }
    }
}
