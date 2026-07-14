namespace DinkCompiler;

using Dink;
using DinkTool;

public class AudioStatuses
{
    // id, Status
    private Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private List<string> _ids = new List<string>();
    private HashSet<string> _idSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Line IDs the writer has flagged for re-recording (from rerecord.json).
    private HashSet<string> _reRecordIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // Synthetic status applied to a flagged line that is actually recorded.
    // It is NOT a config folder status: it never counts as Recorded, and its
    // label surfaces in the recording script and the stats sheets.
    public const string ReRecordLabel = "Re-record";
    private static readonly AudioStatusDefinition ReRecordDef = new AudioStatusDefinition
    {
        Status = ReRecordLabel,
        Folder = "",
        Recorded = false,
        Color = "FF8C00"
    };

    private ProjectEnvironment _env;

    public AudioStatuses(ProjectEnvironment env)
    {
        _env = env;
    }

    // Register the set of line IDs flagged for re-recording. Must be called
    // after Build() and before GetStatus() is consumed (the incremental-build
    // hashes in Compiler read GetStatus, so this feeds cache invalidation).
    public void SetReRecordIds(IEnumerable<string>? ids)
    {
        _reRecordIds = new HashSet<string>(ids ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    // True when a line is flagged for re-record AND is actually recorded. Only
    // recorded lines can be re-recorded - a flag on a not-yet-recorded line is
    // ignored. This is the "at or better than a Recorded status" rule.
    public bool IsReRecord(string id)
    {
        return _reRecordIds.Contains(id) && GetStatusRaw(id).Recorded;
    }

    private void Set(string id, string status)
    {
        if (_idSet.Add(id))
            _ids.Add(id);
        _entries[id] = status;
    }

    public bool HasDefinitions() {return GetDefinitions().Count>1;}

    public List<AudioStatusDefinition> GetDefinitions()
    {
        return _env.AudioStatusSettings;
    }

    // Definitions plus a synthetic "Re-record" entry, but only when at least one
    // line is effectively re-record. Used by the per-line and per-scene stats
    // sheets so flagged lines get their own column instead of showing blank.
    public List<AudioStatusDefinition> GetReportingDefinitions()
    {
        var defs = new List<AudioStatusDefinition>(_env.AudioStatusSettings);
        if (_entries.Keys.Any(IsReRecord))
            defs.Add(ReRecordDef);
        return defs;
    }

    // The status a line has purely from audio-file scanning, ignoring re-record.
    private AudioStatusDefinition GetStatusRaw(string id)
    {
        var audioStatusDef = new AudioStatusDefinition();
        if (_entries.TryGetValue(id, out string? status))
        {
            _env.GetAudioStatusByLabel(status, out audioStatusDef);
        }
        return audioStatusDef;
    }

    public AudioStatusDefinition GetStatus(string id)
    {
        if (IsReRecord(id))
            return ReRecordDef;
        return GetStatusRaw(id);
    }

    public int GetStatusCount(string status)
    {
        return _entries.Values.Count(v => v == status);
    }

    public int GetCount()
    {
        return _ids.Count;
    }

    // Recorded counts lines whose (overridden) status is Recorded, so
    // re-record lines are naturally excluded (ReRecordDef.Recorded == false).
    public int CountRecorded(List<string> idList)
    {
        return idList.Count(id => GetStatus(id).Recorded);
    }

    // Lines flagged and eligible for re-recording.
    public int CountReRecord(List<string> idList)
    {
        return idList.Count(IsReRecord);
    }

    // Ready-to-record / in-draft explicitly exclude re-record lines so the
    // four categories (Recorded, Re-record, Ready, Draft) partition the total.
    public int CountReadyToRecord(WritingStatuses writingStatuses, List<string> idList)
    {
        return idList.Count(id => !IsReRecord(id) && !GetStatus(id).Recorded && writingStatuses.GetStatus(id).Record);
    }

    public int CountInDraft(WritingStatuses writingStatuses, List<string> idList)
    {
        return idList.Count(id => !IsReRecord(id) && !GetStatus(id).Recorded && !writingStatuses.GetStatus(id).Record);
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
        var idSet = new HashSet<string>(idArray, StringComparer.OrdinalIgnoreCase);

        foreach (var id in idArray)
            Set(id, "Unknown");

        for (var i = _env.AudioStatusSettings.Count - 1; i >= 0; i--)
        {
            var audioStatusDef = _env.AudioStatusSettings[i];
            string audioFolderRoot = audioStatusDef.Folder;

            if (string.IsNullOrWhiteSpace(audioFolderRoot) || !Directory.Exists(audioFolderRoot))
                continue;

            foreach (var filePath in Directory.EnumerateFiles(audioFolderRoot, "*", SearchOption.AllDirectories))
            {
                var nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                // Fast path: filename matches an ID exactly (the common case).
                if (idSet.Contains(nameWithoutExt))
                {
                    if (_entries[nameWithoutExt] == "Unknown")
                        _entries[nameWithoutExt] = audioStatusDef.Status;
                    continue;
                }

                // Slow path: filename is a variant like "id_retake" - check
                // prefix match. Only hit for files that aren't exact-ID names.
                foreach (var id in idArray)
                {
                    if (nameWithoutExt.StartsWith(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_entries[id] == "Unknown")
                            _entries[id] = audioStatusDef.Status;
                    }
                }
            }
        }

        return true;
    }
}