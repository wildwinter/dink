#include "DinkStructureParser.h"
#include "DinkStructure.h"
#include "Serialization/JsonSerializer.h"
#include "Policies/CondensedJsonPrintPolicy.h"

// Helper to safely parse the 'Type' enum from a string
static EDinkBeatType ParseBeatType(const FString& TypeStr)
{
    if (TypeStr.Equals(TEXT("Action"), ESearchCase::IgnoreCase))
    {
        return EDinkBeatType::Action;
    }
    return EDinkBeatType::Line; // Default to Line
}

// Helper to parse a single Beat
static FDinkStructureBeat ParseBeat(TSharedPtr<FJsonObject> JsonBeat)
{
    FDinkStructureBeat Beat;

    // Common Fields
    FString TypeStr = JsonBeat->GetStringField(TEXT("Type"));
    Beat.Type = ParseBeatType(TypeStr);
    
    Beat.LineID = FName(*JsonBeat->GetStringField(TEXT("LineID")));
    Beat.Text = JsonBeat->GetStringField(TEXT("Text"));

    const TArray<TSharedPtr<FJsonValue>>* TagsArray;
    if (JsonBeat->TryGetArrayField(TEXT("Tags"), TagsArray))
    {
        Beat.Tags.Reserve(TagsArray->Num());
        for (const auto& TagVal : *TagsArray)
        {
            Beat.Tags.Emplace(TagVal->AsString());
        }
    }

    // Line-specific Fields
    if (Beat.Type == EDinkBeatType::Line)
    {
        Beat.CharacterID = FName(*JsonBeat->GetStringField(TEXT("CharacterID")));
        Beat.Qualifier = JsonBeat->GetStringField(TEXT("Qualifier"));
        Beat.Direction = JsonBeat->GetStringField(TEXT("Direction"));
    }

    const TArray<TSharedPtr<FJsonValue>>* CommentsArray;
    if (JsonBeat->TryGetArrayField(TEXT("Comments"), CommentsArray))
    {
        Beat.Comments.Reserve(CommentsArray->Num());
        for (const auto& CommentVal : *CommentsArray)
            Beat.Comments.Emplace(CommentVal->AsString());
    }

    return Beat;
}

// Helper to parse a Snippet
static FDinkStructureSnippet ParseSnippet(TSharedPtr<FJsonObject> JsonSnippet)
{
    FDinkStructureSnippet Snippet;
    Snippet.SnippetID = FName(*JsonSnippet->GetStringField(TEXT("SnippetID")));

    const TArray<TSharedPtr<FJsonValue>>* SnippetCommentsArray;
    if (JsonSnippet->TryGetArrayField(TEXT("Comments"), SnippetCommentsArray))
    {
        Snippet.Comments.Reserve(SnippetCommentsArray->Num());
        for (const auto& CommentVal : *SnippetCommentsArray)
            Snippet.Comments.Emplace(CommentVal->AsString());
    }

    const TArray<TSharedPtr<FJsonValue>>* BeatsArray;
    if (JsonSnippet->TryGetArrayField(TEXT("Beats"), BeatsArray))
    {
        Snippet.Beats.Reserve(BeatsArray->Num());
        for (const auto& BeatVal : *BeatsArray)
        {
            TSharedPtr<FJsonObject> BeatObj = BeatVal->AsObject();
            if (BeatObj.IsValid())
            {
                Snippet.Beats.Emplace(ParseBeat(BeatObj));
            }
        }
    }
    return Snippet;
}

// Helper to parse a Block
static FDinkStructureBlock ParseBlock(TSharedPtr<FJsonObject> JsonBlock)
{
    FDinkStructureBlock Block;
    // Handle empty block IDs which might come in as null or empty strings
    FString BlockIDStr;
    if (JsonBlock->TryGetStringField(TEXT("BlockID"), BlockIDStr))
    {
        Block.BlockID = FName(*BlockIDStr);
    }

    const TArray<TSharedPtr<FJsonValue>>* BlockCommentsArray;
    if (JsonBlock->TryGetArrayField(TEXT("Comments"), BlockCommentsArray))
    {
        Block.Comments.Reserve(BlockCommentsArray->Num());
        for (const auto& CommentVal : *BlockCommentsArray)
            Block.Comments.Emplace(CommentVal->AsString());
    }

    const TArray<TSharedPtr<FJsonValue>>* SnippetsArray;
    if (JsonBlock->TryGetArrayField(TEXT("Snippets"), SnippetsArray))
    {
        Block.Snippets.Reserve(SnippetsArray->Num());
        for (const auto& SnippetVal : *SnippetsArray)
        {
            TSharedPtr<FJsonObject> SnippetObj = SnippetVal->AsObject();
            if (SnippetObj.IsValid())
            {
                Block.Snippets.Emplace(ParseSnippet(SnippetObj));
            }
        }
    }
    return Block;
}

// Helper to parse a Scene
static FDinkStructureScene ParseScene(TSharedPtr<FJsonObject> JsonScene)
{
    FDinkStructureScene Scene;
    Scene.SceneID = FName(*JsonScene->GetStringField(TEXT("SceneID")));

    const TArray<TSharedPtr<FJsonValue>>* BlocksArray;
    if (JsonScene->TryGetArrayField(TEXT("Blocks"), BlocksArray))
    {
        Scene.Blocks.Reserve(BlocksArray->Num());
        for (const auto& BlockVal : *BlocksArray)
        {
            TSharedPtr<FJsonObject> BlockObj = BlockVal->AsObject();
            if (BlockObj.IsValid())
            {
                Scene.Blocks.Emplace(ParseBlock(BlockObj));
            }
        }
    }

    const TArray<TSharedPtr<FJsonValue>>* TagsArray;
    if (JsonScene->TryGetArrayField(TEXT("Tags"), TagsArray))
    {
        Scene.Tags.Reserve(TagsArray->Num());
        for (const auto& TagVal : *TagsArray)
        {
            Scene.Tags.Emplace(TagVal->AsString());
        }
    }
    return Scene;
}

bool UDinkStructureParser::ParseJSON(const FString& JsonRaw, TArray<FDinkStructureScene>& OutScenes)
{
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonRaw);
    TArray<TSharedPtr<FJsonValue>> JsonRootArray;

    if (FJsonSerializer::Deserialize(Reader, JsonRootArray))
    {
        OutScenes.Reserve(OutScenes.Num()+JsonRootArray.Num());
        for (const auto& SceneVal : JsonRootArray)
        {
            TSharedPtr<FJsonObject> SceneObj = SceneVal->AsObject();
            if (SceneObj.IsValid())
            {
                OutScenes.Emplace(ParseScene(SceneObj));
            }
        }
        return true;
    }

    UE_LOG(LogTemp, Error, TEXT("DinkJSONParser: Failed to deserialize JSON."));
    return false;
}