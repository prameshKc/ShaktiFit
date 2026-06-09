using System.Text.Json;

namespace FitForgeAI.Services;

public class JsonStorageService : IJsonStorageService
{
    private readonly string _dataPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonStorageService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data", "Json");
    }

    public async Task<List<T>> ReadAsync<T>(string fileName)
    {
        var path = Path.Combine(_dataPath, fileName);
        if (!File.Exists(path)) return new List<T>();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
    }

    public async Task WriteAsync<T>(string fileName, List<T> data)
    {
        await _lock.WaitAsync();
        try
        {
            var path = Path.Combine(_dataPath, fileName);
            var json = JsonSerializer.Serialize(data, _options);
            await File.WriteAllTextAsync(path, json);
        }
        finally { _lock.Release(); }
    }

    public async Task<T?> FindByIdAsync<T>(string fileName, Func<T, bool> predicate)
    {
        var items = await ReadAsync<T>(fileName);
        return items.FirstOrDefault(predicate);
    }

    public async Task AddOrUpdateAsync<T>(string fileName, T item, Func<T, string> idSelector)
    {
        var items = await ReadAsync<T>(fileName);
        var id = idSelector(item);
        var existing = items.FindIndex(x => idSelector(x) == id);
        if (existing >= 0) items[existing] = item;
        else items.Add(item);
        await WriteAsync(fileName, items);
    }

    public async Task DeleteAsync<T>(string fileName, Func<T, bool> predicate)
    {
        var items = await ReadAsync<T>(fileName);
        items.RemoveAll(x => predicate(x));
        await WriteAsync(fileName, items);
    }
}
