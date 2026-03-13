using DCFrame;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalizeRules))]
public class LocalizeRulesEditor : Editor {
    private void OnEnable() {
        localizeRules = AssetDatabase.LoadAssetAtPath<LocalizeRules>(Asset.GetAssetPath("Localize/LocalizeRules", Asset.EnumPrefixPath.Settings));
    }
    
    public override void OnInspectorGUI() {
        if (!localizeRules) {
            return;
        }
        RenderBtnTips();
        RenderToolkitInfo();
    }

    /// <summary>
    /// 渲染工具内容
    /// </summary>
    private void RenderToolkitInfo() {
        GUILayout.Space(5);
        if (GUILayout.Button("本地化表生成")) {
            TableEditor.PackageConfig("");
        }
        GUILayout.Space(10);
        if (GUILayout.Button("本地化资源生成")) {
            LocalizeEditor.CreateLocalizeAsset();
        }
    }
    
    /// <summary>
    /// 渲染提示说明
    /// </summary>
    private void RenderBtnTips() {
        if (GUILayout.Button("提示说明")) {
            string str = "";
            str += "本地化表生成：与一键导表的功能一致\n\n";
            str += "本地化资源生成：会根据项目工程内：Game->Localize文件夹下，查找非名为 Text 的文件夹，并生成对应的本地化资源\n\n";
            str += "举例：Prefab 文件夹下：1.Table文件夹：存放localization资源；2.各个本地化的文件夹（如：Zh-CN）：存放对应语言的本地化资源";
            EditorUtility.DisplayDialog("说明介绍", str, "关闭");
        }
        GUILayout.Space(5);
    }
    
    private LocalizeRules localizeRules;
}
