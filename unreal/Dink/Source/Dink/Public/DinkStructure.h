#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "Dink.h"
#include "DinkStructure.generated.h"

struct FDinkBeat;

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

    // These apply only to Line type
    // BEGIN LINE TYPE

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName CharacterID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Qualifier;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Direction;

    // END LINE TYPE

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FString> Comments;

    FString ToString() const;
    void ToRuntime(FDinkBeat& Beat) const;
};

// Equivalent of an Ink flow fragment
USTRUCT(BlueprintType)
struct DINK_API FDinkStructureSnippet
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName SnippetID;

    // Comments that apply to the whole group this snippet belongs to (from "GroupComments" in JSON)
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FString> GroupComments;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FString> Comments;

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
    TArray<FString> Comments;

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
    TArray<FString> Comments;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FDinkStructureBlock> Blocks;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    TArray<FString> Tags;

    FString ToString() const;
};

// Finds a tag starting with "Key:" and returns the value after the colon.
FString DINK_API GetDinkTagValue(const TArray<FString>& Tags, const FString& Key);
// Returns true if "Key" exists exactly, or if a tag starts with "Key:"
bool DINK_API HasDinkTag(const TArray<FString>& Tags, const FString& Key);

/**
 * Blueprint-accessible helpers for reading Dink tags from an array of Ink tags.
 * Thin wrappers around the free functions above (which remain the C++ entry points).
 */
UCLASS()
class DINK_API UDinkTagLibrary : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:
    /** Returns the Dink line ID - the value of the "id:" tag - or None if there isn't one. */
    UFUNCTION(BlueprintPure, Category = "Dink|Tags")
    static FName GetLineID(const TArray<FString>& Tags);

    /** Returns the value of a tag by key, e.g. key "type" returns the value of "type:...". Empty if absent. Case-insensitive. */
    UFUNCTION(BlueprintPure, Category = "Dink|Tags")
    static FString GetDinkTagValue(const TArray<FString>& Tags, const FString& Key);

    /** True if the key exists exactly, or a tag starts with "Key:". */
    UFUNCTION(BlueprintPure, Category = "Dink|Tags")
    static bool HasDinkTag(const TArray<FString>& Tags, const FString& Key);
};