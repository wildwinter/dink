#pragma once

#include "CoreMinimal.h"
#include "Dink.h"
#include "DinkStructure.generated.h"

USTRUCT(BlueprintType)
struct DINK_API FDinkStructureBeat
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    EDinkBeatType Type;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName LineID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FString> Tags;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Text;

    // These apply only to Line type
    // BEGIN LINE TYPE

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName CharacterID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Qualifier;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Direction;

    // END LINE TYPE

    FString ToString() const;
};

// Equivalent of an Ink flow fragment
USTRUCT(BlueprintType)
struct DINK_API FDinkStructureSnippet
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName SnippetID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkStructureBeat> Beats;

    FString ToString() const;
};

// Equivalent of an Ink Stitch
USTRUCT(BlueprintType)
struct DINK_API FDinkStructureBlock
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName BlockID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkStructureSnippet> Snippets;

    FString ToString() const;
};

// Equivalent of an Ink Knot
USTRUCT(BlueprintType)
struct DINK_API FDinkStructureScene
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName SceneID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkStructureBlock> Blocks;

    FString ToString() const;
};