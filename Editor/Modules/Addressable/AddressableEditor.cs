using System.Collections.Generic;
using DCFrame;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.IO;
using FileUtil = DCFrame.Utility.FileUtil;

public class AddressableEditor : Editor {
    [MenuItem("Tools/资源项/打 AA 包")]
    public static void PackageAddressable() {
        AARules = AssetDatabase.LoadAssetAtPath<AARules>(AAConst.AARulesPath);
        if (AARules == null) {
            Debug.LogError("找不到AA规则文件");
            return;
        }
        StartAddressable();
    }

    private static void StartAddressable() {
        settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) {
            Debug.LogError("Addressable Asset Settings not found!");
            return;
        }
        List<float> progressList = new List<float>() { 0, 0.3f, 0.6f, 0.9f, 1 };
        EditorUtility.DisplayProgressBar(ProgressTitle, "打AA包开始", progressList[0]);
        var groupSingle = InitGroupData(AAConst.AAGroupSingle);
        DealWithGroupSingle(groupSingle, progressList[0], progressList[1]);
        var groupLabel = InitGroupData(AAConst.AAGroupLabel);
        DealWithGroupLabel(groupLabel, progressList[1], progressList[2]);
        var groupFolder = InitGroupData(AAConst.AAGroupFolder);
        DealWithGroupFolder(groupFolder, progressList[2], progressList[3]);
        InitGroupData(AAConst.AADefaultLocalGroup);
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        AddressableAssetSettings.BuildPlayerContent();
    }

    /// <summary>
    /// 处理组文件夹
    /// </summary>
    private static void DealWithGroupFolder(AddressableAssetGroup targetGroup, float startValue, float endValue) {
        int addCount = 0;
        int totalCount = AARules.folderList.Count;
        foreach (var item in AARules.folderList) {
            // 收集排除队列
            Dictionary<string, bool> excludeDir = new Dictionary<string, bool>();
            // 地址队列
            List<string> pathList = new List<string>();
            
            var path = AssetDatabase.GetAssetPath(item.folder);
            if (path == null) {
                Debug.LogError("AARules 内找不到 " + item.folder);
                continue;
            }
            FileUtil.TraverseDirectories(path, 0, item.number, pathList);
            foreach (var itemTp in item.excludePathList) {
                var pathTp = AssetDatabase.GetAssetPath(itemTp);
                excludeDir.Add(pathTp, true);
            }
            
            // 添加地址组
            foreach (var pathTp in pathList) {
                if (excludeDir.ContainsKey(pathTp)) {
                    continue;
                }
                var guid = AssetDatabase.AssetPathToGUID(pathTp);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup);
                entry.address = pathTp.Replace("\\", "/");
            }
            addCount += 1;
            float value = ((float)addCount / totalCount) * (endValue - startValue) + startValue;
            EditorUtility.DisplayProgressBar(ProgressTitle, path, value);  
        }
    }
    
    /// <summary>
    /// 处理单一文件
    /// </summary>
    private static void DealWithGroupSingle(AddressableAssetGroup targetGroup, float startValue, float endValue) {
        int addCount = 0;
        int totalCount = AARules.singleList.assetList.Count + AARules.singleList.dirList.Count;
        void SetEntryInfo(string file, string address) {
            var guid = AssetDatabase.AssetPathToGUID(file);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = address.Replace("\\", "/");
            addCount += 1;
            float value = ((float)addCount / totalCount) * (endValue - startValue) + startValue;
            EditorUtility.DisplayProgressBar(ProgressTitle, address, value);
        }

        foreach (var item in AARules.singleList.assetList) {
            // 处理单个文件
            var path = AssetDatabase.GetAssetPath(item);
            SetEntryInfo(path, path);
        }
        foreach (var item in AARules.singleList.dirList) {
            // 处理文件夹下的所有文件
            var path = AssetDatabase.GetAssetPath(item.asset);
            List<string> pathList = new List<string>();
            FileUtil.TraverseDirectories(path, 0, item.number, pathList);
            foreach (var pathTp in pathList) {
                if (item.searchPattern == "") {
                    item.searchPattern = AARules.SearchPattern;
                }
                string[] subDirectories = Directory.GetFiles(pathTp, item.searchPattern, item.option);
                foreach (var subPath in subDirectories) {
                    if (subPath.EndsWith(".meta")) {
                        continue;
                    }
                    SetEntryInfo(subPath, subPath);
                }
            }
        }
    }
    
    /// <summary>
    /// 处理标签数据
    /// </summary>
    private static void DealWithGroupLabel(AddressableAssetGroup targetGroup, float startValue, float endValue) {
        // 自定义一些组设置
        var schemaBundle = targetGroup.GetSchema<BundledAssetGroupSchema>();
        schemaBundle.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel;
        
        // 记入使用的标签队列
        Dictionary<string, bool> labelDic = new Dictionary<string, bool>();

        void SetEntryInfo(string path, string address, string label) {
            var guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup);
            entry.address = address.Replace("\\", "/");
            if (!settings.GetLabels().Contains(label)) {
                settings.AddLabel(label);
            }
            labelDic.TryAdd(label, true);
            entry.SetLabel(label, true, true);
        }
        int addCount = 0;
        int totalCount = AARules.labelList.Count;
        foreach (var item in AARules.labelList) {
            foreach (var data in item.assetList) {
                var path = AssetDatabase.GetAssetPath(data);
                SetEntryInfo(path, path, item.label);
            }
            foreach (var data in item.dirList) {
                // 收集排除队列
                Dictionary<string, bool> excludeDir = new Dictionary<string, bool>();
                // 地址队列
                List<string> pathList = new List<string>();
                var path = AssetDatabase.GetAssetPath(data.folder);
                if (path == null) {
                    Debug.LogError("AARules 内找不到 " + data.folder);
                    continue;
                }
                foreach (var itemTp in data.excludePathList) {
                    var pathTp = AssetDatabase.GetAssetPath(itemTp);
                    excludeDir.Add(pathTp, true);
                }
                FileUtil.TraverseDirectories(path, 0, data.number, pathList);
                // 添加地址组
                foreach (var pathTp in pathList) {
                    if (excludeDir.ContainsKey(pathTp)) {
                        continue;
                    }
                    SetEntryInfo(pathTp, pathTp, item.label);
                }
            }
            addCount += 1;
            float value = ((float)addCount / totalCount) * (endValue - startValue) + startValue;
            EditorUtility.DisplayProgressBar(ProgressTitle, item.label, value);
        }
        // 清空无用的标签
        foreach (var label in settings.GetLabels()) {
            if (!labelDic.ContainsKey(label)) {
                settings.RemoveLabel(label);
            }
        }
    }

    /// <summary>
    /// 初始化组数据
    /// </summary>
    private static AddressableAssetGroup InitGroupData(string groupName) {
        AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
        if (targetGroup == null) {
            // 创建组
            targetGroup = settings.CreateGroup(groupName, false, false, false, null);
            var schemaContent = targetGroup.GetSchema<ContentUpdateGroupSchema>();
            if (schemaContent == null) {
                schemaContent = targetGroup.AddSchema<ContentUpdateGroupSchema>();
                schemaContent.StaticContent = false;
            }
            var schemaBundle = targetGroup.GetSchema<BundledAssetGroupSchema>();
            if (schemaBundle == null) {
                schemaBundle = targetGroup.AddSchema<BundledAssetGroupSchema>();
                schemaBundle.BuildPath.SetVariableByName(settings, "Local.BuildPath");
                schemaBundle.LoadPath.SetVariableByName(settings, "Local.LoadPath");
                schemaBundle.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                schemaBundle.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            }
        } else {
            // 清空组
            var entries = new List<AddressableAssetEntry>(targetGroup.entries);
            foreach (var entry in entries) {
                settings.RemoveAssetEntry(entry.guid);
            }
        }
        return targetGroup;
    }

    private static AddressableAssetSettings settings;
    private static AARules AARules;
    
    private const string ProgressTitle = "Addressable 打包";
}
