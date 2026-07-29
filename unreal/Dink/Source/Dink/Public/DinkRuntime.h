#pragma once

#include "CoreMinimal.h"
#include "Dink.h"
#include "DinkRuntime.generated.h"

USTRUCT(BlueprintType)
struct DINK_API FDinkBeat
{
    GENERATED_BODY()

public:
    // Explicitly initialised: a bare enum member is raw memory, so UE's
    // uninitialised-property validation reports it as an error at load. Line is the
    // zero value, which is what already sits in serialised beat sheets - do not
    // introduce a None sentinel here, as renumbering would reinterpret every
    // existing Line beat as an Action.
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    EDinkBeatType Type = EDinkBeatType::Line;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName LineID;

    // This applies only to Action type, if actions aren't localised
    // BEGIN ACTION TYPE
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Text;

    // END ACTION TYPE

    // These apply only to Line type
    // BEGIN LINE TYPE

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FName CharacterID;

    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    FString Qualifier;

    // END LINE TYPE

    FString ToString() const;
};
