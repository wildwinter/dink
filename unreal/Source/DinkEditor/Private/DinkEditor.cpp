#include "DinkEditor.h"
#include "Modules/ModuleManager.h"
#include "Logging/LogMacros.h"

#define LOCTEXT_NAMESPACE "FDinkEditorModule"

DEFINE_LOG_CATEGORY(LogDinkEditor);

static FDelayedAutoRegisterHelper DelayedAutoRegister(
	EDelayedRegisterRunPhase::EndOfEngineInit,
	[] {
		if (UDinkEditor* dinkEditor = GEngine->GetEngineSubsystem<UDinkEditor>())
			dinkEditor->Register();
	}
);

UDinkEditor* UDinkEditor::Get()
{
	UDinkEditor* dinkEditor = GEngine->GetEngineSubsystem<UDinkEditor>();
	if (!dinkEditor)
	{
		UE_LOG(LogDinkEditor, Fatal, TEXT("DinkEditor subsystem not available."));
	}
	return dinkEditor;
}

UDinkEditor::UDinkEditor()
{

}

void UDinkEditor::Initialize(FSubsystemCollectionBase& InCollection)
{
	Super::Initialize(InCollection);
}

void UDinkEditor::Register()
{
}


void FDinkEditorModule::StartupModule()
{
    UE_LOG(LogDinkEditor, Log, TEXT("DinkEditor module has started."));
}

void FDinkEditorModule::ShutdownModule()
{
}

IMPLEMENT_MODULE(FDinkEditorModule, DinkEditor)

#undef LOCTEXT_NAMESPACE
