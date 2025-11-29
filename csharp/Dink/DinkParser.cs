namespace Dink;

using System.Text.RegularExpressions;
using System.Linq;

public class DinkParser
{
    class BraceContainer
    {
        public BraceContainer? Parent;
        public List<string> Comments = new List<string>();  
        public List<string> GetComments()
        {
            var output = new List<string>(Comments);
            if (Parent != null)
            {
                output.AddRange(Parent.GetComments());
            }
            return output;
        }  
    }

    public static bool Verbose = false;
    public static void Log(string str)
    {
        if (Verbose)
            Console.WriteLine(str);
    }

    public static bool IsBraceOpeningLine(string line)
    {
        int openCount = 0;
        int closeCount = 0;

        foreach (char c in line)
        {
            if (c == '{') openCount++;
            else if (c == '}') closeCount++;
        }

        if (openCount > closeCount)
            return true;
        return false;
    }

    public static bool IsBraceClosingLine(string line)
    {
        int openCount = 0;
        int closeCount = 0;

        foreach (char c in line)
        {
            if (c == '{') openCount++;
            else if (c == '}') closeCount++;
        }

        if (openCount < closeCount)
            return true;
        return false;
    }
    
    public static bool IsFlowBreakingDinkLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line))
            return false;

        if (Regex.IsMatch(line, @"^-\s*[A-Z][A-Z0-9_]*.*:"))
            return true;

        return false;
    }

    public static bool IsFlowBreakingLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line))
            return false;
    
        if (line.StartsWith("*") ||
            line.StartsWith("-") ||
            line.StartsWith("+"))
        {
            return true;
        }

        if (line.Contains("->") || line.Contains("<-"))
        {
            return true;
        }

        return false;
    }
 
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

    private static readonly Random _rng = new Random();
    private const string Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateID()
    {
        char[] buffer = new char[4];
        for (int i = 0; i < 4; i++)
            buffer[i] = Chars[_rng.Next(Chars.Length)];
        return new string(buffer);
    }

    public static bool ContainsInkGroup(string text)
    {
        return Regex.IsMatch(text, @"\bshuffle\b|\bcycle\b|\bonce\b|\bstopping\b");
    }

    public static (string? Expression, bool IsError) ParseExpressionClause(string line)
    {
        // Must start with a dash, then expression, then colon.
        const string pattern = @"^\s*-\s*(?<Expression>[^#]+?)\s*:\s*(?<Rest>.*)$";
        Match match = Regex.Match(line, pattern);
        
        if (!match.Success) 
            return (null, false);

        string expression = match.Groups["Expression"].Value;
        string rest = match.Groups["Rest"].Value;

        bool isCharacterTag = Regex.IsMatch(expression, @"^[A-Z][A-Z0-9_]+$");
        if (isCharacterTag)
            return (null, false);

        if (!string.IsNullOrWhiteSpace(rest))
            return (expression, true);

        return (expression, false);
    }
    
    public static List<DinkScene> ParseInkLines(List<string> lines)
    {
        List<DinkScene> parsedScenes = new List<DinkScene>();
        DinkScene? scene = null;
        DinkBlock? block = null;
        DinkSnippet? snippet = null;
        List<string> comments = new List<string>();
        bool parsing = false;
        BraceContainer? currentBraceContainer = null;

        int currentBraceLevel = 0;
        int activeGroup = 0;
        int activeGroupLevel = 0;

        void addSnippet()
        {
            if (snippet != null && block != null) 
            {
                if (snippet.Beats.Count > 0)
                    block.Snippets.Add(snippet);
            }

            snippet = new DinkSnippet();
            snippet.SnippetID = GenerateID();
            if (currentBraceContainer != null)
            {
                snippet.BraceComments.AddRange(currentBraceContainer.GetComments());
            }
            snippet.Comments.AddRange(comments);
            comments.Clear();
        }

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

            if (IsBraceOpeningLine(trimmedLine))
            {
                currentBraceContainer = new BraceContainer
                {
                    Parent = currentBraceContainer,
                };
                currentBraceContainer.Comments.AddRange(comments);
                comments.Clear();
                currentBraceLevel++;
                if (ContainsInkGroup(trimmedLine))
                {
                    if (activeGroupLevel==0)
                    {
                        activeGroupLevel = currentBraceLevel;
                        activeGroup++;
                    }
                }
                addSnippet();
            }
            else if (IsBraceClosingLine(trimmedLine))
            {
                currentBraceLevel = Math.Max(0, currentBraceLevel - 1);
                if (currentBraceLevel < activeGroupLevel)
                {
                    activeGroupLevel = 0;
                    Console.WriteLine("Active group stopped:"+activeGroup);
                }
                if (currentBraceContainer!=null)
                    currentBraceContainer = currentBraceContainer?.Parent;
                addSnippet();
            }
            else if (IsFlowBreakingLine(trimmedLine))
            {
                if (IsFlowBreakingDinkLine(trimmedLine) && parsing)
                {
                    List<string> saveComments = comments.ToList();
                    comments.Clear();
                    addSnippet();
                    comments.AddRange(saveComments);
                }
                else
                {
                    addSnippet();
                }
            }

            var (expr, isError) = ParseExpressionClause(trimmedLine);
            if (expr != null)
            {
                if (isError)
                {
                    if (parsing)
                    {
                        Console.WriteLine("Dink Format Error: Line starts with expression but has content after colon.");
                        Console.WriteLine($"    {trimmedLine}");
                    }
                }
                else
                {
                    addSnippet();
                    continue;
                }
            }
            else if (ParseKnot(trimmedLine) is string knot)
            {
                if (snippet != null && block != null && snippet.Beats.Count > 0)
                    block.Snippets.Add(snippet);
                if (block != null && scene != null && block.Snippets.Count > 0)
                    scene.Blocks.Add(block);
                if (scene != null && scene.Blocks.Count > 0)
                    parsedScenes.Add(scene);
                parsing = false;

                scene = new DinkScene();
                scene.SceneID = knot;

                block = new DinkBlock();
                block.BlockID = "";
                block.Comments.AddRange(comments);

                snippet = new DinkSnippet();
                snippet.SnippetID = GenerateID();

                comments.Clear();
                Log($"Scene: {scene}");
                continue;
            }
            else if (ParseStitch(trimmedLine) is string stitch)
            {
                if (snippet != null && block != null && snippet.Beats.Count > 0)
                    block.Snippets.Add(snippet);
                if (block != null && scene != null && block.Snippets.Count > 0)
                    scene.Blocks.Add(block);

                block = new DinkBlock();
                block.BlockID = stitch;
                block.Comments.AddRange(comments);

                snippet = new DinkSnippet();
                snippet.SnippetID = GenerateID();
                
                comments.Clear();
                Log($"Snippet: {snippet}");
                continue;
            }
            else if (trimmedLine == "#dink")
            {
                parsing = true;
                continue;
            }
            else if (ParseComment(trimmedLine) is string comment)
            {
                comments.Add(comment);
                continue;
            }
            else if (ParseLine(trimmedLine) is DinkLine dinkLine)
            {
                if (!parsing)
                {
                    Console.WriteLine("A Dink line is present in a block without a #dink tag. This doesn't look right!");
                    Console.WriteLine($"    {trimmedLine}");
                }
                else
                {
                    dinkLine.Comments.AddRange(comments);
                    if (activeGroupLevel>0)
                        dinkLine.Group = activeGroup;
                    snippet?.Beats.Add(dinkLine);
                    comments.Clear();
                    Log(dinkLine.ToString());
                    continue;
                }
            }
            else if (parsing && ParseAction(trimmedLine) is DinkAction dinkAction)
            {
                dinkAction.Comments.AddRange(comments);
                if (activeGroupLevel>0)
                    dinkAction.Group = activeGroup;
                snippet?.Beats.Add(dinkAction);
                comments.Clear();
                Log(dinkAction.ToString());
                continue;
            }
            comments.Clear();
        }

        if (snippet != null && block != null && snippet.Beats.Count > 0)
            block.Snippets.Add(snippet);
        if (block != null && scene != null && block.Snippets.Count > 0)
            scene.Blocks.Add(block);
        if (scene != null && scene.Blocks.Count > 0)
            parsedScenes.Add(scene);

        return parsedScenes;
    }

    private static string RemoveBlockComments(string text)
    {
        const string pattern = @"/\*[\s\S]*?\*/";
        return Regex.Replace(text, pattern, string.Empty, RegexOptions.Singleline);
    }

    // Figures out what existing snippet ID contains the updated set of Line IDs.
    public static string? FindExistingSnippetID(
        IEnumerable<string> newBeatIds,
        IEnumerable<DinkSnippet> existingSnippets,
        double minOverlapScore = 0.5)
    {
        // Turn the new beats into a hash set for fast operations.
        var newSet = new HashSet<string>(newBeatIds, StringComparer.OrdinalIgnoreCase);

        string? bestId = null;
        double bestScore = 0.0;

        foreach (var s in existingSnippets)
        {
            // Extract BeatIDs from the snippet.
            var oldSet = new HashSet<string>(
                s.Beats.Select(b => b.LineID),
                StringComparer.OrdinalIgnoreCase
            );

            int intersection = newSet.Intersect(oldSet).Count();
            int union = newSet.Union(oldSet).Count();

            if (union == 0)
                continue;

            double score = (double)intersection / union;

            if (score > bestScore)
            {
                bestScore = score;
                bestId = s.SnippetID;
            }
        }

        // Only reuse an existing snippet if similarity exceeds threshold.
        return bestScore >= minOverlapScore ? bestId : null;
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
        string textWithoutComments = RemoveBlockComments(text);
        List<string> lines = SplitTextIntoLines(textWithoutComments);
        return ParseInkLines(lines);
    }
}
