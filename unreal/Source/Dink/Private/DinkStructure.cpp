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
    return dump;
}