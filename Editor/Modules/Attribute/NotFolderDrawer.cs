using System.IO;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(NotFolderAttribute))]
public class NotFolderDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);
        Object obj = property.objectReferenceValue;
        if (obj != null) {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path)) {
                Debug.LogError($"错误：{obj.name} 不能是 文件夹 类型");
                // 清空错误的赋值
                property.objectReferenceValue = null;
            }
        }
        property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(object), false);
        EditorGUI.EndProperty();
    }
}