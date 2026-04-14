using DCFrame.UGUI;
using UnityEditor;

namespace DCFrame.Editor.UGUI.Components.ScrollList {
    /// <summary>
    /// ScrollListMono 自定义检视面板。
    /// </summary>
    [CustomEditor(typeof(ScrollListMono))]
    [CanEditMultipleObjects]
    public class ScrollListEditor : UnityEditor.Editor {
        private SerializedProperty goItemProperty;
        private SerializedProperty paddingProperty;
        private SerializedProperty spacingProperty;
        private SerializedProperty layoutTypeProperty;
        private SerializedProperty constraintProperty;
        private SerializedProperty constraintCountProperty;
        private SerializedProperty axisProperty;
        private SerializedProperty isTopToBottomProperty;

        /// <summary>
        /// 缓存序列化字段，供 Inspector 绘制时复用。
        /// </summary>
        private void OnEnable() {
            goItemProperty = serializedObject.FindProperty("goItem");
            paddingProperty = serializedObject.FindProperty("padding");
            spacingProperty = serializedObject.FindProperty("spacing");
            layoutTypeProperty = serializedObject.FindProperty("layoutType");
            constraintProperty = serializedObject.FindProperty("constraint");
            constraintCountProperty = serializedObject.FindProperty("constraintCount");
            axisProperty = serializedObject.FindProperty("axis");
            isTopToBottomProperty = serializedObject.FindProperty("isTopToBottom");
        }

        /// <summary>
        /// 按布局类型绘制字段，仅在网格布局下显示网格专属配置。
        /// </summary>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(goItemProperty);
            EditorGUILayout.PropertyField(paddingProperty);
            EditorGUILayout.PropertyField(spacingProperty);
            ScrollListMono.LayoutType layoutType = (ScrollListMono.LayoutType)layoutTypeProperty.enumValueIndex;
            EditorGUI.BeginChangeCheck();
            layoutType = (ScrollListMono.LayoutType)EditorGUILayout.EnumPopup("Layout Type", layoutType);
            if (EditorGUI.EndChangeCheck()) {
                layoutTypeProperty.enumValueIndex = (int)layoutType;
            }

            if (layoutType != ScrollListMono.LayoutType.Horizontal) {
                EditorGUILayout.PropertyField(isTopToBottomProperty);
            }

            if (layoutType == ScrollListMono.LayoutType.Grid) {
                EditorGUILayout.PropertyField(constraintProperty);
                var constraint = (UnityEngine.UI.GridLayoutGroup.Constraint)constraintProperty.enumValueIndex;
                if (constraint != UnityEngine.UI.GridLayoutGroup.Constraint.Flexible) {
                    EditorGUILayout.PropertyField(constraintCountProperty);
                }
                EditorGUILayout.PropertyField(axisProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
