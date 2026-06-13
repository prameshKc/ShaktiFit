using FitForgeAI.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace FitForgeAI.Services;

public class EmailSettings
{
    public string SmtpHost    { get; set; } = "smtp.gmail.com";
    public int    SmtpPort    { get; set; } = 587;
    public string SenderEmail { get; set; } = "";
    public string SenderName  { get; set; } = "ShaktiFit";
    public string Password    { get; set; } = "";
    public bool   EnableReminders { get; set; } = true;
    public int    ReminderHour    { get; set; } = 7;
}

public class EmailService
{
    private readonly EmailSettings _cfg;
    private readonly ILogger<EmailService> _log;

    public EmailService(IConfiguration config, ILogger<EmailService> log)
    {
        _cfg = config.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        _log = log;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.SenderEmail)
                             && !string.IsNullOrWhiteSpace(_cfg.Password);

    // ── Test send (throws on error, used by /Email/Test) ───────────────────
    public async Task SendTestAsync(string toEmail, string toName)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_cfg.SenderName, _cfg.SenderEmail));
        msg.To.Add(new MailboxAddress(toName, toEmail));
        msg.Subject = "✅ ShaktiFit Email Test";
        msg.Body = new TextPart(TextFormat.Html)
            { Text = "<h2 style='color:#16a34a'>It works! 🎉</h2><p>Your ShaktiFit email is correctly configured.</p>" };

        using var cts  = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var smtp = new SmtpClient();
        _log.LogInformation("Connecting to {Host}:{Port}", _cfg.SmtpHost, _cfg.SmtpPort);
        await smtp.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, SecureSocketOptions.StartTls, cts.Token);
        _log.LogInformation("Authenticating as {Email}", _cfg.SenderEmail);
        await smtp.AuthenticateAsync(_cfg.SenderEmail, _cfg.Password, cts.Token);
        _log.LogInformation("Sending to {To}", toEmail);
        await smtp.SendAsync(msg, cts.Token);
        await smtp.DisconnectAsync(true, cts.Token);
    }

    // ── Send any HTML email ─────────────────────────────────────────────────
    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            _log.LogWarning("Email not configured – skipping send to {Email}", toEmail);
            return false;
        }
        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_cfg.SenderName, _cfg.SenderEmail));
            msg.To.Add(new MailboxAddress(toName, toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var cts  = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, SecureSocketOptions.StartTls, cts.Token);
            await smtp.AuthenticateAsync(_cfg.SenderEmail, _cfg.Password, cts.Token);
            await smtp.SendAsync(msg, cts.Token);
            await smtp.DisconnectAsync(true, cts.Token);
            _log.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    // ── Workout Reminder ────────────────────────────────────────────────────
    public Task<bool> SendWorkoutReminderAsync(User user, Workout? todaysWorkout)
    {
        string subject = todaysWorkout != null
            ? $"💪 Time for your {todaysWorkout.Type} workout, {user.Name}!"
            : $"🌟 Rest day today, {user.Name} — keep it up!";

        string html = BuildReminderHtml(user, todaysWorkout);
        return SendAsync(user.Email, user.Name, subject, html);
    }

    // ── Routine Summary ─────────────────────────────────────────────────────
    public Task<bool> SendRoutineSummaryAsync(User user, string routineName,
        List<(string Day, string Label, List<WorkoutExercise> Exercises)> plan)
    {
        string subject = $"📋 Your new routine is ready: {routineName}";
        string html = BuildRoutineHtml(user, routineName, plan);
        return SendAsync(user.Email, user.Name, subject, html);
    }

    // ── HTML builders ────────────────────────────────────────────────────────

    private static string BuildReminderHtml(User user, Workout? w)
    {
        string workoutBlock = w != null ? $@"
          <div style='background:#f0fdf4;border:1px solid #86efac;border-radius:12px;padding:20px;margin:20px 0'>
            <div style='font-size:13px;font-weight:700;color:#16a34a;text-transform:uppercase;letter-spacing:.08em;margin-bottom:8px'>
              TODAY'S WORKOUT
            </div>
            <div style='font-size:22px;font-weight:900;color:#0f172a;margin-bottom:4px'>{w.Name}</div>
            <div style='color:#64748b;font-size:13px;margin-bottom:16px'>
              ⏱️ {w.DurationMinutes} min &nbsp;·&nbsp; 🔥 ~{w.EstimatedCalories} kcal &nbsp;·&nbsp; 💪 {w.Exercises.Count} exercises
            </div>
            <table style='width:100%;border-collapse:collapse;font-size:13px'>
              <tr style='background:#dcfce7'>
                <th style='padding:8px 12px;text-align:left;color:#16a34a;font-weight:700'>Exercise</th>
                <th style='padding:8px 12px;text-align:center;color:#16a34a;font-weight:700'>Sets</th>
                <th style='padding:8px 12px;text-align:center;color:#16a34a;font-weight:700'>Reps</th>
                <th style='padding:8px 12px;text-align:center;color:#16a34a;font-weight:700'>Rest</th>
              </tr>
              {string.Join("", w.Exercises.Select((e, i) => $@"
              <tr style='background:{(i%2==0?"#fff":"#f8faf8")}'>
                <td style='padding:9px 12px;font-weight:600;color:#0f172a'>{e.ExerciseName}</td>
                <td style='padding:9px 12px;text-align:center;color:#374151'>{e.Sets}</td>
                <td style='padding:9px 12px;text-align:center;font-weight:700;color:#16a34a'>{e.Reps}</td>
                <td style='padding:9px 12px;text-align:center;color:#64748b'>{e.RestSeconds}s</td>
              </tr>"))}
            </table>
          </div>
          <a href='https://shaktifit.up.railway.app/Workout' style='display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#22c55e,#16a34a);color:#fff;font-weight:700;font-size:15px;border-radius:12px;text-decoration:none;margin-bottom:20px'>
            🏋️ Start Workout →
          </a>" :
          $@"<div style='background:#f8faf8;border-radius:12px;padding:24px;text-align:center;margin:20px 0'>
              <div style='font-size:48px;margin-bottom:12px'>🌟</div>
              <div style='font-size:18px;font-weight:700;color:#0f172a;margin-bottom:8px'>Rest Day</div>
              <div style='color:#64748b;font-size:14px'>Recovery is part of training. Rest well and come back stronger!</div>
            </div>";

        return Wrapper(user, $@"
          <h2 style='font-size:22px;font-weight:900;color:#0f172a;margin:0 0 6px'>
            Good morning, {user.Name}! 👋
          </h2>
          <p style='color:#64748b;font-size:14px;margin:0 0 20px'>
            🔥 {user.WorkoutStreak} day streak — keep it going!
          </p>
          {workoutBlock}
          <p style='color:#94a3b8;font-size:12px;margin-top:24px'>
            You're receiving this because you enabled workout reminders in ShaktiFit.<br>
            <a href='https://shaktifit.up.railway.app/Settings' style='color:#22c55e'>Manage notification settings</a>
          </p>
        ");
    }

    private static string BuildRoutineHtml(User user, string routineName,
        List<(string Day, string Label, List<WorkoutExercise> Exercises)> plan)
    {
        string daysHtml = string.Join("", plan.Select(d => $@"
          <div style='background:#f8faf8;border:1px solid rgba(0,0,0,.07);border-radius:12px;padding:16px;margin-bottom:12px'>
            <div style='font-size:12px;font-weight:800;color:#16a34a;text-transform:uppercase;letter-spacing:.08em;margin-bottom:8px'>
              📅 {d.Day} — {d.Label}
            </div>
            {(d.Exercises.Any() ?
              $@"<table style='width:100%;border-collapse:collapse;font-size:13px'>
                {string.Join("", d.Exercises.Select((e,i) => $@"
                <tr style='border-bottom:1px solid rgba(0,0,0,.05)'>
                  <td style='padding:8px 0;font-weight:600;color:#0f172a'>{e.ExerciseName}</td>
                  <td style='padding:8px 12px;text-align:right;color:#16a34a;font-weight:700'>{e.Sets}×{e.Reps}</td>
                  <td style='padding:8px 0;text-align:right;color:#94a3b8;font-size:11px'>{e.RestSeconds}s rest</td>
                </tr>"))}
              </table>" :
              "<div style='color:#94a3b8;font-size:12px'>Rest day</div>")}
          </div>"));

        return Wrapper(user, $@"
          <h2 style='font-size:22px;font-weight:900;color:#0f172a;margin:0 0 6px'>
            Your routine is ready! 🎉
          </h2>
          <p style='color:#64748b;font-size:14px;margin:0 0 20px'>
            Here's your <strong>{routineName}</strong> split routine, {user.Name}.
          </p>
          {daysHtml}
          <a href='https://shaktifit.up.railway.app/Workout' style='display:inline-block;padding:14px 32px;background:linear-gradient(135deg,#22c55e,#16a34a);color:#fff;font-weight:700;font-size:15px;border-radius:12px;text-decoration:none;margin:16px 0'>
            📋 View in App →
          </a>
          <p style='color:#94a3b8;font-size:12px;margin-top:16px'>
            <a href='https://shaktifit.up.railway.app/Settings' style='color:#22c55e'>Manage email settings</a>
          </p>
        ");
    }

    private static string Wrapper(User user, string content) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",sans-serif'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:32px 16px'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%'>

        <!-- Header -->
        <tr><td style='background:linear-gradient(135deg,#0f1f0f,#1a3a1a,#16a34a);border-radius:16px 16px 0 0;padding:28px 32px;text-align:center'>
          <div style='font-size:32px;margin-bottom:8px'>💪</div>
          <div style='font-size:22px;font-weight:900;color:#f0fdf4;letter-spacing:-.02em'>ShaktiFit</div>
          <div style='font-size:12px;color:rgba(240,253,244,.6);margin-top:4px'>Your Personal Fitness Companion</div>
        </td></tr>

        <!-- Body -->
        <tr><td style='background:#ffffff;padding:32px;border-radius:0 0 16px 16px'>
          {content}
        </td></tr>

        <!-- Footer -->
        <tr><td style='padding:20px;text-align:center;font-size:11px;color:#94a3b8'>
          © 2025 ShaktiFit &nbsp;·&nbsp;
          <a href='https://shaktifit.up.railway.app' style='color:#22c55e;text-decoration:none'>Visit App</a>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
}
