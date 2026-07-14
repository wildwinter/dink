#pragma once

#include "CoreMinimal.h"
#include "Engine/DeveloperSettings.h"
#include "DinkyUserSettings.generated.h"

/**
 * Per-user Dinky preference (Editor Preferences, not Project Settings).
 *
 * Config = EditorPerProjectUserSettings routes this to the local, non-source-
 * controlled Saved/Config ini. Unlike UDinkEditorSettings::ProjectFilePath
 * (the same .dinkproj for the whole team), where Dinky is installed is a
 * per-machine fact - every artist/writer is likely to have it in a different
 * local path, so it must not live in the shared, source-controlled project
 * config.
 */
UCLASS(Config = EditorPerProjectUserSettings, meta = (DisplayName = "Dinky"))
class DINKEDITOR_API UDinkyUserSettings : public UDeveloperSettings
{
    GENERATED_BODY()

public:
    virtual FName GetCategoryName() const override { return FName("Plugins"); }
    virtual FName GetSectionName() const override { return FName("Dinky"); }

    // Path to the Dinky executable on this machine (e.g.
    // "C:/Program Files/Dinky/Dinky.exe"). Dinky is normally installed as a
    // regular desktop app, not onto the system PATH, so "Open Dinky" can't
    // just run a bare "dinky" command. Only required for "--goto" navigation
    // (jumping straight to a scene, block, or line from an Unreal context
    // menu) - leave blank to keep opening files via the OS's default file
    // association, with no navigation.
    UPROPERTY(Config, EditAnywhere, Category = "General")
    FString DinkyExecutablePath;
};
