using System;
using System.Collections.Generic;
using UnityEngine;

namespace DCFrame {
    /// <summary>
    /// 简单状态机，支持状态跳转以及节点销毁。
    /// </summary>
    public sealed class FSM : IDisposable {
        /// <summary>
        /// 当前状态节点。
        /// </summary>
        public FSMNode CurrentNode { get; private set; }

        /// <summary>
        /// 是否已经停止工作。
        /// </summary>
        public bool Halted { get; private set; }

        /// <summary>
        /// 注册一个状态节点。
        /// </summary>
        public void AddStateNode(int intKey, FSMNode node) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            node.IntKey = intKey;
            stateMap[intKey] = node;
        }

        /// <summary>
        /// 获取指定状态节点。
        /// </summary>
        public FSMNode GetStateNode(int intKey) {
            FSMNode node;
            return stateMap.TryGetValue(intKey, out node) ? node : null;
        }

        /// <summary>
        /// 切换到指定状态。
        /// </summary>
        public void ChangeState(int intKey, bool noCheck = false) {
            TryChangeState(intKey, noCheck);
        }

        /// <summary>
        /// 尝试切换到指定状态，切换成功时返回 true。
        /// </summary>
        public bool TryChangeState(int intKey, bool noCheck = false) {
            if (Halted) {
                return false;
            }

            if (isChangingState) {
                Debug.LogWarning("FSM.ChangeState 不支持在 OnEnter/OnOut 中重入调用。");
                return false;
            }

            FSMNode node;
            if (!stateMap.TryGetValue(intKey, out node)) {
                return false;
            }

            if (!noCheck && !CanJump(intKey)) {
                return false;
            }

            isChangingState = true;
            try {
                if (CurrentNode != null && CurrentNode.FSMState != null) {
                    CurrentNode.FSMState.OnOut(CurrentNode);
                }

                CurrentNode = node;
                if (node.FSMState != null) {
                    node.FSMState.OnEnter(node);
                }

                return true;
            } finally {
                isChangingState = false;
            }
        }

        /// <summary>
        /// 判断当前状态是否允许跳转到目标状态。
        /// </summary>
        public bool CanJump(int intKey) {
            FSMNode node;
            if (!stateMap.TryGetValue(intKey, out node)) {
                return false;
            }

            if (CurrentNode == null || CurrentNode.FSMState == null) {
                return true;
            }

            return CurrentNode.FSMState.ValidJump(node, CurrentNode);
        }

        /// <summary>
        /// 获取当前状态节点。
        /// </summary>
        public FSMNode GetCurrentState() {
            return CurrentNode;
        }

        /// <summary>
        /// 清理全部状态节点。
        /// </summary>
        public void DestroyNode() {
            foreach (KeyValuePair<int, FSMNode> pair in stateMap) {
                if (pair.Value != null) {
                    pair.Value.Destroy();
                }
            }

            stateMap.Clear();
            CurrentNode = null;
        }

        /// <summary>
        /// 销毁整个状态机。
        /// </summary>
        public void Destroy() {
            Halted = true;
            DestroyNode();
        }

        /// <summary>
        /// 实现 IDisposable，方便统一释放。
        /// </summary>
        public void Dispose() {
            Destroy();
        }

        private readonly Dictionary<int, FSMNode> stateMap = new Dictionary<int, FSMNode>();
        private bool isChangingState;
    }
}
