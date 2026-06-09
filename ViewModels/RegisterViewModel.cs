using System.ComponentModel.DataAnnotations;

namespace FitForgeAI.ViewModels;

public class RegisterViewModel
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;
    [Range(13, 100)] public int? Age { get; set; }
    [Required] public string Gender { get; set; } = string.Empty;
    [Range(100, 250)] public double? Height { get; set; }
    [Range(30, 300)] public double? Weight { get; set; }
    [Required] public string FitnessLevel { get; set; } = "Beginner";
    public List<string> Goals { get; set; } = new();
    public int WorkoutDaysPerWeek { get; set; } = 3;
    public bool HasGymAccess { get; set; } = true;
    public bool PrefersHomeWorkout { get; set; } = false;
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
