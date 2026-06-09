namespace FitForgeAI.Models;

public class Exercise
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string NameNepali { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public List<string> TargetMuscles { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string DescriptionNepali { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public List<string> CommonMistakes { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public int DefaultSets { get; set; } = 3;
    public string DefaultReps { get; set; } = "10-12";
    public int RestSeconds { get; set; } = 60;
    public string ImageUrl { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public bool IsCompound { get; set; } = false;
    public List<string> Equipment { get; set; } = new();
}
