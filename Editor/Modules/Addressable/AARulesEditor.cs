using DCFrame;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AARules))]
public class AARulesEditor : Editor {
    public override void OnInspectorGUI() {
        if (GUILayout.Button("提示说明")) {
            string str = "";
            str += "Number：第几级文件夹\n当前文件夹从0开始，子级文件夹递归+1\n\n";
            str += "SearchPattern:\n *.* 搜索全文件 、*.png 搜索 png文件\n其他搜索格式以 *. 开头即可\n\n";
            str += "Option:\nTopDirectoryOnly：只搜索当前目录\nAllDirectories：递归搜索所有子目录\n";
            EditorUtility.DisplayDialog("说明介绍", str, "关闭");
        }
        EditorGUILayout.LabelField("更改完下方数据后，请点击下方按钮");
        if (GUILayout.Button("数据更新")) {
            AddressableProcessor.InitData(true);
        }
        DrawDefaultInspector();
    }
}
