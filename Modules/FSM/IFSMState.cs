namespace DCFrame {
    /// <summary>
    /// 状态节点接口，负责进入、离开以及合法跳转判断。
    /// </summary>
    public interface IFSMState {
        /// <summary>
        /// 进入状态时触发。
        /// </summary>
        void OnEnter(FSMNode node);

        /// <summary>
        /// 离开状态时触发。
        /// </summary>
        void OnOut(FSMNode node);

        /// <summary>
        /// 判断当前状态是否允许跳到目标状态。
        /// </summary>
        bool ValidJump(FSMNode targetNode, FSMNode currentNode);
    }
}
