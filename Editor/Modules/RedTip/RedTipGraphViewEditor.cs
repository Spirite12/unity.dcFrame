using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class RedTipGraphViewEditor : EditorWindow {
    [MenuItem("Tools/RedTipTool")]
    public static void Open() {
        if (!Application.isPlaying) {
            EditorUtility.DisplayDialog("Error info", "请在游戏运行后打开此工具", "关闭");
            return;
        }
        GetWindow<RedTipGraphViewEditor>("红点视图查看器");
    }
    
    void OnEnable() {
        graphView = new RedTipGraphView { name = "RedTipTool" };
        // 让graphView铺满整个Editor窗口
        graphView.StretchToParentSize();
        // 向当前窗口添加图形视图
        rootVisualElement.Add(graphView);
        // 添加小地图
        minMap = new MiniMap();
        graphView.Add(minMap);
        // 加载资源
        var assetVisual = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/DCFrame/Editor/Modules/RedTip/RedTipTemplate.uxml");
        // 创建新的子节点
        visualElement = assetVisual.CloneTree();
        // 向当前窗口添加图形视图
        rootVisualElement.Add(visualElement);
        RedTipUIToolkit.RenderEditorWindows(visualElement);
    }

    void OnDisable() {
        // 向当前窗口移除图形视图
        rootVisualElement.Remove(graphView);
        rootVisualElement.Remove(visualElement);
        isInitMinMap = false;
    }
    
    void Update() {
        if (!isInitMinMap && graphView.contentRect.width > 0) {
            // 为了设置小地图在右上角，需要等待一帧才能获取到宽度
            int width = 260;
            int height = 180;
            minMap.SetPosition(new Rect(graphView.contentRect.width - width, 0, width, height));
            isInitMinMap = true;
        }
    }

    private bool isInitMinMap = false;
    private MiniMap minMap;
    private GraphView graphView;
    private VisualElement visualElement;
}
