#include "DinkLegacyParser.h"

DEFINE_LOG_CATEGORY_STATIC(LogDinkLegacyParser, Log, All);

static void ParseTags(const FString& tagsRaw, FDinkStructureBeat& outDinkBeat) {
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
        UE_LOG(LogDinkLegacyParser, Warning, TEXT("Dink beat is missing a LineID! %s"), *outDinkBeat.ToString());
    }
}

bool ParseLine(const FString& line, FDinkStructureBeat& outBeat)
{
    /*
    Regex logic for parsing standard Dialogue lines
    */
    const FRegexPattern pattern(TEXT(
        R"(^\s*[-+*]?\s*([A-Z0-9_]+)\s*(?:\(([^)]*)\))?\s*:\s*(?:\(([^)]*)\))?\s*([^\r\n#]*?)\s*(#[^\s#]+(?:\s*#[^\s#]+)*)?$)"
    ));
    FRegexMatcher matcher(pattern, line);

    if (!matcher.FindNext())
        return false; // Line doesn't match expected format

    outBeat.Type = EDinkBeatType::Line;
    outBeat.CharacterID = FName(matcher.GetCaptureGroup(1));
    outBeat.Qualifier = matcher.GetCaptureGroup(2);
    outBeat.Direction = matcher.GetCaptureGroup(3);
    outBeat.Text = matcher.GetCaptureGroup(4).TrimEnd();

    FString tagsRaw = matcher.GetCaptureGroup(5);
    ParseTags(tagsRaw, outBeat);

    return true;
}


bool ParseAction(const FString& line, FDinkStructureBeat& outBeat)
{
    /*
    Regex logic for parsing Action lines
    */
    const FRegexPattern pattern(TEXT(
        R"(^\s*[-]?\s*([^\*\+][^\r\n#]*?)\s*((?:#[^\s#]+)(?:\s*#[^\s#]+)*)\s*$)"
    ));
    FRegexMatcher matcher(pattern, line);

    if (!matcher.FindNext())
        return false; // Line doesn't match expected format

    outBeat.Type = EDinkBeatType::Action;
    outBeat.Text = matcher.GetCaptureGroup(1).TrimEnd();

    FString tagsRaw = matcher.GetCaptureGroup(2);
    ParseTags(tagsRaw, outBeat);

    return true;
}

// Helper functions internal to the legacy parser
static bool IsFlowBreakingLine(const FString& InInput)
{
    FString Input = InInput;
    Input.TrimStartAndEndInline();

    if (Input.IsEmpty()) return false;

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

    for (const TCHAR Char : Input)
    {
        if (Char == TEXT('{')) OpenCount++;
        else if (Char == TEXT('}')) CloseCount++;
    }

    return OpenCount != CloseCount;
}

static bool ParseComment(const FString& line)
{
    return (line.StartsWith("//"));
}

static bool ParseKnot(const FString& Line, FString& outKnot)
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

static bool ParseStitch(const FString& Line, FString& outStitch)
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
    static const int32 CharsetSize = UE_ARRAY_COUNT(Charset) - 1; 

    FString Result;
    Result.Reserve(4);

    for (int32 i = 0; i < 4; i++)
    {
        int32 Index = FMath::RandRange(0, CharsetSize - 1);
        Result.AppendChar(Charset[Index]);
    }

    return Result;
}


static bool ParseExpressionClause(const FString& Line, FString& OutExpression, bool& OutIsError)
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

bool ParseInkLines(const TArray<FString>& lines, TArray<FDinkStructureScene>& outDinkScenes)
{
    FDinkStructureScene scene;
    FDinkStructureBlock block;
    FDinkStructureSnippet snippet;
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

            snippet = FDinkStructureSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
        };

    for (const FString line : lines)
    {
        FString trimmedLine = line.TrimStartAndEnd();
        FString knot;
        FString stitch;
        FDinkStructureBeat dinkBeat;

        UE_LOG(LogDinkLegacyParser, Log, TEXT("Parsing line: %s"), *trimmedLine);

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
                    UE_LOG(LogDinkLegacyParser, Fatal, TEXT("Dink Format Error: Line starts with expression but has content after colon.\n.   %s"), *trimmedLine);
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
            scene = FDinkStructureScene();
            scene.SceneID = FName(knot);
            block = FDinkStructureBlock();
            block.BlockID = "";
            snippet = FDinkStructureSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
            UE_LOG(LogDinkLegacyParser, Log, TEXT("Began scene: %s"), *knot);
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
            block = FDinkStructureBlock();
            block.BlockID = FName(stitch);
            snippet = FDinkStructureSnippet();
            snippet.SnippetID = FName(GenerateShortHash());
            UE_LOG(LogDinkLegacyParser, Log, TEXT("Began snippet: %s"), *stitch);
            continue;
        }
        else if (trimmedLine == "#dink")
        {
            parsing = true;
            UE_LOG(LogDinkLegacyParser, Log, TEXT("Parsing dink snippet."));
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
                UE_LOG(LogDinkLegacyParser, Warning, TEXT("Read line that looks like Dink, but it's not in a #dink-tagged part of the Ink. This looks wrong!"));
                UE_LOG(LogDinkLegacyParser, Warning, TEXT("    %s"), *dinkBeat.ToString());
                continue;
            }
            else
            {
                snippet.Beats.Add(dinkBeat);
                UE_LOG(LogDinkLegacyParser, Log, TEXT("Parsed line: %s"), *dinkBeat.ToString());
                continue;
            }
        }
        else if (parsing && ParseAction(trimmedLine, dinkBeat))
        {
            snippet.Beats.Add(dinkBeat);
            UE_LOG(LogDinkLegacyParser, Log, TEXT("Parsed action: %s"), *dinkBeat.ToString());
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

FString RemoveBlockComments(const FString& Text)
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

bool ParseInk(const FString& text, TArray<FDinkStructureScene>& outDinkScenes)
{
    FString textWithoutComments = RemoveBlockComments(text);
    TArray<FString> lines;
    textWithoutComments.ParseIntoArrayLines(lines, false);
    if (ParseInkLines(lines, outDinkScenes))
        return true;
    return false;
}

FString GetCanonicalFullPath(const FString& path)
{
    FString fullPath = FPaths::ConvertRelativePathToFull(path);
    FPaths::MakeStandardFilename(fullPath);
    return fullPath;
}

bool LoadSourceFile(const FString& filePath, FString& outText)
{
    FString fullPath = FPaths::ConvertRelativePathToFull(filePath);

    if (FFileHelper::LoadFileToString(outText, *fullPath))
        return true;

    UE_LOG(LogDinkLegacyParser, Warning, TEXT("Failed to load file: %s"), *fullPath);
    return false;
}

// Loads all the source from this starting file path into the current InkSourceMap
void LoadAllSource(const FString& startFilePath, TMap<FString, FString>& outSourceMap)
{
    TArray<FString> toLoad;

    FString fullStartPath = GetCanonicalFullPath(startFilePath);
    FString rootPath = FPaths::GetPath(fullStartPath);

    toLoad.Add(FPaths::GetCleanFilename(startFilePath));

    while (toLoad.Num() > 0)
    {
        FString sourceFilePath = toLoad.Pop();
        FString sourceFileFullPath = GetCanonicalFullPath(FPaths::Combine(rootPath, sourceFilePath));

        if (outSourceMap.Contains(sourceFileFullPath))
        {
            // Already loaded, no need to do it again.
            continue;
        }

        FString fileText;
        if (!LoadSourceFile(sourceFileFullPath, fileText))
            continue;

        outSourceMap.Add(sourceFileFullPath, fileText);
        //UE_LOG(LogDinkLegacyParser, Log, TEXT("Loaded ink source file: %s"), *sourceFileFullPath);

        // Search for INCLUDE
        fileText = RemoveBlockComments(fileText);
        TArray<FString> lines;
        fileText.ParseIntoArrayLines(lines, false);

        const FRegexPattern pattern(TEXT("^\\s*INCLUDE\\s+([^\\s\\/\\*]+(?:\\/[^\\s\\/\\*]+)*)\\s*(?:\\/\\/.*|\\*\\*.*)?$"));

        for (const FString& line : lines)
        {
            FRegexMatcher matcher(pattern, line);
            if (matcher.FindNext())
            {
                const FString& newInclude = matcher.GetCaptureGroup(1);
                //UE_LOG(LogDinkLegacyParser, Log, TEXT("Found include required: %s"), *newInclude);
                toLoad.Add(newInclude);
            }
        }

    }
}

void UDinkLegacyParser::ParseDinkScenes(const FString& sourceFile, TArray<FDinkStructureScene>& outParsedScenes) 
{

    FString inkRootName = FPaths::GetBaseFilename(FPaths::GetCleanFilename(sourceFile));

    TMap<FString, FString> InkSourceMap;
    LoadAllSource(sourceFile, InkSourceMap);

    TArray<FString> inkFiles;
    InkSourceMap.GetKeys(inkFiles);

    UE_LOG(LogDinkLegacyParser, Log, TEXT("Source maps loaded."));

    for (const FString& inkFile : inkFiles)
    {
        //UE_LOG(LogDinkSequencesEditor, Log, TEXT("Processing: %s"), *inkFile);
        FString inkFileContent = InkSourceMap[inkFile];
        ParseInk(inkFileContent, outParsedScenes);
    }

    UE_LOG(LogDinkLegacyParser, Log, TEXT("Parsed %d scenes"), outParsedScenes.Num());
}
