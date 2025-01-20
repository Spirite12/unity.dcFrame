using System;
using System.Collections.Generic;
using System.IO;
using DCFrame;
using DCFrame.Utility;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Networking;

[CustomEditor(typeof(UIMono),true)]
public class UIMonoEditor : Editor {

    private const string StrAutoRefStart = "#region AutoRef Area (Don't Delete This Explain)";
    private const string StrAutoRefEnd = "#endregion";

    public void Awake() {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path.Length > 0) {
            EditorPrefs.SetString(EditorPrefabPath, path);
        }
        curLockTime = UILockData.Instance.GetLockTime(Selection.activeGameObject.name);
        EditorApplication.update += Update;
        UIMono.onClickMonoFile += FocusMonoFile;
        UIMono.onClickCtrlFile += FocusCtrlFile;
        UIMono.onClickBtnMethods += OnClickBtnMethods;
    }
    public void OnDestroy() {
        EditorApplication.update = null;
        UIMono.onClickMonoFile = null;
        UIMono.onClickCtrlFile = null;
        UIMono.onClickBtnMethods = null;
    }

    public void Update() {
        if (webReq == null && updateAddCount % frameCount == 0 && TimeUtil.GetCurTimestamp() - curLockTime >= refreshTime) {
            webReq = UILockData.Instance.GetReqNetPath(Selection.activeGameObject.name);
        }
        if (webReq != null && (!string.IsNullOrEmpty(webReq.error) || webReq.isDone)) {
            try {
                UILockData.Instance.AnalysisWebInfo(webReq.downloadHandler.text);
                webReq.Dispose();
                webReq = null;
            } catch (Exception) {
                Debug.Log("同步预制件信息出错，可能你开启了全局翻墙或不能连接到内网！");
            }
        }
        updateAddCount += 1;
    }

    public override void OnInspectorGUI() {
        LockName();
        LockBtnInfo();
        AutoRefMatchGo();
        base.OnInspectorGUI();
    }

    /// <summary>
    /// 定位mono脚本的位置
    /// </summary>
    public static void FocusMonoFile() {
        var asset = AssetDatabase.LoadMainAssetAtPath(GetMonoPath());
        EditorGUIUtility.PingObject(asset);
    }

    /// <summary>
    /// 定位Ctrl脚本的位置
    /// </summary>
    public static void FocusCtrlFile() {
        var asset = AssetDatabase.LoadMainAssetAtPath(GetCtrlPath());
        EditorGUIUtility.PingObject(asset);
    }

    #region Lock Methods

    private void LockName() {
        var strLockName = UILockData.Instance.GetLockName(Selection.activeGameObject.name);
        if (string.IsNullOrEmpty(strLockName)) {
            return;
        }

        GUILayout.Space(2);
        GUIStyle lockStyle = new GUIStyle {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = strLockName == SystemInfo.deviceName ? Color.green : Color.red }
        };
        GUILayout.Label($"上锁中: {strLockName}", lockStyle);
    }

    private void LockBtnInfo() {
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("上锁")) {
            UILockData.Instance.SetLock(Selection.activeGameObject.name);
        }
        if (GUILayout.Button("解锁")) {
            UILockData.Instance.SetUnLock(Selection.activeGameObject.name);
        }
        GUILayout.EndHorizontal();
    }

    #endregion

    #region Auto Ref

    /// <summary>
    /// 自动引用物体
    /// </summary>
    private void AutoRefMatchGo() {
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("自动引用")) {
            OnClickAutoRef();
        }
        if (GUILayout.Button("删除引用")) {
            OnClickDeleteAutoRef();
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 添加按钮函数
    /// </summary>
    public static void OnClickBtnMethods() {
        var path = GetCtrlPath();
        var ctrlContent = File.ReadAllText(path);

        // 处理按钮
        var rectBtnList = GetCurMonoRefList("UnityEngine.UI.Button");
        var outPutMethods = "";
        var outPutListener = "";
        foreach (var btn in rectBtnList) {
            //判断是否添加 Button.onClick
            var strAddListener = String.Format($"uiRef.{btn}.onClick.AddListener");
            if (ctrlContent.Contains(strAddListener)) {
                continue;
            }
            // 判断是否有 AddListener 函数，没有则写在UiRef 变量之前
            if (!ctrlContent.Contains("AddListener()")) {
                var str = string.Format($"private {Selection.activeGameObject.name}Mono uiRef;");
                var strInsert = "private void AddListener() {\r\n\t}\r\n\t";
                ctrlContent = ctrlContent.Replace(str, strInsert + str);
            }
            // 拼接需要写的字符串。固定是btn三个字符了,因此这么写
            var strEndBtn = btn.Remove(0, 3);
            var methodName = string.Format($"OnClick{strEndBtn}");
            var strBtn = String.Format(strAddListener + $"({methodName});");
            // 分情况处理，并添加 button 对象的onClick函数
            var strMethod1 = "private void AddListener() {";
            var strMethod2 = "private void AddListener(){";
            var strMethod = "";
            if (ctrlContent.Contains(strMethod1)) {
                strMethod = strMethod1;
            } else if (ctrlContent.Contains(strMethod2)) {
                strMethod = strMethod2;
            }
            ctrlContent = ctrlContent.Replace(strMethod, strMethod + "\r\n\t\t" + strBtn);
            //判断有没有OnClick这个函数
            var addStrMethod = string.Format($"private void {methodName}()");
            if (!ctrlContent.Contains(addStrMethod)) {
                ctrlContent = ctrlContent.Replace(strMethod, addStrMethod + "{\r\n\t}\r\n\t\r\n\t" + strMethod);
                outPutMethods = outPutMethods + methodName + " ";
            }
            outPutListener = outPutListener + methodName + " ";
        }
        
        // 处理 Item
        var rectItemList = GetCurMonoRefList("UIMgr.UICreateItem");
        foreach (var item in rectItemList) {
            // 判断是否添加实例化变量
            var itemName = GetGoItemName(item);
            var strVariable = GetItemClassFormat(itemName, item);
            if (!ctrlContent.Contains(strVariable)) {
                // 分情况处理，并添加 item 对象的Release函数
                var strMethod = string.Format($"private {Selection.activeGameObject.name}Mono uiRef;");
                ctrlContent = ctrlContent.Replace(strMethod, strMethod + "\r\n\t" + strVariable);
            }

            // 判断是否添加 OnRelease 函数
            var strRelease = GetItemOnReleaseFormat(item);
            if (!ctrlContent.Contains(strRelease)) {
                // 分情况处理，并添加 item 对象的Release函数
                var strMethod1 = "void OnRelease() {";
                var strMethod2 = "void OnRelease(){";
                var strMethod = "";
                if (ctrlContent.Contains(strMethod1)) {
                    strMethod = strMethod1;
                } else if (ctrlContent.Contains(strMethod2)) {
                    strMethod = strMethod2;
                }
                ctrlContent = ctrlContent.Replace(strMethod, strMethod + "\r\n\t\t" + strRelease);
            }

            // 判断是否添加 OnHide 函数
            var strHide = GetItemOnHideFormat(item);
            if (!ctrlContent.Contains(strHide)) {
                // 分情况处理，并添加 item 对象的Release函数
                var strMethod1 = "void OnHide() {";
                var strMethod2 = "void OnHide(){";
                var strMethod = "";
                if (ctrlContent.Contains(strMethod1)) {
                    strMethod = strMethod1;
                } else if (ctrlContent.Contains(strMethod2)) {
                    strMethod = strMethod2;
                }
                ctrlContent = ctrlContent.Replace(strMethod, strMethod + "\r\n\t\t" + strHide);
            }
        }

        File.WriteAllText(path, ctrlContent);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 自动引用
    /// </summary>
    public static void OnClickAutoRef() {
        var findMatchDic = GetMatchDic();
        if (findMatchDic.Count == 0) {
            return;
        }

        string path = GetMonoPath();
        if (path == string.Empty) {
            return;
        }
        string fileContent = File.ReadAllText(path);
        if (fileContent.Contains(StrAutoRefStart)) {
            fileContent = StringUtil.StringMidException(fileContent, StrAutoRefStart, StrAutoRefEnd, false);
        } else {
            var endIdx = fileContent.LastIndexOf('}');
            fileContent = fileContent.Insert(endIdx, "\r\n\t" + StrAutoRefStart + StrAutoRefEnd + "\r\n");
        }

        // 添加数据
        string strRef = StrAutoRefStart;
        strRef += "\r\n\t";
        foreach (var item in findMatchDic) {
            strRef += ("\r\n\tpublic " + item.Value + " " + item.Key.name + ";");
        }
        strRef += "\r\n\t\r\n\t";
        fileContent = fileContent.Replace(StrAutoRefStart, strRef);
        File.WriteAllText(path, fileContent);
        AssetDatabase.Refresh();
        EditorPrefs.SetString(EditorMonoRef, EditorMonoRef);
    }

    /// <summary>
    /// 删除引用
    /// </summary>
    private void OnClickDeleteAutoRef() {
        string ctrlPath = GetCtrlPath();
        if (ctrlPath != string.Empty) {
            // 按钮的处理
            var btnList = GetCurMonoRefList("UnityEngine.UI.Button");
            string fileContent = File.ReadAllText(ctrlPath);
            foreach (var strBtn in btnList) {
                var strEndBtn = strBtn.Remove(0, 3);
                var strAddListener = String.Format($"uiRef.{strBtn}.onClick.AddListener(OnClick{ strEndBtn});");
                fileContent = fileContent.Replace(strAddListener + "\r\n\t", "");
                fileContent = fileContent.Replace(strAddListener, "");
            }

            // 处理 Item
            var rectItemList = GetCurMonoRefList("UIMgr.UICreateItem");
            foreach (var strItem in rectItemList) {
                // 处理 Release 函数
                var strRelease = GetItemOnReleaseFormat(strItem);
                fileContent = fileContent.Replace("\r\n\t\t" + strRelease, "");
                // 处理 Release 函数
                var strHide = GetItemOnHideFormat(strItem);
                fileContent = fileContent.Replace("\r\n\t\t" + strHide, "");
                // 处理变量代码
                var itemName = GetGoItemName(strItem);
                var strVariable = GetItemClassFormat(itemName, strItem);
                fileContent = fileContent.Replace("\r\n\t" + strVariable, "");
            }

            File.WriteAllText(ctrlPath, fileContent);
            AssetDatabase.Refresh();
        }

        string monoPath = GetMonoPath();
        if (monoPath != string.Empty) {
            string fileContent = File.ReadAllText(monoPath);
            if (fileContent.Contains(StrAutoRefStart)) {
                fileContent = StringUtil.StringMidException(fileContent, "\r\n\r\n\t" + StrAutoRefStart, StrAutoRefEnd);
                File.WriteAllText(monoPath, fileContent);
                AssetDatabase.Refresh();
            }
        }
    }

    /// <summary>
    /// 设置实例赋值
    /// </summary>
    [DidReloadScripts]
    [Obsolete("Obsolete")]
    private static void DidReloadScriptsRef() {
        // 处理Ctrl脚本和Mono引用
        var strEditorMono = EditorPrefs.GetString(EditorMonoRef);
        var isNullMono = string.IsNullOrEmpty(strEditorMono);
        if (!isNullMono) {
            EditorPrefs.DeleteKey(EditorMonoRef);
            EditorPrefs.SetBool(EditorMonoIsFin, true);
            SetMonoRefInstance();
            OnClickBtnMethods();
        }
        // 处理保存后接着指定物体
        var strEditorPath = EditorPrefs.GetString(EditorPrefabPath);
        var isNullPath = string.IsNullOrEmpty(strEditorPath);
        var editorMonoIsFin = EditorPrefs.GetBool(EditorMonoIsFin);
        if (editorMonoIsFin && !isNullPath) {
            EditorPrefs.DeleteKey(EditorMonoIsFin);
            var go = PrefabUtility.LoadPrefabContents(strEditorPath);
            var goMono = go.GetComponentInChildren<UIMono>();
            Selection.activeGameObject = goMono.gameObject;
        }
    }

    /// <summary>
    /// 设置引用更新
    /// </summary>
    [Obsolete("Obsolete")]
    private static void SetMonoRefInstance() {
        var findMatchDic = GetMatchDic();
        if (findMatchDic.Count == 0) {
            return;
        }

        bool isSave = false;
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        var filedArray = curMono.GetType().GetFields();
        foreach (var data in findMatchDic) {
            for (int i = filedArray.Length - 1; i >= 0; i--) {
                if (data.Key && data.Key.name == filedArray[i].Name) {
                    if (data.Value == "GameObject") {
                        filedArray[i].SetValue(curMono, data.Key.gameObject);
                        isSave = true;
                    } else {
                        var component = data.Key.GetComponent(data.Value);
                        filedArray[i].SetValue(curMono, component);
                        isSave = true;
                    }
                    break;
                }
            }
        }

        if (!isSave || EditorApplication.isPlaying) {
            return;
        }

        var prefabInstance = PrefabUtility.GetCorrespondingObjectFromSource(curSelectGo);
        if (prefabInstance) {
            //不是预制体模式
            var path = AssetDatabase.GetAssetPath(prefabInstance);
            PrefabUtility.UnpackPrefabInstance(curSelectGo, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(curSelectGo, path,
                InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(curSelectGo, path);
        }else if(!UnityEditor.SceneManagement.EditorSceneManager.IsPreviewSceneObject(curSelectGo)) {
            // Project 模式下点击
            PrefabUtility.SavePrefabAsset(curSelectGo);
        } else {
            // 预制体模式下
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
                PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.prefabAssetPath);
            }
        }
    }

    /// <summary>
    /// 获取Mono文件的路径
    /// </summary>
    /// <returns></returns>
    private static string GetMonoPath() {
        //获取路径
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        string monoName = StringUtil.StringRemoveByStr(curMono.ToString(), "(", ")");
        string[] pathArray = AssetDatabase.FindAssets(monoName);
        if (pathArray.Length > 1) {
            Debug.LogError("有同名文件" + monoName + "获取路径失败");
            return string.Empty;
        } else if (pathArray.Length == 0) {
            Debug.LogError("找不到该脚本名字为：" + monoName);
            return string.Empty;
        }

        return AssetDatabase.GUIDToAssetPath(pathArray[0]);
    }

    /// <summary>
    /// 获取Ctrl文件的路径
    /// </summary>
    /// <returns></returns>
    private static string GetCtrlPath() {
        //获取路径
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        string monoName = StringUtil.StringRemoveByStr(curMono.ToString(), "(", ")");
        string ctrlName = monoName.Replace("Mono", "Ctrl");
        string[] pathArray = AssetDatabase.FindAssets(ctrlName);
        if (pathArray.Length > 1) {
            Debug.LogError("有同名文件" + ctrlName + "获取路径失败");
            return string.Empty;
        } else if (pathArray.Length == 0) {
            Debug.LogError("找不到该脚本名字为：" + ctrlName);
            return string.Empty;
        }

        return AssetDatabase.GUIDToAssetPath(pathArray[0]);
    }

    /// <summary>
    /// 获取当前脚本的与参数对应的List
    /// </summary>
    /// <param name="strSpace">命名空间</param>
    /// <returns></returns>
    private static List<string> GetCurMonoRefList(string strSpace) {
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        var filedArray = curMono.GetType().GetFields();
        if (filedArray.Length == 0) {
            Debug.LogWarning("该Mono脚本并没有字段");
            return new List<string>();
        }

        List<string> rectRefList = new List<string>();
        foreach (var data in filedArray) {
            if (data.FieldType.FullName == strSpace) {
                rectRefList.Add(data.Name);
            }
        }

        return rectRefList;
    }

    /// <summary>
    /// 获取匹配的数据
    /// </summary>
    /// <returns></returns>
    private static Dictionary<Transform, string> GetMatchDic() {
        //key : 物体，Value : 组件名
        Dictionary<Transform, string> findMatchDic = new Dictionary<Transform, string>();
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        var childArray = GetChildData(curMono.transform);
        var assetUiAutoRef = AssetDatabase.LoadAssetAtPath<UIAutoRef>(UIConst.UIAutoRefPath);
        for (int i = 0; i < childArray.Count; i++) {
            var childName = childArray[i].name;
            for (int j = 0; j < assetUiAutoRef.uIPrefixMatchList.Count; j++) {
                var strMatch = assetUiAutoRef.uIPrefixMatchList[j];
                if (childName.StartsWith(strMatch.prefix)) {
                    findMatchDic[childArray[i]] = strMatch.componentName;
                    break;
                }
            }
        }
        return findMatchDic;
    }

    /// <summary>
    /// 获取符合条件的子物体数据
    /// </summary>
    /// <returns></returns>
    private static List<Transform> GetChildData(Transform trans) {
        List<Transform> transformList = new List<Transform>();
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(trans);
        while (queue.Count > 0) {
            Transform current = queue.Dequeue();
            transformList.Add(current);
            for (int i = 0; i < current.childCount; i++) {
                var childGo = current.GetChild(i);
                if (childGo.GetComponent<UIMono>() == null) {
                    queue.Enqueue(childGo);
                }else {
                    transformList.Add(childGo);
                }
            }
        }
        return transformList;
    }

    #region 子预制体的通用函数

    /// <summary>
    /// 获取子预制体的名称
    /// </summary>
    private static string GetGoItemName(string item) {
        var curSelectGo = Selection.activeGameObject;
        var curMono = curSelectGo.GetComponent<UIMono>();
        var field = curMono.GetType().GetField(item);
        var value = field.GetValue(curMono);
        if (value == null) {
            Debug.LogWarning("变量没有赋值，导致没办法获取item脚本的命名");
            return "";
        }
        var goItem = field.FieldType.GetMethod("GetGoItemName");
        string itemName = (string)goItem?.Invoke(value, null);
        return itemName;
    }

    private static string GetItemClassFormat(string itemName, string item) {
        return string.Format($"private readonly {itemName}Ctrl {item}Ctrl = new {itemName}Ctrl();");
    }

    private static string GetItemOnReleaseFormat(string item) {
        return string.Format($"{item}Ctrl.OnRelease();");
    }

    private static string GetItemOnHideFormat(string item) {
        return string.Format($"{item}Ctrl.OnHide();");
    }

    #endregion

    #endregion

    /// <summary>
    /// X分钟刷新一次（秒为单位）
    /// </summary>
    private const float refreshTime = 60;
    /// <summary>
    /// 每几帧刷新一次
    /// </summary>
    private const int frameCount = 60;
    /// <summary>
    /// update执行次数
    /// </summary>
    private int updateAddCount = 0;
    /// <summary>
    /// key 和 Value 都是同一个
    /// </summary>
    private static string EditorMonoRef = "EditorMonoRef";
    private static string EditorMonoIsFin = "EditorMonoIsFin";
    private static string EditorPrefabPath = "";
    private long curLockTime = 0;
    private UnityWebRequest webReq;
}