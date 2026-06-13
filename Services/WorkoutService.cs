using FitForgeAI.Models;

namespace FitForgeAI.Services;

public class WorkoutService
{
    private readonly IJsonStorageService _storage;
    private const string WorkoutsFile = "workouts.json";
    private const string LogsFile = "workout-logs.json";

    public WorkoutService(IJsonStorageService storage) => _storage = storage;

    public Task<List<Workout>> GetAllAsync() => _storage.ReadAsync<Workout>(WorkoutsFile);

    public async Task<Workout?> GetByIdAsync(string id) =>
        await _storage.FindByIdAsync<Workout>(WorkoutsFile, w => w.Id == id);

    public async Task<List<Workout>> GetForUserAsync(User user)
    {
        var all = await GetAllAsync();
        var hasGoals = user.Goals != null && user.Goals.Count > 0;
        return all.Where(w =>
            (w.FitnessLevel == user.FitnessLevel || w.FitnessLevel == "Beginner") &&
            (!hasGoals || w.Goals.Any(g => user.Goals.Contains(g))) &&
            (!w.RequiresGym || user.HasGymAccess)).ToList();
    }

    public async Task<Workout?> GetTodaysWorkoutAsync(User user)
    {
        var workouts = await GetForUserAsync(user);
        if (!workouts.Any()) workouts = await GetAllAsync(); // fallback for brand-new users
        var day = DateTime.Now.DayOfWeek.ToString();
        return workouts.FirstOrDefault(w => w.DayOfWeek == day) ?? workouts.FirstOrDefault();
    }

    public async Task LogWorkoutAsync(WorkoutLog log)
    {
        await _storage.AddOrUpdateAsync(LogsFile, log, l => l.Id);
    }

    public async Task<List<WorkoutLog>> GetUserLogsAsync(string userId)
    {
        var logs = await _storage.ReadAsync<WorkoutLog>(LogsFile);
        return logs.Where(l => l.UserId == userId).OrderByDescending(l => l.CompletedAt).ToList();
    }

    public async Task<List<WorkoutLog>> GetRecentLogsAsync(string userId, int count = 7)
    {
        var logs = await GetUserLogsAsync(userId);
        return logs.Take(count).ToList();
    }

    public async Task<int> GetWeeklyCompletedAsync(string userId)
    {
        var logs = await GetUserLogsAsync(userId);
        var weekStart = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        return logs.Count(l => l.CompletedAt >= weekStart);
    }

    // ── User custom routines (saved from Split Builder) ──────────────────────
    public Task<List<Workout>> GetUserRoutinesAsync(string userId) =>
        _storage.ReadAsync<Workout>($"user_routines_{userId}.json");

    public Task SaveUserRoutinesAsync(string userId, List<Workout> routines) =>
        _storage.WriteAsync($"user_routines_{userId}.json", routines);

    public async Task<List<Workout>> GetHybridPlanAsync()
    {
        var all = await GetAllAsync();
        var plan = new List<Workout>();
        var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        foreach (var day in days)
        {
            var workout = all.FirstOrDefault(w => w.DayOfWeek == day);
            if (workout != null) plan.Add(workout);
        }
        return plan;
    }
}
