namespace FitForgeAI.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public string FitnessLevel { get; set; } = "Beginner";
    public List<string> Goals { get; set; } = new();
    public int WorkoutDaysPerWeek { get; set; } = 3;
    public bool HasGymAccess { get; set; } = true;
    public bool PrefersHomeWorkout { get; set; } = false;
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "dark";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public int WorkoutStreak { get; set; } = 0;
    public int TotalWorkoutsCompleted { get; set; } = 0;
    public List<string> Achievements { get; set; } = new();
    public List<string> FavoriteWorkouts { get; set; } = new();
}
