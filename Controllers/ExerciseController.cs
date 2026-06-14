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
        _exercises    = exercises;
        _translations = translations;
        _workoutApi   = workoutApi;
    }

    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index(string? category, string? search)
    {
        var all = await _workoutApi.GetExercisesAsync();
        var exercises = all.Select(WorkoutApiService.ToExercise).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            exercises = exercises.Where(e =>
                e.Name.ToLower().Contains(q) ||
                e.Category.ToLower().Contains(q) ||
                e.TargetMuscles.Any(m => m.ToLower().Contains(q))).ToList();
        }
        else if (!string.IsNullOrEmpty(category) && category != "All")
        {
            exercises = exercises.Where(e => e.Category == category).ToList();
        }

        var categories = all.Select(WorkoutApiService.ToExercise)
            .Select(e => e.Category).Distinct().OrderBy(c => c).ToList();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategory = category ?? "All";
        ViewBag.Search = search;
        ViewBag.T = _translations.GetAll(Lang);
        return View(exercises);
    }

    public async Task<IActionResult> Details(string id)
    {
        var apiEx = await _workoutApi.GetExerciseByIdAsync(id);
        if (apiEx != null)
        {
            var exercise = WorkoutApiService.ToExercise(apiEx);
            ViewBag.ApiExercise = apiEx;
            ViewBag.T = _translations.GetAll(Lang);
            ViewBag.Lang = Lang;
            return View(exercise);
        }

        // Fallback: local exercise
        var local = await _exercises.GetByIdAsync(id);
        if (local == null) return NotFound();

        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.Lang = Lang;
        return View(local);
    }

    // Legacy image proxy — no longer used (images are direct GitHub URLs)
    [ResponseCache(Duration = 86400)]
    public IActionResult Image(string id) => NotFound();
}
