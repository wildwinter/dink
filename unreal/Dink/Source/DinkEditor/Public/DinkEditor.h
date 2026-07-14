#pragma once

#include "Logging/LogMacros.h"
#include "Modules/ModuleManager.h"
#include "DinkEditor.generated.h"

UCLASS()
class DINKEDITOR_API UDinkEditor : public UEngineSubsystem
{
	GENERATED_BODY()

public:
	UDinkEditor();

	virtual void Initialize(FSubsystemCollectionBase&) override;
	void Register();
	static UDinkEditor* Get();
	void ReportIssues(const FString& Title, const FString& LogContent);

private:
};

class DINKEDITOR_API FDinkEditorModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	// Opens Dinky against the project's .dinkproj file (Dink Editor Settings ->
	// Project File Path). If GotoTarget is non-empty, it is passed as Dinky's
	// "--goto" command-line argument to navigate straight to a line ID or a
	// knot/stitch path (e.g. "MyScene" or "MyScene.MyBlock") - see
	// https://github.com/wildwinter/dinky/blob/main/doc/command-line.md.
	// Other plugins (e.g. HeraDinkBridge) call this to jump into a specific
	// scene/block from their own context menus.
	static void OpenDinky(FString GotoTarget = FString());

private:
	void RegisterMenus();
};

DECLARE_LOG_CATEGORY_EXTERN(LogDinkEditor, Log, All);