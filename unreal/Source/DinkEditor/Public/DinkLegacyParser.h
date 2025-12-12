#pragma once

#include "CoreMinimal.h"
#include "DinkStructure.h"
#include "DinkLegacyParser.generated.h"

UCLASS(BlueprintType)
class DINKEDITOR_API UDinkLegacyParser : public UObject
{
    GENERATED_BODY()

public:

    static void ParseDinkScenes(const FString& sourceFile, TArray<FDinkStructureScene>& outParsedScenes);
};