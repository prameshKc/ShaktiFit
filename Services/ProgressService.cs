using FitForgeAI.Models;

namespace FitForgeAI.Services;

public class ProgressService
{
    private readonly IJsonStorageService _storage;
    private const string ProgressFile = "progress.json";
    private const string RunningFile = "running-logs.json";

    public ProgressService(IJsonStorageService storage) => _storage = storage;

    public async Task LogProgressAsync(ProgressEntry entry) =>
        await _storage.AddOrUpdateAsync(ProgressFile, entry, p => p.Id);

    public async Task<List<ProgressEntry>> GetUserProgressAsync(string userId)
    {
        var all = await _storage.ReadAsync<ProgressEntry>(ProgressFile);
        return all.Where(p => p.UserId == userId).OrderBy(p => p.Date).ToList();
    }

    public async Task<ProgressEntry?> GetLatestAsync(string userId)
    {
        var entries = await GetUserProgressAsync(userId);
        return entries.LastOrDefault();
    }

    public async Task LogRunAsync(RunningLog log) =>
        await _storage.AddOrUpdateAsync(RunningFile, log, r => r.Id);

    public async Task<List<RunningLog>> GetRunningLogsAsync(string userId)
    {
        var all = await _storage.ReadAsync<RunningLog>(RunningFile);
        return all.Where(r => r.UserId == userId).OrderByDescending(r => r.Date).ToList();
    }

    public async Task<double> GetWeeklyMileageAsync(string userId)
    {
        var logs = await GetRunningLogsAsync(userId);
        var weekStart = DateTime.UtcNow.AddDays(-7);
        return logs.Where(r => r.Date >= weekStart).Sum(r => r.DistanceKm);
    }

    public async Task<RunningLog?> GetPersonalBest5KAsync(string userId)
    {
        var logs = await GetRunningLogsAsync(userId);
        return logs.Where(r => r.DistanceKm >= 5).OrderBy(r => r.PaceMinPerKm).FirstOrDefault();
    }

    public static double CalculatePace(double distanceKm, int durationMinutes) =>
        distanceKm > 0 ? Math.Round(durationMinutes / distanceKm, 2) : 0;
}
