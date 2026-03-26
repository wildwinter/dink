#pragma once

#include "CoreMinimal.h"
#include "DinkStructureParser.generated.h"

struct FDinkStructureScene;

UCLASS()
class DINKEDITOR_API UDinkStructureParser : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool ParseJSON(const FString& JsonRaw, TArray<FDinkStructureScene>& OutScenes);
};