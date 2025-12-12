#include "DinkEditor.h"
#include "Modules/ModuleManager.h"
#include "Logging/LogMacros.h"

#define LOCTEXT_NAMESPACE "FDinkEditorModule"

DEFINE_LOG_CATEGORY(LogDinkEditor);

void FDinkEditorModule::StartupModule()
{
    UE_LOG(LogDinkEditor, Log, TEXT("DinkEditor module has started."));
}

void FDinkEditorModule::ShutdownModule()
{
}

IMPLEMENT_MODULE(FDinkEditorModule, DinkEditor)

#undef LOCTEXT_NAMESPACE
