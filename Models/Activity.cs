namespace FitForgeAI.Models;

public class Activity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;        // Run, Ride, Swim, Gym, Walk, HIIT, Yoga, Cycling
    public DateTime Date { get; set; } = DateTime.Now;
    public double? DistanceKm { get; set; }
    public int DurationMinutes { get; set; }
    public int? CaloriesBurned { get; set; }
    public int? HeartRateAvg { get; set; }
    public string Intensity { get; set; } = "Moderate";     // Easy, Moderate, Hard
    public string? Notes { get; set; }
    public string? Title { get; set; }                      // Optional custom title
}
