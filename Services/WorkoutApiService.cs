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
    private const string CacheKeyAll = "free_exercise_db_v3"; // bump version to bust stale cache
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
            var fallback = GetFallbackExercises();
            _cache.Set(CacheKeyAll, fallback, TimeSpan.FromMinutes(30));
            return fallback;
        }
    }

    private static List<WorkoutApiExercise> GetFallbackExercises()
    {
        // Hardcoded fallback — used when GitHub API is unreachable on Railway
        var data = new[]
        {
            // CHEST
            ("bench-press",        "Barbell Bench Press",       "chest",      "intermediate", "barbell",   true,  new[]{"chest","triceps","shoulders"}),
            ("incline-bench",      "Incline Bench Press",       "chest",      "intermediate", "barbell",   true,  new[]{"chest","shoulders"}),
            ("decline-bench",      "Decline Bench Press",       "chest",      "intermediate", "barbell",   true,  new[]{"chest","triceps"}),
            ("dumbbell-fly",       "Dumbbell Fly",              "chest",      "beginner",     "dumbbell",  false, new[]{"chest"}),
            ("push-up",            "Push Up",                   "chest",      "beginner",     "body only", true,  new[]{"chest","triceps","shoulders"}),
            ("cable-crossover",    "Cable Crossover",           "chest",      "intermediate", "cable",     false, new[]{"chest"}),
            ("dips",               "Chest Dips",                "chest",      "intermediate", "body only", true,  new[]{"chest","triceps"}),
            // BACK
            ("pull-up",            "Pull Up",                   "back",       "intermediate", "body only", true,  new[]{"lats","biceps"}),
            ("lat-pulldown",       "Lat Pulldown",              "back",       "beginner",     "cable",     true,  new[]{"lats","biceps"}),
            ("barbell-row",        "Barbell Row",               "back",       "intermediate", "barbell",   true,  new[]{"lats","rhomboids","biceps"}),
            ("seated-cable-row",   "Seated Cable Row",          "back",       "beginner",     "cable",     true,  new[]{"lats","rhomboids"}),
            ("deadlift",           "Conventional Deadlift",     "back",       "expert",       "barbell",   true,  new[]{"lower back","glutes","hamstrings"}),
            ("db-row",             "Dumbbell Single-Arm Row",   "back",       "beginner",     "dumbbell",  true,  new[]{"lats","rhomboids"}),
            ("face-pull",          "Face Pull",                 "back",       "beginner",     "cable",     false, new[]{"rear deltoids","traps"}),
            // LEGS
            ("squat",              "Barbell Back Squat",        "legs",       "intermediate", "barbell",   true,  new[]{"quads","glutes","hamstrings"}),
            ("leg-press",          "Leg Press",                 "legs",       "beginner",     "machine",   true,  new[]{"quads","glutes"}),
            ("romanian-deadlift",  "Romanian Deadlift",         "legs",       "intermediate", "barbell",   true,  new[]{"hamstrings","glutes"}),
            ("leg-curl",           "Lying Leg Curl",            "legs",       "beginner",     "machine",   false, new[]{"hamstrings"}),
            ("leg-extension",      "Leg Extension",             "legs",       "beginner",     "machine",   false, new[]{"quads"}),
            ("lunge",              "Dumbbell Lunge",            "legs",       "beginner",     "dumbbell",  true,  new[]{"quads","glutes"}),
            ("calf-raise",         "Standing Calf Raise",       "legs",       "beginner",     "machine",   false, new[]{"calves"}),
            ("goblet-squat",       "Goblet Squat",              "legs",       "beginner",     "dumbbell",  true,  new[]{"quads","glutes"}),
            ("hip-thrust",         "Barbell Hip Thrust",        "legs",       "intermediate", "barbell",   true,  new[]{"glutes","hamstrings"}),
            // SHOULDERS
            ("ohp",                "Overhead Press",            "shoulders",  "intermediate", "barbell",   true,  new[]{"shoulders","triceps"}),
            ("db-shoulder-press",  "Dumbbell Shoulder Press",   "shoulders",  "beginner",     "dumbbell",  true,  new[]{"shoulders","triceps"}),
            ("lateral-raise",      "Lateral Raise",             "shoulders",  "beginner",     "dumbbell",  false, new[]{"side deltoids"}),
            ("front-raise",        "Front Raise",               "shoulders",  "beginner",     "dumbbell",  false, new[]{"front deltoids"}),
            ("arnold-press",       "Arnold Press",              "shoulders",  "intermediate", "dumbbell",  true,  new[]{"shoulders","triceps"}),
            ("rear-delt-fly",      "Rear Delt Fly",             "shoulders",  "beginner",     "dumbbell",  false, new[]{"rear deltoids"}),
            // ARMS
            ("barbell-curl",       "Barbell Curl",              "arms",       "beginner",     "barbell",   false, new[]{"biceps"}),
            ("db-curl",            "Dumbbell Curl",             "arms",       "beginner",     "dumbbell",  false, new[]{"biceps"}),
            ("hammer-curl",        "Hammer Curl",               "arms",       "beginner",     "dumbbell",  false, new[]{"biceps","brachialis"}),
            ("tricep-pushdown",    "Tricep Cable Pushdown",     "arms",       "beginner",     "cable",     false, new[]{"triceps"}),
            ("skull-crusher",      "Skull Crusher",             "arms",       "intermediate", "barbell",   false, new[]{"triceps"}),
            ("overhead-tricep",    "Overhead Tricep Extension", "arms",       "beginner",     "dumbbell",  false, new[]{"triceps"}),
            ("cable-curl",         "Cable Curl",                "arms",       "beginner",     "cable",     false, new[]{"biceps"}),
            ("preacher-curl",      "Preacher Curl",             "arms",       "beginner",     "machine",   false, new[]{"biceps"}),
            // CORE
            ("plank",              "Plank",                     "abdominals", "beginner",     "body only", false, new[]{"abdominals","core"}),
            ("crunch",             "Crunch",                    "abdominals", "beginner",     "body only", false, new[]{"abdominals"}),
            ("hanging-leg-raise",  "Hanging Leg Raise",         "abdominals", "intermediate", "body only", false, new[]{"abdominals","hip flexors"}),
            ("russian-twist",      "Russian Twist",             "abdominals", "beginner",     "body only", false, new[]{"obliques"}),
            ("ab-wheel",           "Ab Wheel Rollout",          "abdominals", "intermediate", "other",     false, new[]{"abdominals","core"}),
            ("cable-crunch",       "Cable Crunch",              "abdominals", "beginner",     "cable",     false, new[]{"abdominals"}),
            ("bicycle-crunch",     "Bicycle Crunch",            "abdominals", "beginner",     "body only", false, new[]{"abdominals","obliques"}),
            // CARDIO
            ("burpee",             "Burpee",                    "cardio",     "beginner",     "body only", true,  new[]{"full body"}),
            ("jumping-jack",       "Jumping Jack",              "cardio",     "beginner",     "body only", false, new[]{"cardio"}),
            ("mountain-climber",   "Mountain Climber",          "cardio",     "beginner",     "body only", false, new[]{"cardio","core"}),
            ("box-jump",           "Box Jump",                  "cardio",     "intermediate", "other",     true,  new[]{"legs","cardio"}),
            ("battle-rope",        "Battle Rope Waves",         "cardio",     "beginner",     "other",     false, new[]{"cardio","shoulders"}),
            ("jump-rope",          "Jump Rope",                 "cardio",     "beginner",     "other",     false, new[]{"cardio","calves"}),
        };

        return data.Select(d => new WorkoutApiExercise
        {
            Id             = d.Item1,
            Code           = d.Item1,
            Name           = d.Item2,
            Level          = d.Item4,
            Equipment      = d.Item5,
            PrimaryMuscles = d.Item7.Select(m => new WorkoutApiMuscle { Name = m, Id = m, Code = m }).ToList(),
            Types          = d.Item6 ? new List<WorkoutApiType> { new() { Code = "POLYARTICULAR", Name = "Compound" } } : new(),
            Categories     = new List<WorkoutApiCategory> { new() { Name = d.Item3, Code = d.Item3 } },
        }).ToList();
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
        string primaryMuscle = src.PrimaryMuscles.FirstOrDefault()?.Name ?? "";
        string dbCategory = src.Categories.FirstOrDefault()?.Name ?? "";

        // Generic DB categories (strength, powerlifting, etc.) don't tell us the muscle group.
        // Use primary muscle instead so exercises appear under Chest / Back / Legs etc.
        bool isGenericCategory = dbCategory.ToLower() is
            "strength" or "powerlifting" or "olympic weightlifting"
            or "stretching" or "plyometrics" or "";

        string category = isGenericCategory && !string.IsNullOrEmpty(primaryMuscle)
            ? NormalizeCategory(primaryMuscle)
            : NormalizeCategory(!string.IsNullOrEmpty(dbCategory) ? dbCategory : primaryMuscle);

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
            // Chest
            "chest" or "pectorals" or "pectoralis major"                       => "Chest",
            // Back
            "back" or "lats" or "latissimus dorsi" or "middle back"
                or "lower back" or "trapezius" or "traps" or "rhomboids"       => "Back",
            // Shoulders
            "shoulders" or "deltoids" or "front deltoids"
                or "side deltoids" or "rear deltoids"                          => "Shoulders",
            // Arms
            "arms" or "biceps" or "biceps brachii" or "triceps"
                or "forearms" or "brachialis"                                  => "Arms",
            // Legs
            "legs" or "quads" or "quadriceps" or "hamstrings" or "glutes"
                or "calves" or "adductors" or "abductors" or "hip flexors"
                or "glute" or "gluteus maximus"                                => "Legs",
            // Core
            "core" or "abdominals" or "abs" or "obliques"
                or "transverse abdominus"                                      => "Core",
            // Cardio
            "cardio" or "plyometrics"                                          => "Cardio",
            // Flexibility
            "stretching"                                                       => "Flexibility",
            // Catch-all for generic categories — use "Other"
            "strength" or "powerlifting" or "olympic weightlifting"            => "Other",
            _                                                                  => TitleCase(raw)
        };

    private static string TitleCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}
