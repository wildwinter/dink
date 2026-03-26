#pragma once

#include "CoreMinimal.h"
#include "Dink.h"
#include "DinkRuntime.generated.h"

USTRUCT(BlueprintType)
struct DINK_API FDinkBeat
{
    GENERATED_BODY()

public:
    UPROPERTY(BlueprintReadOnly, VisibleAnywhere, Category = "Dink")
    EDinkBeatType Type;

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
