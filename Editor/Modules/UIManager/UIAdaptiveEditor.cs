using DCFrame;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIAdaptive)), CanEditMultipleObjects]
public class UIAdaptiveEditor : UnityEditor.Editor {
    private UIAdaptive uiAdaptive;
    private void OnEnable() {
        uiAdaptive = target as UIAdaptive;
    }
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("安全区域生效")) {
            uiAdaptive.OnClickTakeEffect();
        }
        GUILayout.EndHorizontal();
    }
}