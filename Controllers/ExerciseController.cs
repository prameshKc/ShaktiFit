using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class ExerciseController : Controller
{
    private readonly ExerciseService _exercises;
    private readonly TranslationService _translations;
    private readonly WorkoutApiService _workoutApi;

    public ExerciseController(
        ExerciseService exercises,
        TranslationService translations,
        WorkoutApiService workoutApi)
    {
        _exercises   = exercises;
        _translations = translations;
        _workoutApi  = workoutApi;
    }

    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index(string? category, string? search)
    {
        // Load API exercises (cached after first call)
        var apiTask = _workoutApi.GetExercisesAsync();
        await apiTask;

        // Use API exercises only
        var apiExercises = (await apiTask)
            .Select(WorkoutApiService.ToExercise)
            .ToList();

        // Filter if needed
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            apiExercises = apiExercises.Where(e =>
                e.Name.ToLower().Contains(q) ||
                e.Category.ToLower().Contains(q) ||
                e.TargetMuscles.Any(m => m.ToLower().Contains(q))).ToList();
        }
        else if (!string.IsNullOrEmpty(category) && category != "All")
        {
            apiExercises = apiExercises.Where(e => e.Category == category).ToList();
        }

        var merged = apiExercises;

        var categories = (await apiTask)
            .Select(WorkoutApiService.ToExercise)
            .Select(e => e.Category).Distinct().OrderBy(c => c).ToList();

        // All API ids are GUIDs — no need for a name→id map
        var apiIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category ?? "All";
        ViewBag.Search = search;
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ApiIdByName = apiIdByName;
        return View(merged);
    }

    public async Task<IActionResult> Details(string id)
    {
        // Try local JSON first
        var exercise = await _exercises.GetByIdAsync(id);

        if (exercise == null)
        {
            // Try WorkoutAPI
            var apiEx = await _workoutApi.GetExerciseByIdAsync(id);
            if (apiEx != null)
            {
                exercise = WorkoutApiService.ToExercise(apiEx);
                ViewBag.ApiExercise = apiEx;
            }
        }

        // If it's a local exercise, check if WorkoutAPI has an image for it by name match
        if (exercise != null && ViewBag.ApiExercise == null)
        {
            var all = await _workoutApi.GetExercisesAsync();
            // Try exact match first, then fuzzy (API name contains local name or vice versa)
            var localLower = exercise.Name.ToLower();
            var match = all.FirstOrDefault(e =>
                string.Equals(e.Name, exercise.Name, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(e => e.Name.ToLower().Contains(localLower))
                ?? all.FirstOrDefault(e => localLower.Contains(e.Name.ToLower()));
            if (match != null)
                ViewBag.ApiExercise = match;
        }

        if (exercise == null) return NotFound();

        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.Lang = Lang;
        return View(exercise);
    }

    // ── Proxy SVG image from WorkoutAPI (keeps API key server-side) ──────────
    [ResponseCache(Duration = 86400)] // cache 24 h in browser
    public async Task<IActionResult> Image(string id)
    {
        var svg = await _workoutApi.GetExerciseImageAsync(id);
        if (svg == null) return NotFound();
        return Content(svg, "image/svg+xml");
    }
}
