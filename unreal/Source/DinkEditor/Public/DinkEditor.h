#pragma once

#include "Logging/LogMacros.h"
#include "Modules/ModuleManager.h"

class FDinkEditorModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

private:
};

DECLARE_LOG_CATEGORY_EXTERN(LogDinkEditor, Log, All);