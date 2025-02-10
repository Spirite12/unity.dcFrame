using System;
using System.Collections.Generic;
using DCFrame;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using EventBase = DCFrame.EventBase;
using Toggle = UnityEngine.UIElements.Toggle;

public class RedTipGraphViewNode : Node {
    public RedTipGraphViewNode(bool isParent, RedTipBase redTipBase, Action<RedTipBase> action) {
        this.isParent = isParent;
        actionJumpClick = action;
        redTip = RedTipMgr.GetRedTip(redTipBase.name, redTipBase.id);
        title = redTipBase.name;
        expanded = true;
        RenderPortInfo();
        RenderBaseInfo();
        RenderBtnInfo();
        EventMgr.RemoveListener<string, bool, int>(EventBase.Frame.RedTipRefreshActive, RedTipRefreshActive);
        EventMgr.AddListener<string, bool, int>(EventBase.Frame.RedTipRefreshActive, RedTipRefreshActive);
        EventMgr.RemoveListener<string, bool>(EventBase.Frame.RedTipChildModify, RedTipChildModify);
        EventMgr.AddListener<string, bool>(EventBase.Frame.RedTipChildModify, RedTipChildModify);
    }

    private void RedTipRefreshActive(string name, bool isActive, int id) {
        if (name != redTip.name || id != redTip.id) {
            return;
        }
        ModifyRedTip();
    }

    private void RedTipChildModify(string name, bool isCreate) {
        if (name != redTip.name) {
            return;
        }
        ModifyRedTip();
    }

    private void ModifyRedTip() {
        foreach (var keyValue in elementList) {
            keyValue.Value.Remove(keyValue.Key);
        }
        elementList.Clear();
        RenderBaseInfo();
        RenderBtnInfo();
    }

    private void RenderBaseInfo() {
        TextField textFieldId = new TextField("Id:") {
            value = redTip.id.ToString(),
        };
        mainContainer.Add(textFieldId);
        elementList.Add(textFieldId, mainContainer);

        Toggle toggleActive = new Toggle("是否激活: ") {
            value = redTip.IsActive()
        };
        mainContainer.Add(toggleActive);
        elementList.Add(toggleActive, mainContainer);

        Toggle toggleOpen = new Toggle("是否开放: ") {
            value = redTip.GetIsOpen()
        };
        mainContainer.Add(toggleOpen);
        elementList.Add(toggleOpen, mainContainer);

        TextField textField = new TextField("子节点数量:") {
            value = redTip.GetChildNum().ToString()
        };
        mainContainer.Add(textField);
        elementList.Add(textField, mainContainer);
    }

    /// <summary>
    /// 获取创建的节点（目前只会有1出 or 1入）
    /// </summary>
    /// <returns></returns>
    public Port GetCreatePort() {
        if (isParent) {
            return outputContainer[0].Q<Port>();
        }else {
            return inputContainer[0].Q<Port>();
        }
    }

    private void RenderPortInfo() {
        var portIn = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Port));
        portIn.portName = "In";
        // portIn.portColor = Color.green;
        inputContainer.Add(portIn);
        elementTypeColor = portIn.portColor;
        var portOut = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Port));
        portOut.portName = "Out";
        // portOut.portColor = Color.yellow;
        outputContainer.Add(portOut);
        elementTypeColor = portOut.portColor;
    }

    private void RenderBtnInfo() {
        Button btnCopy = new Button(() => { UnityEngine.GUIUtility.systemCopyBuffer = redTip.name; }) { text = "复制" };
        titleContainer.Add(btnCopy);
        elementList.Add(btnCopy, titleContainer);

        if (isParent && redTip.parent == null || !isParent && redTip.GetChildNum() == 0) {
            return;
        }
        string btnName = isParent ? "返回上一级" : "跳转此红点";
        Button btn = new Button(() => {
            var goRedTip = isParent ? redTip.parent : redTip;
            actionJumpClick?.Invoke(goRedTip);
        }) { text = btnName };
        mainContainer.Add(btn);
        elementList.Add(btn, mainContainer);
    }

    private readonly bool isParent;
    private readonly RedTipBase redTip;
    private readonly Action<RedTipBase> actionJumpClick;
    private readonly Dictionary<BindableElement, VisualElement> elementList = new Dictionary<BindableElement, VisualElement>();
}
