using FitForgeAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway / cloud hosting: listen on $PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // for EmailService (Brevo API)

// Google OAuth — DefaultScheme only (no global DefaultChallengeScheme to avoid redirect loops)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies", o =>
{
    o.Cookie.Name = "ShaktiFit.OAuth";
    o.ExpireTimeSpan = TimeSpan.FromMinutes(10); // short-lived, just for OAuth handshake
})
.AddGoogle("Google", options =>
{
    options.ClientId     = builder.Configuration["Google:ClientId"]     ?? "";
    options.ClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
    options.CallbackPath = "/signin-google";
    options.SignInScheme = "Cookies"; // store Google result in the temp cookie
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
