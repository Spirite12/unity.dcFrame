using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DCFrame;

[InitializeOnLoad]
public class UIHierarchy {
    static UIHierarchy() {
        hierarchyItemCallback += DrawHierarchyIcon;
        EditorApplication.hierarchyWindowItemOnGUI =
            (EditorApplication.HierarchyWindowItemCallback) Delegate.Combine(
                EditorApplication.hierarchyWindowItemOnGUI, hierarchyItemCallback);
        assetUiAutoRef = AssetDatabase.LoadAssetAtPath<UIAutoRef>(UIConst.UIAutoRefPath);
    }

    /// <summary>
    /// 绘制Icon方法
    /// </summary>
    /// <param name="instanceId"></param>
    /// <param name="selectionRect"></param>
    private static void DrawHierarchyIcon(int instanceId, Rect selectionRect) {
        GameObject go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
        if (go == null) {
            return;
        }

        // 设置icon的位置与尺寸（Hierarchy窗口的左上角是起点）
        if (go.GetComponent<UIMono>() != null) {
            DrawIcon(selectionRect, UIConst.UIMonoIconPath);
            return;
        }

        // 绘制Item的前缀匹配内容
        for (int j = 0; j < assetUiAutoRef.uIPrefixMatchList.Count; j++) {
            if (go.name.StartsWith(assetUiAutoRef.uIPrefixMatchList[j].prefix)) {
                DrawIcon(selectionRect, UIConst.UIItemIconPath);
                return;
            }
        }
    }

    /// <summary>
    /// 绘制图片
    /// </summary>
    /// <param name="selectionRect"></param>
    /// <param name="strPath"></param>
    private static void DrawIcon(Rect selectionRect, string strPath) {
        Rect rect = new Rect(selectionRect.x + selectionRect.width - 16f, selectionRect.y, 16f, 16f);
        GUI.DrawTexture(rect, GetHierarchyIcon(strPath));
    }

    /// <summary>
    /// 保存图片的资源
    /// </summary>
    /// <param name="sign"></param>
    /// <returns></returns>
    private static Texture2D GetHierarchyIcon(string sign) {
        if (hierarchyIconList.ContainsKey(sign) == false) {
            hierarchyIconList.Add(sign, AssetDatabase.LoadAssetAtPath<Texture2D>(sign));
        }

        return hierarchyIconList[sign];
    }

    /// <summary>
    /// 面板的Icon显示
    /// </summary>
    private static readonly Dictionary<string, Texture2D> hierarchyIconList = new Dictionary<string, Texture2D>();

    private static readonly EditorApplication.HierarchyWindowItemCallback hierarchyItemCallback;
    private static readonly UIAutoRef assetUiAutoRef;
}