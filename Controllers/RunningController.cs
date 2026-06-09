using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class RunningController : Controller
{
    private readonly ProgressService _progress;
    private readonly TranslationService _translations;

    public RunningController(ProgressService progress, TranslationService translations)
    {
        _progress = progress; _translations = translations;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? "";
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Account");
        var logs = await _progress.GetRunningLogsAsync(UserId);
        var pb = await _progress.GetPersonalBest5KAsync(UserId);
        var weekly = await _progress.GetWeeklyMileageAsync(UserId);

        ViewBag.Logs = logs;
        ViewBag.PersonalBest = pb;
        ViewBag.WeeklyMileage = weekly;
        ViewBag.T = _translations.GetAll(Lang);
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> LogRun(double distance, int duration, string runType, string? notes)
    {
        if (string.IsNullOrEmpty(UserId)) return Unauthorized();
        var log = new RunningLog
        {
            UserId = UserId,
            DistanceKm = distance,
            DurationMinutes = duration,
            PaceMinPerKm = ProgressService.CalculatePace(distance, duration),
            RunType = runType ?? "Easy",
            Notes = notes ?? ""
        };
        await _progress.LogRunAsync(log);
        return RedirectToAction("Index");
    }

    public IActionResult PaceCalculator() => View();
}
