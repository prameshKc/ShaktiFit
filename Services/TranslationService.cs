using System.Text.Json;

namespace FitForgeAI.Services;

public class TranslationService
{
    private Dictionary<string, Dictionary<string, string>> _translations = new();
    private readonly IWebHostEnvironment _env;

    public TranslationService(IWebHostEnvironment env)
    {
        _env = env;
        LoadTranslations();
    }

    private void LoadTranslations()
    {
        var path = Path.Combine(_env.ContentRootPath, "Data", "Json", "translations.json");
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        _translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new();
    }

    public string Get(string key, string lang = "en")
    {
        if (_translations.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (_translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
            return enVal;
        return key;
    }

    public Dictionary<string, string> GetAll(string lang = "en")
    {
        return _translations.TryGetValue(lang, out var dict) ? dict :
               _translations.TryGetValue("en", out var en) ? en : new();
    }
}
