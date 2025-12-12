#include "Dink.h"

#define LOCTEXT_NAMESPACE "FDinkModule"

DEFINE_LOG_CATEGORY(LogDink);

static FDelayedAutoRegisterHelper DelayedAutoRegister(
	EDelayedRegisterRunPhase::EndOfEngineInit,
	[] {
		if (UDink* dink = GEngine->GetEngineSubsystem<UDink>())
			dink->Register();
	}
);

UDink* UDink::Get()
{
    UDink* dink = GEngine->GetEngineSubsystem<UDink>();
    if (!dink)
    {
        UE_LOG(LogDink, Fatal, TEXT("Dink subsystem not available."));
    }
    return dink;
}

UDink::UDink()
{
	
}

void UDink::Initialize(FSubsystemCollectionBase& InCollection)
{
	Super::Initialize(InCollection);
}

void UDink::Register()
{
}

void FDinkModule::StartupModule()
{

}

void FDinkModule::ShutdownModule()
{

}

IMPLEMENT_MODULE(FDinkModule, Dink)

#undef LOCTEXT_NAMESPACE