using FitForgeAI.Models;

namespace FitForgeAI.Services;

public class ExerciseService
{
    private readonly IJsonStorageService _storage;
    private const string File = "exercises.json";

    public ExerciseService(IJsonStorageService storage) => _storage = storage;

    public Task<List<Exercise>> GetAllAsync() => _storage.ReadAsync<Exercise>(File);

    public async Task<Exercise?> GetByIdAsync(string id) =>
        await _storage.FindByIdAsync<Exercise>(File, e => e.Id == id);

    public async Task<List<Exercise>> GetByCategoryAsync(string category)
    {
        var all = await GetAllAsync();
        return category == "All" ? all : all.Where(e => e.Category == category).ToList();
    }

    public async Task<List<Exercise>> SearchAsync(string query)
    {
        var all = await GetAllAsync();
        query = query.ToLower();
        return all.Where(e =>
            e.Name.ToLower().Contains(query) ||
            e.Category.ToLower().Contains(query) ||
            e.TargetMuscles.Any(m => m.ToLower().Contains(query))).ToList();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        var all = await GetAllAsync();
        return all.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();
    }
}
