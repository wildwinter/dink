namespace DinkCompiler;
using Dink;

public class WritingStatuses
{
    // id, wsTag
    private Dictionary<string, string> _entries = new Dictionary<string, string>();
    private List<string> _ids = new List<string>();

    private CompilerEnvironment _env;

    public WritingStatuses(CompilerEnvironment env)
    {
        _env = env;
    }

    public bool IsEmpty() {return _ids.Count==0;}

    public bool HasDefinitions() {return GetDefinitions().Count>1;}

    private void Set(string id, string wsTag)
    {
        if (!_ids.Contains(id))
        {
            _ids.Add(id);
        }
        _entries[id] = wsTag;
    }

    public WritingStatusDefinition GetStatus(string id)
    {
        if (_entries.TryGetValue(id, out string? wsTag))
            return GetDefinitionByTag(wsTag);
        return new WritingStatusDefinition();
    }

    public List<WritingStatusDefinition> GetDefinitions()
    {
        return _env.WritingStatusOptions;
    }

    public WritingStatusDefinition GetDefinitionByTag(string wsTag)
    {
        var result = _env.WritingStatusOptions.FirstOrDefault(x => x.WsTag == wsTag);
        if (result!=null)
            return result;
        return new WritingStatusDefinition();
    }

    public int GetTagCount(string wsTag)
    {
        return _entries.Values.Count(v => v == wsTag);
    }

    public int GetCount()
    {
        return _ids.Count;
    }

    public int GetSceneTagCount(DinkScene scene, string? wsTag=null)
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
                        if (wsTag==null || GetStatus(beat.LineID).WsTag == wsTag)
                            count++;
                    } 
                    else if (_env.LocActionBeats && beat is DinkAction action)
                    {
                        if (wsTag==null || GetStatus(beat.LineID).WsTag == wsTag)
                            count++;
                    }
                }
            }
        }
        return count;
    }

    public int GetNonDinkTagCount(List<NonDinkLine> ndLines, string? wsTag=null)
    {
        int count = 0;

        foreach (var ndLine in ndLines)
        {
            if (wsTag==null || GetStatus(ndLine.ID).WsTag == wsTag)
                count++;
        }
 
        return count;
    }

    public bool Build(List<DinkScene> dinkScenes, List<NonDinkLine> nonDinkLines, LocStrings locStrings)
    {
        if (_env.WritingStatusOptions.Count == 0)
            return true;

        Console.WriteLine("Extracting writing statuses...");

        foreach (var scene in dinkScenes)
        {
            foreach (var block in scene.Blocks)
            {
                foreach (var snippet in block.Snippets)
                { 
                    foreach (var beat in snippet.Beats)
                    {
                        if (beat is DinkLine line)
                        {
                            string statusTag = line.GetTagsFor(["ws"]).FirstOrDefault() ?? "";
                            if (statusTag.StartsWith("ws:"))
                                statusTag = statusTag.Substring(3);

                            Set(line.LineID, statusTag);
                        } 
                        else if (_env.LocActionBeats && beat is DinkAction action)
                        {
                            string statusTag = action.GetTagsFor(["ws"]).FirstOrDefault() ?? "";
                            if (statusTag.StartsWith("ws:"))
                                statusTag = statusTag.Substring(3);

                            Set(action.LineID, statusTag);
                        }
                    }
                }
            }
        }

        foreach(var ndLine in nonDinkLines)
        {
            string statusTag = ndLine.GetTags(["ws"]).FirstOrDefault() ?? "";
            if (statusTag.StartsWith("ws:"))
                statusTag = statusTag.Substring(3);

            Set(ndLine.ID, statusTag);
        }

        return true;
    }
}