namespace FitForgeAI.Models;

public class ProgressEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public double? WeightKg { get; set; }
    public double? ChestCm { get; set; }
    public double? WaistCm { get; set; }
    public double? HipsCm { get; set; }
    public double? BicepsCm { get; set; }
    public double? ThighCm { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class RunningLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public double DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public double PaceMinPerKm { get; set; }
    public string RunType { get; set; } = "Easy";
    public string Notes { get; set; } = string.Empty;
}

public class PersonalRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string ExerciseId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public DateTime AchievedAt { get; set; } = DateTime.UtcNow;
}
