using DCFrame;

public interface  ITableType {
    /// <summary>
    /// 初始化表数据
    /// </summary>
    public abstract bool Init(TableRules.TableRule tableRule);
    /// <summary>
    /// 销毁数据
    /// </summary>
    public abstract void Destroy();
    /// <summary>
    /// 创建表GUI数据
    /// </summary>
    public abstract void OnInspectorGUI();
    /// <summary>
    /// 统筹处理数据
    /// </summary>
    public abstract void OnDealWithData();
    /// <summary>
    /// 处理代码表数据
    /// </summary>
    public abstract void OnDealWithFile();
}
