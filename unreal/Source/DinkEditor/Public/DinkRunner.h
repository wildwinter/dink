#pragma once

#include "CoreMinimal.h"
#include "DinkRunner.generated.h"

UCLASS()
class DINKEDITOR_API UDinkRunner : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool CompileMinimal(const FString& sourceFile, const FString& destFolder);

    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool CompileProject(const FString& projectFile);

    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool CompileWithStructure(const FString& sourceFile, const FString& destFolder);
};