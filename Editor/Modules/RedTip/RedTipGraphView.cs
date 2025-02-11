using System.Collections.Generic;
using DCFrame;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class RedTipGraphView : GraphView {
    public RedTipGraphView() {
        redTip = RedTipMgr.redTipMain;
        InitGraphViewInfo();
        CreateNodeInfo();
        AddEvent();
    }

    /// 初始化信息
    private void InitGraphViewInfo() {
        // 允许对Graph进行Zoom in/out（效果是：可放大放小视野）
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        // 允许拖拽Content（效果是：可上下左右拖界面）
        this.AddManipulator(new ContentDragger());
        // 允许选择当前节点的内容（效果是：可选物体）
        this.AddManipulator(new SelectionDragger());
        // GraphView允许进行框选（效果是：多选物体）
        this.AddManipulator(new RectangleSelector());
    }

    /// 创建红点树节点(只显示当前节点和所有子节点)
    private void CreateNodeInfo() {
        if (redTip == null) {
            return;
        }
        var redTopX = 80;
        var redTopY = 80;
        var redTipWidth = 220;
        var redTipHeight = 140;
        var redTipPaddingH = 80;
        var redTipPaddingV = 60;

        RedTipGraphViewNode node = new RedTipGraphViewNode(true, redTip, OnClickNodeJump);
        node.SetPosition(new Rect(redTopX, redTopY, redTipWidth, redTipHeight));
        AddElement(node);
        nodeList.Add(node);
        var portOut = node.GetCreatePort();
        // 显示位置
        int addCount = 0;
        foreach (var keyValue in redTip.GetChildDic()) {
            if (isOnlyActive && !keyValue.Value.IsActive() || isOnlyOpen && !keyValue.Value.GetIsOpen()) {
                continue;
            }
            RedTipGraphViewNode nodeTp = new RedTipGraphViewNode(false, keyValue.Value, OnClickNodeJump);
            var xValue = redTopX + addCount * redTipWidth + (addCount - 1 <= 0 ? 0 : addCount - 1) * redTipPaddingH;
            var yValue = redTopY + redTipHeight + redTipPaddingV;
            nodeTp.SetPosition(new Rect(xValue, yValue, redTipWidth, redTipHeight));
            AddElement(nodeTp);
            nodeList.Add(nodeTp);
            var portIn = nodeTp.GetCreatePort();
            AddEdgeByPorts(portOut, portIn);
            addCount += 1;
        }
    }

    private void OnClickNodeJump(RedTipBase redTip) {
        this.redTip = redTip;
        foreach (var node in nodeList) {
            RemoveElement(node);
        }
        nodeList.Clear();
        foreach (var edge in edgeList) {
            RemoveElement(edge);
        }
        edgeList.Clear();
        CreateNodeInfo();
    }
    
    /// 两个端口的连接
    private void AddEdgeByPorts(Port outputPort, Port inputPort) {
        Edge tempEdge = new Edge() {
            output = outputPort,
            input = inputPort
        };
        tempEdge.input.Connect(tempEdge);
        tempEdge.output.Connect(tempEdge);
        Add(tempEdge);
        edgeList.Add(tempEdge);
    }

    #region Event

    private void AddEvent() {
        RedTipUIToolkit.OnEventModifyActive += OnEventModifyActive;
        RedTipUIToolkit.OnEventModifyOpen += OnEventModifyOpen;
        RedTipUIToolkit.OnEventSearchText += OnEventSearchText;
    }

    private void OnEventSearchText(string redTipName, int redTipId) {
        var redTipBase = RedTipMgr.GetRedTip(redTipName, redTipId);
        if (redTipBase == null) {
            EditorUtility.DisplayDialog("Error info", "当前输入的红点无法查询到\nName ：" + redTipName + "\nID ：" + redTipId, "关闭");
            return;
        }
        OnClickNodeJump(redTipBase);
    }

    private void OnEventModifyOpen(bool isOpen) {
        isOnlyOpen = isOpen;
        OnClickNodeJump(redTip);
    }

    private void OnEventModifyActive(bool isActive) {
        isOnlyActive = isActive;
        OnClickNodeJump(redTip);
    }

    #endregion

    private bool isOnlyActive = false;
    private bool isOnlyOpen = false;
    private RedTipBase redTip;
    private readonly List<Node> nodeList = new List<Node>();
    private readonly List<Edge> edgeList = new List<Edge>();
}
