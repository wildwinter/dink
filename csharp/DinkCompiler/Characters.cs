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
    /// Normalise a grammatical gender for display. The canonical values are
    /// snapped to consistent casing ("male" -> "Male"). Grammatical gender is a
    /// free-text field - some languages need genders beyond masculine/feminine/
    /// neuter (common, animate, inanimate...) - so any other non-empty value is
    /// passed through verbatim rather than discarded. Blank, and the legacy
    /// "Non-specified" dropdown label, mean "no gender set" and yield "".
    /// </summary>
    public static string GenderName(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return "";

        var trimmed = gender.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "male" => "Male",
            "female" => "Female",
            "neuter" => "Neuter",
            // Was the fixed dropdown's label for "no value" before the field
            // became free text; treat it as blank rather than a gender.
            "non-specified" => "",
            _ => trimmed
        };
    }

    /// <summary>
    /// Map a character's grammatical gender to the value used in the
    /// localisation spreadsheet column. The canonical three abbreviate to
    /// M/F/N; any other value passes through as written (see GenderName).
    /// </summary>
    public static string GenderCode(string? gender)
    {
        var name = GenderName(gender);
        return name switch
        {
            "Male" => "M",
            "Female" => "F",
            "Neuter" => "N",
            _ => name
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