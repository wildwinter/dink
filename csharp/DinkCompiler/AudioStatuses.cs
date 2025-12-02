namespace DinkCompiler;
using Dink;

public class AudioStatuses
{
    // id, Status
    private Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private List<string> _ids = new List<string>();

    private CompilerEnvironment _env;

    public AudioStatuses(CompilerEnvironment env)
    {
        _env = env;
    }

    private void Set(string id, string status)
    {
        if (!_ids.Contains(id))
        {
            _ids.Add(id);
        }
        _entries[id] = status;
    }

    public bool HasDefinitions() {return GetDefinitions().Count>1;}

    public List<AudioStatusDefinition> GetDefinitions()
    {
        return _env.AudioStatusOptions;
    }

    public AudioStatusDefinition GetStatus(string id)
    {
        if (_entries.TryGetValue(id, out string? status))
            return GetDefinitionByLabel(status);
        return new AudioStatusDefinition();
    }

    public int GetStatusCount(string status)
    {
        return _entries.Values.Count(v => v == status);
    }

    public int GetCount()
    {
        return _ids.Count;
    }

    public AudioStatusDefinition GetDefinitionByLabel(string status)
    {
        var result = _env.AudioStatusOptions.FirstOrDefault(x => x.Status == status);
        if (result!=null)
            return result;
        return new AudioStatusDefinition();
    }

    public int GetSceneTagCount(DinkScene scene, string? status=null)
    {
        int count = 0;

        foreach(var block in scene.Blocks)
        {
            foreach(var snippet in block.Snippets)
            {
                foreach (var beat in snippet.Beats)
                {
                    if (beat is DinkLine line)
                    {
                        if (status==null || GetStatus(beat.LineID).Status == status)
                            count++;
                    }
                }
            }
        }
        return count;
    }
    
    public bool Build(VoiceLines voiceLines)
    {
        var idArray = voiceLines.OrderedEntries.Select(v => v.ID).ToArray();
        foreach (var id in idArray)
        {
            Set(id, "Unknown");
        }

        foreach (var audioStatusDef in _env.AudioStatusOptions)
        {
            string audioFolderRoot = audioStatusDef.Folder;

            if (string.IsNullOrWhiteSpace(audioFolderRoot) || !Directory.Exists(audioFolderRoot))
                continue;

            foreach (var filePath in Directory.EnumerateFiles(audioFolderRoot, "*", SearchOption.AllDirectories))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                // Only compare IDs to the *filename*, never to folder names
                foreach (var id in idArray)
                {
                    if (nameWithoutExt.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_entries[id]=="Unknown")
                            _entries[id]=audioStatusDef.Status;
                    }
                }
            }
        }

        return true;
    }
}