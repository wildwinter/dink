#pragma once

#include "CoreMinimal.h"
#include "DinkFormat.h"
#include "DinkLegacyParser.generated.h"

UCLASS(BlueprintType)
class DINK_API UDinkLegacyParser : public UObject
{
    GENERATED_BODY()

public:
    static bool ParseAction(const FString& line, FDinkBeat& outBeat);
    static bool ParseLine(const FString& line, FDinkBeat& outBeat);
    static bool ParseInkLines(const TArray<FString>& lines, TArray<FDinkScene>& outDinkScenes);
    static bool ParseInk(const FString& text, TArray<FDinkScene>& outDinkScenes);
    static FString RemoveBlockComments(const FString& Text);
};