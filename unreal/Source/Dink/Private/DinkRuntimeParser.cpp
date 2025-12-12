#include "DinkRuntimeParser.h"
#include "DinkRuntime.h"
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

static FDinkBeat ParseMinimalBeat(const FName& LineID, TSharedPtr<FJsonObject> JsonBeat)
{
    FDinkBeat Beat;
    Beat.LineID = LineID;

    // Type
    FString TypeStr;
    if (JsonBeat->TryGetStringField(TEXT("Type"), TypeStr))
    {
        Beat.Type = ParseBeatType(TypeStr);
    }

    // Text (Usually only present for Action types in this format)
    JsonBeat->TryGetStringField(TEXT("Text"), Beat.Text);

    // CharacterID
    FString CharIDStr;
    if (JsonBeat->TryGetStringField(TEXT("CharacterID"), CharIDStr))
    {
        Beat.CharacterID = FName(*CharIDStr);
    }

    // Qualifier
    JsonBeat->TryGetStringField(TEXT("Qualifier"), Beat.Qualifier);

    return Beat;
}

bool UDinkRuntimeParser::ParseJSON(const FString& JsonRaw, TMap<FName, FDinkBeat>& OutBeats)
{
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonRaw);
    TSharedPtr<FJsonObject> JsonRootObject;

    if (FJsonSerializer::Deserialize(Reader, JsonRootObject) && JsonRootObject.IsValid())
    {
        for (auto It = JsonRootObject->Values.CreateConstIterator(); It; ++It)
        {
            const FString& KeyLineID = It.Key();
            const TSharedPtr<FJsonValue>& Value = It.Value();

            TSharedPtr<FJsonObject> BeatObj = Value->AsObject();
            if (BeatObj.IsValid())
            {
                FName LineIDName = FName(*KeyLineID);
                FDinkBeat Beat = ParseMinimalBeat(LineIDName, BeatObj);
                OutBeats.Add(LineIDName, Beat);
            }
        }
        return true;
    }

    UE_LOG(LogDink, Error, TEXT("Failed to deserialize Dink runtime JSON."));
    return false;
}