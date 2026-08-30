using System;
using System.IO;
using DCFrame;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity 命令行任务的项目侧适配器。
/// </summary>
public static class CodexBatchVerify {
    /// <summary>
    /// 执行全量导表。
    /// </summary>
    public static void RunTablePackageConfig() {
        Execute("导表", () => {
            EnsureTableRules();
            TableEditor.PackageConfig();
        });
    }

    /// <summary>
    /// 执行资源本地化表生成。
    /// </summary>
    public static void RunLocalizeCreateAsset() {
        Execute("资源本地化生成", () => {
            EnsureLocalizeRoot();
            LocalizeEditor.CreateLocalizeAsset();
        });
    }

    /// <summary>
    /// 按本地化工具顺序执行导表与资源本地化生成。
    /// </summary>
    public static void RunInitLocalize() {
        Execute("本地化初始化", () => {
            EnsureTableRules();
            TableEditor.PackageConfig();
            if (!Directory.Exists(LocalizeConst.LocalizeTableRootPath)) {
                Debug.Log("未生成本地化资源目录，已完成导表步骤并跳过资源本地化生成。");
                return;
            }
            LocalizeEditor.CreateLocalizeAsset();
        });
    }

    /// <summary>
    /// 校验导表规则资产存在。
    /// </summary>
    private static void EnsureTableRules() {
        string tableRulesPath = Asset.GetAssetPath("Table/TableRules", Asset.PrefixPath.Settings);
        if (AssetDatabase.LoadAssetAtPath<TableRules>(tableRulesPath) == null) {
            throw new InvalidOperationException($"未找到导表规则：{tableRulesPath}");
        }
    }

    /// <summary>
    /// 校验资源本地化根目录存在。
    /// </summary>
    private static void EnsureLocalizeRoot() {
        if (!Directory.Exists(LocalizeConst.LocalizeTableRootPath)) {
            throw new DirectoryNotFoundException($"本地化资源目录不存在：{LocalizeConst.LocalizeTableRootPath}");
        }
    }

    /// <summary>
    /// 统一输出批处理结果，并将异常交给 Unity 返回失败退出码。
    /// </summary>
    private static void Execute(string taskName, Action action) {
        try {
            action();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"{taskName}完成。");
        }
        catch (Exception exception) {
            Debug.LogException(exception);
            throw;
        }
    }
}
