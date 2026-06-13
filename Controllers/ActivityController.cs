using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class ActivityController : Controller
{
    private readonly ActivityService _activities;
    private readonly TranslationService _translations;
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public ActivityController(ActivityService activities, TranslationService translations)
    {
        _activities = activities; _translations = translations;
    }

    private string? UserId => HttpContext.Session.GetString("UserId");

    // GET /Activity
    public async Task<IActionResult> Index()
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        var activities = await _activities.GetForUserAsync(UserId);
        var stats      = await _activities.GetWeeklyStatsAsync(UserId);
        var suggestions = await _activities.GetSuggestionsAsync(UserId);
        ViewBag.Stats       = stats;
        ViewBag.Suggestions = suggestions;
        ViewBag.ActiveNav   = "activity";
        ViewBag.T = _translations.GetAll(Lang);
        return View(activities);
    }

    // GET /Activity/Log
    public IActionResult Log()
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        ViewBag.ActiveNav = "activity";
        ViewBag.T = _translations.GetAll(Lang);
        return View();
    }

    // POST /Activity/Log
    [HttpPost]
    public async Task<IActionResult> Log(string type, DateTime date, double? distanceKm,
        int durationMinutes, int? caloriesBurned, int? heartRateAvg,
        string intensity, string? notes, string? title)
    {
        if (UserId == null) return RedirectToAction("Login", "Account");

        var calories = caloriesBurned ?? ActivityService.EstimateCalories(type, durationMinutes, intensity);

        var activity = new Activity
        {
            UserId          = UserId,
            Type            = type,
            Date            = date == default ? DateTime.Now : date,
            DistanceKm      = distanceKm,
            DurationMinutes = durationMinutes,
            CaloriesBurned  = calories,
            HeartRateAvg    = heartRateAvg,
            Intensity       = intensity,
            Notes           = notes,
            Title           = title
        };

        await _activities.AddAsync(activity);
        TempData["Success"] = "Activity logged successfully!";
        return RedirectToAction("Index");
    }

    // POST /Activity/Delete
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        await _activities.DeleteAsync(id, UserId);
        return RedirectToAction("Index");
    }
}
