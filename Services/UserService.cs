using FitForgeAI.Models;
using System.Security.Cryptography;
using System.Text;

namespace FitForgeAI.Services;

public class UserService
{
    private readonly IJsonStorageService _storage;
    private const string File = "users.json";

    public UserService(IJsonStorageService storage) => _storage = storage;

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var users = await _storage.ReadAsync<User>(File);
        var hash = HashPassword(password);
        return users.FirstOrDefault(u => u.Email == email && u.PasswordHash == hash);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var users = await _storage.ReadAsync<User>(File);
        return users.Any(u => u.Email == email);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        user.PasswordHash = HashPassword(password);
        await _storage.AddOrUpdateAsync(File, user, u => u.Id);
        return user;
    }

    public Task<List<User>> GetAllAsync() => _storage.ReadAsync<User>(File);

    public async Task<User?> GetByIdAsync(string id) =>
        await _storage.FindByIdAsync<User>(File, u => u.Id == id);

    public async Task UpdateAsync(User user) =>
        await _storage.AddOrUpdateAsync(File, user, u => u.Id);

    public async Task IncrementStreakAsync(string userId)
    {
        var user = await GetByIdAsync(userId);
        if (user == null) return;
        user.WorkoutStreak++;
        user.TotalWorkoutsCompleted++;
        if (user.TotalWorkoutsCompleted == 1)
            user.Achievements.Add("ach001");
        if (user.WorkoutStreak == 7)
            user.Achievements.Add("ach002");
        if (user.WorkoutStreak == 30)
            user.Achievements.Add("ach003");
        if (user.TotalWorkoutsCompleted == 100)
            user.Achievements.Add("ach004");
        await UpdateAsync(user);
    }

    private static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password + "FitForgeAI_Salt"));
        return Convert.ToBase64String(bytes);
    }
}
