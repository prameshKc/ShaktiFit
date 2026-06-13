using FitForgeAI.Models;

namespace FitForgeAI.Services;

/// <summary>
/// Background service that runs daily and sends workout reminder emails
/// to all users who have email reminders enabled.
/// </summary>
public class WorkoutReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<WorkoutReminderService> _log;
    private readonly int _reminderHour;

    public WorkoutReminderService(IServiceProvider services,
        IConfiguration config, ILogger<WorkoutReminderService> log)
    {
        _services     = services;
        _log          = log;
        _reminderHour = config.GetValue<int>("Email:ReminderHour", 7);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _log.LogInformation("WorkoutReminderService started. Reminder hour: {Hour}:00 UTC", _reminderHour);

        while (!ct.IsCancellationRequested)
        {
            var now  = DateTime.UtcNow;
            // Calculate next fire time (next occurrence of _reminderHour:00 UTC)
            var next = new DateTime(now.Year, now.Month, now.Day, _reminderHour, 0, 0, DateTimeKind.Utc);
            if (next <= now) next = next.AddDays(1);

            var delay = next - now;
            _log.LogInformation("Next reminder batch in {Hours:0.0}h (at {Next:HH:mm} UTC)",
                delay.TotalHours, next);

            try { await Task.Delay(delay, ct); }
            catch (TaskCanceledException) { break; }

            await SendDailyRemindersAsync(ct);
        }
    }

    private async Task SendDailyRemindersAsync(CancellationToken ct)
    {
        _log.LogInformation("Sending daily workout reminders…");
        try
        {
            using var scope = _services.CreateScope();
            var userService    = scope.ServiceProvider.GetRequiredService<UserService>();
            var workoutService = scope.ServiceProvider.GetRequiredService<WorkoutService>();
            var emailService   = scope.ServiceProvider.GetRequiredService<EmailService>();

            if (!emailService.IsConfigured)
            {
                _log.LogWarning("Email not configured — skipping reminder batch");
                return;
            }

            var allUsers = await userService.GetAllAsync();
            var today    = DateTime.UtcNow.DayOfWeek.ToString()[..3]; // "Mon","Tue",…

            int sent = 0;
            foreach (var user in allUsers)
            {
                if (ct.IsCancellationRequested) break;
                if (!user.EmailRemindersEnabled) continue;
                if (string.IsNullOrWhiteSpace(user.Email)) continue;

                // Check if today is a reminder day for this user
                var days = user.ReminderDays.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!days.Any(d => d.Trim().StartsWith(today, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var todaysWorkout = await workoutService.GetTodaysWorkoutAsync(user);
                var ok = await emailService.SendWorkoutReminderAsync(user, todaysWorkout);
                if (ok) sent++;

                await Task.Delay(300, ct); // throttle: max ~3 emails/sec
            }

            _log.LogInformation("Reminder batch done — {Count} emails sent", sent);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error in daily reminder batch");
        }
    }
}
