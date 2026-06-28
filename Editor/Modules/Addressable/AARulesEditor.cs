using DCFrame;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.AddressableAssets.Build;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using Debug = UnityEngine.Debug;

[CustomEditor(typeof(AARules))]
public class AARulesEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();

        // 热更路径
        EditorGUILayout.LabelField("热更配置", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        bool enableHotUpdate = LoadHotUpdateSettings();
        bool newEnableHotUpdate = EditorGUILayout.ToggleLeft("设置热更配置", enableHotUpdate);
        if (newEnableHotUpdate != enableHotUpdate) {
            SaveHotUpdateSettings(newEnableHotUpdate);
            enableHotUpdate = newEnableHotUpdate;
        }
        if (enableHotUpdate) {
            if (GUILayout.Button("打开热更配置")) {
                OpenUploadConfig();
            }
            if (GUILayout.Button("应用热更路径")) {
                ApplyRemoteAddressablePathFromLocalConfig();
            }
            if (GUILayout.Button("构建Addressables")) {
                BuildAddressablesByLocalConfig();
            }
            if (GUILayout.Button("上传AA到远端")) {
                UploadAddressablesByLocalConfig();
            }
        }
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // 资源打包
        EditorGUILayout.LabelField("打包规则", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        if (GUILayout.Button("提示说明")) {
            string str = "";
            str += "Number：第几级文件夹\n当前文件夹从0开始，子级文件夹递归+1\n\n";
            str += "SearchPattern:\n *.* 搜索全文件 、*.png 搜索 png文件\n其他搜索格式以 *. 开头即可\n\n";
            str += "Option:\nTopDirectoryOnly：只搜索当前目录\nAllDirectories：递归搜索所有子目录\n";
            EditorUtility.DisplayDialog("说明介绍", str, "关闭");
        }
        EditorGUILayout.Space();
        DrawRulesInspector(enableHotUpdate);
        EditorGUILayout.Space();
        if (GUILayout.Button("数据更新")) {
            AddressableProcessor.InitData(true);
        }
        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 读取本地热更启动配置。
    /// </summary>
    private static bool LoadHotUpdateSettings() {
        string path = GetHotUpdateSettingsFullPath();
        if (!File.Exists(path)) {
            return false;
        }

        try {
            HotUpdateSettingsData data = JsonUtility.FromJson<HotUpdateSettingsData>(File.ReadAllText(path));
            return data != null && data.enableHotUpdate;
        }
        catch (Exception ex) {
            Debug.LogException(ex);
            return false;
        }
    }

    /// <summary>
    /// 保存本地热更启动配置。
    /// </summary>
    private static void SaveHotUpdateSettings(bool enableHotUpdate) {
        string path = GetHotUpdateSettingsFullPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllText(path, JsonUtility.ToJson(new HotUpdateSettingsData {
            enableHotUpdate = enableHotUpdate
        }, true));
        AssetDatabase.ImportAsset(AAConst.AAHotUpdateSettingsPath);
        if (enableHotUpdate) {
            CreateUploadConfig();
            return;
        }

        DeleteUploadConfig();
    }

    /// <summary>
    /// 按热更模式绘制 AA 规则，避免单机模式暴露远端字段。
    /// </summary>
    private void DrawRulesInspector(bool enableHotUpdate) {
        DrawSingleList(enableHotUpdate);
        DrawFolderList(enableHotUpdate);
        if (enableHotUpdate){
            DrawLabelList();
        }
    }

    /// <summary>
    /// 绘制单资源规则；联网模式下才显示远端与标签配置。
    /// </summary>
    private void DrawSingleList(bool enableHotUpdate) {
        SerializedProperty singleList = serializedObject.FindProperty("singleList");
        if (!BeginRootFoldout(singleList, "以单资源打包")) {
            return;
        }

        EditorGUI.indentLevel++;
        DrawSingleAssetList(singleList.FindPropertyRelative("assetList"), enableHotUpdate);
        DrawSingleDirList(singleList.FindPropertyRelative("dirList"), enableHotUpdate);
        EditorGUI.indentLevel--;
        EndRootFoldout();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 绘制单资源列表。
    /// </summary>
    private void DrawSingleAssetList(SerializedProperty list, bool enableHotUpdate) {
        EditorGUILayout.PropertyField(list, new GUIContent("Asset List"), false);
        if (!list.isExpanded) {
            return;
        }
        EditorGUI.indentLevel++;
        list.arraySize = EditorGUILayout.IntField("Size", list.arraySize);
        for (int i = 0; i < list.arraySize; i++) {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            bool isExpanded = BeginArrayElement(item, i);
            if (isExpanded) {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("asset"));
                if (enableHotUpdate) {
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("isRemote"), new GUIContent("Is Remote"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("label"));
                }
            }
            EndArrayElement(isExpanded);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制单文件夹列表。
    /// </summary>
    private void DrawSingleDirList(SerializedProperty list, bool enableHotUpdate) {
        EditorGUILayout.PropertyField(list, new GUIContent("Dir List"), false);
        if (!list.isExpanded) {
            return;
        }
        EditorGUI.indentLevel++;
        list.arraySize = EditorGUILayout.IntField("Size", list.arraySize);
        for (int i = 0; i < list.arraySize; i++) {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            bool isExpanded = BeginArrayElement(item, i);
            if (isExpanded) {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("asset"));
                if (enableHotUpdate) {
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("isRemote"), new GUIContent("Is Remote"));
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("label"));
                }
                EditorGUILayout.PropertyField(item.FindPropertyRelative("number"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("searchPattern"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("option"));
            }
            EndArrayElement(isExpanded);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制文件夹规则；FolderList 不显示标签字段。
    /// </summary>
    private void DrawFolderList(bool enableHotUpdate) {
        SerializedProperty list = serializedObject.FindProperty("folderList");
        if (!BeginRootFoldout(list, "以文件夹打包：文件夹之间没有关联")) {
            return;
        }

        EditorGUI.indentLevel++;
        list.arraySize = EditorGUILayout.IntField("Size", list.arraySize);
        for (int i = 0; i < list.arraySize; i++) {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            bool isExpanded = BeginArrayElement(item, i);
            if (isExpanded) {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("folder"));
                if (enableHotUpdate) {
                    EditorGUILayout.PropertyField(item.FindPropertyRelative("isRemote"), new GUIContent("Is Remote"));
                }
                EditorGUILayout.PropertyField(item.FindPropertyRelative("number"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("excludePathList"), true);
            }
            EndArrayElement(isExpanded);
        }
        EditorGUI.indentLevel--;
        EndRootFoldout();
    }

    /// <summary>
    /// 绘制游戏内热更标签规则；LabelList 固定进入远端组，不显示 IsRemote。
    /// </summary>
    private void DrawLabelList() {
        SerializedProperty list = serializedObject.FindProperty("labelList");
        if (!BeginRootFoldout(list, "以标签名打包：主要处理游戏内热更")) {
            return;
        }

        EditorGUI.indentLevel++;
        list.arraySize = EditorGUILayout.IntField("Size", list.arraySize);
        for (int i = 0; i < list.arraySize; i++) {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            bool isExpanded = BeginArrayElement(item, i);
            if (isExpanded) {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("label"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("assetList"), true);
                DrawLabelDirList(item.FindPropertyRelative("dirList"));
            }
            EndArrayElement(isExpanded);
        }
        EditorGUI.indentLevel--;
        EndRootFoldout();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 绘制标签规则下的文件夹列表。
    /// </summary>
    private void DrawLabelDirList(SerializedProperty list) {
        EditorGUILayout.PropertyField(list, new GUIContent("Dir List"), false);
        if (!list.isExpanded) {
            return;
        }
        EditorGUI.indentLevel++;
        list.arraySize = EditorGUILayout.IntField("Size", list.arraySize);
        for (int i = 0; i < list.arraySize; i++) {
            SerializedProperty item = list.GetArrayElementAtIndex(i);
            bool isExpanded = BeginArrayElement(item, i);
            if (isExpanded) {
                EditorGUILayout.PropertyField(item.FindPropertyRelative("folder"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("number"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("excludePathList"), true);
            }
            EndArrayElement(isExpanded);
        }
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制数组元素折叠框，保留自定义字段隐藏规则下的默认伸缩体验。
    /// </summary>
    private static bool BeginArrayElement(SerializedProperty item, int index) {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, $"Element {index}", true);
        if (!item.isExpanded) {
            return false;
        }

        EditorGUI.indentLevel++;
        return true;
    }

    /// <summary>
    /// 结束数组元素折叠框绘制。
    /// </summary>
    private static void EndArrayElement(bool isExpanded) {
        if (isExpanded && EditorGUI.indentLevel > 0) {
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制根级规则折叠标题，统一三个主列表的三角展开样式。
    /// </summary>
    private static bool BeginRootFoldout(SerializedProperty property, string title) {
        property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, title, true);
        if (!property.isExpanded) {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 结束根级规则折叠区域绘制。
    /// </summary>
    private static void EndRootFoldout() {
    }

    /// <summary>
    /// 将 AARules 中配置的远程路径应用到 Addressables Profile 与远程 Catalog。
    /// </summary>
    private static void ApplyRemoteAddressablePath(string remoteBuildPath, string remoteLoadPath) {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!settings) {
            Debug.LogError("AddressableAssetSettings 未找到");
            return;
        }
        if (string.IsNullOrWhiteSpace(remoteBuildPath) || string.IsNullOrWhiteSpace(remoteLoadPath)) {
            EditorUtility.DisplayDialog("应用失败", "远程构建路径和远程加载路径不能为空。", "关闭");
            return;
        }

        string profileId = settings.activeProfileId;
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, remoteBuildPath.Trim());
        settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, remoteLoadPath.Trim());
        settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

        // 开启远程 Catalog，并让启动更新检查交给项目自己的热更流程控制。
        settings.BuildRemoteCatalog = true;
        settings.DisableCatalogUpdateOnStartup = true;
        settings.CatalogRequestsTimeout = Mathf.Max(settings.CatalogRequestsTimeout, 10);
        settings.UniqueBundleIds = true;
        ApplyRemotePathToGroup(settings, AAConst.AARemoteSingle);
        ApplyRemotePathToGroup(settings, AAConst.AARemoteLabel, true);
        ApplyRemotePathToGroup(settings, AAConst.AARemoteFolder);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"已应用 Addressables 远程路径：BuildPath={remoteBuildPath}, LoadPath={remoteLoadPath}");
    }

    /// <summary>
    /// 从本地上传配置读取远程路径，并应用到 Addressables 设置。
    /// </summary>
    private static void ApplyRemoteAddressablePathFromLocalConfig() {
        HotUpdateUploadConfig config = LoadUploadConfig();
        if (config == null) {
            return;
        }
        string remoteBuildPath = GetRemoteBuildPath(config);
        string remoteLoadPath = GetRemoteLoadPath(config);
        if (string.IsNullOrWhiteSpace(remoteLoadPath)) {
            EditorUtility.DisplayDialog("应用失败", "本地配置缺少 publicBaseUrl，无法生成 remoteLoadPath。", "关闭");
            return;
        }

        ApplyRemoteAddressablePath(remoteBuildPath, remoteLoadPath);
    }

    /// <summary>
    /// 调用上传脚本，把当前平台 Addressables 构建产物上传到远端。
    /// </summary>
    private static void UploadAddressablesByLocalConfig() {
        HotUpdateUploadConfig config = LoadUploadConfig();
        if (config == null) {
            return;
        }

        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        string scriptPath = GetUploadScriptPath();
        string configPath = GetUploadConfigPath();
        if (!File.Exists(scriptPath)) {
            EditorUtility.DisplayDialog("上传失败", $"上传脚本不存在：{scriptPath}", "关闭");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = "powershell",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ConfigPath \"{configPath}\" -Platform \"{platform}\"",
            WorkingDirectory = Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        try {
            Process process = Process.Start(startInfo);
            if (process == null) {
                EditorUtility.DisplayDialog("上传失败", "PowerShell 进程启动失败。", "关闭");
                return;
            }
            StringBuilder outputBuilder = new StringBuilder();
            StringBuilder errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    outputBuilder.AppendLine(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    errorBuilder.AppendLine(args.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit(UploadTimeoutMilliseconds)) {
                process.Kill();
                EditorUtility.DisplayDialog("上传失败", "上传超时，已终止上传进程。请检查 SSH Key、服务器地址、端口和网络连通性。", "关闭");
                return;
            }
            process.WaitForExit();

            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();
            if (!string.IsNullOrWhiteSpace(output)) {
                Debug.Log(output);
            }
            if (process.ExitCode != 0) {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("上传失败", error, "关闭");
                return;
            }
            EditorUtility.DisplayDialog("上传完成", string.IsNullOrWhiteSpace(output) ? "Addressables 上传完成。" : output, "关闭");
        }
        catch (Exception ex) {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("上传失败", ex.Message, "关闭");
        }
    }

    /// <summary>
    /// 从本地配置读取平台，并执行 Addressables 构建。
    /// </summary>
    private static void BuildAddressablesByLocalConfig() {
        HotUpdateUploadConfig config = LoadUploadConfig();
        if (config == null) {
            return;
        }

        string platform = EditorUserBuildSettings.activeBuildTarget.ToString();
        if (!AddressableEditor.PrepareAddressableData()) {
            return;
        }
        BuildAddressables(platform);
    }

    /// <summary>
    /// 构建 Addressables；存在上一轮状态文件时优先尝试 Update，条件不满足时自动回退到 New Build。
    /// </summary>
    private static bool BuildAddressables(string platform) {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (!settings) {
            EditorUtility.DisplayDialog("构建失败", "AddressableAssetSettings 未找到。", "关闭");
            return false;
        }

        string contentStatePath = GetContentStatePath(platform);
        bool useContentUpdate = File.Exists(contentStatePath);
        string buildMode = useContentUpdate ? "Update Previous Build" : "New Build";
        Debug.Log($"开始 Addressables 构建：{buildMode}，平台：{platform}");

        AddressablesPlayerBuildResult result;
        if (useContentUpdate) {
            result = ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);
            if (result == null) {
                const string fallbackReason = "检测到上一轮内容状态文件，但当前设置不满足增量更新构建条件；已自动回退为全量 New Build。若要给旧版 Player 做热更新，请保持 Remote Catalog Load Path 与生成该 Player 时一致。";
                Debug.LogWarning(fallbackReason);
                result = BuildNewAddressablesContent();
                buildMode = "New Build";
            }
        } else {
            result = BuildNewAddressablesContent();
        }

        if (result == null) {
            EditorUtility.DisplayDialog("构建失败", $"Addressables {buildMode} 未返回构建结果。请检查 Addressables 控制台日志。", "关闭");
            return false;
        }
        if (!string.IsNullOrEmpty(result.Error)) {
            Debug.LogError(result.Error);
            EditorUtility.DisplayDialog("构建失败", result.Error, "关闭");
            return false;
        }

        AssetDatabase.Refresh();
        Debug.Log($"Addressables 构建完成：{buildMode}");
        EditorUtility.DisplayDialog("构建完成", $"Addressables {buildMode} 构建完成。", "关闭");
        return true;
    }

    /// <summary>
    /// 执行 Addressables 全量构建，并返回 Unity 构建结果。
    /// </summary>
    private static AddressablesPlayerBuildResult BuildNewAddressablesContent() {
        AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
        return result;
    }

    /// <summary>
    /// 将指定 Addressables 组的构建与加载路径切换为远程路径。
    /// </summary>
    private static void ApplyRemotePathToGroup(AddressableAssetSettings settings, string groupName, bool packTogetherByLabel = false) {
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (!group) {
            group = settings.CreateGroup(groupName, false, false, false, null);
        }

        ContentUpdateGroupSchema contentSchema = group.GetSchema<ContentUpdateGroupSchema>();
        if (!contentSchema) {
            contentSchema = group.AddSchema<ContentUpdateGroupSchema>();
        }
        contentSchema.StaticContent = false;

        BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
        if (!schema) {
            schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
        }
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
        schema.BundleMode = packTogetherByLabel ? BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel : BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        EditorUtility.SetDirty(contentSchema);
        EditorUtility.SetDirty(schema);
        EditorUtility.SetDirty(group);
    }

    /// <summary>
    /// 读取本机热更上传配置。
    /// </summary>
    private static HotUpdateUploadConfig LoadUploadConfig() {
        string configPath = GetUploadConfigPath();
        if (!File.Exists(configPath)) {
            EditorUtility.DisplayDialog("配置不存在", $"请先复制并填写本地配置：\n{configPath}", "关闭");
            return null;
        }

        try {
            return JsonUtility.FromJson<HotUpdateUploadConfig>(File.ReadAllText(configPath));
        }
        catch (Exception ex) {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("配置读取失败", ex.Message, "关闭");
            return null;
        }
    }

    /// <summary>
    /// 创建本机热更上传配置；不存在时从模板复制。
    /// </summary>
    private static void CreateUploadConfig() {
        string configPath = GetUploadConfigPath();
        if (File.Exists(configPath)) {
            return;
        }

        string examplePath = GetUploadExampleConfigPath();
        if (!File.Exists(examplePath)) {
            EditorUtility.DisplayDialog("创建失败", $"配置模板不存在：\n{examplePath}", "关闭");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? string.Empty);
        File.Copy(examplePath, configPath);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 打开本机热更上传配置。
    /// </summary>
    private static void OpenUploadConfig() {
        string configPath = GetUploadConfigPath();
        if (!UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(configPath, 1)) {
            EditorUtility.OpenWithDefaultApp(configPath);
        }
    }

    /// <summary>
    /// 删除本机热更上传配置。
    /// </summary>
    private static void DeleteUploadConfig() {
        string fullPath = GetUploadConfigPath();
        if (!File.Exists(fullPath)) {
            return;
        }

        File.Delete(fullPath);
        string metaPath = $"{fullPath}.meta";
        if (File.Exists(metaPath)) {
            File.Delete(metaPath);
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 获取本机热更上传配置路径。
    /// </summary>
    private static string GetUploadConfigPath() {
        return Path.Combine(Directory.GetCurrentDirectory(), GetUploadAssetConfigPath());
    }

    /// <summary>
    /// 获取本机热更的资源路径
    /// </summary>
    private static string GetUploadAssetConfigPath() {
        return "Tools/HotUpdate/AAUploadData.json";
    }

    /// <summary>
    /// 获取热更上传配置模板路径。
    /// </summary>
    private static string GetUploadExampleConfigPath() {
        return Path.Combine(Directory.GetCurrentDirectory(), "Tools/HotUpdate/upload-addressables.temp.json");
    }

    /// <summary>
    /// 获取热更上传脚本路径。
    /// </summary>
    private static string GetUploadScriptPath() {
        return Path.Combine(Directory.GetCurrentDirectory(), "Tools/HotUpdate/UploadAddressables.ps1");
    }

    /// <summary>
    /// 获取本地热更启动配置完整路径。
    /// </summary>
    private static string GetHotUpdateSettingsFullPath() {
        return Path.Combine(Directory.GetCurrentDirectory(), AAConst.AAHotUpdateSettingsPath);
    }

    /// <summary>
    /// 获取 Addressables 远程构建路径；配置为空时使用项目默认路径。
    /// </summary>
    private static string GetRemoteBuildPath(HotUpdateUploadConfig config) {
        return string.IsNullOrWhiteSpace(config.remoteBuildPath) ? DefaultRemoteBuildPath : ResolveConfigText(config, config.remoteBuildPath).Trim();
    }

    /// <summary>
    /// 根据公开访问根地址生成 Addressables 远程加载路径。
    /// </summary>
    private static string GetRemoteLoadPath(HotUpdateUploadConfig config) {
        if (string.IsNullOrWhiteSpace(config.publicBaseUrl)) {
            return string.Empty;
        }
        return $"{ResolveConfigText(config, config.publicBaseUrl).TrimEnd('/')}/[BuildTarget]";
    }

    /// <summary>
    /// 替换热更配置中的基础占位符。
    /// </summary>
    private static string ResolveConfigText(HotUpdateUploadConfig config, string value) {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace("{host}", config.host);
    }

    /// <summary>
    /// 获取指定平台的 Addressables 内容状态文件路径，用于判断是否可做增量更新构建。
    /// </summary>
    private static string GetContentStatePath(string platform) {
        string contentStatePlatform = GetContentStatePlatformFolder(platform);
        return Path.Combine(Directory.GetCurrentDirectory(), "Assets/AddressableAssetsData", contentStatePlatform, "addressables_content_state.bin");
    }

    /// <summary>
    /// 将上传平台名转换为 Addressables 保存状态文件时使用的平台目录名。
    /// </summary>
    private static string GetContentStatePlatformFolder(string platform) {
        switch (platform) {
            case "StandaloneWindows":
            case "StandaloneWindows64":
                return "Windows";
            case "StandaloneOSX":
                return "OSX";
            case "StandaloneLinux64":
                return "Linux";
            case "WSAPlayer":
                return "WindowsUniversal";
            default:
                return platform;
        }
    }

    [Serializable]
    private class HotUpdateSettingsData {
        public bool enableHotUpdate;
    }

    [Serializable]
    private class HotUpdateUploadConfig {
        public string host;
        public string publicBaseUrl;
        public string remoteBuildPath;
    }

    private const string DefaultRemoteBuildPath = "ServerData/[BuildTarget]";
    private const int UploadTimeoutMilliseconds = 120000;
}
