#include "DinkRunner.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/Paths.h"
#include "DinkEditor.h"
#include "HAL/PlatformProcess.h"
#include "DinkEditorSettings.h"

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

bool RunCompiler(TArray<FString>& args)
{
    FString AbsoluteExePath;
    if (!FindExePath(AbsoluteExePath))
        return false;

    FString Params = FString::Join(args, TEXT(" "));

    UE_LOG(LogDinkEditor, Log, TEXT("Calling DinkCompiler with params:\"%s\""), *Params);

    void* PipeRead = nullptr;
    void* PipeWrite = nullptr;

    if (!FPlatformProcess::CreatePipe(PipeRead, PipeWrite)) // <--- IMPORTANT
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

        while (FPlatformProcess::IsProcRunning(Handle))
        {
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

bool UDinkRunner::CompileMinimal(const FString& sourceFile, const FString& destFolder)
{
    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--source \"%s\""), *sourceFile));
    args.Add(FString::Printf(TEXT("--destFolder \"%s\""), *destFolder));
    if (RunCompiler(args))
        return true;
	return false;
}


bool UDinkRunner::CompileProject(TArray<FString> additionalArgs)
{
    const UDinkEditorSettings* Settings = GetDefault<UDinkEditorSettings>();
    if (!Settings||Settings->ProjectFilePath.IsEmpty())
    {
        UE_LOG(LogDinkEditor, Error, TEXT("No Dink project file set up in Project Settings."));
        return false;
    }

    FString projectFile = Settings->ProjectFilePath;
    FString fullProjectPath = FPaths::Combine(FPaths::ProjectDir(), projectFile);
    fullProjectPath = FPaths::ConvertRelativePathToFull(fullProjectPath);
    if (!FPaths::FileExists(fullProjectPath))
    {
        UE_LOG(LogDinkEditor, Error, TEXT("Couldn't find Dink project file: %s"), *fullProjectPath);
        return false;
    }

    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--project \"%s\""), *fullProjectPath));
    args.Append(additionalArgs);
    if (RunCompiler(args))
        return true;
    return false;
}

bool UDinkRunner::CompileWithProject(const FString& sourceFile, const FString& destFolder, bool withStructure)
{
    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--source \"%s\""), *sourceFile));
    args.Add(FString::Printf(TEXT("--destFolder \"%s\""), *destFolder));
    if (withStructure)
        args.Add(TEXT("--dinkStructure"));
    return CompileProject(args);
}