using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DCFrame {
    public class AARules : ScriptableObject {
        [Header("以单资源打包")]
        public HierarchySingle singleList = new();
    
        [Header("以标签名打包")]
        public List<HierarchyLabel> labelList = new();
    
        [Header("以文件夹打包：文件夹之间没有关联")]
        public List<HierarchyDir> folderList = new();
    
        [System.Serializable]
        public class HierarchySingle {
            public List<HierarchySingleAsset> assetList = new();
            public List<HierarchySingleDir> dirList = new();
        }

        [System.Serializable]
        public class HierarchySingleAsset {
            [NotFolder]
            public Object asset;
            public bool isRemote = false;
            public string label = "";
        }
    
        [System.Serializable]
        public class HierarchySingleDir {
            [OnlyFolder]
            public Object asset;
            public bool isRemote = false;
            public string label = "";
            [Range(0, 5)]
            public int number = 0;
            public string searchPattern = SearchPattern;
            public SearchOption option = SearchOption.TopDirectoryOnly;
        }
    
        [System.Serializable]
        public class HierarchyLabel {
            public string label = "";
            [NotFolder]
            public List<Object> assetList = new();
            public List<HierarchyDir> dirList = new();
        }

        [System.Serializable]
        public class HierarchyDir {
            [OnlyFolder]
            public Object folder;
            public bool isRemote = false;
            [Range(0, 5)]
            [Header("第几级文件夹")]
            public int number = 0;
            [Header("过滤的文件夹")]
            [OnlyFolder]
            public List<Object> excludePathList = new();
        }

        public const string SearchPattern = "*.*";
    }
}
