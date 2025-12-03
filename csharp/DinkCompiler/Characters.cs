namespace DinkCompiler;

using System.Text.Json;

public struct Character
{
    public required string ID { get; set; }
    public string Actor { get; set; }
    public string TTSVoice { get; set; }
}

public class Characters
{
    private Dictionary<string, Character> _entries = new Dictionary<string, Character>();
    private List<string> _ids = new List<string>();

    public IEnumerable<Character> OrderedEntries => _ids.Select(id => _entries[id]);

    public void Set(Character entry)
    {
        if (!_ids.Contains(entry.ID))
        {
            _ids.Add(entry.ID);
        }

        _entries[entry.ID] = entry;
    }

    public Character? Get(string id)
    {
        if (_entries.TryGetValue(id, out var charEntry))
            return charEntry;
        return null;
    }

    public bool Has(string id)
    {
        return _entries.ContainsKey(id);
    }

    public static Characters FromJson(string jsonString)
    {
        Characters characters = new();

        Character[]? chars = JsonSerializer.Deserialize<Character[]>(jsonString);
        if (chars != null)
        {
            foreach (var charEntry in chars)
            {
                Character adjusted = charEntry;
                adjusted.ID = adjusted.ID.ToUpper();
                characters.Set(adjusted);
            }
        }

        return characters;
    }
}