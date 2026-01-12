#include "DinkStructure.h"

FString FDinkStructureBeat::ToString() const
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

FString FDinkStructureSnippet::ToString() const
{
    FString dump = FString::Printf(TEXT("  Snippet:%s Beats:%d"), *SnippetID.ToString(), Beats.Num());
    for (const FDinkStructureBeat& beat : Beats)
    {
        dump += FString::Printf(TEXT("\n    %s"), *beat.ToString());
    }
    return dump;
}

FString FDinkStructureBlock::ToString() const
{
    FString dump = FString::Printf(TEXT("  Block:%s Snippets:%d"), *BlockID.ToString(), Snippets.Num());
    for (const FDinkStructureSnippet& snippet : Snippets)
    {
        dump += FString::Printf(TEXT("\n        %s"), *snippet.ToString());
    }
    return dump;
}

FString FDinkStructureScene::ToString() const
{
    FString dump = FString::Printf(TEXT("Scene:%s Blocks:%d"), *SceneID.ToString(), Blocks.Num());
    for (const FDinkStructureBlock& block : Blocks)
    {
        dump += FString::Printf(TEXT("\n%s"), *block.ToString());
    }
    if (Tags.Num() > 0)
    {
        dump += TEXT(" | Tags:");
        for (const FString& tag : Tags)
            dump += FString::Printf(TEXT(" #%s"), *tag);
    }
    return dump;
}

FString GetDinkTagValue(const TArray<FString>& Tags, const FString& Key)
{
    // Build the prefix to look for (e.g., "type" becomes "type:")
    const FString SearchPrefix = Key + TEXT(":");

    for (const FString& Tag : Tags)
    {
        // Check if this tag starts with our prefix (Default is Case Insensitive)
        if (Tag.StartsWith(SearchPrefix, ESearchCase::IgnoreCase))
        {
            // Return everything after the prefix length
            return Tag.RightChop(SearchPrefix.Len());
        }
    }

    // Return empty string if not found
    return FString();
}

bool HasDinkTag(const TArray<FString>& Tags, const FString& Key)
{
    const FString SearchPrefix = Key + TEXT(":");

    for (const FString& Tag : Tags)
    {
        // Check for Exact Match (e.g. "type")
        if (Tag.Equals(Key, ESearchCase::IgnoreCase))
        {
            return true;
        }

        // Check for Key+Colon Start (e.g. "type:conversation")
        if (Tag.StartsWith(SearchPrefix, ESearchCase::IgnoreCase))
        {
            return true;
        }
    }

    return false;
}