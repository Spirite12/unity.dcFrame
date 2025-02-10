namespace DCFrame {
    /// <summary>
    /// 框架事件 （此写法方便模块查找且防止当前脚本越写越多）
    /// </summary>
    public abstract class EventBase {
        public static EventFrame Frame => new();
    }
}

