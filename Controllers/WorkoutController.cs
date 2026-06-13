using System.Text.Json;
using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class WorkoutController : Controller
{
    private readonly WorkoutService _workouts;
    private readonly UserService _users;
    private readonly TranslationService _translations;
    private readonly ExerciseService _exercises;
    private readonly WorkoutApiService _workoutApi;

    public WorkoutController(WorkoutService workouts, UserService users,
        TranslationService translations, ExerciseService exercises,
        WorkoutApiService workoutApi)
    {
        _workouts = workouts; _users = users; _translations = translations;
        _exercises = exercises; _workoutApi = workoutApi;
    }

    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = await _users.GetByIdAsync(userId);
        var workouts = user != null ? await _workouts.GetForUserAsync(user) : await _workouts.GetAllAsync();

        // Merge in any user-saved custom routines from Split Builder
        var customRoutines = await _workouts.GetUserRoutinesAsync(userId);
        if (customRoutines.Any())
            workouts = customRoutines.Concat(workouts).ToList();

        ViewBag.T = _translations.GetAll(Lang);
        return View(workouts);
    }

    public async Task<IActionResult> Details(string id)
    {
        var workout = await _workouts.GetByIdAsync(id);
        if (workout == null) return NotFound();
        ViewBag.T = _translations.GetAll(Lang);
        return View(workout);
    }

    public async Task<IActionResult> Hybrid()
    {
        var plan = await _workouts.GetHybridPlanAsync();
        ViewBag.T = _translations.GetAll(Lang);
        return View(plan);
    }

    [HttpPost]
    public async Task<IActionResult> SaveRoutine(string planName, string split, string level, int daysPerWeek, string planJson)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        // Parse the plan JSON
        var days = JsonSerializer.Deserialize<List<DayPlan>>(planJson ?? "[]", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<DayPlan>();

        // Create one Workout per active day (days that have exercises)
        var workoutsFile = $"user_routines_{userId}.json";
        var existing = await _workouts.GetUserRoutinesAsync(userId);

        var newWorkouts = days
            .Where(d => d.Exercises.Count > 0)
            .Select(d => new Workout
            {
                Id   = Guid.NewGuid().ToString(),
                Name = $"{planName} – {d.Day}",
                Type = "Strength",
                FitnessLevel = level,
                Description  = $"{split.ToUpper()} split · {d.Label} day",
                DayOfWeek    = d.Day,
                DurationMinutes = d.Exercises.Count * 8 + 10,
                EstimatedCalories = d.Exercises.Count * 45,
                Goals = new List<string> { split, level },
                Exercises = d.Exercises.Select(e => new WorkoutExercise
                {
                    ExerciseId   = e.Id,
                    ExerciseName = e.Name,
                    Sets = e.IsCompound ? 4 : 3,
                    Reps = e.IsCompound ? "6-10" : "10-15",
                    RestSeconds = e.IsCompound ? 90 : 60,
                }).ToList()
            }).ToList();

        existing.AddRange(newWorkouts);
        await _workouts.SaveUserRoutinesAsync(userId, existing);

        TempData["Success"] = $"✅ '{planName}' saved with {newWorkouts.Count} workout days!";
        return RedirectToAction("Index");
    }

    // Split Routine Builder – passes WorkoutAPI exercises as JSON for the JS wizard
    public async Task<IActionResult> Builder()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var apiExercises = await _workoutApi.GetExercisesAsync();

        // Build a slim JSON-friendly list for the view
        var slim = apiExercises.Select(e => new {
            id       = e.Id,
            name     = e.Name,
            category = WorkoutApiService.ToExercise(e).Category,
            muscles  = e.PrimaryMuscles.Select(m => m.Name).ToList(),
            isCompound = e.Types.Any(t => t.Code.Equals("POLYARTICULAR", StringComparison.OrdinalIgnoreCase)),
            level    = e.Types.Any(t => t.Code.Equals("POLYARTICULAR", StringComparison.OrdinalIgnoreCase)) ? "Intermediate" : "Beginner"
        }).ToList();

        ViewBag.ExercisesJson = System.Text.Json.JsonSerializer.Serialize(slim);
        ViewBag.T = _translations.GetAll(Lang);
        return View();
    }
}
