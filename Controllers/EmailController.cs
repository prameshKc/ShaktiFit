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

    public EmailController(EmailService email, UserService users, WorkoutService workouts)
    {
        _email = email; _users = users; _workouts = workouts;
    }

    private string? UserId => HttpContext.Session.GetString("UserId");

    // POST /Email/SendReminder  – send today's workout reminder right now
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

        var todaysWorkout = await _workouts.GetTodaysWorkoutAsync(user);
        var ok = await _email.SendWorkoutReminderAsync(user, todaysWorkout);

        TempData[ok ? "EmailSuccess" : "EmailError"] = ok
            ? $"✅ Reminder sent to {user.Email}!"
            : "❌ Email failed — check SMTP settings.";

        return RedirectToAction("Index", "Dashboard");
    }

    // POST /Email/SendRoutine  – send the just-saved routine as email
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

        var ok = await _email.SendRoutineSummaryAsync(user, routineName, plan);

        TempData[ok ? "EmailSuccess" : "EmailError"] = ok
            ? $"✅ Routine emailed to {user.Email}!"
            : "❌ Email failed — check SMTP settings in Settings.";

        return RedirectToAction("Index", "Workout");
    }

    // POST /Email/SavePreferences – toggle reminders + set time
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
        user.ReminderDays          = string.IsNullOrWhiteSpace(reminderDays) ? "Mon,Tue,Wed,Thu,Fri" : reminderDays;
        await _users.UpdateAsync(user);

        TempData["Success"] = emailRemindersEnabled
            ? $"✅ Reminders enabled — you'll get emails at {reminderHour}:00 UTC on {user.ReminderDays}"
            : "Reminders disabled.";
        return RedirectToAction("Index", "Settings");
    }
}
