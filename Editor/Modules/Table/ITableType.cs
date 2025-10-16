using DCFrame;

public interface  ITableType {
    /// <summary>
    /// 初始化表数据
    /// </summary>
    public abstract void Init(TableRules.TableRule tableRule);
    /// <summary>
    /// 销毁数据
    /// </summary>
    public abstract void Destroy();
    /// <summary>
    /// 创建表GUI数据
    /// </summary>
    public abstract void OnInspectorGUI();
    /// <summary>
    /// 分析并创建脚本代码
    /// </summary>
    public abstract void AnalyzeAndCreateScripts();
}
