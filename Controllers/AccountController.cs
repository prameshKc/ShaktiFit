using FitForgeAI.Models;
using FitForgeAI.Services;
using FitForgeAI.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class AccountController : Controller
{
    private readonly UserService _users;

    public AccountController(UserService users) => _users = users;

    [HttpGet] public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.AuthenticateAsync(model.Email, model.Password);
        if (user == null) { ModelState.AddModelError("", "Invalid email or password."); return View(model); }

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("Lang", user.Language);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet] public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        if (await _users.EmailExistsAsync(model.Email))
        { ModelState.AddModelError("Email", "Email already registered."); return View(model); }

        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Age = model.Age ?? 0,
            Gender = model.Gender,
            HeightCm = model.Height ?? 170,
            WeightKg = model.Weight ?? 70,
            FitnessLevel = model.FitnessLevel,
            Goals = model.Goals,
            WorkoutDaysPerWeek = model.WorkoutDaysPerWeek,
            HasGymAccess = model.HasGymAccess,
            PrefersHomeWorkout = model.PrefersHomeWorkout
        };
        await _users.CreateAsync(user, model.Password);
        HttpContext.Session.SetString("UserId", user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("Lang", user.Language);
        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
