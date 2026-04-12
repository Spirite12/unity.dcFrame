namespace DCFrame {
    /// <summary>
    /// 简单状态机节点。
    /// </summary>
    public sealed class FSMNode {
        /// <summary>
        /// 构造一个状态节点。
        /// </summary>
        public FSMNode(int key, object customData = null) {
            IntKey = key;
            CustomData = customData;
        }

        /// <summary>
        /// 节点整型标识。
        /// </summary>
        public int IntKey { get; internal set; }

        /// <summary>
        /// 业务层自定义数据。
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// 当前节点绑定的状态接口实现。
        /// </summary>
        public IFSMState FSMState { get; private set; }

        /// <summary>
        /// 绑定节点状态实现。
        /// </summary>
        public void AddFSMState(IFSMState fsmState) {
            FSMState = fsmState;
        }

        /// <summary>
        /// 清理节点状态引用。
        /// </summary>
        public void Destroy() {
            FSMState = null;
            CustomData = null;
        }
    }
}
