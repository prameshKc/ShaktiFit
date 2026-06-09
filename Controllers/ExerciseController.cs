using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class ExerciseController : Controller
{
    private readonly ExerciseService _exercises;
    private readonly TranslationService _translations;

    public ExerciseController(ExerciseService exercises, TranslationService translations)
    {
        _exercises = exercises; _translations = translations;
    }

    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index(string? category, string? search)
    {
        var exercises = string.IsNullOrWhiteSpace(search)
            ? await _exercises.GetByCategoryAsync(category ?? "All")
            : await _exercises.SearchAsync(search);

        ViewBag.Categories = await _exercises.GetCategoriesAsync();
        ViewBag.SelectedCategory = category ?? "All";
        ViewBag.Search = search;
        ViewBag.T = _translations.GetAll(Lang);
        return View(exercises);
    }

    public async Task<IActionResult> Details(string id)
    {
        var exercise = await _exercises.GetByIdAsync(id);
        if (exercise == null) return NotFound();
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.Lang = Lang;
        return View(exercise);
    }
}
