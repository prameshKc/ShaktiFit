using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class HomeController : Controller
{
    private readonly TranslationService _translations;
    public HomeController(TranslationService translations) => _translations = translations;

    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("UserId") != null)
            return RedirectToAction("Index", "Dashboard");
        var lang = HttpContext.Session.GetString("Lang") ?? "en";
        ViewBag.T = _translations.GetAll(lang);
        return View();
    }

    public IActionResult GuestSuggest(string goal, string level)
    {
        var lang = HttpContext.Session.GetString("Lang") ?? "en";
        ViewBag.Goal = goal;
        ViewBag.Level = level;
        ViewBag.T = _translations.GetAll(lang);
        ViewBag.IsLanding = true;
        return View();
    }

    public IActionResult SetLang(string lang, string returnUrl = "/")
    {
        HttpContext.Session.SetString("Lang", lang ?? "en");
        return Redirect(returnUrl);
    }
}
