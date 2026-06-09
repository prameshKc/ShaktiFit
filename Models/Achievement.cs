namespace FitForgeAI.Models;

public class Achievement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameNepali { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int RequiredValue { get; set; }
}

public class UserAchievement
{
    public string AchievementId { get; set; } = string.Empty;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
