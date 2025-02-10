using System;
using UnityEditor;
using UnityEngine.UIElements;

public class RedTipUIToolkit {

    /// <summary>
    /// 开放状态变化
    /// </summary>
    public static event Action<bool> OnEventModifyOpen;
    /// <summary>
    /// 激活状态变化
    /// </summary>
    public static event Action<bool> OnEventModifyActive;
    /// <summary>
    /// 点击搜索文本
    /// </summary>
    public static event Action<string, int> OnEventSearchText;

    /// <summary>
    /// 渲染窗口
    /// </summary>
    public static void RenderEditorWindows(VisualElement visualElementParam) {
        visualElement = visualElementParam;

        Toggle togOpen = visualElement.Q<Toggle>("TogOpen");
        togOpen.RegisterCallback<ChangeEvent<bool>>((eventParam) => {
            OnEventModifyOpen?.Invoke(eventParam.newValue);
        });

        Toggle togActive = visualElement.Q<Toggle>("TogActive");
        togActive.RegisterCallback<ChangeEvent<bool>>((eventParam) => {
            OnEventModifyActive?.Invoke(eventParam.newValue);
        });

        TextField txtField = visualElement.Q<TextField>("TxtField");
        TextField txtFieldId = visualElement.Q<TextField>("TxtFieldId");

        Button btnRedTip = visualElement.Q<Button>("BtnJump");
        btnRedTip.clickable = new Clickable(() => {
            if (txtField.value.Length <= 0) {
                EditorUtility.DisplayDialog("Error info", "请输入红点名称", "关闭");
                return;
            }
            if (int.TryParse(txtFieldId.value, out int num)) {
                OnEventSearchText?.Invoke(txtField.value, int.Parse(txtFieldId.value));
            } else {
                EditorUtility.DisplayDialog("Error info", "请在红点ID输入框内输入数字", "关闭");
            }
        });
    }


    private static VisualElement visualElement;
}
