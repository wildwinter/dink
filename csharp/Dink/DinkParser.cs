namespace Dink;

using System.Text.RegularExpressions;
using System.Linq;

public class DinkParser
{
    public static string? ParseComment(string line)
    {
        if (line.StartsWith("//"))
            return line.Substring(2).Trim();
        return null;
    }

    private static string? ExtractID(List<string> tags)
    {
        const string prefix = "id:";

        string? foundTag = tags.FirstOrDefault(t => 
            t.StartsWith(prefix) && t.Length > prefix.Length);

        if (foundTag == null)
            return null;

        string idValue = foundTag.Substring(prefix.Length);
        tags.Remove(foundTag); 
        return idValue;
    }
    
    public static DinkAction? ParseAction(string line)
    {
        // ^!                                     - Must start with '!'
        // \s* - Optional whitespace
        // (?:(?<Type>[A-Z0-9_]+)\s*:\s*)?      - Optional Type group
        // (?<Content>.*?)                      - Capture Content non-greedily (everything until the tags start)
        // (?:\s+\#(?<TagValue>\S+))* - Non-capturing group for zero or more tags:
        //                                      - \s+\# - Must have 1+ whitespace, then '#' (per spec)
        //                                      - (?<TagValue>\S+) - Capture group 'TagValue' (the tag string without '#')
        // $                                      - End of the string

        const string combinedPattern =
            @"^!\s*(?:(?<Type>[A-Z0-9_]+)\s*:\s*)?(?<Content>.*?)(?:\s+\#(?<TagValue>\S+))*$";

        Match match = Regex.Match(line, combinedPattern, RegexOptions.Singleline);

        if (!match.Success)
            return null;

        var action = new DinkAction();
        action.Type = match.Groups["Type"].Value;
        List<string> tags = match.Groups["TagValue"].Captures
            .Select(c => c.Value)
            .ToList();
        action.LineID = ExtractID(tags) ?? string.Empty;
        action.Tags = tags;
        action.Content = match.Groups["Content"].Value.Trim();

        return action;
    }
    
    public static DinkLine? ParseLine(string line)
    {
        // Combined Pattern Breakdown:
        // ^\s* - Optional leading whitespace
        // (?<CharacterID>[A-Z0-9_]+)           - Capture Group 'CharacterID' (caps, numbers, underscores)
        // \s* - Optional whitespace
        // (?:\(\s*(?<Qualifier>.*?)\s*\))?    - Optional Qualifier group: [ optional space, Capture 'Qualifier' (non-greedy), optional space ]
        // \s* - Optional whitespace
        // :                                      - Mandatory colon
        // \s* - Optional whitespace
        // (?:\(\s*(?<Direction>.*?)\s*\})?     - Optional Direction group: [ optional space, Capture 'Direction' (non-greedy), optional space ]
        // \s* - Optional whitespace
        // (?<Content>.*?)                      - Capture Content non-greedily (everything until the tags start)
        // (?:\s+\#(?<TagValue>\S+))* - Zero or more tags: (whitespace, #, Capture 'TagValue' without #)
        // $                                      - End of the string

        const string pattern = 
            @"^\s*(?<CharacterID>[A-Z0-9_]+)\s*(?:\(\s*(?<Qualifier>.*?)\s*\))?\s*:\s*(?:\(\s*(?<Direction>.*?)\s*\))?\s*(?<Content>.*?)(?:\s+\#(?<TagValue>\S+))*$";   

        Match match = Regex.Match(line, pattern, RegexOptions.Singleline);

        if (!match.Success)
            return null;

        var dinkLine = new DinkLine();
        
        dinkLine.CharacterID = match.Groups["CharacterID"].Value;
        dinkLine.Qualifier = match.Groups["Qualifier"].Value; // Empty string if not present
        dinkLine.Direction = match.Groups["Direction"].Value; // Empty string if not present
        List<string> tags = match.Groups["TagValue"].Captures
            .Select(c => c.Value)
            .ToList();
        dinkLine.LineID = ExtractID(tags) ?? string.Empty;
        dinkLine.Tags = tags;
        dinkLine.Content = match.Groups["Content"].Value.Trim();
        return dinkLine;
    }
}