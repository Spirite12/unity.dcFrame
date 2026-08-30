using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DCFrame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using FileUtil = DCFrame.Utility.FileUtil;

public class AddressableProcessor : AssetPostprocessor {

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        InitData();
        // 资源导入回调
        foreach (string assetPath in importedAssets) {
            CheckAsset(assetPath);
        }
        // 检测移动的资源
        foreach (var assetPath in movedAssets) {
            CheckAsset(assetPath);
        }
    }

    /// <summary>
    /// 检查资源是否可以标记
    /// </summary>
    private static void CheckAsset(string assetPath) {
        assetPath = assetPath.Replace("\\", "/");
        var path = Path.GetDirectoryName(assetPath);
        if (path == null) {
            return;
        }
        path = path.Replace("\\", "/");
        if (pathSingleDic.TryGetValue(path, out var value)) {
            string fileName = Path.GetFileName(assetPath);
            string pattern = "^" + Regex.Escape(value).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            if (Regex.IsMatch(fileName, pattern)) {
                pathSingleLabelDic.TryGetValue(path, out var label);
                MarkAsAddressable(assetPath, label);
            }
        }else if (Directory.Exists(assetPath)) {
            foreach (var keyValue in pathFolderDic) {
                string folderPath = keyValue.Key.TrimEnd('/');
                if (assetPath != folderPath && !assetPath.StartsWith(folderPath + "/", System.StringComparison.Ordinal)) {
                    continue;
                }
                string relativePath = Path.GetRelativePath(folderPath, assetPath);
                int depth = relativePath.Split(Path.DirectorySeparatorChar).Length;
                if (depth == keyValue.Value.number) {
                    MarkAsAddressable(assetPath, keyValue.Value.label);
                }
            }
        }
    }
    
    /// <summary>
    /// 初始化数据
    /// </summary>
    public static void InitData(bool isForce = false) {
        if (AARules == null) {
            AARules = AssetDatabase.LoadAssetAtPath<AARules>(AAConst.AARulesPath);
        }
        if (pathSingleDic != null && pathSingleLabelDic != null && !isForce && pathFolderDic != null) {
            return;
        }
        pathSingleDic = new Dictionary<string, string>();
        pathSingleLabelDic = new Dictionary<string, string>();
        pathFolderDic = new Dictionary<string, FolderKey>();
        DealWithGroupFolder();
        DealWithGroupSingle();
        DealWithGroupLabel();
    }

    /// <summary>
    /// 处理组文件夹
    /// </summary>
    private static void DealWithGroupFolder() {
        bool enableHotUpdate = IsHotUpdateEnabled();
        foreach (var data in AARules.folderList) {
            if (data.number <= 0) continue;
            var path = AssetDatabase.GetAssetPath(data.folder);
            var newPath = path.Replace("\\", "/");
            pathFolderDic.TryAdd(newPath, new FolderKey(){ number = data.number, label = enableHotUpdate && data.isRemote ? AAConst.AAGameStartUpLabel : "" });
        }
    }
    
    /// <summary>
    /// 处理标签数据内的文件夹数据
    /// </summary>
    private static void DealWithGroupLabel() {
        if (!IsHotUpdateEnabled()) {
            return;
        }
        foreach (var data in AARules.labelList) {
            if (string.IsNullOrWhiteSpace(data.label) || data.label == AAConst.AAGameStartUpLabel) {
                continue;
            }
            if (data.dirList.Count > 0) {
                foreach (var dirData in data.dirList) {
                    if (dirData.number <= 0) continue;
                    var path = AssetDatabase.GetAssetPath(dirData.folder);
                    var newPath = path.Replace("\\", "/");
                    pathFolderDic.TryAdd(newPath, new FolderKey(){ number = dirData.number, label = data.label});
                }
            }
        }
    }
    
    /// <summary>
    /// 处理单一文件内的文件夹数据
    /// </summary>
    private static void DealWithGroupSingle() {
        bool enableHotUpdate = IsHotUpdateEnabled();
        foreach (var data in AARules.singleList.dirList) {
            var path = AssetDatabase.GetAssetPath(data.asset);
            string label = enableHotUpdate && data.isRemote ? GetRemoteLabel(data.label) : "";
            List<string> pathList = new List<string>();
            FileUtil.TraverseDirectories(path, 0, data.number, pathList);
            foreach (var pathTp in pathList) {
                if (data.option == SearchOption.AllDirectories) {
                    var newPath = path.Replace("\\", "/");
                    string[] subDirectories = Directory.GetDirectories(pathTp, "*", SearchOption.AllDirectories);
                    pathSingleDic.TryAdd(newPath, data.searchPattern);
                    pathSingleLabelDic.TryAdd(newPath, label);
                    foreach (var subPath in subDirectories) {
                        newPath = subPath.Replace("\\", "/");
                        pathSingleDic.TryAdd(newPath, data.searchPattern);
                        pathSingleLabelDic.TryAdd(newPath, label);
                    }
                }
                else {
                    var newPath = pathTp.Replace("\\", "/");
                    pathSingleDic.TryAdd(newPath, data.searchPattern);
                    pathSingleLabelDic.TryAdd(newPath, label);
                }
            }
        }
    }

    /// <summary>
    /// 将资源标记为 Addressable
    /// </summary>
    private static void MarkAsAddressable(string assetPath, string label = "") {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) {
            Debug.LogError("AddressableAssetSettings 未找到");
            return;
        }
        AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
        if (entry != null) {
            if (entry.address != assetPath) {
                entry.address = assetPath;
            }
            if (label != "") {
                entry.SetLabel(label, true, true);
            }
            return;
        }
        AddressableAssetGroup defaultGroup = settings.DefaultGroup;
        entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), defaultGroup);
        entry.address = assetPath;
        if (label != "") {
            entry.SetLabel(label, true, true);
        }
    }

    /// <summary>
    /// 读取当前是否开启热更，用于决定自动标签是否生效。
    /// </summary>
    private static bool IsHotUpdateEnabled() {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), AAConst.AAHotUpdateSettingsPath);
        if (!File.Exists(fullPath)) {
            return false;
        }

        try {
            HotUpdateSettingsData data = JsonUtility.FromJson<HotUpdateSettingsData>(File.ReadAllText(fullPath));
            return data != null && data.enableHotUpdate;
        }
        catch {
            return false;
        }
    }

    /// <summary>
    /// 获取远端单资源自动标签；空标签归入启动前下载。
    /// </summary>
    private static string GetRemoteLabel(string label) {
        return string.IsNullOrWhiteSpace(label) ? AAConst.AAGameStartUpLabel : label.Trim();
    }
    
    private static AARules AARules;
    /// <summary>
    /// key : 地址，Vale : Search Pattern
    /// </summary>
    private static Dictionary<string, string> pathSingleDic;
    private static Dictionary<string, string> pathSingleLabelDic;
    /// <summary>
    /// key : 地址
    /// </summary>
    private static Dictionary<string, FolderKey> pathFolderDic;
    private class FolderKey {
        public int number = 0;
        public string label = "";
    }

    [System.Serializable]
    private class HotUpdateSettingsData {
        public bool enableHotUpdate;
    }
}
