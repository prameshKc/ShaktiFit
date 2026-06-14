using FitForgeAI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Railway / cloud hosting: listen on $PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // for EmailService (Brevo API)

// Persist Data Protection keys so OAuth state survives Railway redeployments
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "Data", "Keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir))
    .SetApplicationName("ShaktiFit");

// Google OAuth — DefaultScheme only (no global DefaultChallengeScheme to avoid redirect loops)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies", o =>
{
    o.Cookie.Name = "ShaktiFit.OAuth";
    o.Cookie.SameSite = SameSiteMode.None;       // needed for cross-site OAuth redirect
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(15);
})
.AddGoogle("Google", options =>
{
    options.ClientId     = builder.Configuration["Google:ClientId"]     ?? "";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SignInScheme = "Cookies";
    // Correlation cookie must be SameSite=None so browser sends it back after Google redirect
    options.CorrelationCookie.SameSite = SameSiteMode.None;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    options.Scope.Add("email");
    options.Scope.Add("profile");
});
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromHours(8); o.Cookie.HttpOnly = true; });
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IJsonStorageService, JsonStorageService>();
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<ActivityService>();

// Email service
builder.Services.AddSingleton<EmailService>();
builder.Services.AddHostedService<WorkoutReminderService>();

// WorkoutAPI integration
var workoutApiBase = builder.Configuration["WorkoutApi:BaseUrl"] ?? "https://api.workoutapi.com";
var workoutApiKey  = builder.Configuration["WorkoutApi:ApiKey"] ?? "";
builder.Services.AddHttpClient<WorkoutApiService>(client =>
{
    client.BaseAddress = new Uri(workoutApiBase);
    client.DefaultRequestHeaders.Add("x-api-key", workoutApiKey);
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Tell ASP.NET Core it's behind Railway's HTTPS reverse proxy
// Without this, OAuth correlation cookies are set for HTTP → state mismatch
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Show detailed errors in production temporarily for debugging
    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async ctx =>
        {
            var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            ctx.Response.ContentType = "text/plain";
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsync($"Error: {ex?.Error?.Message}\n\n{ex?.Error?.StackTrace}");
        });
    });
    app.UseHsts();
}

if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
