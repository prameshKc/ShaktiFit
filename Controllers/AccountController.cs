using System.Security.Claims;
using FitForgeAI.Models;
using FitForgeAI.Services;
using FitForgeAI.ViewModels;
using Microsoft.AspNetCore.Authentication;
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

    // ── Google OAuth ─────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action("GoogleCallback", "Account"),
            Items = { ["returnUrl"] = returnUrl ?? "/Dashboard" }
        };
        return Challenge(props, "Google");
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback()
    {
        // Read the result from Google
        var result = await HttpContext.AuthenticateAsync("Cookies");
        if (!result.Succeeded)
        {
            TempData["Error"] = "Google login failed. Please try again.";
            return RedirectToAction("Login");
        }

        var claims    = result.Principal?.Claims.ToList() ?? new List<Claim>();
        var email     = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? "";
        var name      = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "User";
        var googleId  = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
        var avatarUrl = claims.FirstOrDefault(c => c.Type == "urn:google:picture")?.Value ?? "";

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Could not get email from Google.";
            return RedirectToAction("Login");
        }

        // Find existing user or create new one
        var user = await _users.GetByEmailAsync(email);
        if (user == null)
        {
            // Auto-register with Google account
            user = new User
            {
                Name     = name,
                Email    = email,
                Language = "en",
                FitnessLevel = "Beginner",
                HeightCm = 170,
                WeightKg = 70,
                Goals    = new List<string>(),
            };
            // Create without password (Google users won't use password login)
            await _users.CreateGoogleUserAsync(user);
        }

        // Sign in: set our session exactly like normal login
        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);
        HttpContext.Session.SetString("UserId",   user.Id);
        HttpContext.Session.SetString("UserName", user.Name);
        HttpContext.Session.SetString("Lang",     user.Language);

        // Clean up the temp Google cookie
        await HttpContext.SignOutAsync("Cookies");

        return RedirectToAction("Index", "Dashboard");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
