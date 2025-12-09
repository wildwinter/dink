namespace Dink;

using System.Text.RegularExpressions;
using System.Linq;

public class NonDinkLine
{
    public DinkOrigin Origin = new DinkOrigin();
    
    public string ID = "";
    public List<string> Tags = new List<string>();  

    public List<string> GetTags(params string[] prefixes) 
    {
        return Tags
            .Where(tag => prefixes.Any(prefix => 
                tag.StartsWith(prefix + ":"))) 
            .ToList();
    }
}

public class DinkParser
{
    class ParsingLine
    {
        public string Text="";
        public DinkOrigin Origin = new DinkOrigin();
    }

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

    private static int GetBraceDelta(string line)
    {
        int delta = 0;
        foreach (char c in line)
        {
            if (c == '{') delta++;
            else if (c == '}') delta--;
        }
        return delta;
    }
    
    private static readonly Regex _rxFlowBreaking = new Regex(
        @"^-\s*[A-Z][A-Z0-9_]*.*:", 
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static bool IsFlowBreakingDinkLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line))
            return false;

        return _rxFlowBreaking.IsMatch(line);
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

    // Matches a line that *only* consists of optional leading/trailing space 
    // and one or more tag structures (e.g., " #tag1 #tag2 ").
    private static readonly Regex _rxTagLine = new Regex(
        @"^\s*(?:\#(?<TagValue>\S+)(\s+\#(?<TagValue>\S+))*)\s*$", 
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static List<string>? ParseTagLine(string line)
    {
        Match match = _rxTagLine.Match(line);

        if (!match.Success)
            return null;

        List<string> tags = match.Groups["TagValue"].Captures
            .Select(c => c.Value.Trim())
            .ToList();
            
        return tags;
    }

    public static bool ParseNonDinkLine(string line, out NonDinkLine ndLine)
    {
        line = line.Trim();

        ndLine = new NonDinkLine();
 
        int firstHashIndex = line.IndexOf('#');
        if (firstHashIndex == -1 || firstHashIndex == line.Length - 1)
            return false;
    
        int endBoundaryIndex = line.IndexOf(']', firstHashIndex);
        if (endBoundaryIndex == -1)
            endBoundaryIndex = line.Length;
    
        string tagBlock = line.Substring(firstHashIndex, endBoundaryIndex - firstHashIndex);
        string[] rawTags = tagBlock.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (rawTags.Length == 0)
            return false;

        List<string> tagValues = rawTags
            .Where(t => t.StartsWith("#"))
            .Select(t => t.TrimStart('#'))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        if (tagValues.Count == 0) 
            return false;

        string? id = ExtractID(tagValues);
        if (id==null)
            return false;

        ndLine.Tags = tagValues;
        ndLine.ID = id;

        return true;
    }
    
    // ^\s* - Optional leading whitespace
    // [-] - Optional minus symbol (for shuffles etc.)
    // \s* - Optional whitespace
    // (?<Text>[\w].*?)                      - Capture Text non-greedily (everything until the tags start)
    // (?:\s+\#(?<TagValue>\S+))* - Non-capturing group for zero or more tags:
    //                                      - \s+\# - Must have 1+ whitespace, then '#' (per spec)
    //                                      - (?<TagValue>\S+) - Capture group 'TagValue' (the tag string without '#')
    // $   
    //    
    private static readonly Regex _rxAction = new Regex(
        @"^\s*[-]?\s*(?<Text>[^\*\+].*?)(?:\s+\#(?<TagValue>\S+))+$", 
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static DinkAction? ParseAction(string line)
    {
        Match match = _rxAction.Match(line);

        if (!match.Success)
            return null;

        var action = new DinkAction();
        List<string> tags = match.Groups["TagValue"].Captures
            .Select(c => c.Value)
            .ToList();
        action.LineID = ExtractID(tags) ?? string.Empty;
        action.Tags = tags;
        action.Text = match.Groups["Text"].Value.Trim();

        return action;
    }

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
    private static readonly Regex _rxLine = new Regex(
        @"^\s*[-]?\s*(?<CharacterID>[A-Z0-9_]+)\s*(?:\(\s*(?<Qualifier>.*?)\s*\))?\s*:\s*(?:\(\s*(?<Direction>.*?)\s*\))?\s*(?<Text>[\w].*?)(?:\s+\#(?<TagValue>\S+))*$", 
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static DinkLine? ParseLine(string line)
    {
        Match match = _rxLine.Match(line);

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

    private static readonly Regex _rxOption = new Regex(
        @"^\s*[*+]\s*\[\s*([^#\]]+?)\s*(?:#.*?)*\s*\]\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline);
    public static string? ParseOption(string line)
    {
        Match match = _rxOption.Match(line);
        if (match.Success)
        {
            string content = match.Groups[1].Value.Trim();
            return content;
        }
        return null;
    }

    public static bool ParseGather(string line)
    {
        return line.Trim()=="-";
    }

    private static readonly Regex _rxKnot = new Regex(
        @"^\s*={2,}\s*(?<Identifier>\w+)\b.*$",
        RegexOptions.Compiled | RegexOptions.Singleline);
    public static string? ParseKnot(string line)
    {
        Match match = _rxKnot.Match(line);

        if (match.Success)
        {
            return match.Groups["Identifier"].Value;
        }

        return null;
    }

    private static readonly Regex _rxStitch = new Regex(
        @"^\s*=\s*(?<Identifier>\w+)\b.*$",
        RegexOptions.Compiled | RegexOptions.Singleline);    
    public static string? ParseStitch(string line)
    {
        Match match = _rxStitch.Match(line);
        
        if (match.Success)
        {
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

    private static readonly Regex _rxInkGroup = new Regex(
        @"^\s*\{\s*(\w+)\s*:\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline);   
    public static bool ContainsInkGroup(string text)
    {
        return _rxInkGroup.IsMatch(text);
    }

    public static bool ContainsCode(string text)
    {
        return text.StartsWith("INCLUDE")
                ||text.StartsWith("VAR")
                ||text.StartsWith("~");
    }

    // Must start with a dash, then expression, then colon.
    private static readonly Regex _rxExpressionClause = new Regex(
        @"^\s*-\s*(?<Expression>[^#]+?)\s*:\s*(?<Rest>.*)$",
        RegexOptions.Compiled | RegexOptions.Singleline);   
    public static (string? Expression, bool IsError) ParseExpressionClause(string line)
    {
        Match match = _rxExpressionClause.Match(line);
        
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
    
    private static List<DinkScene> ParseInkLines(List<ParsingLine> lines, List<NonDinkLine> outNonDinkLines)
    {
        List<string> IDs = new();
        List<DinkScene> parsedScenes = new List<DinkScene>();
        DinkScene? scene = null;
        DinkBlock? block = null;
        DinkSnippet? snippet = null;
        List<string> comments = new List<string>();
        bool parsing = false;
        BraceContainer? currentBraceContainer = null;
        bool inOptions = false;

        int currentBraceLevel = 0;
        int activeGroup = 0;
        int activeGroupLevel = 0;

        bool hitFirstFileContent = false;
        bool hitFirstKnotContent = false;
        bool hitFirstStitchContent = false;
        List<string> fileTags = new List<string>();
        List<string> knotTags = new List<string>();
        List<string> stitchTags = new List<string>();

        bool CheckID(string id, DinkOrigin origin)
        {
            if (IDs.Contains(id))
            {
                Console.Error.WriteLine($"Duplicate ID at {origin}");
                return false;
            }
            IDs.Add(id);
            return true;
        }

        void createSnippet()
        {
            snippet = new DinkSnippet();
            snippet.SnippetID = GenerateID();
            if (activeGroupLevel>0)
                snippet.Group = activeGroup;
            if (currentBraceContainer != null)
            {
                snippet.GroupComments.AddRange(currentBraceContainer.GetComments());
            }
            snippet.Comments.AddRange(comments);
            comments.Clear();
        }

        void addSnippet()
        {
            if (snippet != null && block != null) 
            { 
                if (snippet.Beats.Count > 0)
                {
                    snippet.Origin = snippet.Beats[0].Origin;
                    block.Snippets.Add(snippet);
                }
            }
        }

        void addAndCreateSnippet()
        {
            addSnippet();
            createSnippet();
        }

        void createBlock(DinkOrigin origin, string id="")
        {
            block = new DinkBlock();
            block.BlockID = id;
            block.Origin = origin;
            block.Comments.AddRange(comments);
            comments.Clear();

            activeGroupLevel = 0;
            activeGroup = 0;
            currentBraceContainer = null;
            currentBraceLevel = 0;
        }

        void addBlock()
        {
            if (block!=null && scene!=null && block.Snippets.Count > 0)
            {
                block.Tags.AddRange(stitchTags);
                stitchTags.Clear();
                scene.Blocks.Add(block);

                // Figure out the group indices and counts.
                int groupMax = block.Snippets.Max(s => s.Group);
                foreach(var group in block.Snippets.GroupBy(b => b.Group))
                {
                    if (group.Key==0)
                        continue;

                    var snippets = group.ToList();
                    for(var i=0;i<snippets.Count;i++)
                    {
                        var groupSnippet = snippets[i];
                        if (groupSnippet==null)
                            continue;

                        if (snippets.Count==1)
                        {
                            groupSnippet.Group=0;
                            continue;
                        }
                        
                        groupSnippet.GroupIndex = i+1;
                        groupSnippet.GroupCount = snippets.Count;
                    }
                }
            }
        }

        void createScene(DinkOrigin origin, string id)
        {
            scene = new DinkScene();
            scene.SceneID = id;
            scene.Origin = origin;
            scene.Comments.AddRange(comments);
            comments.Clear();
        }

        void addScene()
        {
            if (scene != null && scene.Blocks.Count > 0)
            {
                scene.Tags.AddRange(knotTags);
                knotTags.Clear();
                parsedScenes.Add(scene);
            }
        }

        void hitContent()
        {
            hitFirstFileContent = true;
            hitFirstKnotContent = true;
            hitFirstStitchContent = true;
        }

        string? getTagPrefix(string tag)
        {
            int index = tag.IndexOf(':');
            if (index > 0)
            {
                string candidate = tag.Substring(0, index + 1);
                return tag.Substring(0,index+1);
            }
            return null;
        }

        void addOnlyNewTags(DinkBeat beat, List<string> tags)
        {
            foreach(var tag in tags)
            {
                string? prefix = getTagPrefix(tag);
                if (prefix!=null)
                {
                    if (!beat.Tags.Any(s => s.StartsWith(prefix)))
                        beat.Tags.Add(tag);
                }
                else
                    beat.Tags.Add(tag);
            }
        }
        
        void addTags(DinkBeat beat)
        {
            addOnlyNewTags(beat, stitchTags);
            addOnlyNewTags(beat, knotTags);
            addOnlyNewTags(beat, fileTags);
        }

        foreach (var line in lines)
        {
            var trimmedLine = line.Text.Trim();

            // Check for comment at end.
            int commentIndex = trimmedLine.LastIndexOf("//");
            if (commentIndex > 0)
            {
                string comment = trimmedLine.Substring(commentIndex + 2).Trim();
                comments.Add(comment);
                trimmedLine = trimmedLine.Substring(0, commentIndex).TrimEnd();
            }

            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            if (ParseTagLine(trimmedLine) is List<string> tags)
            {
                if (tags.Contains("dink"))
                {
                    parsing = true;
                    tags.Remove("dink");
                }
                if (!hitFirstFileContent)
                {
                    fileTags.AddRange(tags);
                }
                else if (!hitFirstKnotContent)
                {
                    knotTags.AddRange(tags);
                }
                else if (!hitFirstStitchContent)
                {
                    stitchTags.AddRange(tags);
                }
                continue;
            }
            
            if (ContainsCode(trimmedLine))
                continue;

            hitContent();

            int braceDelta = GetBraceDelta(trimmedLine);
            if (braceDelta>0)
            {
                currentBraceContainer = new BraceContainer
                {
                    Parent = currentBraceContainer,
                };
                currentBraceContainer.Comments.AddRange(comments);
                comments.Clear();
                currentBraceLevel++;

                List<string> savingComments = new();
                if (ContainsInkGroup(trimmedLine))
                {
                    if (activeGroupLevel==0)
                    {
                        if (snippet!=null && snippet.Comments.Count>0)
                            currentBraceContainer.Comments.InsertRange(0, snippet.Comments);
                        activeGroupLevel = currentBraceLevel;
                        activeGroup++;
                    }
                }
                addAndCreateSnippet();
            }
            else if (braceDelta<0)
            {
                currentBraceLevel = Math.Max(0, currentBraceLevel - 1);
                if (currentBraceLevel < activeGroupLevel)
                {
                    activeGroupLevel = 0;
                    inOptions = true;
                    comments.Add("MERGE");
                }
                if (currentBraceContainer!=null)
                    currentBraceContainer = currentBraceContainer?.Parent;
                addAndCreateSnippet();
            }
            else if (IsFlowBreakingLine(trimmedLine))
            {
                comments.Clear();
                if (IsFlowBreakingDinkLine(trimmedLine) && parsing)
                {
                    addAndCreateSnippet();
                }
                else
                {
                    if (ParseOption(trimmedLine) is string option)
                    {
                        comments.Add($"OPTION \"{option}\"");
                        inOptions = true;
                    }
                    else if (ParseGather(trimmedLine) && inOptions)
                    {
                        comments.Add("MERGE");
                        inOptions = false;
                    }
                    else
                    {
                        inOptions = false;
                    }
                    addAndCreateSnippet();
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
                    addAndCreateSnippet();
                    continue;
                }
            }
            else if (ParseKnot(trimmedLine) is string knot)
            {
                addSnippet();
                addBlock();
                addScene();
                
                parsing = false;

                hitFirstKnotContent = false;
                knotTags.Clear();

                createScene(line.Origin, knot);
                createBlock(line.Origin);
                createSnippet();

                Log($"Scene: {scene}");
                continue;
            }
            else if (ParseStitch(trimmedLine) is string stitch)
            {
                addSnippet();
                addBlock();

                hitFirstStitchContent = false;
                stitchTags.Clear();

                createBlock(line.Origin, stitch);
                createSnippet();

                Log($"Snippet: {snippet}");
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
                    if (!CheckID(dinkLine.LineID,line.Origin))
                        return parsedScenes;
                    dinkLine.Origin = line.Origin;
                    dinkLine.Comments.AddRange(comments);
                    snippet?.Beats.Add(dinkLine);
                    comments.Clear();
                    addTags(dinkLine);
                    Log(dinkLine.ToString());
                    continue;
                }
            }
            else if (parsing && ParseAction(trimmedLine) is DinkAction dinkAction)
            {
                if (!CheckID(dinkAction.LineID,line.Origin))
                    return parsedScenes;
                dinkAction.Origin = line.Origin;
                dinkAction.Comments.AddRange(comments);
                snippet?.Beats.Add(dinkAction);
                comments.Clear();
                addTags(dinkAction);
                Log(dinkAction.ToString());
                continue;
            }
            
            if (ParseNonDinkLine(trimmedLine, out NonDinkLine ndLine))
            {
                if (ndLine.ID!=null)
                {
                    if (!CheckID(ndLine.ID,line.Origin))
                        return parsedScenes;
                    ndLine.Tags.AddRange(stitchTags);
                    ndLine.Tags.AddRange(knotTags);
                    ndLine.Tags.AddRange(fileTags);
                    ndLine.Origin = line.Origin;
                    outNonDinkLines.Add(ndLine);
                }
            }
            comments.Clear();
        }

        addAndCreateSnippet();
        addBlock();
        addScene();

        return parsedScenes;
    }

    private static readonly Regex _rxBlockComments = new Regex(
        @"/\*.*?\*/",
        RegexOptions.Compiled | RegexOptions.Singleline);  

    private static string RemoveBlockComments(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        string processedText = _rxBlockComments.Replace(text, match =>
        {
            // Filter the matched comment string to keep ONLY newline characters (\r or \n).
            // This effectively deletes the text content of the comment but 
            // preserves the vertical spacing.
            char[] newlinesOnly = match.Value
                                    .Where(c => c == '\r' || c == '\n')
                                    .ToArray();

            // If it's an inline comment (no newlines), this returns an empty string.
            // If it's a multi-line comment, this returns the exact blank lines needed to preserve numbering.
            return new string(newlinesOnly);
        });
        return processedText;
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
            options: StringSplitOptions.None
        );
        return linesArray.ToList();
    }

    public static List<DinkScene> ParseInk(string text, string sourceFilePath, List<NonDinkLine> outNonDinkLines)
    {
        string textWithoutComments = RemoveBlockComments(text);
        List<string> lines = SplitTextIntoLines(textWithoutComments);
        List<ParsingLine> parsingLines = new List<ParsingLine>();
        for (int i=0;i<lines.Count;i++)
        {
            var parsingLine = new ParsingLine();
            parsingLine.Text = lines[i];
            parsingLine.Origin.SourceFilePath = sourceFilePath;
            parsingLine.Origin.LineNum = i+1;
            parsingLines.Add(parsingLine);
        }
        return ParseInkLines(parsingLines, outNonDinkLines);
    }
}
