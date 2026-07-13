namespace DinkCompiler;

using System.Text.Json;

public struct Character
{
    public required string ID { get; set; }
    public string Actor { get; set; }
    public string TTSVoice { get; set; }

    // Grammatical gender of the speaker, used by translators to choose the
    // correct grammatical forms. Written by Dinky as "Male" / "Female" /
    // "Neuter", or absent/empty when non-specified.
    public string Gender { get; set; }
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

    /// <summary>
    /// Map a character's grammatical gender to the single-letter code used in
    /// the localisation spreadsheet. Anything unrecognised - including null,
    /// empty, and "Non-specified" - yields an empty string, so the column is
    /// simply left blank rather than guessing.
    /// </summary>
    public static string GenderCode(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return "";

        return gender.Trim().ToLowerInvariant() switch
        {
            "male" => "M",
            "female" => "F",
            "neuter" => "N",
            _ => ""
        };
    }

    /// <summary>
    /// Canonical display name of a grammatical gender ("Male" / "Female" /
    /// "Neuter"), or "" when non-specified or unrecognised. Derived from
    /// GenderCode so that hand-edited casing is normalised.
    /// </summary>
    public static string GenderName(string? gender)
    {
        return GenderCode(gender) switch
        {
            "M" => "Male",
            "F" => "Female",
            "N" => "Neuter",
            _ => ""
        };
    }

    /// <summary>
    /// Gender code for a character ID, or "" if the character is unknown.
    /// </summary>
    public string GenderCodeFor(string characterID)
    {
        return GenderCode(Get(characterID)?.Gender);
    }

    /// <summary>
    /// Canonical gender name for a character ID, or "" if the character is
    /// unknown or has no grammatical gender set.
    /// </summary>
    public string GenderNameFor(string characterID)
    {
        return GenderName(Get(characterID)?.Gender);
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