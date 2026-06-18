using System.IO;
using System.Text;
using DCFrame;
using DCFrame.Editor;
using UnityEditor;
using UnityEngine;

public class UISystemInitEditor : EditorWindow {
    [MenuItem("Assets/工具箱/工具项/创建UI功能模板", false)]
    public static void OpenWindow() {
        Rect rect = new Rect(0, 0, 360, 210);
        UISystemInitEditor window = (UISystemInitEditor)GetWindowWithRect(typeof(UISystemInitEditor), rect, true, "创建UI功能模板");
        window.Show();
    }

    /// <summary>
    /// 绘制创建功能系统模板窗口。
    /// </summary>
    private void OnGUI() {
        InitTable();

        GUILayout.Space(10);
        systemName = EditorGUILayout.TextField("功能系统名称：", systemName);

        GUILayout.Space(8);
        createTable.Draw();

        GUILayout.Space(12);
        if (GUILayout.Button("确定")) {
            CreateFeatureSystemTemplate();
        }
    }

    /// <summary>
    /// 根据输入名称创建功能系统相关模板。
    /// </summary>
    private void CreateFeatureSystemTemplate() {
        string folderName = systemName.Trim();
        if (string.IsNullOrEmpty(folderName)) {
            EditorUtility.DisplayDialog("提示弹窗", "功能系统名称不能为空", "关闭");
            return;
        }

        if (HasInvalidFolderName(folderName)) {
            EditorUtility.DisplayDialog("提示弹窗", "功能系统名称只能包含 A-Z 或 a-z 字母", "关闭");
            return;
        }

        if (isCreatePrefabPath) {
            CreateFeatureSystemFolder(folderName, LocateTarget.PrefabPath);
        }

        if (isCreateCacheScript) {
            CreateCacheScript(folderName, LocateTarget.CacheScript);
        }

        if (isCreateEventScript) {
            CreateEventScript(folderName, LocateTarget.EventScript);
        }

        if (isCreateRedTipScript) {
            CreateRedTipScript(folderName, LocateTarget.RedTipScript);
        }

        AssetDatabase.Refresh();
        Close();
    }

    /// <summary>
    /// 根据输入名称在预制体目录下创建功能系统文件夹。
    /// </summary>
    private void CreateFeatureSystemFolder(string folderName, LocateTarget locateTarget) {
        if (!AssetDatabase.IsValidFolder(PrefabRootPath)) {
            return;
        }

        string folderPath = $"{PrefabRootPath}/{folderName}";
        if (AssetDatabase.IsValidFolder(folderPath)) {
            return;
        }

        AssetDatabase.CreateFolder(PrefabRootPath, folderName);
        LocateCreatedAsset(folderPath, locateTarget);
    }

    /// <summary>
    /// 创建功能系统缓存脚本。
    /// </summary>
    private void CreateCacheScript(string folderName, LocateTarget locateTarget) {
        if (!Directory.Exists(CacheScriptRootPath)) {
            return;
        }

        string filePath = $"{CacheScriptRootPath}/{folderName}Cache.cs";
        if (File.Exists(filePath)) {
            return;
        }

        string fileContent =
$@"using DCFrame;

namespace Game {{
    /// <summary>
    /// {folderName} 缓存数据。
    /// </summary>
    public class {folderName}Cache : CacheBaseSingleton<{folderName}Cache> {{
    }}
}}
";

        File.WriteAllText(filePath, fileContent, Encoding.UTF8);
        LocateCreatedAsset(filePath, locateTarget);
    }

    /// <summary>
    /// 创建功能系统事件脚本。
    /// </summary>
    private void CreateEventScript(string folderName, LocateTarget locateTarget) {
        if (!Directory.Exists(EventScriptRootPath)) {
            return;
        }

        string filePath = $"{EventScriptRootPath}/{folderName}Event.cs";
        if (File.Exists(filePath)) {
            return;
        }

        string fileContent =
$@"using DCFrame;

namespace Game {{
    /// <summary>
    /// {folderName} 事件声明。
    /// </summary>
    public class {folderName}Event : EventBase {{
    }}
}}
";

        File.WriteAllText(filePath, fileContent, Encoding.UTF8);
        LocateCreatedAsset(filePath, locateTarget);
    }

    /// <summary>
    /// 创建功能系统红点脚本。
    /// </summary>
    private void CreateRedTipScript(string folderName, LocateTarget locateTarget) {
        if (!Directory.Exists(RedTipScriptRootPath)) {
            return;
        }

        string filePath = $"{RedTipScriptRootPath}/{folderName}RedTip.cs";
        if (File.Exists(filePath)) {
            return;
        }

        string fileContent =
$@"using DCFrame;

namespace Game {{
    /// <summary>
    /// {folderName} 红点节点。
    /// </summary>
    public class {folderName}RedTip : RedTipBase {{
        public {folderName}RedTip(string name, RedTipBase parent) : base(name, parent) {{
        }}
    }}
}}
";

        File.WriteAllText(filePath, fileContent, Encoding.UTF8);
        LocateCreatedAsset(filePath, locateTarget);
    }

    /// <summary>
    /// 在 Project 面板中定位本次创建的资源。
    /// </summary>
    private void LocateCreatedAsset(string assetPath, LocateTarget locateTarget) {
        if (currentLocateTarget != locateTarget) {
            return;
        }

        EditorApplication.delayCall += () => {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (!asset) {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        };
    }

    /// <summary>
    /// 判断输入名称是否只由 A-Z 或 a-z 字母组成。
    /// </summary>
    private bool HasInvalidFolderName(string folderName) {
        for (int i = 0; i < folderName.Length; i++) {
            char ch = folderName[i];
            if ((ch < 'A' || ch > 'Z') && (ch < 'a' || ch > 'z')) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 规范化 Unity 资源文件夹路径，避免尾部分隔符影响 AssetDatabase 查询。
    /// </summary>
    private static string NormalizeAssetFolderPath(string assetFolderPath) {
        return assetFolderPath.TrimEnd('/', '\\');
    }

    /// <summary>
    /// 初始化创建选项表格。
    /// </summary>
    private void InitTable() {
        if (createTable != null) {
            return;
        }

        createTable = new EditorGUILayoutTable<LocateTarget, TableColumn>();
        createTable.AddColumn(TableColumn.Name, "", DrawTableCell, NameColumnWidth);
        createTable.AddColumn(TableColumn.Create, "创建", DrawTableCell, ToggleColumnWidth);
        createTable.AddColumn(TableColumn.Locate, "定位", DrawTableCell, ToggleColumnWidth);
        createTable.AddRow(LocateTarget.PrefabPath);
        createTable.AddRow(LocateTarget.CacheScript);
        createTable.AddRow(LocateTarget.EventScript);
        createTable.AddRow(LocateTarget.RedTipScript);
    }

    /// <summary>
    /// 绘制创建选项表格单元格。
    /// </summary>
    private void DrawTableCell(LocateTarget row, TableColumn column) {
        switch (column) {
            case TableColumn.Name:
                GUILayout.Label(GetTableRowLabel(row), GUILayout.Width(NameColumnWidth));
                break;
            case TableColumn.Create:
                SetCreateValue(row, EditorGUILayout.Toggle(GetCreateValue(row), GUILayout.Width(ToggleWidth)));
                break;
            case TableColumn.Locate:
                bool isLocateTarget = currentLocateTarget == row;
                EditorGUI.BeginDisabledGroup(!GetCreateValue(row));
                bool newLocateTarget = EditorGUILayout.Toggle(isLocateTarget, GUILayout.Width(ToggleWidth));
                EditorGUI.EndDisabledGroup();
                if (newLocateTarget != isLocateTarget) {
                    currentLocateTarget = newLocateTarget ? row : LocateTarget.None;
                    GUI.FocusControl(null);
                }
                break;
        }
    }

    /// <summary>
    /// 获取表格行显示文本。
    /// </summary>
    private string GetTableRowLabel(LocateTarget row) {
        switch (row) {
            case LocateTarget.PrefabPath:
                return "创建预制路径：";
            case LocateTarget.CacheScript:
                return "创建缓存脚本：";
            case LocateTarget.EventScript:
                return "创建事件脚本：";
            case LocateTarget.RedTipScript:
                return "创建红点脚本：";
            default:
                return string.Empty;
        }
    }

    /// <summary>
    /// 获取指定行的创建勾选状态。
    /// </summary>
    private bool GetCreateValue(LocateTarget row) {
        switch (row) {
            case LocateTarget.PrefabPath:
                return isCreatePrefabPath;
            case LocateTarget.CacheScript:
                return isCreateCacheScript;
            case LocateTarget.EventScript:
                return isCreateEventScript;
            case LocateTarget.RedTipScript:
                return isCreateRedTipScript;
            default:
                return false;
        }
    }

    /// <summary>
    /// 设置指定行的创建勾选状态。
    /// </summary>
    private void SetCreateValue(LocateTarget row, bool value) {
        switch (row) {
            case LocateTarget.PrefabPath:
                isCreatePrefabPath = value;
                break;
            case LocateTarget.CacheScript:
                isCreateCacheScript = value;
                break;
            case LocateTarget.EventScript:
                isCreateEventScript = value;
                break;
            case LocateTarget.RedTipScript:
                isCreateRedTipScript = value;
                break;
        }

        if (!value && currentLocateTarget == row) {
            currentLocateTarget = LocateTarget.None;
        }
    }
    
    private static readonly string ScriptRootPath = "Assets/Game/Scripts/";
    private static readonly string PrefabRootPath = NormalizeAssetFolderPath(UIConst.outPutPrefabHeadPath);
    private static readonly string CacheScriptRootPath = ScriptRootPath + "Cache";
    private static readonly string EventScriptRootPath = ScriptRootPath + "Event";
    private static readonly string RedTipScriptRootPath = ScriptRootPath + "RedTip";
    private const float NameColumnWidth = 150f;
    private const float ToggleColumnWidth = 40f;
    private const float ToggleWidth = 16f;

    private enum TableColumn {
        Name,
        Create,
        Locate,
    }

    private enum LocateTarget {
        None,
        PrefabPath,
        CacheScript,
        EventScript,
        RedTipScript,
    }

    private EditorGUILayoutTable<LocateTarget, TableColumn> createTable;
    private LocateTarget currentLocateTarget = LocateTarget.None;
    private bool isCreatePrefabPath = true;
    private bool isCreateCacheScript;
    private bool isCreateEventScript;
    private bool isCreateRedTipScript;
    private string systemName = string.Empty;
}
