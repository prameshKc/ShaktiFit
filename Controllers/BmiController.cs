using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class BmiController : Controller
{
    private readonly TranslationService _translations;
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public BmiController(TranslationService translations) => _translations = translations;

    public IActionResult Index()
    {
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ActiveNav = "bmi";
        return View();
    }

    [HttpPost]
    public IActionResult Calculate(double height, double weight, string? unit)
    {
        double heightM = height / 100.0;
        double bmi = heightM > 0 ? Math.Round(weight / (heightM * heightM), 1) : 0;
        ViewBag.Bmi = bmi;
        ViewBag.Height = height;
        ViewBag.Weight = weight;
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ActiveNav = "bmi";
        return View("Index");
    }
}
