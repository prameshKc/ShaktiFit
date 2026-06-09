namespace FitForgeAI.Services;

public interface IJsonStorageService
{
    Task<List<T>> ReadAsync<T>(string fileName);
    Task WriteAsync<T>(string fileName, List<T> data);
    Task<T?> FindByIdAsync<T>(string fileName, Func<T, bool> predicate);
    Task AddOrUpdateAsync<T>(string fileName, T item, Func<T, string> idSelector);
    Task DeleteAsync<T>(string fileName, Func<T, bool> predicate);
}
