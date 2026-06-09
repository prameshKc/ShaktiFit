using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class SettingsController : Controller
{
    private readonly UserService _users;
    private readonly TranslationService _translations;

    public SettingsController(UserService users, TranslationService translations)
    {
        _users = users; _translations = translations;
    }

    private string UserId => HttpContext.Session.GetString("UserId") ?? "";
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public async Task<IActionResult> Index()
    {
        if (string.IsNullOrEmpty(UserId)) return RedirectToAction("Login", "Account");
        var user = await _users.GetByIdAsync(UserId);
        ViewBag.T = _translations.GetAll(Lang);
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProfile(string name, int age, double height, double weight,
        string fitnessLevel, List<string> goals, int workoutDays, bool gymAccess, string language, string theme)
    {
        if (string.IsNullOrEmpty(UserId)) return Unauthorized();
        var user = await _users.GetByIdAsync(UserId);
        if (user == null) return NotFound();

        user.Name = name; user.Age = age; user.HeightCm = height;
        user.WeightKg = weight; user.FitnessLevel = fitnessLevel;
        user.Goals = goals; user.WorkoutDaysPerWeek = workoutDays;
        user.HasGymAccess = gymAccess; user.Language = language; user.Theme = theme;
        await _users.UpdateAsync(user);

        HttpContext.Session.SetString("Lang", language);
        HttpContext.Session.SetString("UserName", name);
        TempData["Success"] = "Settings saved!";
        return RedirectToAction("Index");
    }
}
