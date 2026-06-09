using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class WorkoutController : Controller
{
    private readonly WorkoutService _workouts;
    private readonly UserService _users;
    private readonly TranslationService _translations;
    private readonly ExerciseService _exercises;

    public WorkoutController(WorkoutService workouts, UserService users, TranslationService translations, ExerciseService exercises)
    {
        _workouts = workouts; _users = users; _translations = translations; _exercises = exercises;
    }

    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = await _users.GetByIdAsync(userId);
        var workouts = user != null ? await _workouts.GetForUserAsync(user) : await _workouts.GetAllAsync();

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

    public async Task<IActionResult> Builder()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");
        var exercises = await _exercises.GetAllAsync();
        ViewBag.T = _translations.GetAll(Lang);
        return View(exercises);
    }
}
