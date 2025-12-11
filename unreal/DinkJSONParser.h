UCLASS()
class DINK_API UDinkJSONParser : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()
public:
    UFUNCTION(BlueprintCallable, Category = "Dink")
    static bool ParseDinkJSON(const FString& JsonRaw, TArray<FDinkScene>& OutScenes);
};