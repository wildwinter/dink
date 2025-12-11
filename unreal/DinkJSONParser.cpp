#include "DinkFormat.h" // Ensure this includes your structs (FDinkScene, etc.)
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
static FDinkBeat ParseBeat(TSharedPtr<FJsonObject> JsonBeat)
{
    FDinkBeat Beat;

    // 1. Common Fields
    FString TypeStr = JsonBeat->GetStringField(TEXT("Type"));
    Beat.Type = ParseBeatType(TypeStr);
    
    Beat.LineID = FName(*JsonBeat->GetStringField(TEXT("LineID")));
    Beat.Text = JsonBeat->GetStringField(TEXT("Text"));

    const TArray<TSharedPtr<FJsonValue>>* TagsArray;
    if (JsonBeat->TryGetArrayField(TEXT("Tags"), TagsArray))
    {
        for (const auto& TagVal : *TagsArray)
        {
            Beat.Tags.Add(TagVal->AsString());
        }
    }

    // 2. Line-specific Fields
    if (Beat.Type == EDinkBeatType::Line)
    {
        Beat.CharacterID = FName(*JsonBeat->GetStringField(TEXT("CharacterID")));
        Beat.Qualifier = JsonBeat->GetStringField(TEXT("Qualifier"));
        Beat.Direction = JsonBeat->GetStringField(TEXT("Direction"));
    }

    // Note: 'Origin' and 'Comments' in the JSON are currently ignored 
    // as they don't exist in the FDinkBeat struct in DinkFormat.h.

    return Beat;
}

// Helper to parse a Snippet
static FDinkSnippet ParseSnippet(TSharedPtr<FJsonObject> JsonSnippet)
{
    FDinkSnippet Snippet;
    Snippet.SnippetID = FName(*JsonSnippet->GetStringField(TEXT("SnippetID")));

    const TArray<TSharedPtr<FJsonValue>>* BeatsArray;
    if (JsonSnippet->TryGetArrayField(TEXT("Beats"), BeatsArray))
    {
        for (const auto& BeatVal : *BeatsArray)
        {
            TSharedPtr<FJsonObject> BeatObj = BeatVal->AsObject();
            if (BeatObj.IsValid())
            {
                Snippet.Beats.Add(ParseBeat(BeatObj));
            }
        }
    }
    return Snippet;
}

// Helper to parse a Block
static FDinkBlock ParseBlock(TSharedPtr<FJsonObject> JsonBlock)
{
    FDinkBlock Block;
    // Handle empty block IDs which might come in as null or empty strings
    FString BlockIDStr;
    if (JsonBlock->TryGetStringField(TEXT("BlockID"), BlockIDStr))
    {
        Block.BlockID = FName(*BlockIDStr);
    }

    const TArray<TSharedPtr<FJsonValue>>* SnippetsArray;
    if (JsonBlock->TryGetArrayField(TEXT("Snippets"), SnippetsArray))
    {
        for (const auto& SnippetVal : *SnippetsArray)
        {
            TSharedPtr<FJsonObject> SnippetObj = SnippetVal->AsObject();
            if (SnippetObj.IsValid())
            {
                Block.Snippets.Add(ParseSnippet(SnippetObj));
            }
        }
    }
    return Block;
}

// Helper to parse a Scene
static FDinkScene ParseScene(TSharedPtr<FJsonObject> JsonScene)
{
    FDinkScene Scene;
    Scene.SceneID = FName(*JsonScene->GetStringField(TEXT("SceneID")));

    const TArray<TSharedPtr<FJsonValue>>* BlocksArray;
    if (JsonScene->TryGetArrayField(TEXT("Blocks"), BlocksArray))
    {
        for (const auto& BlockVal : *BlocksArray)
        {
            TSharedPtr<FJsonObject> BlockObj = BlockVal->AsObject();
            if (BlockObj.IsValid())
            {
                Scene.Blocks.Add(ParseBlock(BlockObj));
            }
        }
    }
    return Scene;
}

bool UDinkJSONParser::ParseDinkJSON(const FString& JsonRaw, TArray<FDinkScene>& OutScenes)
{
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonRaw);
    TArray<TSharedPtr<FJsonValue>> JsonRootArray;

    if (FJsonSerializer::Deserialize(Reader, JsonRootArray))
    {
        for (const auto& SceneVal : JsonRootArray)
        {
            TSharedPtr<FJsonObject> SceneObj = SceneVal->AsObject();
            if (SceneObj.IsValid())
            {
                OutScenes.Add(ParseScene(SceneObj));
            }
        }
        return true;
    }

    UE_LOG(LogTemp, Error, TEXT("DinkJSONParser: Failed to deserialize JSON."));
    return false;
}