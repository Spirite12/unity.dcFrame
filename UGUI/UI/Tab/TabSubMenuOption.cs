namespace DCFrame.UGUI {
    /// <summary>
    /// 单个二级子菜单项的数据结构。
    /// </summary>
    public sealed class TabSubMenuOption {
        /// <summary>
        /// 构造一个二级子菜单项。
        /// </summary>
        public TabSubMenuOption(string label, object customData = null) {
            Label = label;
            CustomData = customData;
        }

        /// <summary>
        /// 展示文案。
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// 业务侧自定义数据。
        /// </summary>
        public object CustomData { get; private set; }
    }
}
