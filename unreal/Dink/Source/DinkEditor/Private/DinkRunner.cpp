#include "DinkRunner.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/Paths.h"
#include "DinkEditor.h"
#include "HAL/PlatformProcess.h"
#include "DinkEditorSettings.h"
#include "Misc/ScopedSlowTask.h"
#include "Misc/FileHelper.h"
#include "HAL/FileManager.h"
#include "Misc/Crc.h"
#include "Dom/JsonObject.h"
#include "Serialization/JsonReader.h"
#include "Serialization/JsonSerializer.h"
#include "Internationalization/StringTable.h"
#include "Internationalization/StringTableCore.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "UObject/Package.h"
#include "FileHelpers.h"

bool FindExePath(FString& outPath)
{
	FString PluginBaseDir = IPluginManager::Get().FindPlugin("Dink")->GetBaseDir();
	FString ExePath = FPaths::Combine(PluginBaseDir, TEXT("ThirdParty"), TEXT("Dink"), TEXT("DinkCompiler.exe"));
	FString AbsoluteExePath = FPaths::ConvertRelativePathToFull(ExePath);

	if (!FPaths::FileExists(AbsoluteExePath))
	{
		UE_LOG(LogDinkEditor, Error, TEXT("Couldn't find Dink executable: %s"), *AbsoluteExePath);
		return false;
	}
	outPath=AbsoluteExePath;
	return true;
}

bool RunCompiler(TArray<FString>& args, const FString& DisplayName = FString())
{
    FString AbsoluteExePath;
    if (!FindExePath(AbsoluteExePath))
        return false;

    FString Params = FString::Join(args, TEXT(" "));

    UE_LOG(LogDinkEditor, Log, TEXT("Calling DinkCompiler with params:\"%s\""), *Params);

    void* PipeRead = nullptr;
    void* PipeWrite = nullptr;

    if (!FPlatformProcess::CreatePipe(PipeRead, PipeWrite))
    {
        UE_LOG(LogDinkEditor, Error, TEXT("Failed to create pipes for compiler output!"));
        return false;
    }

    uint32 ProcessID = 0;
    FProcHandle Handle = FPlatformProcess::CreateProc(
        *AbsoluteExePath,
        *Params,
        false,   // bLaunchDetached (true = fire and forget, false = child process)
        true,  // bLaunchHidden (true = no window created)
        true,  // bLaunchReallyHidden
        NULL,   // PriorityModifier
        0,      // ProcessID (out)
        nullptr, // Working Directory (nullptr = same as executable)
        PipeWrite, // PipeWriteChild
        nullptr // PipeReadChild (nullptr = don't pipe input)
    );

    if (Handle.IsValid())
    {
        FString StdOutput;
        FString LatestOutput;

        FString Title = DisplayName.IsEmpty()
            ? TEXT("Dink: Compiling...")
            : FString::Printf(TEXT("Dink: Compiling %s..."), *DisplayName);
        FScopedSlowTask SlowTask(0, FText::FromString(Title));
        SlowTask.MakeDialog(/*bShowCancelButton=*/false);

        while (FPlatformProcess::IsProcRunning(Handle))
        {
            SlowTask.EnterProgressFrame(0);
            LatestOutput = FPlatformProcess::ReadPipe(PipeRead);
            StdOutput += LatestOutput;
            if (!LatestOutput.IsEmpty())
                UE_LOG(LogDinkEditor, Log, TEXT("%s"), *LatestOutput);

            FPlatformProcess::Sleep(0.1f);
        }

        LatestOutput = FPlatformProcess::ReadPipe(PipeRead);
        StdOutput += LatestOutput;
        if (!LatestOutput.IsEmpty())
            UE_LOG(LogDinkEditor, Log, TEXT("%s"), *LatestOutput);

        int32 ReturnCode = 0;
        FPlatformProcess::GetProcReturnCode(Handle, &ReturnCode);

        FPlatformProcess::CloseProc(Handle);
        FPlatformProcess::ClosePipe(PipeRead, PipeWrite);

        if (ReturnCode != 0)
        {
            UE_LOG(LogDinkEditor, Error, TEXT("Dink Compiler failed with code %d"), ReturnCode);
            UE_LOG(LogDinkEditor, Error, TEXT("Output: %s"), *StdOutput);

            UDinkEditor::Get()->ReportIssues(TEXT("Dink Compilation Failed"), StdOutput);

            return false;
        }
    }
    else
    {
        UE_LOG(LogDinkEditor, Error, TEXT("Failed to launch Dink compiler!"));
    }
    UE_LOG(LogDinkEditor, Log, TEXT("Dink compiler complete!"));
    return true;
}

static bool GetProjectFilePath(FString& OutFullPath)
{
    const UDinkEditorSettings* Settings = GetDefault<UDinkEditorSettings>();
    if (!Settings || Settings->ProjectFilePath.IsEmpty())
    {
        UE_LOG(LogDinkEditor, Error, TEXT("No Dink project file set up in Project Settings."));
        return false;
    }
    OutFullPath = FPaths::ConvertRelativePathToFull(
        FPaths::Combine(FPaths::ProjectDir(), Settings->ProjectFilePath));
    if (!FPaths::FileExists(OutFullPath))
    {
        UE_LOG(LogDinkEditor, Error, TEXT("Couldn't find Dink project file: %s"), *OutFullPath);
        return false;
    }
    return true;
}

bool UDinkRunner::CompileMinimal(const FString& sourceFile, const FString& destFolder)
{
    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--source \"%s\""), *sourceFile));
    args.Add(FString::Printf(TEXT("--destFolder \"%s\""), *destFolder));
	return RunCompiler(args);
}

bool UDinkRunner::CompileProject(TArray<FString> additionalArgs)
{
    FString fullProjectPath;
    if (!GetProjectFilePath(fullProjectPath)) return false;

    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--project \"%s\""), *fullProjectPath));
    args.Append(additionalArgs);
    return RunCompiler(args);
}

bool UDinkRunner::CompileWithProject(const FString& sourceFile, const FString& destFolder, bool withStructure)
{
    FString fullProjectPath;
    if (!GetProjectFilePath(fullProjectPath)) return false;

    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--project \"%s\""), *fullProjectPath));
    args.Add(FString::Printf(TEXT("--source \"%s\""), *sourceFile));
    args.Add(FString::Printf(TEXT("--destFolder \"%s\""), *destFolder));
    if (withStructure)
        args.Add(TEXT("--dinkStructure"));
    return RunCompiler(args, FPaths::GetBaseFilename(sourceFile));
}

bool UDinkRunner::ReadLocActions(bool& OutLocActions)
{
    OutLocActions = false;
    FString ProjPath;
    if (!GetProjectFilePath(ProjPath))
        return false;

    FString JsonContent;
    if (!FFileHelper::LoadFileToString(JsonContent, *ProjPath))
        return false;

    TSharedPtr<FJsonObject> JsonObj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonContent);
    if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid())
        return false;

    bool bVal = false;
    if (JsonObj->TryGetBoolField(TEXT("locActions"), bVal))
        OutLocActions = bVal;
    return true;
}

bool UDinkRunner::ImportStringTable(const FString& OutputDir, const FString& InkRootName,
    const FString& DestPackagePath, uint32& InOutHash, TArray<UPackage*>& OutPackagesToSave)
{
    // The compiler always emits {InkRootName}-strings-{LocaleCode}.json.
    // We glob for it so we don't hard-code the locale.
    TArray<FString> MatchingFiles;
    IFileManager::Get().FindFiles(MatchingFiles,
        *(OutputDir / (InkRootName + TEXT("-strings-*.json"))), true, false);

    if (MatchingFiles.IsEmpty())
    {
        UE_LOG(LogDinkEditor, Warning,
            TEXT("ImportStringTable: No strings file found in '%s' for '%s'"),
            *OutputDir, *InkRootName);
        return false;
    }

    FString StringsFilePath = OutputDir / MatchingFiles[0];

    FString JsonContent;
    if (!FFileHelper::LoadFileToString(JsonContent, *StringsFilePath))
    {
        UE_LOG(LogDinkEditor, Warning,
            TEXT("ImportStringTable: Could not load '%s'"), *StringsFilePath);
        return false;
    }

    // Fingerprint: skip reimport if file content unchanged.
    uint32 NewHash = FCrc::StrCrc32(*JsonContent);
    if (NewHash == InOutHash)
    {
        UE_LOG(LogDinkEditor, Log,
            TEXT("ImportStringTable: Strings unchanged for '%s', skipping."), *InkRootName);
        return true;
    }

    // Parse flat JSON: { "lineId": "text", ... }
    TSharedPtr<FJsonObject> JsonObj;
    TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(JsonContent);
    if (!FJsonSerializer::Deserialize(Reader, JsonObj) || !JsonObj.IsValid())
    {
        UE_LOG(LogDinkEditor, Warning,
            TEXT("ImportStringTable: Failed to parse JSON from '%s'"), *StringsFilePath);
        return false;
    }

    TMap<FString, FString> Strings;
    for (const auto& Pair : JsonObj->Values)
    {
        FString Value;
        if (Pair.Value->TryGetString(Value))
            Strings.Add(Pair.Key, Value);
    }

    // Create or find the UStringTable asset at DestPackagePath/DinkStrings_{InkRootName}
    FString AssetName = FString::Printf(TEXT("DinkStrings_%s"), *InkRootName);
    FString FullPackagePath = DestPackagePath / AssetName;

    UPackage* Package = CreatePackage(*FullPackagePath);
    Package->FullyLoad();

    UStringTable* StringTable = FindObject<UStringTable>(Package, *AssetName);
    bool bNewAsset = (StringTable == nullptr);
    if (bNewAsset)
    {
        StringTable = NewObject<UStringTable>(Package, FName(*AssetName),
            RF_Public | RF_Standalone | RF_Transactional);
    }

    // Clear existing entries then repopulate
    FStringTableRef ST = StringTable->GetMutableStringTable();
    TArray<FString> ExistingKeys;
    ST->EnumerateSourceStrings([&](const FString& Key, const FString&) -> bool {
        ExistingKeys.Add(Key);
        return true;
    });
    for (const FString& Key : ExistingKeys)
        ST->RemoveSourceString(FTextKey(Key));

    for (const auto& Pair : Strings)
        ST->SetSourceString(FTextKey(Pair.Key), Pair.Value);

    StringTable->MarkPackageDirty();
    OutPackagesToSave.AddUnique(Package);

    if (bNewAsset)
    {
        FAssetRegistryModule::AssetCreated(StringTable);
    }

    InOutHash = NewHash;

    UE_LOG(LogDinkEditor, Log,
        TEXT("ImportStringTable: Updated '%s' with %d entries."), *AssetName, Strings.Num());
    return true;
}
