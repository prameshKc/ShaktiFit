using FitForgeAI.Services;
using FitForgeAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class DashboardController : Controller
{
    private readonly UserService _users;
    private readonly WorkoutService _workouts;
    private readonly ProgressService _progress;
    private readonly TranslationService _translations;
    private readonly ActivityService _activities;

    private static readonly string[] Quotes = {
        "Discipline beats motivation.", "Progress over perfection.", "Stronger every day.",
        "The only bad workout is the one that didn't happen.",
        "Push harder than yesterday if you want a different tomorrow.",
        "It never gets easier, you just get stronger.",
        "Your body can stand almost anything. It's your mind you have to convince."
    };

    public DashboardController(UserService users, WorkoutService workouts, ProgressService progress, TranslationService translations, ActivityService activities)
    {
        _users = users; _workouts = workouts; _progress = progress; _translations = translations; _activities = activities;
    }

    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return RedirectToAction("Login", "Account");

        var user = await _users.GetByIdAsync(userId);
        if (user == null) return RedirectToAction("Login", "Account");

        var lang = user.Language;
        var vm = new DashboardViewModel
        {
            User = user,
            TodaysWorkout = await _workouts.GetTodaysWorkoutAsync(user),
            WeeklyCompleted = await _workouts.GetWeeklyCompletedAsync(userId),
            WorkoutDaysTarget = user.WorkoutDaysPerWeek,
            RecentWorkouts = await _workouts.GetRecentLogsAsync(userId),
            LatestProgress = await _progress.GetLatestAsync(userId),
            RecentRuns = (await _progress.GetRunningLogsAsync(userId)).Take(5).ToList(),
            WeeklyMileage = await _progress.GetWeeklyMileageAsync(userId),
            MotivationalQuote = Quotes[DateTime.Now.DayOfYear % Quotes.Length],
            Translations = _translations.GetAll(lang),
            Lang = lang
        };
        ViewBag.ActivityStats       = await _activities.GetWeeklyStatsAsync(userId);
        ViewBag.RecentActivities    = (await _activities.GetForUserAsync(userId)).Take(3).ToList();
        ViewBag.ActivitySuggestions = await _activities.GetSuggestionsAsync(userId);
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> CompleteWorkout(string workoutId, int duration)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null) return Unauthorized();

        var workout = await _workouts.GetByIdAsync(workoutId);
        if (workout == null) return NotFound();

        var log = new FitForgeAI.Models.WorkoutLog
        {
            UserId = userId,
            WorkoutId = workoutId,
            WorkoutName = workout.Name,
            DurationMinutes = duration,
            CaloriesBurned = workout.EstimatedCalories
        };
        await _workouts.LogWorkoutAsync(log);
        await _users.IncrementStreakAsync(userId);
        return RedirectToAction("Index");
    }
}
