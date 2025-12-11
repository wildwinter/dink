#include "DinkFormat.h"

DEFINE_LOG_CATEGORY_STATIC(LogDinkFormat, Log, All);

FString FDinkBeat::ToString() const
{
    FString dump = FString::Printf(TEXT("[%s] "), *LineID.ToString());

    if (Type == EDinkBeatType::Line) {

        dump += FString::Printf(TEXT("Line | CharacterID: %s"), *CharacterID.ToString());

        if (!Qualifier.IsEmpty())
            dump += FString::Printf(TEXT(" | Qualifier: %s"), *Qualifier);

        if (!Direction.IsEmpty())
            dump += FString::Printf(TEXT(" | Direction: %s"), *Direction);
    }
    else if (Type == EDinkBeatType::Action)
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