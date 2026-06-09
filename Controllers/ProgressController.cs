using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class ProgressController : Controller
{
    private readonly ProgressService _progress;
    private readonly TranslationService _translations;
    private readonly UserService _users;

    public ProgressController(ProgressService progress, TranslationService translations, UserService users)
    {
        _progress = progress; _translations = translations; _users = users;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? "";
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Account");
        var entries = await _progress.GetUserProgressAsync(UserId);
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.User = await _users.GetByIdAsync(UserId);
        return View(entries);
    }

    [HttpPost]
    public async Task<IActionResult> LogProgress(double? weight, double? chest, double? waist, double? hips, double? biceps, double? thigh, string? notes)
    {
        if (string.IsNullOrEmpty(UserId)) return Unauthorized();
        var entry = new ProgressEntry
        {
            UserId = UserId,
            WeightKg = weight,
            ChestCm = chest,
            WaistCm = waist,
            HipsCm = hips,
            BicepsCm = biceps,
            ThighCm = thigh,
            Notes = notes ?? ""
        };
        await _progress.LogProgressAsync(entry);
        return RedirectToAction("Index");
    }
}
