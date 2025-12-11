#pragma once

#include "CoreMinimal.h"
#include "DinkFormat.generated.h"

UENUM(BlueprintType)
enum class EDinkBeatType : uint8
{
    Line    UMETA(DisplayName = "Line"),
    Action  UMETA(DisplayName = "Action")
};


USTRUCT(BlueprintType)
struct DINK_API FDinkBeat
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
    static void ParseTags(const FString& tagsRaw, FDinkBeat& outDinkBeat);
};

// Equivalent of an Ink flow fragment
USTRUCT(BlueprintType)
struct DINK_API FDinkSnippet
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName SnippetID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkBeat> Beats;

    FString ToString() const;
};

// Equivalent of an Ink Stitch
USTRUCT(BlueprintType)
struct DINK_API FDinkBlock
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName BlockID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkSnippet> Snippets;

    FString ToString() const;
};

// Equivalent of an Ink Knot
USTRUCT(BlueprintType)
struct DINK_API FDinkScene
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName SceneID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkBlock> Blocks;

    FString ToString() const;
};