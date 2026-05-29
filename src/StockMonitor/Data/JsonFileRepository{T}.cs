using System.IO;
using StockMonitor.Core.Services.Interfaces;
using System.Text.Json;

namespace StockMonitor.Data;

/// <summary>
/// 基于 JSON 文件的泛型仓储实现，支持原子写入、备份恢复和内存缓存
/// </summary>
/// <typeparam name="T">持久化数据类型</typeparam>
public class JsonFileRepository<T> : IRepository<T> where T : new()
{
    private readonly string _filePath;
    private readonly string _tmpFilePath;
    private readonly string _bakFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private T? _cache;

    /// <summary>
    /// 初始化 JSON 文件仓储
    /// </summary>
    /// <param name="filePath">数据文件路径</param>
    public JsonFileRepository(string filePath)
    {
        _filePath = filePath;
        _tmpFilePath = filePath + ".tmp";
        _bakFilePath = filePath + ".bak";
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// 加载数据：优先主文件，主文件损坏时回退到备份文件，均不存在则返回默认实例
    /// </summary>
    public T Load()
    {
        if (_cache != null) return _cache;

        var result = TryReadFile(_filePath);
        if (result != null)
        {
            _cache = result;
            return _cache;
        }

        result = TryReadFile(_bakFilePath);
        if (result != null)
        {
            _cache = result;
            return _cache;
        }

        _cache = new T();
        return _cache;
    }

    /// <summary>
    /// 保存数据：先备份现有文件，再写入临时文件后重命名，确保原子写入防止数据损坏
    /// </summary>
    /// <param name="data">要保存的数据</param>
    public void Save(T data)
    {
        _cache = data;
        var json = JsonSerializer.Serialize(data, _jsonOptions);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(_filePath))
        {
            File.Copy(_filePath, _bakFilePath, overwrite: true);
        }

        File.WriteAllText(_tmpFilePath, json);
        File.Move(_tmpFilePath, _filePath, overwrite: true);
    }

    /// <summary>
    /// 判断数据文件是否存在
    /// </summary>
    public bool Exists()
    {
        return File.Exists(_filePath);
    }

    /// <summary>
    /// 尝试从指定路径读取并反序列化 JSON 文件，失败返回 null
    /// </summary>
    /// <param name="path">文件路径</param>
    private T? TryReadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return default;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
