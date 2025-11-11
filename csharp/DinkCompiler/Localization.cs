namespace DinkCompiler;

using System.Text.Json;

public struct LocEntry
{
    public required string ID { get; set; }
    public required string text { get; set; }
    public required List<string> comments { get; set; }
    public required string speaker { get; set; }
}

class LocStrings
{
    private Dictionary<string, LocEntry> _entries = new Dictionary<string, LocEntry>();
    private List<string> _ids = new List<string>();

    public IEnumerable<LocEntry> OrderedEntries => _ids.Select(id => _entries[id]);

    public void SetEntry(LocEntry entry)
    {
        if (!_ids.Contains(entry.ID))
        {
            _ids.Add(entry.ID);
        }

        _entries[entry.ID] = entry;
    }

    public bool GetEntry(string id, out LocEntry entry)
    {
        if (_entries.TryGetValue(id, out var locEntry))
        {
            entry = locEntry;
            return true;
        }
        entry = default;
        return false;
    }

    public void RemoveEntry(string id)
    {
        _entries.Remove(id);
        _ids.Remove(id);
    }
    
    public string ToJson()
    {
        var entriesToSerialize = this.OrderedEntries.ToList();
        return JsonSerializer.Serialize(entriesToSerialize, new JsonSerializerOptions { WriteIndented = true });
    }

    public static LocStrings FromJson(string jsonString)
    {
        var newLocStrings = new LocStrings();
        
        var deserializedEntries = JsonSerializer.Deserialize<List<LocEntry>>(jsonString);
        if (deserializedEntries != null)
        {
            foreach (var entry in deserializedEntries)
            {
                newLocStrings.SetEntry(entry);
            }
        }
        
        return newLocStrings;
    }
}