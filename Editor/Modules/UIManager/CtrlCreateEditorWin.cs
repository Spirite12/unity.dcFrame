using System;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Reflection;
using DCFrame;
using DCFrame.Utility;

public class CtrlCreateEditor : EditorWindow {
    public static string ConfigFilePath => Path.Combine(Environment.CurrentDirectory, @"Tools\ScriptTemplates\");
    public const string ConfigCtrlFileName = "CtrlClassTp.txt";
    public const string ConfigMonoFileName = "MonoClassTp.txt";
    public const string ConfigCtrlItemFileName = "CtrlItemClassTp.txt";
    public const string ConfigMonoItemFileName = "MonoItemClassTp.txt";

	[MenuItem("Assets/Create/Ctrl Script", false, 82)]
	public static void CreateCtrlWin() {
		selectedObject = Selection.activeObject as GameObject;

		Rect rect = new Rect(0,0,350,240);
		CtrlCreateEditor window = (CtrlCreateEditor)EditorWindow.GetWindowWithRect(typeof(CtrlCreateEditor), rect, true, "Create Ctrl File");
		window.Show();
		RefreshWinInfo();
	}

    [MenuItem("Assets/Create/Ctrl Script", true, 82)]
    public static bool CreateCtrlWinValidate() {
		var theSelection = Selection.activeObject;
        var currentPath = AssetDatabase.GetAssetPath(theSelection);
        return Path.GetExtension(currentPath) == ".prefab";
    }

	/// <summary>
	/// 绘制窗口
	/// </summary>
	private void OnGUI() {
		GUILayout.Space(10);
		selectedObject = EditorGUILayout.ObjectField("Select Prefab :", selectedObject, typeof(GameObject), false) as GameObject;

		GUILayout.Space(6);
		outPutMonoPath = EditorGUILayout.TextField("Mono FilePath :", outPutMonoPath);
		outPutMonoName = EditorGUILayout.TextField("Create FileName :", outPutMonoName);

		GUILayout.Space(10);
		outPutCtrlPath = EditorGUILayout.TextField("Ctrl FilePath :", outPutCtrlPath);
		outPutCtrlName = EditorGUILayout.TextField("Create FileName :", outPutCtrlName);

		GUILayout.Space(10);
        isPanel = EditorGUILayout.Toggle(new GUIContent("Is Panel:"), isPanel);

        if (isPanel) {
			GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("UIBase Type:", GUILayout.Width(150));
            uiBaseType = (UIBase.UIBaseType)EditorGUILayout.EnumPopup(uiBaseType);
            EditorGUILayout.EndHorizontal();
		}else {
            GUILayout.Space(5);
            isAddListenFunc = EditorGUILayout.Toggle(new GUIContent("Is Add ListenerFunc:"), isAddListenFunc);
		}

        GUILayout.Space(6);
        isFocus = EditorGUILayout.Toggle(new GUIContent("Is Focus Ctrl:", "To focus ctrl file after the script is created"), isFocus);

		GUILayout.Space(8);
		EditorGUILayout.BeginHorizontal();
		var isRefresh = GUILayout.Button("Refresh");
		if (isRefresh) {
			RefreshWinInfo();
		}

		var isClick = GUILayout.Button("Create Script");
		if (isClick) {
			OnClickCreate();
		}
		EditorGUILayout.EndHorizontal();
	}

	/// <summary>
	/// 执行刷新按钮
	/// </summary>
	private static void RefreshWinInfo() {
        outPutMonoPath = UIConst.outPutMonoHeadPath + getPrefabLoadPath();
		outPutMonoName = $"{selectedObject?.name}Mono";

        outPutCtrlPath = UIConst.outPutCtrlHeadPath + getPrefabLoadPath();
		outPutCtrlName = $"{selectedObject?.name}Ctrl";
	}

    /// <summary>
    /// 获取预制体加载路径(去除结尾预制体名和前面Asset路径)
    /// </summary>
    private static string getPrefabLoadPath() {
        var currentPath = AssetDatabase.GetAssetPath(selectedObject);
        var strPath = "/" + selectedObject.name + ".prefab";
        strPath = currentPath.Replace(strPath, "");
        strPath = strPath.Replace(UIConst.outPutPrefabHeadPath, "");
        return strPath;
    }

	/// <summary>
	/// 点击创建脚本
	/// </summary>
	private void OnClickCreate() {
		if (DealErrorOp()) {
			return;
		}
        string strAsset = "Assets\\";
        outPutCtrlPath = strAsset + outPutCtrlPath;
		outPutMonoPath = strAsset + outPutMonoPath;
		CtrlFileOperation();
		MonoFileOperation();

		if (isFocus) {
			var asset = AssetDatabase.LoadMainAssetAtPath(outPutCtrlPath);
			EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
		}

		AssetDatabase.Refresh();
		this.Close();
	}

	/// <summary>
	/// 对错误的操作进行处理
	/// </summary>
	private bool DealErrorOp() {
		if(selectedObject == null) {
			EditorUtility.DisplayDialog("Error info Win", "所选择的物体为空", "关闭");
			return true;
		} else if(outPutCtrlPath.Length == 0 || outPutMonoPath.Length == 0) {
			EditorUtility.DisplayDialog("Error info Win", "输出的文件夹路径为空，请重新设置", "关闭");
			return true;
		} else if(outPutCtrlName.Length == 0 || outPutMonoName.Length == 0) {
			EditorUtility.DisplayDialog("Error info Win", "输出的文件名为空，请重新设置", "关闭");
			return true;
		}

		return false;
	}

    /// <summary>
	/// 写入Mono脚本
	/// </summary>
	private void MonoFileOperation() {
		if(!Directory.Exists(outPutMonoPath)) {
			Directory.CreateDirectory(outPutMonoPath);
		}
		var filePath = outPutMonoPath + "\\" + outPutMonoName;
		if(File.Exists(filePath)) {
			return;
		}

        string strConfig = isPanel ? ConfigMonoFileName : ConfigMonoItemFileName;
		string fileContent = File.ReadAllText(ConfigFilePath + strConfig);
		fileContent = fileContent.Replace("#SCRIPTNAME#", selectedObject?.name);
        EditorPrefs.SetString(EditorCtrlCreate, selectedObject?.name);
        File.WriteAllText(filePath + ".cs", fileContent);
	}

	/// <summary>
	/// 创建Ctrl脚本并写入相关内容
	/// </summary>
	private void CtrlFileOperation() {
		if(!Directory.Exists(outPutCtrlPath)) {
			Directory.CreateDirectory(outPutCtrlPath);
		}
		var filePath = outPutCtrlPath + "\\" + outPutCtrlName;
		if (File.Exists(filePath)) {
			return;
		}

        string strConfig = isPanel ? ConfigCtrlFileName : ConfigCtrlItemFileName;
		// 脚本名字的处理
		string fileContent = File.ReadAllText(ConfigFilePath + strConfig);
		fileContent = fileContent.Replace("#SCRIPTNAME#", selectedObject?.name);

		// 对待 Panel 和 Item 的特殊处理
        if (isPanel) {
            fileContent = CtrlFileOpDetail(fileContent);
        }else {
			fileContent = CtrlItemFileOpDetail(fileContent);
		}
		
        File.WriteAllText(filePath + ".cs", fileContent);
	}

    private string CtrlFileOpDetail(string fileContent) {
        // 对待 AssetPath and AssetName 的处理
        var strPath = getPrefabLoadPath();
        fileContent = fileContent.Replace("#SCRIPTPATH#", strPath);

        // 对待 UIBaseType 的处理
        var strUIBaseTypeStart = "//--UIBaseType";
        var strUIBaseTypeEnd = "--//";
        if (uiBaseType == UIBase.UIBaseType.FullScene) {
            fileContent = StringUtil.StringMidException(fileContent, strUIBaseTypeStart, strUIBaseTypeEnd);
        } else {
            fileContent = fileContent.Replace("#UIBASETYPE#", uiBaseType.ToString());
            fileContent = fileContent.Replace(strUIBaseTypeStart, "");
            fileContent = fileContent.Replace(strUIBaseTypeEnd, "");
        }

        return fileContent;
    }

    private string CtrlItemFileOpDetail(string fileContent) {
        // 对待 ListenerFunc 的处理
        var strListenerFuncStart = "//--ListenerFunc";
        var strListenerFuncEnd = "--//";
        if (isAddListenFunc) {
            fileContent = fileContent.Replace(strListenerFuncStart + "\r\n", "");
            fileContent = fileContent.Replace(strListenerFuncEnd + "\r\n", "");
		} else {
			// 这里执行3遍，是因为模板数据里面有3次
            for (int i = 0; i < 3; i++) {
                string strStart = strListenerFuncStart + "\r\n";
                string strEnd = strListenerFuncEnd;
                if (i == 2) {
					// 最后一个特殊处理
					strEnd += "\r\n\r\n";
				}
				fileContent = StringUtil.StringMidException(fileContent, strStart, strEnd);
			}
		}

		return fileContent;
    }

	/// <summary>
	/// 对预制体添加相关的脚本
	/// </summary>
	[DidReloadScripts]
	private static void AddComponentToObj() {
		string monoName = EditorPrefs.GetString(EditorCtrlCreate,"");
		if (string.IsNullOrEmpty(monoName)) {
			return;
		}
		EditorPrefs.DeleteKey(EditorCtrlCreate);
        
		if (selectedObject && monoName != selectedObject.name) {
			return;
		}

        if (selectedObject == null) {
            selectedObject = Selection.activeObject as GameObject;
		}

        //对预制体进行绑定脚本操作
		var currentPath = AssetDatabase.GetAssetPath(selectedObject);
        var origin = AssetDatabase.LoadAssetAtPath(currentPath, typeof(GameObject)) as GameObject;
        if (origin != null) {
            bool isSave = false;

            var tmpAssembly = Assembly.Load(UIConst.ResAssemblyName);
            var type = tmpAssembly.GetType(monoName + "Mono");
            if (origin.GetComponent(type) == null) {
                origin.AddComponent(type);
                isSave = true;
            }

            tmpAssembly = Assembly.Load(UIConst.UIOrderAssemblyName);
            type = tmpAssembly.GetType(UIConst.UIOrder);
            if (origin.GetComponent(type) == null) {
                origin.AddComponent(type);
                isSave = true;
            }

            if (isSave) {
                PrefabUtility.SavePrefabAsset(origin);
                AssetDatabase.Refresh();
            }
        }
    }

	/// <summary>
	/// EditorPref 保存的 key ： 创建ctrl脚本，value是脚本名
	/// </summary>
	private static string EditorCtrlCreate = "EditorCtrlCreate";
    /// <summary>
	/// 所选择的物体
	/// </summary>
	private static GameObject selectedObject;
	/// <summary>
	/// 输出的Mono文件路径
	/// </summary>
	private static string outPutMonoPath;
	/// <summary>
	/// 输出的Mono文件名
	/// </summary>
	private static string outPutMonoName;
	/// <summary>
	/// 输出的Ctrl文件路径
	/// </summary>
	private static string outPutCtrlPath;
	/// <summary>
	/// 输出的ctrl文件名
	/// </summary>
	private static string outPutCtrlName;
	/// <summary>
	/// UIBase 界面类型
	/// </summary>
	private static UIBase.UIBaseType uiBaseType;
	/// <summary>
	/// 是否焦距到新建的文件
	/// </summary>
	private static bool isFocus = true;
	/// <summary>
	/// 是否是界面
	/// </summary>
    private static bool isPanel = true;
	/// <summary>
	/// 是否添加监听函数
	/// </summary>
    private static bool isAddListenFunc = false;
}
