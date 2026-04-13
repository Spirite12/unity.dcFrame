namespace DCFrame.UGUI {
    /// <summary>
    /// 拖拽控制器接口，供管理器统一持有不同泛型实例。
    /// </summary>
    public interface IDrag {
        /// <summary>
        /// 销毁当前控制器并释放拖拽实例。
        /// </summary>
        void Destroy();
    }
}

