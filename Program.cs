using FitForgeAI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromHours(8); o.Cookie.HttpOnly = true; });
builder.Services.AddSingleton<IJsonStorageService, JsonStorageService>();
builder.Services.AddSingleton<TranslationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ExerciseService>();
builder.Services.AddScoped<WorkoutService>();
builder.Services.AddScoped<ProgressService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
