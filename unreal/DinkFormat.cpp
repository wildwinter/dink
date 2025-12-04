#include "DinkFormat.h"

DEFINE_LOG_CATEGORY_STATIC(LogDinkFormat, Log, All);

FString FDinkBeat::ToString() const
{
    FString dump = FString::Printf(TEXT("[%s] "), *LineID.ToString());

    if (BeatType == EDinkBeatType::Line) {

        dump += FString::Printf(TEXT("Line | CharacterID: %s"), *CharacterID.ToString());

        if (!Qualifier.IsEmpty())
            dump += FString::Printf(TEXT(" | Qualifier: %s"), *Qualifier);

        if (!Direction.IsEmpty())
            dump += FString::Printf(TEXT(" | Direction: %s"), *Direction);
    }
    else if (BeatType == EDinkBeatType::Action)
    {
        dump += FString::Printf(TEXT("Action"), *LineID.ToString());
    }

    dump += FString::Printf(TEXT(" | Text: \"%s\""), *Text);

    if (Tags.Num() > 0)
    {
        dump += TEXT(" | Tags:");
        for (const FString& tag : Tags)
            dump += FString::Printf(TEXT(" #%s"), *tag);
    }

    return dump;
}

void FDinkBeat::ParseTags(const FString& tagsRaw, FDinkBeat& outDinkBeat) {
    if (!tagsRaw.IsEmpty())
    {
        TArray<FString> rawTags;
        tagsRaw.ParseIntoArray(rawTags, TEXT("#"), true);
        for (const FString& tag : rawTags)
        {
            if (!tag.IsEmpty())
            {
                FString trimmed = tag.TrimStart();
                if (trimmed.StartsWith("id:")) {
                    outDinkBeat.LineID = FName(trimmed.Mid(3));
                }
                else
                {
                    outDinkBeat.Tags.Add(trimmed);
                }
            }
        }
    }

    if (outDinkBeat.LineID.IsNone()) {
        UE_LOG(LogDinkFormat, Warning, TEXT("Dink beat is missing a LineID! %s"), *outDinkBeat.ToString());
    }
}


bool UDinkParser::ParseLine(const FString& line, FDinkBeat& outBeat)
{
    /*

    ^\s*                Matches any leading whitespace at the beginning of the line.
    [-+*]?              Optionally matches a single character from the set -, +, or *.
    \s*                 Matches optional whitespace after the symbol.
    ([A-Z0-9_]+)        Capture Group 1: CharacterID (e.g. FRED) - Matches the identifier, consisting of uppercase letters, digits, or underscores.
    \s*                 Optional whitespace.
    (?:\(([^)]*)\))?    Capture Group 2 (optional): Qualifier (e.g. O.S., V.O.) - Matches a qualifier inside parentheses.
    \s*:\s*             Matches a colon surrounded by optional whitespace.
    (?:\(([^)]*)\))?    Capture Group 3 (optional): Direction (e.g. screaming) - Matches a direction inside parentheses.
    \s*                 Optional whitespace.
    ([^\r\n#]*?)        Capture Group 4: Dialogue Line - Matches the main text, stopping at a #, carriage return, or newline. Non-greedy.
    \s*                 Optional whitespace.
    (#[^\s#]+(?:\s*#[^\s#]+)*)? Capture Group 5 (optional): Tags (e.g. #fred #bucketid:5) - Matches one or more tags, each starting with # and separated by optional whitespace.
    $                   End of line.
    */
    const FRegexPattern pattern(TEXT(
        R"(^\s*[-+*]?\s*([A-Z0-9_]+)\s*(?:\(([^)]*)\))?\s*:\s*(?:\(([^)]*)\))?\s*([^\r\n#]*?)\s*(#[^\s#]+(?:\s*#[^\s#]+)*)?$)"
    ));
    FRegexMatcher matcher(pattern, line);

    if (!matcher.FindNext())
        return false; // Line doesn't match expected format

    outBeat.BeatType = EDinkBeatType::Line;
    outBeat.CharacterID = FName(matcher.GetCaptureGroup(1));
    outBeat.Qualifier = matcher.GetCaptureGroup(2);
    outBeat.Direction = matcher.GetCaptureGroup(3);
    outBeat.Text = matcher.GetCaptureGroup(4).TrimEnd();

    FString tagsRaw = matcher.GetCaptureGroup(5);
    FDinkBeat::ParseTags(tagsRaw, outBeat);

    return true;
}


bool UDinkParser::ParseAction(const FString& line, FDinkBeat& outBeat)
{
    /*
    ^\s*[-]?\s* - Start, optional dash, optional whitespace
    ([^\r\n#]*?)        - Group 1: Text (Non-greedy, stops before the first #)
    \s* - Whitespace between text and tags
    (                   - Group 2: All Tags (Combined string)
       (?:#[^\s#]+)     - Match the first tag (Must exist)
       (?:\s*#[^\s#]+)* - Match zero or more subsequent tags
    )
    \s* - Trailing whitespace
    $                   - End of line
    */
    
    // CHANGED:
    // 1. Removed the '?' at the very end (Tags are now mandatory).
    // 2. Changed inner group quantifier from '+' to '*' (Allows single tag).
    const FRegexPattern pattern(TEXT(
        R"(^\s*[-]?\s*([^\r\n#]*?)\s*((?:#[^\s#]+)(?:\s*#[^\s#]+)*)\s*$)"
    ));
    FRegexMatcher matcher(pattern, line);

    if (!matcher.FindNext())
        return false; // Line doesn't match expected format

    outBeat.BeatType = EDinkBeatType::Action;
    outBeat.Text = matcher.GetCaptureGroup(1).TrimEnd();

    FString tagsRaw = matcher.GetCaptureGroup(2);
    FDinkBeat::ParseTags(tagsRaw, outBeat);

    return true;
}

FString FDinkSnippet::ToString() const
{
    FString dump = FString::Printf(TEXT("  Snippet:%s Beats:%d"), *SnippetID.ToString(), Beats.Num());
    for (const FDinkBeat& beat : Beats)
    {
        dump += FString::Printf(TEXT("\n    %s"), *beat.ToString());
    }
    return dump;
}

FString FDinkBlock::ToString() const
{
    FString dump = FString::Printf(TEXT("  Block:%s Snippets:%d"), *BlockID.ToString(), Snippets.Num());
    for (const FDinkSnippet& snippet : Snippets)
    {
        dump += FString::Printf(TEXT("\n        %s"), *snippet.ToString());
    }
    return dump;
}

FString FDinkScene::ToString() const
{
    FString dump = FString::Printf(TEXT("Scene:%s Blocks:%d"), *SceneID.ToString(), Blocks.Num());
    for (const FDinkBlock& block : Blocks)
    {
        dump += FString::Printf(TEXT("\n%s"), *block.ToString());
    }
    return dump;
}

bool IsFlowBreakingLine(const FString& InInput)
{
    FString Input = InInput;
    Input.TrimStartAndEndInline();

    if (Input.IsEmpty())
    {
        return false;
    }

    if (Input.StartsWith(TEXT("*")) ||
        Input.StartsWith(TEXT("-")) ||
        Input.StartsWith(TEXT("+")))
    {
        return true;
    }

    if (Input.Contains(TEXT("->")) || Input.Contains(TEXT("<-")))
    {
        return true;
    }

    int32 OpenCount = 0;
    int32 CloseCount = 0;

    // Range-based for loop iterates over the TCHAR array inside FString
    for (const TCHAR Char : Input)
    {
        if (Char == TEXT('{'))
        {
            OpenCount++;
        }
        else if (Char == TEXT('}'))
        {
            CloseCount++;
        }
    }

    return OpenCount != CloseCount;

    return false;
}

bool ParseComment(const FString& line)
{
    return (line.StartsWith("//"));
}

bool ParseKnot(const FString& Line, FString& outKnot)
{
    const FRegexPattern Pattern(TEXT("^\\s*={2,}\\s*(\\w+)\\b.*$"));
    FRegexMatcher Matcher(Pattern, Line);

    if (Matcher.FindNext())
    {
        outKnot = Matcher.GetCaptureGroup(1);
        return true;
    }

    return false;
}

bool ParseStitch(const FString& Line, FString& outStitch)
{
    const FRegexPattern Pattern(TEXT("^\\s*=\\s*(\\w+)\\b.*$"));
    FRegexMatcher Matcher(Pattern, Line);

    if (Matcher.FindNext())
    {
        outStitch = Matcher.GetCaptureGroup(1);
        return true;
    }

    return false;
}

static FString GenerateShortHash()
{
    static const TCHAR Charset[] = TEXT("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");
    static const int32 CharsetSize = UE_ARRAY_COUNT(Charset) - 1; // exclude null terminator

    FString Result;
    Result.Reserve(4);

    for (int32 i = 0; i < 4; i++)
    {
        int32 Index = FMath::RandRange(0, CharsetSize - 1);
        Result.AppendChar(Charset[Index]);
    }

    return Result;
}


bool ParseExpressionClause(const FString& Line, FString& OutExpression, bool& OutIsError)
{
    static const FRegexPattern MainPattern(TEXT("^\\s*-\\s*([^#]+?)\\s*:\\s*(.*)$"));

    FRegexMatcher Matcher(MainPattern, Line);

    if (!Matcher.FindNext())
    {
        return false;
    }

    FString ExtractedExpr = Matcher.GetCaptureGroup(1);
    FString Rest = Matcher.GetCaptureGroup(2);

    static const FRegexPattern CharTagPattern(TEXT("^[A-Z][A-Z0-9_]+$"));
    FRegexMatcher CharMatcher(CharTagPattern, ExtractedExpr);

    if (CharMatcher.FindNext())
    {
        return false;
    }

    OutExpression = ExtractedExpr;
    OutIsError = !Rest.TrimStartAndEnd().IsEmpty();

    return true;
}

bool UDinkParser::ParseInkLines(const TArray<FString>& lines, TArray<FDinkScene>& outDinkScenes)
{
    FDinkScene scene;
    FDinkBlock block;
    FDinkSnippet snippet;
    bool parsing = false;

    auto addSnippet = [&]()
        {
            if (snippet.Beats.Num() > 0)
            {
                block.Snippets.Add(snippet);
            }
            else
            {
                return;
            }

            snippet = FDinkSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
        };

    for (const FString line : lines)
    {
        FString trimmedLine = line.TrimStartAndEnd();
        FString knot;
        FString stitch;
        FDinkBeat dinkBeat;

        UE_LOG(LogDinkFormat, Log, TEXT("Parsing line: %s"), *trimmedLine);

        // Check for comment at end.
        int32 commentIndex = trimmedLine.Find(TEXT("//"), ESearchCase::IgnoreCase, ESearchDir::FromEnd);
        if (commentIndex > 0)
        {
            trimmedLine = trimmedLine.Left(commentIndex).TrimEnd();
        }

        if (IsFlowBreakingLine(trimmedLine))
        {
            addSnippet();
        }

        FString expr;
        bool isError;
        if (ParseExpressionClause(trimmedLine, expr, isError))
        {
            if (isError)
            {
                if (parsing)
                {
                    UE_LOG(LogDinkFormat, Fatal, TEXT("Dink Format Error: Line starts with expression but has content after colon.\n.   %s"), *trimmedLine);
                    return false;
                }
            }
            else
            {
                addSnippet();
                continue;
            }
        }
        else if (ParseKnot(trimmedLine, knot))
        {
            if (snippet.Beats.Num() > 0) {
                block.Snippets.Add(snippet);
            }
            if (block.Snippets.Num() > 0) {
                scene.Blocks.Add(block);
            }
            if (scene.Blocks.Num() > 0) {
                outDinkScenes.Add(scene);
            }
            parsing = false;
            scene = FDinkScene();
            scene.SceneID = FName(knot);
            block = FDinkBlock();
            block.BlockID = "";
            snippet = FDinkSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
            UE_LOG(LogDinkFormat, Log, TEXT("Began scene: %s"), *knot);
            continue;
        }
        else if (ParseStitch(trimmedLine, stitch))
        {
            if (snippet.Beats.Num() > 0) {
                block.Snippets.Add(snippet);
            }
            if (block.Snippets.Num() > 0) {
                scene.Blocks.Add(block);
            }
            block = FDinkBlock();
            block.BlockID = FName(stitch);
            snippet = FDinkSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
            UE_LOG(LogDinkFormat, Log, TEXT("Began snippet: %s"), *stitch);
            continue;
        }
        else if (trimmedLine == "#dink")
        {
            parsing = true;
            UE_LOG(LogDinkFormat, Log, TEXT("Parsing dink snippet."));
            continue;
        }
        else if (ParseComment(trimmedLine))
        {
            continue;
        }
        else if (ParseLine(trimmedLine, dinkBeat))
        {
            if (!parsing)
            {
                UE_LOG(LogDinkFormat, Warning, TEXT("Read line that looks like Dink, but it's not in a #dink-tagged part of the Ink. This looks wrong!"));
                UE_LOG(LogDinkFormat, Warning, TEXT("    %s"), *dinkBeat.ToString());
                continue;
            }
            else
            {
                snippet.Beats.Add(dinkBeat);
                UE_LOG(LogDinkFormat, Log, TEXT("Parsed line: %s"), *dinkBeat.ToString());
                continue;
            }
        }
        else if (parsing && ParseAction(trimmedLine, dinkBeat))
        {
            snippet.Beats.Add(dinkBeat);
            UE_LOG(LogDinkFormat, Log, TEXT("Parsed action: %s"), *dinkBeat.ToString());
            continue;
        }
    }
    if (snippet.Beats.Num() > 0) {
        block.Snippets.Add(snippet);
    }
    if (block.Snippets.Num() > 0) {
        scene.Blocks.Add(block);
    }
    if (scene.Blocks.Num() > 0) {
        outDinkScenes.Add(scene);
    }
    return true;
}

static FName FindExistingSnippetID(
    const TArray<FName>& NewBeatIds,
    const TArray<FDinkSnippet>& ExistingSnippets,
    double MinOverlapScore = 0.5)
{
    // Build a set of new beat IDs for fast lookup
    TSet<FName> NewSet;
    NewSet.Reserve(NewBeatIds.Num());
    for (const FName& BeatId : NewBeatIds)
    {
        if (!BeatId.IsNone())
        {
            NewSet.Add(BeatId);
        }
    }

    if (NewSet.Num() == 0 || ExistingSnippets.Num() == 0)
    {
        return FName(); // no basis for comparison
    }

    FName BestId;
    double BestScore = 0.0;

    for (const FDinkSnippet& Snippet : ExistingSnippets)
    {
        // Build a set of existing beat IDs for this snippet
        TSet<FName> OldSet;
        OldSet.Reserve(Snippet.Beats.Num());
        for (const FDinkBeat& Beat : Snippet.Beats)
        {
            if (!Beat.LineID.IsNone())
            {
                OldSet.Add(Beat.LineID);
            }
        }

        if (OldSet.Num() == 0)
        {
            continue;
        }

        // Compute intersection size
        int32 IntersectionCount = 0;
        for (const FName& BeatId : NewSet)
        {
            if (OldSet.Contains(BeatId))
            {
                ++IntersectionCount;
            }
        }

        // Compute union size: |A ? B| = |A| + |B| - |A ? B|
        const int32 UnionCount = NewSet.Num() + OldSet.Num() - IntersectionCount;
        if (UnionCount <= 0)
        {
            continue;
        }

        const double Score = static_cast<double>(IntersectionCount) / static_cast<double>(UnionCount);

        if (Score > BestScore)
        {
            BestScore = Score;
            BestId = Snippet.SnippetID;
        }
    }

    // Only reuse an existing snippet if similarity exceeds threshold
    if (BestScore >= MinOverlapScore)
    {
        return BestId;
    }

    return FName(); // no suitable match
}

FString UDinkParser::RemoveBlockComments(const FString& Text)
{
    const FRegexPattern Pattern(TEXT("/\\*[\\s\\S]*?\\*/"));
    FRegexMatcher Matcher(Pattern, Text);

    FString Result = Text;
    while (Matcher.FindNext())
    {
        int32 Start = Matcher.GetMatchBeginning();
        int32 End = Matcher.GetMatchEnding();
        Result.RemoveAt(Start, End - Start);
        Matcher = FRegexMatcher(Pattern, Result);
    }

    return Result;
}

bool UDinkParser::ParseInk(const FString& text, TArray<FDinkScene>& outDinkScenes)
{
    FString textWithoutComments = RemoveBlockComments(text);
    TArray<FString> lines;
    textWithoutComments.ParseIntoArrayLines(lines, false);
    if (ParseInkLines(lines, outDinkScenes))
        return true;
    return false;
}
