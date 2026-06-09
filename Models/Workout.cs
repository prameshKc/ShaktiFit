namespace FitForgeAI.Models;

public class Workout
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string NameNepali { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FitnessLevel { get; set; } = string.Empty;
    public List<string> Goals { get; set; } = new();
    public List<WorkoutExercise> Exercises { get; set; } = new();
    public int DurationMinutes { get; set; } = 45;
    public string Description { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public bool RequiresGym { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public int EstimatedCalories { get; set; }
}

public class WorkoutExercise
{
    public string ExerciseId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; } = 3;
    public string Reps { get; set; } = "10";
    public int RestSeconds { get; set; } = 60;
    public string Notes { get; set; } = string.Empty;
}

public class WorkoutLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string WorkoutId { get; set; } = string.Empty;
    public string WorkoutName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int DurationMinutes { get; set; }
    public int CaloriesBurned { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<ExerciseLog> ExerciseLogs { get; set; } = new();
}

public class ExerciseLog
{
    public string ExerciseId { get; set; } = string.Empty;
    public string ExerciseName { get; set; } = string.Empty;
    public List<SetLog> Sets { get; set; } = new();
}

public class SetLog
{
    public int SetNumber { get; set; }
    public int Reps { get; set; }
    public double WeightKg { get; set; }
}
