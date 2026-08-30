using System.IO;
using UnityEngine;
using DCFrame.Utility;

namespace DCFrame {
    /// <summary>
    /// 本地存储记录
    /// 不支持：字典形式、私有变量
    /// </summary>
    public class CacheBase {
        public CacheBase() {
            className = GetType().Name;
            GetFileSave();
            CacheMgr.AddCacheBase(this);
        }
        
        /// <summary>
        /// 设置保存的路径
        /// </summary>
        /// <returns></returns>
        public void SaveFileData() {
            string path = GetSavePath();
            string strSave = JsonUtility.ToJson(this);
            if (!FileUtil.ExistDirectory(FileUtil.RecordDirName)) {
                Directory.CreateDirectory(FileUtil.GetFullPath(FileUtil.RecordDirName));
            }
            FileUtil.WriteFile(path, strSave);
        }
        
        /// <summary>
        /// 获取保存的路径
        /// </summary>
        /// <returns></returns>
        protected virtual string GetSavePath() {
            string value = CacheMgr.GetSaveTypeValue(saveType);
            if (value.Length != 0) {
                value = $"{value}\\";
            }
            return $"{FileUtil.RecordDirName}\\Cache\\{value}{className}.txt";
        }

        /// <summary>
        /// 获取文件数据
        /// </summary>
        protected virtual void GetFileSave() {
            // 获取数据
            string path = GetSavePath();
            if (!FileUtil.ExistFile(path)) {
                return;
            }
            string content = FileUtil.ReadFile(path);
            // 低于当前版本则不读取
            CacheVersionData fileData = JsonUtility.FromJson<CacheVersionData>(content);
            if (fileData == null || fileData.version < version) {
                return;
            }
            JsonUtility.FromJsonOverwrite(content, this);
        }

        /// <summary>
        /// 版本值，若文件内版本小于当前版本则不读
        /// </summary>
        public int version = 1;
        /// <summary>
        /// 本地记录类型
        /// </summary>
        public CacheMgr.SaveType saveType = CacheMgr.SaveType.PlayerId;
        
        [System.Serializable]
        private class CacheVersionData {
            public int version = 0;
        }

        private readonly string className;
    }
}
