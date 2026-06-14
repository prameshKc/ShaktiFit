using System.Text.Json;
using System.Text.Json.Serialization;
using FitForgeAI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FitForgeAI.Services;

// ── free-exercise-db models (github.com/yuhonas/free-exercise-db) ─────────────

public class WorkoutApiMuscle
{
    public string Id   { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "";
}

public class WorkoutApiType
{
    public string Id   { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class WorkoutApiCategory
{
    public string Id   { get; set; } = "";
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}

public class FreeExerciseDbEntry
{
    [JsonPropertyName("id")]               public string Id               { get; set; } = "";
    [JsonPropertyName("name")]             public string Name             { get; set; } = "";
    [JsonPropertyName("force")]            public string? Force           { get; set; }
    [JsonPropertyName("level")]            public string Level            { get; set; } = "beginner";
    [JsonPropertyName("mechanic")]         public string? Mechanic        { get; set; }
    [JsonPropertyName("equipment")]        public string? Equipment       { get; set; }
    [JsonPropertyName("primaryMuscles")]   public List<string> PrimaryMuscles   { get; set; } = new();
    [JsonPropertyName("secondaryMuscles")] public List<string> SecondaryMuscles { get; set; } = new();
    [JsonPropertyName("instructions")]     public List<string> Instructions     { get; set; } = new();
    [JsonPropertyName("category")]         public string Category         { get; set; } = "";
    [JsonPropertyName("images")]           public List<string> Images     { get; set; } = new();
}

// Keep this so ExerciseController.Details can store it in ViewBag without changes
public class WorkoutApiExercise
{
    public string Id               { get; set; } = "";
    public string Code             { get; set; } = "";
    public string Name             { get; set; } = "";
    public string Description      { get; set; } = "";
    public List<WorkoutApiMuscle>   PrimaryMuscles   { get; set; } = new();
    public List<WorkoutApiMuscle>   SecondaryMuscles { get; set; } = new();
    public List<WorkoutApiType>     Types            { get; set; } = new();
    public List<WorkoutApiCategory> Categories       { get; set; } = new();
    // Extended fields from free-exercise-db
    public string Level            { get; set; } = "beginner";
    public string? Equipment       { get; set; }
    public List<string> Instructions { get; set; } = new();
    public List<string> Images     { get; set; } = new();

    // Direct image URLs (frame 0 and frame 1 for animation)
    public string? ImageUrl        { get; set; }
    public string? ImageUrl2       { get; set; }
}

// ── Service ───────────────────────────────────────────────────────────────────

public class WorkoutApiService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private const string CacheKeyAll = "free_exercise_db_all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(48);

    // Raw JSON from the free exercise DB
    private const string JsonUrl =
        "https://raw.githubusercontent.com/yuhonas/free-exercise-db/main/dist/exercises.json";

    // Base URL for images
    private const string ImgBase =
        "https://raw.githubusercontent.com/yuhonas/free-exercise-db/main/exercises/";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WorkoutApiService(HttpClient http, IMemoryCache cache)
    {
        _http  = http;
        _cache = cache;
    }

    // ── Fetch all exercises (cached 48 h) ────────────────────────────────────
    public async Task<List<WorkoutApiExercise>> GetExercisesAsync()
    {
        if (_cache.TryGetValue(CacheKeyAll, out List<WorkoutApiExercise>? cached) && cached != null)
            return cached;

        try
        {
            var json = await _http.GetStringAsync(JsonUrl);
            var raw  = JsonSerializer.Deserialize<List<FreeExerciseDbEntry>>(json, JsonOpts)
                       ?? new();

            var list = raw.Select(ToApiExercise).ToList();
            _cache.Set(CacheKeyAll, list, CacheDuration);
            return list;
        }
        catch
        {
            return new List<WorkoutApiExercise>();
        }
    }

    public async Task<WorkoutApiExercise?> GetExerciseByIdAsync(string id)
    {
        var all = await GetExercisesAsync();
        return all.FirstOrDefault(e => e.Id == id);
    }

    // Kept for controller compatibility — images are now direct URLs, not proxied SVGs
    public Task<string?> GetExerciseImageAsync(string id) =>
        Task.FromResult<string?>(null);

    // ── Map free-exercise-db entry → WorkoutApiExercise ──────────────────────
    private static WorkoutApiExercise ToApiExercise(FreeExerciseDbEntry src)
    {
        var primaryMuscles = src.PrimaryMuscles.Select(m => new WorkoutApiMuscle { Name = m, Id = m, Code = m }).ToList();
        var secondaryMuscles = src.SecondaryMuscles.Select(m => new WorkoutApiMuscle { Name = m, Id = m, Code = m }).ToList();

        bool isCompound = src.Mechanic?.Equals("compound", StringComparison.OrdinalIgnoreCase) == true;
        var types = isCompound
            ? new List<WorkoutApiType> { new() { Code = "POLYARTICULAR", Name = "Compound" } }
            : new List<WorkoutApiType>();

        var imageUrl  = src.Images.Count > 0 ? ImgBase + src.Images[0] : null;
        var imageUrl2 = src.Images.Count > 1 ? ImgBase + src.Images[1] : null;

        return new WorkoutApiExercise
        {
            Id               = src.Id,
            Code             = src.Id,
            Name             = src.Name,
            Description      = string.Join(" ", src.Instructions),
            PrimaryMuscles   = primaryMuscles,
            SecondaryMuscles = secondaryMuscles,
            Types            = types,
            Categories       = new List<WorkoutApiCategory>
                               { new() { Name = src.Category, Code = src.Category } },
            Level            = src.Level,
            Equipment        = src.Equipment,
            Instructions     = src.Instructions,
            Images           = src.Images,
            ImageUrl         = imageUrl,
            ImageUrl2        = imageUrl2,
        };
    }

    // ── Map WorkoutApiExercise → app Exercise model ──────────────────────────
    public static Exercise ToExercise(WorkoutApiExercise src)
    {
        string primaryMuscle = src.PrimaryMuscles.FirstOrDefault()?.Name ?? "Other";
        string category = NormalizeCategory(
            !string.IsNullOrEmpty(src.Categories.FirstOrDefault()?.Name)
                ? src.Categories[0].Name
                : primaryMuscle);

        var muscles = src.PrimaryMuscles.Select(m => m.Name)
            .Concat(src.SecondaryMuscles.Select(m => m.Name))
            .Distinct().ToList();

        bool isCompound = src.Types.Any(t =>
            t.Code.Equals("POLYARTICULAR", StringComparison.OrdinalIgnoreCase));

        string difficulty = src.Level?.ToLower() switch
        {
            "beginner"     => "Beginner",
            "intermediate" => "Intermediate",
            "expert"       => "Advanced",
            _              => "Beginner"
        };

        return new Exercise
        {
            Id            = src.Id,
            Name          = src.Name,
            Category      = category,
            Difficulty    = difficulty,
            TargetMuscles = muscles,
            Description   = src.Description,
            Instructions  = src.Instructions,
            Equipment     = src.Equipment != null ? new List<string> { src.Equipment } : new(),
            IsCompound    = isCompound,
            DefaultSets   = isCompound ? 4 : 3,
            DefaultReps   = isCompound ? "6-10" : "10-15",
            RestSeconds   = isCompound ? 90 : 60,
            ImageUrl      = src.ImageUrl,
            ImageUrl2     = src.ImageUrl2,
        };
    }

    // ── Get direct image URL for an exercise id ───────────────────────────────
    public async Task<string?> GetImageUrlAsync(string id)
    {
        var ex = await GetExerciseByIdAsync(id);
        return ex?.ImageUrl;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string NormalizeCategory(string raw) =>
        raw.ToLower() switch
        {
            "chest"          => "Chest",
            "back"           => "Back",
            "shoulders"      => "Shoulders",
            "arms"           => "Arms",
            "legs"           => "Legs",
            "abdominals"     => "Core",
            "core"           => "Core",
            "cardio"         => "Cardio",
            "olympic weightlifting" => "Olympic",
            "powerlifting"   => "Powerlifting",
            "strength"       => "Strength",
            "stretching"     => "Flexibility",
            "plyometrics"    => "Cardio",
            "trapezius"      => "Back",
            "triceps"        => "Arms",
            "biceps"         => "Arms",
            "forearms"       => "Arms",
            "lats"           => "Back",
            "lower back"     => "Back",
            "quads"          => "Legs",
            "quadriceps"     => "Legs",
            "hamstrings"     => "Legs",
            "glutes"         => "Legs",
            "calves"         => "Legs",
            "abs"            => "Core",
            "obliques"       => "Core",
            _                => TitleCase(raw)
        };

    private static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}
