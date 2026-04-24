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
    static bool CompileProject(TArray<FString> additionalArgs);

    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool CompileWithProject(const FString& sourceFile, const FString& destFolder, bool withStructure = false);

    // Finds the compiler-output strings JSON in OutputDir, parses it, and creates/updates
    // a UStringTable asset named "DinkStrings_{InkRootName}" inside DestPackagePath.
    // InOutHash is compared against the file's CRC32; if unchanged the asset is left alone.
    // On change, InOutHash is updated and the dirty package is appended to OutPackagesToSave.
    static bool ImportStringTable(const FString& OutputDir, const FString& InkRootName,
        const FString& DestPackagePath, uint32& InOutHash, TArray<UPackage*>& OutPackagesToSave);

    // Reads the locActions setting from the configured .dinkproj file.
    // Returns false if the project file cannot be found or parsed; OutLocActions defaults to false.
    static bool ReadLocActions(bool& OutLocActions);
};