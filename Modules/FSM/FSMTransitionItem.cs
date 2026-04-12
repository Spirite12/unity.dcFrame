namespace DCFrame {
    /// <summary>
    /// 单个过渡队列元素。
    /// </summary>
    public sealed class FSMTransitionItem {
        /// <summary>
        /// 构造一个过渡项。
        /// </summary>
        public FSMTransitionItem(int state, float duration, bool? conditionFlag = null) {
            State = state;
            Duration = duration;
            ConditionFlag = conditionFlag;
        }

        /// <summary>
        /// 目标状态。
        /// </summary>
        public int State { get; private set; }

        /// <summary>
        /// 过渡持续时间，负数表示需要等待外部条件。
        /// </summary>
        public float Duration { get; private set; }

        /// <summary>
        /// 条件控制标记。
        /// </summary>
        public bool? ConditionFlag { get; set; }
    }
}
