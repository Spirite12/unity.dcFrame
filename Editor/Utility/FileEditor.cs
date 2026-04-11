using System.IO;
using UnityEditor;
using UnityEngine;

public class FileEditor : Editor{
    [MenuItem("Assets/工具箱/工具项/获取完整路径", false)]
    public static void GetFullPath(){
        // 将选中资源的物理绝对路径（含扩展名）复制到剪贴板，便于直接粘贴使用
        Object activeObject = Selection.activeObject;
        if (activeObject == null)
        {
            Debug.LogWarning("未选中任何资源，无法获取路径。");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(activeObject);
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("选中的对象不在项目资源内，无法获取路径。");
            return;
        }

        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
        string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        string extension = Path.GetExtension(assetPath);
        if (!string.IsNullOrEmpty(extension) && !fullPath.EndsWith(extension)){
            fullPath += extension;
        }

        EditorGUIUtility.systemCopyBuffer = fullPath;
    }

    [MenuItem("Assets/工具箱/工具项/获取完整路径", true)]
    public static bool GetFullPathValidate(){
        return Selection.activeObject != null;
    }
}

