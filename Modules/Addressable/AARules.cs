using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DCFrame {
    public class AARules : ScriptableObject {
    
        [Header("以单资源打包")]
        public HierarchySingle singleList = new HierarchySingle();
    
        [Header("以标签名打包")]
        public List<HierarchyLabel> labelList = new List<HierarchyLabel>();
    
        [Header("以文件夹打包：文件夹之间没有关联")]
        public List<HierarchyDir> folderList = new List<HierarchyDir>();
    
        [System.Serializable]
        public class HierarchySingle {
            [NotFolder]
            public List<Object> assetList = new List<Object>();
            public List<HierarchySingleDir> dirList = new List<HierarchySingleDir>();
        }
    
        [System.Serializable]
        public class HierarchySingleDir {
            [OnlyFolder]
            public Object asset;
            [Range(0, 5)]
            public int number = 0;
            public string searchPattern = SearchPattern;
            public SearchOption option = SearchOption.TopDirectoryOnly;
        }
    
        [System.Serializable]
        public class HierarchyLabel {
            public string label = "";
            [NotFolder]
            public List<Object> assetList = new List<Object>();
            public List<HierarchyDir> dirList = new List<HierarchyDir>();
        }

        [System.Serializable]
        public class HierarchyDir {
            [OnlyFolder]
            public Object folder;
            [Range(0, 5)]
            [Header("第几级文件夹")]
            public int number = 0;
            [Header("过滤的文件夹")]
            [OnlyFolder]
            public List<Object> excludePathList = new List<Object>();
        }
        
        public const string SearchPattern = "*.*";
    }
}
