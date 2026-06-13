using System.Text.Json;
using FitForgeAI.Models;
using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class EmailController : Controller
{
    private readonly EmailService   _email;
    private readonly UserService    _users;
    private readonly WorkoutService _workouts;
    private readonly ILogger<EmailController> _log;

    public EmailController(EmailService email, UserService users,
        WorkoutService workouts, ILogger<EmailController> log)
    {
        _email = email; _users = users; _workouts = workouts; _log = log;
    }

    private string? UserId => HttpContext.Session.GetString("UserId");

    // POST /Email/SendReminder
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder()
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        var user = await _users.GetByIdAsync(UserId);
        if (user == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["EmailError"] = "No email address on your account.";
            return RedirectToAction("Index", "Dashboard");
        }

        if (!_email.IsConfigured)
        {
            TempData["EmailError"] = "⚠️ Email not configured yet — add SMTP credentials in Railway environment variables.";
            return RedirectToAction("Index", "Dashboard");
        }

        // Fire-and-forget so the page doesn't hang on SMTP
        var todaysWorkout = await _workouts.GetTodaysWorkoutAsync(user);
        _ = Task.Run(async () =>
        {
            try { await _email.SendWorkoutReminderAsync(user, todaysWorkout); }
            catch (Exception ex) { _log.LogError(ex, "Background reminder send failed"); }
        });

        TempData["EmailSuccess"] = $"📧 Sending to {user.Email}… check your inbox in a moment!";
        return RedirectToAction("Index", "Dashboard");
    }

    // POST /Email/SendRoutine
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendRoutine(string routineName, string planJson)
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        var user = await _users.GetByIdAsync(UserId);
        if (user == null) return RedirectToAction("Login", "Account");

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            TempData["EmailError"] = "No email address on your account.";
            return RedirectToAction("Index", "Workout");
        }

        if (!_email.IsConfigured)
        {
            TempData["EmailError"] = "⚠️ Email not configured — add SMTP credentials in Railway environment variables.";
            return RedirectToAction("Index", "Workout");
        }

        var days = JsonSerializer.Deserialize<List<DayPlan>>(planJson ?? "[]",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new List<DayPlan>();

        var plan = days.Select(d => (
            Day:       d.Day,
            Label:     d.Label,
            Exercises: d.Exercises.Select(e => new WorkoutExercise
            {
                ExerciseName = e.Name,
                Sets         = e.IsCompound ? 4 : 3,
                Reps         = e.IsCompound ? "6-10" : "10-15",
                RestSeconds  = e.IsCompound ? 90 : 60
            }).ToList()
        )).ToList();

        // Fire-and-forget
        _ = Task.Run(async () =>
        {
            try { await _email.SendRoutineSummaryAsync(user, routineName, plan); }
            catch (Exception ex) { _log.LogError(ex, "Background routine email failed"); }
        });

        TempData["EmailSuccess"] = $"📧 Sending routine to {user.Email}… check your inbox!";
        return RedirectToAction("Index", "Workout");
    }

    // POST /Email/SavePreferences
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePreferences(bool emailRemindersEnabled,
        int reminderHour, string reminderDays)
    {
        if (UserId == null) return RedirectToAction("Login", "Account");
        var user = await _users.GetByIdAsync(UserId);
        if (user == null) return RedirectToAction("Login", "Account");

        user.EmailRemindersEnabled = emailRemindersEnabled;
        user.ReminderHour          = Math.Clamp(reminderHour, 0, 23);
        user.ReminderDays          = string.IsNullOrWhiteSpace(reminderDays)
                                     ? "Mon,Tue,Wed,Thu,Fri" : reminderDays;
        await _users.UpdateAsync(user);

        TempData["Success"] = emailRemindersEnabled
            ? $"✅ Daily reminders enabled at {reminderHour:D2}:00 UTC on {user.ReminderDays}"
            : "Reminders disabled.";
        return RedirectToAction("Index", "Settings");
    }
}
