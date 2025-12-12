#pragma once

#include "Logging/LogMacros.h"
#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Dink.generated.h"

UENUM(BlueprintType)
enum class EDinkBeatType : uint8
{
	Line    UMETA(DisplayName = "Line"),
	Action  UMETA(DisplayName = "Action")
};

UCLASS()
class DINK_API UDink : public UEngineSubsystem
{
	GENERATED_BODY()

public:
	UDink();

	virtual void Initialize(FSubsystemCollectionBase&) override;
	void Register();
	static UDink* Get();

private:
};

class FDinkModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};

DECLARE_LOG_CATEGORY_EXTERN(LogDink, Log, All);