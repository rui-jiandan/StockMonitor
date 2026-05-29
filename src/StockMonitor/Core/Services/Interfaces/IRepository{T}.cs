namespace StockMonitor.Core.Services.Interfaces;

/// <summary>
/// 泛型仓储接口
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public interface IRepository<T>
{
    /// <summary>
    /// 加载数据
    /// </summary>
    T Load();

    /// <summary>
    /// 保存数据
    /// </summary>
    void Save(T data);

    /// <summary>
    /// 数据文件是否存在
    /// </summary>
    bool Exists();
}
