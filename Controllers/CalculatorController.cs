using Microsoft.AspNetCore.Mvc;

namespace FitForgeAI.Controllers;

public class CalculatorController : Controller
{
    private string Lang => HttpContext.Session.GetString("Lang") ?? "en";

    public IActionResult Index() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    // ── Body Fat % (US Navy Method) ─────────────────────────────────────────
    public IActionResult BodyFat() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult BodyFat(string gender, double height, double neck, double waist, double hip = 0)
    {
        SetGuest();
        double bf = 0;
        if (gender == "female")
            bf = 495.0 / (1.29579 - 0.35004 * Math.Log10(waist + hip - neck) + 0.22100 * Math.Log10(height)) - 450.0;
        else
            bf = 495.0 / (1.0324 - 0.19077 * Math.Log10(waist - neck) + 0.15456 * Math.Log10(height)) - 450.0;

        bf = Math.Round(Math.Max(1.0, Math.Min(60.0, bf)), 1);

        string cat = (gender == "female") ? bf switch {
            < 14 => "Essential Fat", < 21 => "Athletic", < 25 => "Fit", < 32 => "Acceptable", _ => "Obese"
        } : bf switch {
            < 6  => "Essential Fat", < 14 => "Athletic", < 18 => "Fit", < 25 => "Acceptable", _ => "Obese"
        };

        string catColor = cat switch {
            "Athletic" => "#3b82f6", "Fit" => "#22c55e",
            "Acceptable" => "#f59e0b", "Obese" => "#ef4444", _ => "#94a3b8"
        };

        ViewBag.Bf = bf; ViewBag.Cat = cat; ViewBag.CatColor = catColor;
        ViewBag.Gender = gender; ViewBag.Height = height; ViewBag.Neck = neck;
        ViewBag.Waist = waist; ViewBag.Hip = hip;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Ideal Weight (4 formulas) ────────────────────────────────────────────
    public IActionResult IdealWeight() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult IdealWeight(string gender, double height)
    {
        SetGuest();
        double hIn = height / 2.54; // cm → inches
        double over60 = hIn - 60;   // inches over 5 feet

        double devine = gender == "male" ? 50.0 + 2.3 * over60 : 45.5 + 2.3 * over60;
        double robinson = gender == "male" ? 52.0 + 1.9 * over60 : 49.0 + 1.7 * over60;
        double miller = gender == "male" ? 56.2 + 1.41 * over60 : 53.1 + 1.36 * over60;
        double hamwi = gender == "male" ? 48.0 + 2.7 * over60 : 45.4 + 2.2 * over60;
        double avg = Math.Round((devine + robinson + miller + hamwi) / 4, 1);

        ViewBag.Devine = Math.Round(devine, 1); ViewBag.Robinson = Math.Round(robinson, 1);
        ViewBag.Miller = Math.Round(miller, 1); ViewBag.Hamwi = Math.Round(hamwi, 1);
        ViewBag.Avg = avg; ViewBag.Gender = gender; ViewBag.Height = height;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Protein Intake ───────────────────────────────────────────────────────
    public IActionResult Protein() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult Protein(double weight, string activity, string goal)
    {
        SetGuest();
        // Ranges per kg from ISSN / ACSM position stands
        (double min, double max) = (activity, goal) switch {
            (_, "lose")         => (1.6, 2.4),
            (_, "muscle")       => (1.8, 2.2),
            ("athlete", _)      => (1.6, 1.8),
            ("endurance", _)    => (1.4, 1.7),
            ("sedentary", _)    => (0.8, 1.0),
            _                   => (1.2, 1.6)
        };
        int minG = (int)Math.Round(weight * min);
        int maxG = (int)Math.Round(weight * max);
        int midG = (int)Math.Round(weight * (min + max) / 2);
        int minCal = minG * 4;
        int maxCal = maxG * 4;

        ViewBag.MinG = minG; ViewBag.MaxG = maxG; ViewBag.MidG = midG;
        ViewBag.MinCal = minCal; ViewBag.MaxCal = maxCal;
        ViewBag.MinRate = min; ViewBag.MaxRate = max;
        ViewBag.Weight = weight; ViewBag.Activity = activity; ViewBag.Goal = goal;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Water Intake ─────────────────────────────────────────────────────────
    public IActionResult Water() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult Water(double weight, string activity, string climate)
    {
        SetGuest();
        // Base: 35 ml/kg (European Food Safety Authority guideline)
        double baseML = weight * 35;

        double actExtra = activity switch {
            "light"    => 350,
            "moderate" => 700,
            "intense"  => 1050,
            "athlete"  => 1500,
            _ => 0
        };
        double climateExtra = climate switch {
            "warm" => 500, "hot" => 1000, _ => 0
        };

        double totalML = baseML + actExtra + climateExtra;
        int totalMl = (int)Math.Round(totalML);
        int cups = (int)Math.Round(totalML / 240.0);

        ViewBag.TotalMl = totalMl;
        ViewBag.Weight = weight; ViewBag.Activity = activity; ViewBag.Climate = climate;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── 1 Rep Max ────────────────────────────────────────────────────────────
    public IActionResult OneRepMax() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult OneRepMax(double liftWeight, int reps)
    {
        SetGuest();
        // Clamp to valid range (formulas break at reps > 20)
        reps = Math.Max(1, Math.Min(20, reps));

        double epley     = reps == 1 ? liftWeight : liftWeight * (1.0 + reps / 30.0);
        double brzycki   = reps == 1 ? liftWeight : liftWeight / (1.0278 - 0.0278 * reps);
        double lander    = (100.0 * liftWeight) / (101.3 - 2.67123 * reps);
        double lombardi  = liftWeight * Math.Pow(reps, 0.10);
        double oneRM     = Math.Round((epley + brzycki + lander + lombardi) / 4, 1);

        ViewBag.OneRM = oneRM;
        ViewBag.Epley = Math.Round(epley, 1); ViewBag.Brzycki = Math.Round(brzycki, 1);
        ViewBag.Lander = Math.Round(lander, 1); ViewBag.Lombardi = Math.Round(lombardi, 1);
        ViewBag.LiftWeight = liftWeight; ViewBag.Reps = reps;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Running Pace ─────────────────────────────────────────────────────────
    public IActionResult Pace() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult Pace(double distVal, int hours, int minutes, int seconds, string unit = "km")
    {
        SetGuest();
        double totalSeconds = hours * 3600 + minutes * 60 + seconds;
        // Reconstruct timeStr for display (pre-fill the form on postback)
        string timeStr = hours > 0
            ? $"{hours:D2}:{minutes:D2}:{seconds:D2}"
            : $"{minutes:D2}:{seconds:D2}";
        ViewBag.TimeStr = timeStr;
        if (totalSeconds <= 0 || distVal <= 0) return View();

        // Normalise to km for all calculations
        double distKm = unit == "mi" ? distVal * 1.60934 : distVal;

        // Pace per km
        double paceSecPerKm = totalSeconds / distKm;
        // Pace per mile
        double paceSecPerMi = paceSecPerKm * 1.60934;

        int paceMinKm = (int)(paceSecPerKm / 60), paceSecKm = (int)(paceSecPerKm % 60);
        int paceMinMi = (int)(paceSecPerMi / 60), paceSecMi = (int)(paceSecPerMi % 60);

        double speedKph = Math.Round(distKm / (totalSeconds / 3600), 2);
        double speedMph = Math.Round(speedKph / 1.60934, 2);

        // Riegel: T2 = T1 × (D2/D1)^1.06
        string Fmt(double sec) { int fh=(int)(sec/3600),fm=(int)((sec%3600)/60),fs=(int)(sec%60); return fh>0?$"{fh}:{fm:D2}:{fs:D2}":$"{fm}:{fs:D2}"; }

        ViewBag.PaceMinKm = paceMinKm; ViewBag.PaceSecKm = paceSecKm;
        ViewBag.PaceMinMi = paceMinMi; ViewBag.PaceSecMi = paceSecMi;
        ViewBag.SpeedKph = speedKph;   ViewBag.SpeedMph = speedMph;
        ViewBag.Pred5k       = Fmt(totalSeconds * Math.Pow(5.0     / distKm, 1.06));
        ViewBag.Pred10k      = Fmt(totalSeconds * Math.Pow(10.0    / distKm, 1.06));
        ViewBag.PredHalf     = Fmt(totalSeconds * Math.Pow(21.0975 / distKm, 1.06));
        ViewBag.PredMarathon = Fmt(totalSeconds * Math.Pow(42.195  / distKm, 1.06));
        ViewBag.DistVal = distVal; ViewBag.Unit = unit;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Heart Rate Zones (Karvonen) ──────────────────────────────────────────
    public IActionResult HeartRate() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult HeartRate(int age, int restHr)
    {
        SetGuest();
        // Tanaka formula (more accurate than 220-age for adults)
        int maxHr = (int)Math.Round(208 - 0.7 * age);
        int hrr   = maxHr - restHr; // Heart Rate Reserve

        ViewBag.MaxHr = maxHr; ViewBag.RestHr = restHr;
        ViewBag.Age = age; ViewBag.RestHrInput = restHr;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Sleep Calculator ─────────────────────────────────────────────────────
    public IActionResult Sleep() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult Sleep(string mode, string inputTime)
    {
        SetGuest();
        const int cycleMins = 90;
        const int fallAsleepMins = 15;

        // Parse HH:MM time string
        var parts = (inputTime ?? "00:00").Split(':');
        int h = int.TryParse(parts.ElementAtOrDefault(0), out int ph) ? ph : 0;
        int m = int.TryParse(parts.ElementAtOrDefault(1), out int pm) ? pm : 0;

        var times = new List<string>();
        string resultLabel;
        if (mode == "bedtime")
        {
            // User wants to wake at inputTime — suggest bedtimes (4-6 cycles)
            resultLabel = $"Go to bed at these times to wake at {inputTime}";
            var wake = new TimeSpan(h, m, 0);
            for (int cycles = 6; cycles >= 4; cycles--)
            {
                var bed = wake - TimeSpan.FromMinutes(cycles * cycleMins + fallAsleepMins);
                if (bed.TotalMinutes < 0) bed = bed.Add(TimeSpan.FromHours(24));
                times.Add($"{bed.Hours:D2}:{bed.Minutes:D2}");
            }
        }
        else
        {
            // User wants to sleep at inputTime — suggest wake times (4-6 cycles)
            resultLabel = $"Wake up at these times if you sleep at {inputTime}";
            var bed = new TimeSpan(h, m, 0) + TimeSpan.FromMinutes(fallAsleepMins);
            for (int cycles = 4; cycles <= 6; cycles++)
            {
                var wake = bed + TimeSpan.FromMinutes(cycles * cycleMins);
                if (wake.TotalMinutes >= 1440) wake -= TimeSpan.FromHours(24);
                times.Add($"{wake.Hours:D2}:{wake.Minutes:D2}");
            }
        }

        ViewBag.Mode = mode; ViewBag.InputTime = inputTime;
        ViewBag.WakeTimes = times; ViewBag.ResultLabel = resultLabel;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    // ── Waist-to-Height Ratio ────────────────────────────────────────────────
    public IActionResult WaistHeight() { SetGuest(); ViewBag.ActiveNav = "calculators"; return View(); }

    [HttpPost]
    public IActionResult WaistHeight(double waist, double height, string gender)
    {
        SetGuest();
        double ratio = Math.Round(waist / height, 3);
        double pct   = Math.Round(ratio * 100, 1);

        string cat, desc, color;
        if (gender == "female") {
            (cat, desc, color) = ratio switch {
                < 0.42 => ("Extremely Slim", "May indicate undernourishment. Consider consulting a doctor.", "#60a5fa"),
                < 0.49 => ("Healthy",        "Low cardiovascular and metabolic risk. Great range to maintain.", "#22c55e"),
                < 0.54 => ("Overweight",     "Slightly elevated risk. Consider lifestyle adjustments.", "#f59e0b"),
                < 0.58 => ("High Risk",      "Increased risk of cardiovascular disease and diabetes.", "#f97316"),
                _      => ("Very High Risk", "Significantly elevated health risks. Medical consultation advised.", "#ef4444")
            };
        } else {
            (cat, desc, color) = ratio switch {
                < 0.43 => ("Extremely Slim", "May indicate undernourishment. Consider consulting a doctor.", "#60a5fa"),
                < 0.53 => ("Healthy",        "Low cardiovascular and metabolic risk. Great range to maintain.", "#22c55e"),
                < 0.58 => ("Overweight",     "Slightly elevated risk. Consider lifestyle adjustments.", "#f59e0b"),
                < 0.63 => ("High Risk",      "Increased risk of cardiovascular disease and diabetes.", "#f97316"),
                _      => ("Very High Risk", "Significantly elevated health risks. Medical consultation advised.", "#ef4444")
            };
        }

        ViewBag.Ratio = ratio; ViewBag.Pct = pct;
        ViewBag.Cat = cat; ViewBag.Desc = desc; ViewBag.CatColor = color;
        ViewBag.Waist = waist; ViewBag.Height = height; ViewBag.Gender = gender;
        ViewBag.ActiveNav = "calculators";
        return View();
    }

    private void SetGuest()
    {
        var isAuth = HttpContext.Session.GetString("UserId") != null;
        if (!isAuth) ViewBag.IsLanding = true;
    }
}
