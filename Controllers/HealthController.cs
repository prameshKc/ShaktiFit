using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class HealthController : Controller
{
    private readonly TranslationService _translations;
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public HealthController(TranslationService translations) => _translations = translations;

    public IActionResult Index()
    {
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ActiveNav = "health";
        return View();
    }

    public IActionResult Topic(string id)
    {
        ViewBag.TopicId = id;
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ActiveNav = "health";
        return View();
    }
}
