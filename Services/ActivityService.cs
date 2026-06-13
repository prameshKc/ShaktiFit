using FitForgeAI.Models;

namespace FitForgeAI.Services;

public class ActivityService
{
    private readonly IJsonStorageService _storage;
    private const string Key = "activities";

    public ActivityService(IJsonStorageService storage) => _storage = storage;

    public async Task<List<Activity>> GetAllAsync()
        => await _storage.ReadAsync<Activity>(Key) ?? new();

    public async Task<List<Activity>> GetForUserAsync(string userId)
    {
        var all = await GetAllAsync();
        return all.Where(a => a.UserId == userId).OrderByDescending(a => a.Date).ToList();
    }

    public async Task<List<Activity>> GetRecentAsync(string userId, int days = 7)
    {
        var cutoff = DateTime.Now.AddDays(-days);
        var all = await GetForUserAsync(userId);
        return all.Where(a => a.Date >= cutoff).ToList();
    }

    public async Task AddAsync(Activity activity)
    {
        var all = await GetAllAsync();
        all.Add(activity);
        await _storage.WriteAsync(Key, all);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var all = await GetAllAsync();
        all.RemoveAll(a => a.Id == id && a.UserId == userId);
        await _storage.WriteAsync(Key, all);
    }

    // ── Smart suggestions based on recent activity ───────────────
    public async Task<List<string>> GetSuggestionsAsync(string userId)
    {
        var recent7 = await GetRecentAsync(userId, 7);
        var recent1 = await GetRecentAsync(userId, 1);
        var recent3 = await GetRecentAsync(userId, 3);
        var suggestions = new List<string>();

        var totalMinutes = recent7.Sum(a => a.DurationMinutes);
        var totalKm      = recent7.Where(a => a.DistanceKm.HasValue).Sum(a => a.DistanceKm!.Value);
        var hardSessions = recent7.Count(a => a.Intensity == "Hard");
        var lastActivity = recent7.FirstOrDefault();

        // No activity in 3+ days
        if (!recent3.Any())
            suggestions.Add("💤 No activity in 3+ days — time to get moving! Even a 20-min walk counts.");

        // Ran yesterday (hard) → suggest recovery
        var yesterday = recent1.FirstOrDefault(a => a.Intensity == "Hard");
        if (yesterday != null)
            suggestions.Add($"🔄 You had a hard {yesterday.Type} session yesterday — consider a recovery or mobility day today.");

        // 3+ hard sessions this week → deload
        if (hardSessions >= 3)
            suggestions.Add("⚠️ 3+ hard sessions this week — your body needs a deload. Try a light session or rest.");

        // Hit weekly distance goal
        if (totalKm >= 20)
            suggestions.Add($"🏆 Great week! You've covered {totalKm:0.0} km — excellent endurance work!");
        else if (totalKm > 0 && totalKm < 10)
            suggestions.Add($"🏃 You've run {totalKm:0.0} km this week. Try to hit 10 km for a solid base.");

        // Low weekly volume
        if (totalMinutes < 90 && recent7.Any())
            suggestions.Add($"📈 Only {totalMinutes} min of activity this week. Aim for 150 min for health benefits.");
        else if (totalMinutes >= 150)
            suggestions.Add($"✅ {totalMinutes} min of activity this week — you're hitting WHO guidelines!");

        // Streak check
        if (recent7.Count >= 5)
            suggestions.Add("🔥 5+ sessions this week — amazing consistency! Keep the streak alive.");

        // No suggestions yet → generic
        if (!suggestions.Any())
            suggestions.Add("👟 Log your first activity to get personalized suggestions!");

        return suggestions;
    }

    // ── Weekly stats ─────────────────────────────────────────────
    public async Task<ActivityStats> GetWeeklyStatsAsync(string userId)
    {
        var recent = await GetRecentAsync(userId, 7);
        return new ActivityStats
        {
            TotalSessions  = recent.Count,
            TotalMinutes   = recent.Sum(a => a.DurationMinutes),
            TotalKm        = recent.Where(a => a.DistanceKm.HasValue).Sum(a => a.DistanceKm!.Value),
            TotalCalories  = recent.Where(a => a.CaloriesBurned.HasValue).Sum(a => a.CaloriesBurned!.Value),
            HardSessions   = recent.Count(a => a.Intensity == "Hard"),
            LastActivity   = recent.FirstOrDefault()
        };
    }

    // ── Estimate calories if not provided ────────────────────────
    public static int EstimateCalories(string type, int minutes, string intensity)
    {
        double met = type.ToLower() switch {
            "run"      => intensity == "Hard" ? 11.0 : intensity == "Easy" ? 7.0 : 9.0,
            "ride" or "cycling" => intensity == "Hard" ? 10.0 : 6.5,
            "swim"     => 8.0,
            "hiit"     => 10.5,
            "gym"      => intensity == "Hard" ? 6.0 : 4.5,
            "walk"     => 3.5,
            "yoga"     => 2.5,
            "hike"     => 6.0,
            _          => 5.0
        };
        // MET × 70kg (average) × hours
        return (int)(met * 70 * (minutes / 60.0));
    }
}

public class ActivityStats
{
    public int    TotalSessions  { get; set; }
    public int    TotalMinutes   { get; set; }
    public double TotalKm        { get; set; }
    public int    TotalCalories  { get; set; }
    public int    HardSessions   { get; set; }
    public Activity? LastActivity { get; set; }
}
