using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DCFrame;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using FileUtil = DCFrame.Utility.FileUtil;

public class AddressableEditor : Editor {
    [MenuItem("Tools/资源项/打 AA 包")]
    public static void PackageAddressable() {
        if (!PrepareAddressableData()) {
            return;
        }
        AddressableAssetSettings.BuildPlayerContent();
    }

    /// <summary>
    /// 按 AARules 同步 Addressables 分组与热更标签。
    /// </summary>
    public static bool PrepareAddressableData() {
        AARules = AssetDatabase.LoadAssetAtPath<AARules>(AAConst.AARulesPath);
        if (AARules == null) {
            Debug.LogError("找不到 AA 规则文件");
            return false;
        }
        enableHotUpdate = LoadHotUpdateSettings();
        return StartAddressable();
    }

    private static bool StartAddressable() {
        settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) {
            Debug.LogError("Addressable Asset Settings not found!");
            return false;
        }

        List<float> progressList = new List<float>() { 0, 0.3f, 0.6f, 0.9f, 1 };
        EditorUtility.DisplayProgressBar(ProgressTitle, "打 AA 包开始", progressList[0]);
        InitPackageGroups();
        DealWithGroupSingle(progressList[0], progressList[1]);
        DealWithGroupLabel(progressList[1], progressList[2]);
        DealWithGroupFolder(progressList[2], progressList[3]);
        DealWithHotUpdateSettings();
        InitGroupData(AAConst.AADefaultLocalGroup);
        AssetDatabase.SaveAssets();
        EditorUtility.ClearProgressBar();
        return true;
    }

    /// <summary>
    /// 处理文件夹规则；远端资源默认打 GameStartUp 标签。
    /// </summary>
    private static void DealWithGroupFolder(float startValue, float endValue) {
        int addCount = 0;
        int totalCount = AARules.folderList.Count;
        foreach (var item in AARules.folderList) {
            Dictionary<string, bool> excludeDir = new Dictionary<string, bool>();
            List<string> pathList = new List<string>();

            var path = AssetDatabase.GetAssetPath(item.folder);
            if (path == null) {
                Debug.LogError("AARules 内找不到 " + item.folder);
                continue;
            }
            FileUtil.TraverseDirectories(path, 0, item.number, pathList);
            foreach (var itemTp in item.excludePathList) {
                var pathTp = AssetDatabase.GetAssetPath(itemTp);
                excludeDir[pathTp] = true;
            }

            bool isRemote = IsRemote(item.isRemote);
            AddressableAssetGroup targetGroup = GetFolderGroup(isRemote);
            foreach (var pathTp in pathList) {
                if (excludeDir.ContainsKey(pathTp)) {
                    continue;
                }
                SetEntryInfo(targetGroup, pathTp, pathTp, isRemote ? AAConst.AAGameStartUpLabel : string.Empty);
            }
            addCount += 1;
            ShowProgress(path, addCount, totalCount, startValue, endValue);
        }
    }

    /// <summary>
    /// 处理单资源规则；远端资源可指定业务标签，空标签默认 GameStartUp。
    /// </summary>
    private static void DealWithGroupSingle(float startValue, float endValue) {
        int addCount = 0;
        int totalCount = AARules.singleList.assetList.Count + AARules.singleList.dirList.Count;
        foreach (var item in AARules.singleList.assetList) {
            var path = AssetDatabase.GetAssetPath(item.asset);
            bool isRemote = IsRemote(item.isRemote);
            AddressableAssetGroup targetGroup = GetSingleGroup(isRemote);
            SetEntryInfo(targetGroup, path, path, GetRemoteLabel(isRemote, item.label));
            addCount += 1;
            ShowProgress(path, addCount, totalCount, startValue, endValue);
        }
        foreach (var item in AARules.singleList.dirList) {
            bool isRemote = IsRemote(item.isRemote);
            AddressableAssetGroup targetGroup = GetSingleGroup(isRemote);
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
                    SetEntryInfo(targetGroup, subPath, subPath, GetRemoteLabel(isRemote, item.label));
                }
            }
            addCount += 1;
            ShowProgress(path, addCount, totalCount, startValue, endValue);
        }
    }

    /// <summary>
    /// 处理业务标签规则；LabelList 始终进入远端标签组。
    /// </summary>
    private static void DealWithGroupLabel(float startValue, float endValue) {
        if (!enableHotUpdate) {
            return;
        }

        int addCount = 0;
        int totalCount = AARules.labelList.Count;
        foreach (var item in AARules.labelList) {
            if (string.IsNullOrWhiteSpace(item.label)) {
                Debug.LogWarning("LabelList 内存在空标签，已跳过。");
                continue;
            }
            if (item.label == AAConst.AAGameStartUpLabel) {
                Debug.LogWarning($"LabelList 不应手动配置 {AAConst.AAGameStartUpLabel}，已跳过。");
                continue;
            }

            AddressableAssetGroup targetGroup = remoteLabelGroup;
            foreach (var data in item.assetList) {
                var path = AssetDatabase.GetAssetPath(data);
                SetEntryInfo(targetGroup, path, path, item.label);
            }
            foreach (var data in item.dirList) {
                Dictionary<string, bool> excludeDir = new Dictionary<string, bool>();
                List<string> pathList = new List<string>();
                var path = AssetDatabase.GetAssetPath(data.folder);
                if (path == null) {
                    Debug.LogError("AARules 内找不到 " + data.folder);
                    continue;
                }
                foreach (var itemTp in data.excludePathList) {
                    var pathTp = AssetDatabase.GetAssetPath(itemTp);
                    excludeDir[pathTp] = true;
                }
                FileUtil.TraverseDirectories(path, 0, data.number, pathList);
                foreach (var pathTp in pathList) {
                    if (excludeDir.ContainsKey(pathTp)) {
                        continue;
                    }
                    SetEntryInfo(targetGroup, pathTp, pathTp, item.label);
                }
            }
            addCount += 1;
            ShowProgress(item.label, addCount, totalCount, startValue, endValue);
        }
    }

    /// <summary>
    /// 初始化本地与远端打包组，确保每次重新打包前清空旧条目。
    /// </summary>
    private static void InitPackageGroups() {
        localSingleGroup = InitGroupData(AAConst.AALocalSingle);
        localFolderGroup = InitGroupData(AAConst.AALocalFolder);
        ClearGroupData(AAConst.AALocalLabel);

        remoteSingleGroup = HasRemoteSingle() ? InitGroupData(AAConst.AARemoteSingle, true) : ClearGroupData(AAConst.AARemoteSingle);
        remoteLabelGroup = enableHotUpdate ? InitGroupData(AAConst.AARemoteLabel, true, true) : ClearGroupData(AAConst.AARemoteLabel);
        remoteFolderGroup = HasRemoteFolder() ? InitGroupData(AAConst.AARemoteFolder, true) : ClearGroupData(AAConst.AARemoteFolder);
    }

    /// <summary>
    /// 根据远端标记获取单资源目标组。
    /// </summary>
    private static AddressableAssetGroup GetSingleGroup(bool isRemote) {
        return isRemote ? remoteSingleGroup : localSingleGroup;
    }

    /// <summary>
    /// 根据远端标记获取文件夹目标组。
    /// </summary>
    private static AddressableAssetGroup GetFolderGroup(bool isRemote) {
        return isRemote ? remoteFolderGroup : localFolderGroup;
    }

    /// <summary>
    /// 热更关闭时强制使用本地组。
    /// </summary>
    private static bool IsRemote(bool isRemote) {
        return enableHotUpdate && isRemote;
    }

    /// <summary>
    /// 获取远端单资源标签；空标签归入启动前下载。
    /// </summary>
    private static string GetRemoteLabel(bool isRemote, string label) {
        if (!isRemote) {
            return string.Empty;
        }
        return string.IsNullOrWhiteSpace(label) ? AAConst.AAGameStartUpLabel : label.Trim();
    }

    /// <summary>
    /// 单资源规则是否需要远端组。
    /// </summary>
    private static bool HasRemoteSingle() {
        if (!enableHotUpdate) {
            return false;
        }
        if (AARules.singleList.assetList.Any(item => item.isRemote)) {
            return true;
        }
        return AARules.singleList.dirList.Any(item => item.isRemote);
    }

    /// <summary>
    /// 文件夹规则是否需要远端组。
    /// </summary>
    private static bool HasRemoteFolder() {
        return enableHotUpdate && AARules.folderList.Any(item => item.isRemote);
    }

    /// <summary>
    /// 生成本地热更启动配置，并确保配置文件固定在本地单文件组。
    /// </summary>
    private static void DealWithHotUpdateSettings() {
        CreateHotUpdateSettingsJson();
        MarkHotUpdateSettingsAsLocalAddressable();
    }

    /// <summary>
    /// 初始化组数据。
    /// </summary>
    private static AddressableAssetGroup InitGroupData(string groupName, bool isRemote = false, bool packTogetherByLabel = false) {
        AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
        if (targetGroup == null) {
            targetGroup = settings.CreateGroup(groupName, false, false, false, null);
            var schemaContent = targetGroup.GetSchema<ContentUpdateGroupSchema>();
            if (!schemaContent) {
                schemaContent = targetGroup.AddSchema<ContentUpdateGroupSchema>();
                schemaContent.StaticContent = false;
            }
            var schemaBundle = targetGroup.GetSchema<BundledAssetGroupSchema>();
            if (!schemaBundle) {
                schemaBundle = targetGroup.AddSchema<BundledAssetGroupSchema>();
                schemaBundle.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            }
            ApplyGroupPath(schemaBundle, isRemote);
            schemaBundle.BundleMode = packTogetherByLabel ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel : BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        } else {
            var entries = new List<AddressableAssetEntry>(targetGroup.entries);
            foreach (var entry in entries) {
                settings.RemoveAssetEntry(entry.guid);
            }
            var schemaBundle = targetGroup.GetSchema<BundledAssetGroupSchema>();
            if (schemaBundle) {
                ApplyGroupPath(schemaBundle, isRemote);
                schemaBundle.BundleMode = packTogetherByLabel ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel : BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                EditorUtility.SetDirty(schemaBundle);
            }
        }
        return targetGroup;
    }

    /// <summary>
    /// 清空已存在但本轮未使用的组。
    /// </summary>
    private static AddressableAssetGroup ClearGroupData(string groupName) {
        AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
        if (targetGroup == null) {
            return null;
        }

        var entries = new List<AddressableAssetEntry>(targetGroup.entries);
        foreach (var entry in entries) {
            settings.RemoveAssetEntry(entry.guid);
        }
        return targetGroup;
    }

    /// <summary>
    /// 按组类型应用本地或远端构建与加载路径。
    /// </summary>
    private static void ApplyGroupPath(BundledAssetGroupSchema schemaBundle, bool isRemote) {
        var buildName = isRemote ? AddressableAssetSettings.kRemoteBuildPath : "Local.BuildPath";
        var loadName = isRemote ? AddressableAssetSettings.kRemoteLoadPath : "Local.LoadPath";
        schemaBundle.BuildPath.SetVariableByName(settings, buildName);
        schemaBundle.LoadPath.SetVariableByName(settings, loadName);
    }

    /// <summary>
    /// 创建或移动 Addressables 条目，并按需设置热更标签。
    /// </summary>
    private static void SetEntryInfo(AddressableAssetGroup targetGroup, string file, string address, string label) {
        var guid = AssetDatabase.AssetPathToGUID(file);
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, targetGroup);
        entry.address = address.Replace("\\", "/");
        SetEntryLabel(entry, label);
    }

    /// <summary>
    /// 为条目设置标签；空标签会保留为普通本地或远端资源。
    /// </summary>
    private static void SetEntryLabel(AddressableAssetEntry entry, string label) {
        if (string.IsNullOrWhiteSpace(label)) {
            return;
        }
        label = label.Trim();
        if (!settings.GetLabels().Contains(label)) {
            settings.AddLabel(label);
        }
        entry.SetLabel(label, true, true);
    }

    /// <summary>
    /// 创建本地热更启动配置 JSON。
    /// </summary>
    private static void CreateHotUpdateSettingsJson() {
        HotUpdateSettingsData data = new HotUpdateSettingsData {
            enableHotUpdate = enableHotUpdate
        };

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), AAConst.AAHotUpdateSettingsPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? string.Empty);
        File.WriteAllText(fullPath, JsonUtility.ToJson(data, true));
        AssetDatabase.ImportAsset(AAConst.AAHotUpdateSettingsPath);
    }

    /// <summary>
    /// 读取本地热更启动配置。
    /// </summary>
    private static bool LoadHotUpdateSettings() {
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), AAConst.AAHotUpdateSettingsPath);
        if (!File.Exists(fullPath)) {
            return false;
        }

        try {
            HotUpdateSettingsData data = JsonUtility.FromJson<HotUpdateSettingsData>(File.ReadAllText(fullPath));
            return data != null && data.enableHotUpdate;
        }
        catch (Exception ex) {
            Debug.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// 将本地热更启动配置 JSON 强制放入本地单文件组。
    /// </summary>
    private static void MarkHotUpdateSettingsAsLocalAddressable() {
        string guid = AssetDatabase.AssetPathToGUID(AAConst.AAHotUpdateSettingsPath);
        if (string.IsNullOrEmpty(guid) || localSingleGroup == null) {
            Debug.LogError("本地热更启动配置 JSON 未能加入本地 Addressables 组");
            return;
        }

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, localSingleGroup);
        entry.address = AAConst.AAHotUpdateSettingsPath;
    }

    private static void ShowProgress(string info, int addCount, int totalCount, float startValue, float endValue) {
        if (totalCount <= 0) {
            return;
        }
        float value = ((float)addCount / totalCount) * (endValue - startValue) + startValue;
        EditorUtility.DisplayProgressBar(ProgressTitle, info, value);
    }

    private static AddressableAssetSettings settings;
    private static AARules AARules;
    private static AddressableAssetGroup localSingleGroup;
    private static AddressableAssetGroup localFolderGroup;
    private static AddressableAssetGroup remoteSingleGroup;
    private static AddressableAssetGroup remoteLabelGroup;
    private static AddressableAssetGroup remoteFolderGroup;
    private static bool enableHotUpdate;

    private const string ProgressTitle = "Addressable 打包";

    [Serializable]
    private class HotUpdateSettingsData {
        public bool enableHotUpdate;
    }
}
