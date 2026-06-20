using FitForgeAI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class CalorieController : Controller
{
    private readonly TranslationService _translations;
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public CalorieController(TranslationService translations) => _translations = translations;

    public IActionResult Index()
    {
        ViewBag.T = _translations.GetAll(Lang);
        ViewBag.ActiveNav = "calorie";
        return View();
    }

    [HttpPost]
    public IActionResult Calculate(double age, double height, double weight, string gender, string activity, string goal)
    {
        // Mifflin-St Jeor BMR
        double bmr = gender == "female"
            ? (10 * weight) + (6.25 * height) - (5 * age) - 161
            : (10 * weight) + (6.25 * height) - (5 * age) + 5;

        double activityMultiplier = activity switch {
            "sedentary"   => 1.2,
            "light"       => 1.375,
            "moderate"    => 1.55,
            "active"      => 1.725,
            "very_active" => 1.9,
            _ => 1.55
        };

        double tdee = Math.Round(bmr * activityMultiplier);

        double targetCalories = goal switch {
            "lose_fast"  => Math.Round(tdee - 750),
            "lose"       => Math.Round(tdee - 500),
            "maintain"   => tdee,
            "gain"       => Math.Round(tdee + 400),
            "gain_fast"  => Math.Round(tdee + 700),
            _ => tdee
        };

        // Macros (protein first approach)
        double proteinG  = Math.Round(weight * 2.0);           // 2g/kg
        double fatG      = Math.Round(targetCalories * 0.28 / 9);
        double carbG     = Math.Round((targetCalories - (proteinG * 4) - (fatG * 9)) / 4);

        ViewBag.Bmr        = (int)bmr;
        ViewBag.Tdee       = (int)tdee;
        ViewBag.Target     = (int)targetCalories;
        ViewBag.ProteinG   = (int)proteinG;
        ViewBag.FatG       = (int)fatG;
        ViewBag.CarbG      = Math.Max((int)carbG, 50);
        ViewBag.Age        = age;
        ViewBag.Height     = height;
        ViewBag.Weight     = weight;
        ViewBag.Gender     = gender;
        ViewBag.Activity   = activity;
        ViewBag.Goal       = goal;
        ViewBag.T          = _translations.GetAll(Lang);
        ViewBag.ActiveNav  = "calorie";
        return View("Index");
    }
}
