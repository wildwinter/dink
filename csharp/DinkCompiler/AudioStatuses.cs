namespace DinkCompiler;

using Dink;
using DinkTool;

public class AudioStatuses
{
    // id, Status
    private Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private List<string> _ids = new List<string>();

    private ProjectEnvironment _env;

    public AudioStatuses(ProjectEnvironment env)
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
        return _env.AudioStatusSettings;
    }

    public AudioStatusDefinition GetStatus(string id)
    {
        var audioStatusDef = new AudioStatusDefinition();
        if (_entries.TryGetValue(id, out string? status))
        {
            _env.GetAudioStatusByLabel(status, out audioStatusDef);
        }
        return audioStatusDef;
    }

    public int GetStatusCount(string status)
    {
        return _entries.Values.Count(v => v == status);
    }

    public int GetCount()
    {
        return _ids.Count;
    }

    public int CountRecorded(List<string> idList)
    {
        return idList.Count(id => GetStatus(id).Recorded);
    }

    public int CountReadyToRecord(WritingStatuses writingStatuses, List<string> idList)
    {
        return idList.Count(id => !GetStatus(id).Recorded && writingStatuses.GetStatus(id).Record);
    }

    public int CountInDraft(WritingStatuses writingStatuses, List<string> idList)
    {
        return idList.Count(id => !GetStatus(id).Recorded && !writingStatuses.GetStatus(id).Record);
    }

    public int GetSceneTagCount(DinkScene scene, string? status=null)
    {
        int count = 0;

        foreach(var line in scene.IterateLines())
        {
            if (status==null || GetStatus(line.LineID).Status == status)
                count++;
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

        for (var i=_env.AudioStatusSettings.Count-1;i>=0;i--)
        {
            var audioStatusDef=_env.AudioStatusSettings[i];
            string audioFolderRoot = audioStatusDef.Folder;

            if (string.IsNullOrWhiteSpace(audioFolderRoot) || !Directory.Exists(audioFolderRoot))
                continue;

            foreach (var filePath in Directory.EnumerateFiles(audioFolderRoot, "*", SearchOption.AllDirectories))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
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