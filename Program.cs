using FitForgeAI.Services;

var builder = WebApplication.CreateBuilder(args);

// Railway / cloud hosting: listen on $PORT env var
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromHours(8); o.Cookie.HttpOnly = true; });
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IJsonStorageService, JsonStorageService>();
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<ActivityService>();

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
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
