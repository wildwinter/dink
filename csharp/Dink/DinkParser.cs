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
        // ^\s* - Optional leading whitespace
        // [-] - Optional minus symbol (for shuffles etc.)
        // \s* - Optional whitespace
        // (?:\(\s*(?<Type>[\w].*?)\s*\))?    - Optional Type group: ( optional space, Capture 'Qualifier' (non-greedy), optional space )
        // \s* - Optional whitespace
        // (?<Text>[\w].*?)                      - Capture Text non-greedily (everything until the tags start)
        // (?:\s+\#(?<TagValue>\S+))* - Non-capturing group for zero or more tags:
        //                                      - \s+\# - Must have 1+ whitespace, then '#' (per spec)
        //                                      - (?<TagValue>\S+) - Capture group 'TagValue' (the tag string without '#')
        // $   
        //                                    - End of the string
        const string pattern =
            @"^\s*[-]?\s*(?:\(\s*(?<Type>[\w].*?)\s*\))?\s*(?<Text>[\w].*?)(?:\s+\#(?<TagValue>\S+))*$";

        Match match = Regex.Match(line, pattern, RegexOptions.Singleline);

        if (!match.Success)
            return null;

        var action = new DinkAction();
        action.Type = match.Groups["Type"].Value;
        List<string> tags = match.Groups["TagValue"].Captures
            .Select(c => c.Value)
            .ToList();
        action.LineID = ExtractID(tags) ?? string.Empty;
        action.Tags = tags;
        action.Text = match.Groups["Text"].Value.Trim();

        return action;
    }

    public static DinkLine? ParseLine(string line)
    {
        // Combined Pattern Breakdown:
        // ^\s* - Optional leading whitespace
        // [-] - Optional minus symbol (for shuffles etc.)
        // \s* - Optional whitespace
        // (?<CharacterID>[A-Z0-9_]+)           - Capture Group 'CharacterID' (caps, numbers, underscores)
        // \s* - Optional whitespace
        // (?:\(\s*(?<Qualifier>.*?)\s*\))?    - Optional Qualifier group: ( optional space, Capture 'Qualifier' (non-greedy), optional space )
        // \s* - Optional whitespace
        // :                                      - Mandatory colon
        // \s* - Optional whitespace
        // (?:\(\s*(?<Direction>.*?)\s*\})?     - Optional Direction group: ( optional space, Capture 'Direction' (non-greedy), optional space )
        // \s* - Optional whitespace
        // (?<Text>[\w].*?)                      - Capture Text non-greedily (everything until the tags start)
        // (?:\s+\#(?<TagValue>\S+))* - Zero or more tags: (whitespace, #, Capture 'TagValue' without #)
        // $                                      - End of the string

        const string pattern =
            @"^\s*[-]?\s*(?<CharacterID>[A-Z0-9_]+)\s*(?:\(\s*(?<Qualifier>.*?)\s*\))?\s*:\s*(?:\(\s*(?<Direction>.*?)\s*\))?\s*(?<Text>[\w].*?)(?:\s+\#(?<TagValue>\S+))*$";

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
        dinkLine.Text = match.Groups["Text"].Value.Trim();
        return dinkLine;
    }

    public static string? ParseKnot(string line)
    {
        // Pattern to extract the identifier
        const string pattern = @"^\s*={2,}\s*(?<Identifier>\w+)\b.*$";

        Match match = Regex.Match(line, pattern);

        if (match.Success)
        {
            // Return the captured 'Identifier' group value
            return match.Groups["Identifier"].Value;
        }

        return null;
    }
    
    public static string? ParseStitch(string line)
    {
        // Pattern to extract the identifier
        const string pattern = @"^\s*=\s*(?<Identifier>\w+)\b.*$";
        
        Match match = Regex.Match(line, pattern);
        
        if (match.Success)
        {
            // Return the captured 'Identifier' group value
            return match.Groups["Identifier"].Value;
        }
        
        return null;
    }

    public static List<DinkScene> ParseInkLines(List<string> lines)
    {
        List<DinkScene> parsedScenes = new List<DinkScene>();
        DinkScene? scene = null;
        List<string> comments = new List<string>();
        string lastKnot = "";
        string lastStitch = "";
        bool parsing = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Check for comment at end.
            int commentIndex = trimmedLine.LastIndexOf("//");
            if (commentIndex > 0)
            {
                string comment = trimmedLine.Substring(commentIndex + 2).Trim();
                comments.Add(comment);
                trimmedLine = trimmedLine.Substring(0, commentIndex).TrimEnd();
            }

            if (ParseKnot(trimmedLine) is string knot)
            {
                comments.Clear();
                if (scene != null && scene.Beats.Count > 0)
                    parsedScenes.Add(scene);
                lastKnot = knot;
                parsing = false;
                scene = new DinkScene();
                scene.SceneID = knot;
                Console.WriteLine($"Scene: {scene}");
            }
            else if (ParseStitch(trimmedLine) is string stitch)
            {
                comments.Clear();
                if (scene != null && scene.Beats.Count > 0)
                    parsedScenes.Add(scene);
                lastStitch = stitch;
                parsing = false;
                scene = new DinkScene();
                scene.SceneID = $"{lastKnot}.{stitch}";
                Console.WriteLine($"Scene: {scene}");
            }
            else if (trimmedLine == "#dink")
            {
                parsing = true;
            }
            else if (parsing && ParseComment(trimmedLine) is string comment)
            {
                comments.Add(comment);
            }
            else if (parsing && ParseLine(trimmedLine) is DinkLine dinkLine)
            {
                dinkLine.Comments.AddRange(comments);
                scene?.Beats.Add(dinkLine);
                comments.Clear();
                Console.WriteLine(dinkLine);
            }
            else if (parsing && ParseAction(trimmedLine) is DinkAction dinkAction)
            {
                dinkAction.Comments.AddRange(comments);
                scene?.Beats.Add(dinkAction);
                comments.Clear();
                Console.WriteLine(dinkAction);
            }
            else
            {
                comments.Clear();
            }
        }
        if (scene != null && scene.Beats.Count > 0)
            parsedScenes.Add(scene);
        return parsedScenes;
    }

    private static string RemoveBlockComments(string text)
    {
        const string pattern = @"/\*[\s\S]*?\*/";
        return Regex.Replace(text, pattern, string.Empty, RegexOptions.Singleline);
    }

    public static List<string> SplitTextIntoLines(string text)
    {
        string[] separators = new string[] { "\r\n", "\n", "\r" };
        string[] linesArray = text.Split(
            separator: separators, 
            options: StringSplitOptions.RemoveEmptyEntries
        );
        return linesArray.ToList();
    }
    
    public static List<DinkScene> ParseInk(string text)
    {
        List<DinkScene> parsedScenes = new List<DinkScene>();
        string textWithoutComments = RemoveBlockComments(text);
        List<string> lines = SplitTextIntoLines(textWithoutComments);
        return ParseInkLines(lines);
    }
}