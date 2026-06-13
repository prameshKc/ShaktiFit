using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using FitForgeAI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FitForgeAI.Services;

// ── API response models ──────────────────────────────────────────────────────

public class WorkoutApiMuscle
{
    [JsonPropertyName("id")]   public string Id   { get; set; } = "";
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("color")] public string Color { get; set; } = "";
}

public class WorkoutApiType
{
    [JsonPropertyName("id")]   public string Id   { get; set; } = "";
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class WorkoutApiCategory
{
    [JsonPropertyName("id")]   public string Id   { get; set; } = "";
    [JsonPropertyName("code")] public string Code { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

public class WorkoutApiExercise
{
    [JsonPropertyName("id")]               public string Id               { get; set; } = "";
    [JsonPropertyName("code")]             public string Code             { get; set; } = "";
    [JsonPropertyName("name")]             public string Name             { get; set; } = "";
    [JsonPropertyName("description")]      public string Description      { get; set; } = "";
    [JsonPropertyName("primaryMuscles")]   public List<WorkoutApiMuscle>   PrimaryMuscles   { get; set; } = new();
    [JsonPropertyName("secondaryMuscles")] public List<WorkoutApiMuscle>   SecondaryMuscles { get; set; } = new();
    [JsonPropertyName("types")]            public List<WorkoutApiType>     Types            { get; set; } = new();
    [JsonPropertyName("categories")]       public List<WorkoutApiCategory> Categories       { get; set; } = new();
}

// ── Service ──────────────────────────────────────────────────────────────────

public class WorkoutApiService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private const string CacheKeyAll = "workout_api_all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkoutApiService(HttpClient http, IMemoryCache cache)
    {
        _http  = http;
        _cache = cache;
    }

    // ── Fetch all exercises (cached 24 h) ────────────────────────────────────
    public async Task<List<WorkoutApiExercise>> GetExercisesAsync()
    {
        if (_cache.TryGetValue(CacheKeyAll, out List<WorkoutApiExercise>? cached) && cached != null)
            return cached;

        try
        {
            var json = await _http.GetStringAsync("/exercises");
            var list = JsonSerializer.Deserialize<List<WorkoutApiExercise>>(json, JsonOpts)
                       ?? new List<WorkoutApiExercise>();

            _cache.Set(CacheKeyAll, list, CacheDuration);
            return list;
        }
        catch
        {
            return new List<WorkoutApiExercise>();
        }
    }

    // ── Fetch single exercise by API id ──────────────────────────────────────
    public async Task<WorkoutApiExercise?> GetExerciseByIdAsync(string id)
    {
        // Try the cache first to avoid an extra round-trip
        var all = await GetExercisesAsync();
        var fromCache = all.FirstOrDefault(e => e.Id == id);
        if (fromCache != null) return fromCache;

        try
        {
            var json = await _http.GetStringAsync($"/exercises/{id}");
            return JsonSerializer.Deserialize<WorkoutApiExercise>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    // ── Map API exercise → app Exercise model ────────────────────────────────
    public static Exercise ToExercise(WorkoutApiExercise src)
    {
        // Derive a category from the primary muscle group (or equipment category)
        string category = src.PrimaryMuscles.FirstOrDefault()?.Name
                       ?? src.Categories.FirstOrDefault()?.Name
                       ?? "Other";

        // Build target muscles list (primary + secondary)
        var muscles = src.PrimaryMuscles.Select(m => m.Name)
            .Concat(src.SecondaryMuscles.Select(m => m.Name))
            .Distinct()
            .ToList();

        // Equipment from the API categories
        var equipment = src.Categories.Select(c => c.Name).ToList();

        // Determine if compound (Polyarticular type = compound movement)
        bool isCompound = src.Types.Any(t =>
            t.Code.Equals("POLYARTICULAR", StringComparison.OrdinalIgnoreCase));

        // Split the description into instruction sentences for step-by-step display
        var instructions = SplitIntoSteps(src.Description);

        return new Exercise
        {
            Id           = src.Id,
            Name         = src.Name,
            Category     = NormalizeCategory(category),
            Difficulty   = isCompound ? "Intermediate" : "Beginner",
            TargetMuscles = muscles,
            Description  = src.Description,
            Instructions = instructions,
            Equipment    = equipment,
            IsCompound   = isCompound,
            DefaultSets  = isCompound ? 4 : 3,
            DefaultReps  = isCompound ? "6-10" : "10-15",
            RestSeconds  = isCompound ? 90 : 60,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> SplitIntoSteps(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return new List<string>();

        // Split on sentences — ". " boundary, keeping reasonable length
        var raw = description.Split(new[] { ". " }, StringSplitOptions.RemoveEmptyEntries);
        var steps = new List<string>();
        foreach (var part in raw)
        {
            var trimmed = part.Trim().TrimEnd('.');
            if (!string.IsNullOrWhiteSpace(trimmed))
                steps.Add(trimmed + ".");
        }
        return steps;
    }

    private static string NormalizeCategory(string raw)
    {
        return raw.ToLower() switch
        {
            "trapezius"  => "Back",
            "shoulders"  => "Shoulders",
            "chest"      => "Chest",
            "triceps"    => "Arms",
            "biceps"     => "Arms",
            "biceps brachii" => "Arms",
            "forearms"   => "Arms",
            "back"       => "Back",
            "lats"       => "Back",
            "rhomboids"  => "Back",
            "lower back" => "Back",
            "quads"      => "Legs",
            "quadriceps" => "Legs",
            "hamstrings" => "Legs",
            "glutes"     => "Legs",
            "calves"     => "Legs",
            "abdominals" => "Core",
            "abs"        => "Core",
            "core"       => "Core",
            "obliques"   => "Core",
            "free weight" => "Other",
            "machine"    => "Other",
            "bodyweight" => "Other",
            _            => TitleCase(raw)
        };
    }

    private static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}
