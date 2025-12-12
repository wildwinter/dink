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



private:
};

class FDinkEditorModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

private:
};

DECLARE_LOG_CATEGORY_EXTERN(LogDinkEditor, Log, All);