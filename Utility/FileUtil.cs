#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DCFrame.Foundation;
using UnityEngine;

namespace DCFrame.Utility {
    public abstract class FileUtil {
        
        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        /// <param name="isFullPath">是否是全路径（默认否）</param>
        public static bool ExistFile(string path, bool isFullPath = false) {
            if (!isFullPath) {
                path = GetFullPath(path);
            }
            return File.Exists(path);
        }

        /// <summary>
        /// 判断文件夹是否存在
        /// </summary>
        /// <param name="isFullPath">是否是全路径（默认否）</param>
        public static bool ExistDirectory(string path, bool isFullPath = false) {
            if (!isFullPath) {
                path = GetFullPath(path);
            }
            return Directory.Exists(path);
        }

        /// <summary>
        /// 获取全路径
        /// </summary>
        /// <returns></returns>
        public static string GetFullPath(string path) {
            string prePath = "";
#if UNITY_EDITOR
            prePath = Environment.CurrentDirectory;
#else
            prePath = Application.persistentDataPath;
#endif
            prePath = prePath + "\\";
            return Path.Combine(prePath, path);
        }
        
        /// <summary>
        /// 获取文件是否打开
        /// </summary>
        public static bool IsFileLocked(string filePath) {
            FileStream? stream = null;
            try {
                // 以只读模式打开，不允许共享写入
                stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false; // 文件没被占用
            }catch (IOException) {
                return true; // 文件被占用
            }finally {
                stream?.Close();
            }
        }

        #region 读写文件

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="strSave"></param>
        /// <param name="isCreateDir">当路径不存在时，是否需要创建文件夹（默认要）</param>
        /// <param name="isEncrypt">是否需要加密（默认要）</param>
        public static void WriteFile(string path, string strSave, bool isCreateDir = true, bool isEncrypt = true) {
#if !UNITY_EDITOR
            if (isEncrypt) {
                strSave = EncryptData(strSave);
            }
#endif
            path = GetFullPath(path);
            if (isCreateDir) {
                string strPrePath = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(strPrePath) && !ExistDirectory(strPrePath)) {
                    Directory.CreateDirectory(strPrePath);
                }
            }
            File.WriteAllText(path, strSave);
        }

        /// <summary>
        /// 阅读文件内容
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isEncrypt">是否需要加密（默认要）</param>
        /// <param name="isFullPath">是否是全路径（默认否）</param>
        public static string ReadFile(string path, bool isEncrypt = true, bool isFullPath = false) {
            if (!ExistFile(path, isFullPath)) {
                Debug.LogError("ReadFile Path is Not Exist : " + path);
                return "";
            }
            if (!isFullPath) {
                path = GetFullPath(path);
            }
            string strContent = File.ReadAllText(path);
#if !UNITY_EDITOR
            if (isEncrypt) {
                strContent = DecryptData(strContent);
            }
#endif
            return strContent;
        }

        /// <summary>
        /// 使用 XXTEA 加密数据，并输出为 Base64 字符串。
        /// </summary>
        public static string EncryptData(string strContent) {
            return XXTEA.EncryptToBase64String(strContent, EncryptKey);
        }

        /// <summary>
        /// 使用 XXTEA 解密 Base64 字符串。
        /// </summary>
        public static string DecryptData(string strContent) {
            return XXTEA.DecryptBase64StringToString(strContent, EncryptKey);
        }
        
        #endregion
        
        /// 递归遍历目录，获取指定级别的子目录。
        /// <param name="currentPath"> 当前路径 </param>
        /// <param name="currentLevel"> 当前层级 </param>
        /// <param name="targetLevel"> 目标层级 </param>
        /// <param name="result"> 存储结果的列表 </param>
        public static void TraverseDirectories(string currentPath, int currentLevel, int targetLevel, List<string> result) {
            // 如果已经到达目标层级，添加当前路径到结果
            if (currentLevel == targetLevel) {
                string path = currentPath.Replace("\\", "/");
                result.Add(path);
                return;
            }
            // 获取当前路径下的所有子目录
            string[] subDirectories = Directory.GetDirectories(currentPath);
            // 递归处理每个子目录
            foreach (string subDir in subDirectories) {
                TraverseDirectories(subDir, currentLevel + 1, targetLevel, result);
            }
        }

        public const string RecordDirName = "Record";

        /// <summary>
        /// 文件工具默认使用的 XXTEA 密钥。
        /// </summary>
        private const string EncryptKey = "DCFrame";
    }
}
