using DCFrame;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIOrder),true)]
public class UIOrderEditor : Editor {

    private UIOrder uiOrder;

    private void OnEnable() {
        uiOrder = target as UIOrder;
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        GUILayout.BeginHorizontal();
        GUILayout.Label("order Value :");
        GUILayout.Label(uiOrder.GetOrderValue().ToString());
        GUILayout.EndHorizontal();
    }
}