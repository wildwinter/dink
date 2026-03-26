#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "DinkEditorSettings.generated.h"

/**
 * Editor settings for Dink.
 */
UCLASS(Config = Dink, defaultconfig, meta = (DisplayName = "Dink Editor Settings"))
class DINKEDITOR_API UDinkEditorSettings : public UDeveloperSettings
{
    GENERATED_BODY()

public:
    // SETTINGS CONFIGURATION
    // -------------------------------------------------

    // The category in the Project Settings window (e.g., "Game", "Engine", or a custom one)
    virtual FName GetCategoryName() const override { return FName("Plugins"); }

    // The section name within the category
    virtual FName GetSectionName() const override { return FName("Dink"); }

    // SETTINGS PROPERTIES
    // -------------------------------------------------

    // Where is the project file, if there is one?
    UPROPERTY(Config, EditAnywhere, BlueprintReadOnly, Category = "General")
    FString ProjectFilePath;
};