using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FitForgeAI.Models;

namespace FitForgeAI.Services;

public class EmailSettings
{
    public string ApiKey    { get; set; } = "";
    public string SenderEmail { get; set; } = "";
    public string SenderName  { get; set; } = "ShaktiFit";
}

public class EmailService
{
    private readonly EmailSettings _cfg;
    private readonly ILogger<EmailService> _log;
    private readonly IHttpClientFactory _http;

    public EmailService(IConfiguration config, ILogger<EmailService> log, IHttpClientFactory http)
    {
        _cfg  = config.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
        _log  = log;
        _http = http;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_cfg.ApiKey)
                             && !string.IsNullOrWhiteSpace(_cfg.SenderEmail);

    // ── Core send via Brevo HTTP API ────────────────────────────────────────
    public async Task SendTestAsync(string toEmail, string toName)
    {
        await SendCoreAsync(toEmail, toName,
            "✅ ShaktiFit Email Test",
            "<h2 style='color:#16a34a'>It works! 🎉</h2><p>Your ShaktiFit email is correctly configured.</p>");
    }

    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        if (!IsConfigured)
        {
            _log.LogWarning("Email not configured – skipping send to {Email}", toEmail);
            return false;
        }
        try
        {
            await SendCoreAsync(toEmail, toName, subject, htmlBody);
            _log.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false;
        }
    }

    private async Task SendCoreAsync(string toEmail, string toName, string subject, string html)
    {
        var client = _http.CreateClient();
        client.BaseAddress = new Uri("https://api.brevo.com");
        client.DefaultRequestHeaders.Add("api-key", _cfg.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var payload = new
        {
            sender  = new { name = _cfg.SenderName, email = _cfg.SenderEmail },
            to      = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = html
        };

        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var resp = await client.PostAsync("/v3/smtp/email", content);
        var body = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"Brevo API {(int)resp.StatusCode}: {body}");
    }

    // ── Email templates ─────────────────────────────────────────────────────

    public Task<bool> SendWorkoutReminderAsync(User user, Workout? todaysWorkout)
    {
        string subject = todaysWorkout != null
            ? $"💪 Time for your {todaysWorkout.Type} workout, {user.Name}!"
            : $"🌟 Rest day today, {user.Name} — keep it up!";
        return SendAsync(user.Email, user.Name, subject, BuildReminderHtml(user, todaysWorkout));
    }

    public Task<bool> SendRoutineSummaryAsync(User user, string routineName,
        List<(string Day, string Label, List<WorkoutExercise> Exercises)> plan)
    {
        return SendAsync(user.Email, user.Name,
            $"📋 Your new routine: {routineName}",
            BuildRoutineHtml(user, routineName, plan));
    }

    // ── HTML builders ────────────────────────────────────────────────────────

    private static string BuildReminderHtml(User user, Workout? w)
    {
        string workoutBlock = w != null ? $@"
          <div style='background:#f0fdf4;border:1px solid #86efac;border-radius:12px;padding:20px;margin:20px 0'>
            <div style='font-size:13px;font-weight:700;color:#16a34a;text-transform:uppercase;margin-bottom:8px'>TODAY'S WORKOUT</div>
            <div style='font-size:22px;font-weight:900;color:#0f172a;margin-bottom:4px'>{w.Name}</div>
            <div style='color:#64748b;font-size:13px;margin-bottom:16px'>⏱️ {w.DurationMinutes} min · 🔥 ~{w.EstimatedCalories} kcal · 💪 {w.Exercises.Count} exercises</div>
            <table style='width:100%;border-collapse:collapse;font-size:13px'>
              <tr style='background:#dcfce7'>
                <th style='padding:8px 12px;text-align:left;color:#16a34a'>Exercise</th>
                <th style='padding:8px;text-align:center;color:#16a34a'>Sets</th>
                <th style='padding:8px;text-align:center;color:#16a34a'>Reps</th>
                <th style='padding:8px;text-align:center;color:#16a34a'>Rest</th>
              </tr>
              {string.Join("", w.Exercises.Select((e, i) => $"<tr style='background:{(i % 2 == 0 ? "#fff" : "#f8faf8")}'><td style='padding:9px 12px;font-weight:600'>{e.ExerciseName}</td><td style='padding:9px;text-align:center'>{e.Sets}</td><td style='padding:9px;text-align:center;color:#16a34a;font-weight:700'>{e.Reps}</td><td style='padding:9px;text-align:center;color:#64748b'>{e.RestSeconds}s</td></tr>"))}
            </table>
          </div>
          <a href='https://shaktifit.up.railway.app/Workout' style='display:inline-block;padding:14px 32px;background:#16a34a;color:#fff;font-weight:700;font-size:15px;border-radius:12px;text-decoration:none'>🏋️ Start Workout →</a>"
        : @"<div style='background:#f8faf8;border-radius:12px;padding:24px;text-align:center;margin:20px 0'>
              <div style='font-size:48px'>🌟</div>
              <div style='font-size:18px;font-weight:700;color:#0f172a;margin:12px 0 8px'>Rest Day</div>
              <div style='color:#64748b'>Recovery is part of training. Rest well and come back stronger!</div>
            </div>";

        return Wrapper($@"
          <h2 style='font-size:22px;font-weight:900;color:#0f172a;margin:0 0 6px'>Good morning, {user.Name}! 👋</h2>
          <p style='color:#64748b;margin:0 0 20px'>🔥 {user.WorkoutStreak} day streak — keep it going!</p>
          {workoutBlock}
          <p style='color:#94a3b8;font-size:12px;margin-top:24px'>
            <a href='https://shaktifit.up.railway.app/Settings' style='color:#22c55e'>Manage notification settings</a>
          </p>");
    }

    private static string BuildRoutineHtml(User user, string routineName,
        List<(string Day, string Label, List<WorkoutExercise> Exercises)> plan)
    {
        var days = string.Join("", plan.Select(d => $@"
          <div style='background:#f8faf8;border:1px solid rgba(0,0,0,.07);border-radius:12px;padding:16px;margin-bottom:12px'>
            <div style='font-size:12px;font-weight:800;color:#16a34a;text-transform:uppercase;margin-bottom:8px'>📅 {d.Day} — {d.Label}</div>
            {(d.Exercises.Any()
              ? "<table style='width:100%;border-collapse:collapse;font-size:13px'>" +
                string.Join("", d.Exercises.Select(e => $"<tr style='border-bottom:1px solid rgba(0,0,0,.05)'><td style='padding:8px 0;font-weight:600;color:#0f172a'>{e.ExerciseName}</td><td style='padding:8px;text-align:right;color:#16a34a;font-weight:700'>{e.Sets}×{e.Reps}</td><td style='padding:8px 0;text-align:right;color:#94a3b8;font-size:11px'>{e.RestSeconds}s rest</td></tr>")) + "</table>"
              : "<div style='color:#94a3b8;font-size:12px'>Rest day</div>")}
          </div>"));

        return Wrapper($@"
          <h2 style='font-size:22px;font-weight:900;color:#0f172a;margin:0 0 6px'>Your routine is ready! 🎉</h2>
          <p style='color:#64748b;margin:0 0 20px'>Here's your <strong>{routineName}</strong> plan, {user.Name}.</p>
          {days}
          <a href='https://shaktifit.up.railway.app/Workout' style='display:inline-block;padding:14px 32px;background:#16a34a;color:#fff;font-weight:700;font-size:15px;border-radius:12px;text-decoration:none;margin-top:8px'>📋 View in App →</a>");
    }

    private static string Wrapper(string content) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,""Segoe UI"",sans-serif'>
<table width='100%' cellpadding='0' cellspacing='0' style='padding:32px 16px'>
<tr><td align='center'>
<table width='600' cellpadding='0' cellspacing='0' style='max-width:600px;width:100%'>
  <tr><td style='background:linear-gradient(135deg,#0f1f0f,#1a3a1a,#16a34a);border-radius:16px 16px 0 0;padding:28px 32px;text-align:center'>
    <div style='font-size:32px;margin-bottom:8px'>💪</div>
    <div style='font-size:22px;font-weight:900;color:#f0fdf4'>ShaktiFit</div>
    <div style='font-size:12px;color:rgba(240,253,244,.6);margin-top:4px'>Your Personal Fitness Companion</div>
  </td></tr>
  <tr><td style='background:#ffffff;padding:32px;border-radius:0 0 16px 16px'>{content}</td></tr>
  <tr><td style='padding:20px;text-align:center;font-size:11px;color:#94a3b8'>
    © 2025 ShaktiFit &nbsp;·&nbsp;
    <a href='https://shaktifit.up.railway.app' style='color:#22c55e;text-decoration:none'>Visit App</a>
  </td></tr>
</table>
</td></tr></table>
</body></html>";
}
