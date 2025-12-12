#pragma once

#include "CoreMinimal.h"
#include "DinkRuntimeParser.generated.h"

struct FDinkBeat;

UCLASS()
class DINK_API UDinkRuntimeParser : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool ParseJSON(const FString& JsonRaw, TMap<FName, FDinkBeat>& OutBeats);
};