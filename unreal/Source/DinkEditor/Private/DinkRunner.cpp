#include "DinkRunner.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/Paths.h"
#include "DinkEditor.h"
#include "HAL/PlatformProcess.h"

bool FindExePath(FString& outPath)
{
	FString PluginBaseDir = IPluginManager::Get().FindPlugin("Dink")->GetBaseDir();
	FString ExePath = FPaths::Combine(PluginBaseDir, TEXT("ThirdParty"), TEXT("Dink"), TEXT("DinkCompiler.exe"));
	FString AbsoluteExePath = FPaths::ConvertRelativePathToFull(ExePath);

	if (!FPaths::FileExists(AbsoluteExePath))
	{
		UE_LOG(LogDinkEditor, Log, TEXT("Couldn't find Dink executable: %s"), *AbsoluteExePath);
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

    uint32 ProcessID = 0;
    FProcHandle Handle = FPlatformProcess::CreateProc(
        *AbsoluteExePath,
        *Params,
        true,   // bLaunchDetached (true = fire and forget, false = child process)
        true,  // bLaunchHidden (true = no window created)
        true,  // bLaunchReallyHidden
        NULL,   // PriorityModifier
        0,      // ProcessID (out)
        nullptr, // Working Directory (nullptr = same as executable)
        nullptr  // PipeWriteChild (nullptr = don't pipe output)
    );

    if (Handle.IsValid())
    {
        // Success! 

        // Wait for it to finish
        FPlatformProcess::WaitForProc(Handle);

        // Always close the handle when done tracking it
        FPlatformProcess::CloseProc(Handle);
    }
    else
    {
        UE_LOG(LogDinkEditor, Error, TEXT("Failed to launch Dink compiler!"));
    }
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

bool UDinkRunner::CompileWithStructure(const FString& sourceFile, const FString& destFolder)
{
    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--source \"%s\""), *sourceFile));
    args.Add(FString::Printf(TEXT("--destFolder \"%s\""), *destFolder));
    args.Add(FString::Printf(TEXT("--dinkStructure"), *destFolder));
    if (RunCompiler(args))
        return true;
    return false;
}

bool UDinkRunner::CompileProject(const FString& projectFile)
{
    TArray<FString> args;
    args.Add(FString::Printf(TEXT("--project \"%s\""), *projectFile));
    if (RunCompiler(args))
        return true;
    return false;
}