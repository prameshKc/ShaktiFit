using FitForgeAI.Models;

namespace FitForgeAI.ViewModels;

public class DashboardViewModel
{
    public User User { get; set; } = new();
    public Workout? TodaysWorkout { get; set; }
    public int WeeklyCompleted { get; set; }
    public int WorkoutDaysTarget { get; set; }
    public List<WorkoutLog> RecentWorkouts { get; set; } = new();
    public ProgressEntry? LatestProgress { get; set; }
    public List<RunningLog> RecentRuns { get; set; } = new();
    public double WeeklyMileage { get; set; }
    public string MotivationalQuote { get; set; } = string.Empty;
    public Dictionary<string, string> Translations { get; set; } = new();
    public string Lang { get; set; } = "en";
}
