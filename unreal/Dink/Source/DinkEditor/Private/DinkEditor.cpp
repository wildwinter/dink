#include "DinkEditor.h"
#include "Modules/ModuleManager.h"
#include "Logging/LogMacros.h"
#include "DinkEditorSettings.h"
#include "DinkyUserSettings.h"
#include "ToolMenus.h"
#include "Misc/Paths.h"
#include "HAL/PlatformProcess.h"
#include "PropertyEditorModule.h"
#include "Internationalization/StringTable.h"
#include "DinkStringTableCustomization.h"
#include "Toolkits/AssetEditorToolkitMenuContext.h"

#define LOCTEXT_NAMESPACE "FDinkEditorModule"

DEFINE_LOG_CATEGORY(LogDinkEditor);

namespace
{
    // Best-effort guess at a standard Dinky install location, used only when
    // Editor Preferences -> Plugins -> Dinky -> Dinky Executable Path is blank
    // or stale. Not authoritative - an explicitly configured path always wins,
    // and this is just a fallback so a fresh install works with zero setup.
    bool FindDefaultDinkyExecutable(FString& OutPath)
    {
#if PLATFORM_WINDOWS
        const FString ProgramFiles = FPlatformMisc::GetEnvironmentVariable(TEXT("ProgramFiles"));
        const FString ProgramFilesX86 = FPlatformMisc::GetEnvironmentVariable(TEXT("ProgramFiles(x86)"));
        const FString LocalAppData = FPlatformMisc::GetEnvironmentVariable(TEXT("LOCALAPPDATA"));

        const TArray<FString> Candidates = {
            FPaths::Combine(ProgramFiles, TEXT("Dinky"), TEXT("Dinky.exe")),
            FPaths::Combine(ProgramFilesX86, TEXT("Dinky"), TEXT("Dinky.exe")),
            // electron-builder's default per-user install location.
            FPaths::Combine(LocalAppData, TEXT("Programs"), TEXT("dinky"), TEXT("Dinky.exe")),
            FPaths::Combine(LocalAppData, TEXT("Programs"), TEXT("Dinky"), TEXT("Dinky.exe")),
        };
#elif PLATFORM_MAC
        const TArray<FString> Candidates = {
            TEXT("/Applications/Dinky.app"),
            FPaths::Combine(FPlatformProcess::UserHomeDir(), TEXT("Applications"), TEXT("Dinky.app")),
        };
#else
        const TArray<FString> Candidates;
#endif
        for (const FString& Candidate : Candidates)
        {
            if (!Candidate.IsEmpty() && (FPaths::FileExists(Candidate) || FPaths::DirectoryExists(Candidate)))
            {
                OutPath = Candidate;
                return true;
            }
        }
        return false;
    }
}

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

void UDinkEditor::ReportIssues(const FString& Title, const FString& LogContent)
{
    const float WindowWidth = 800.0f;
    const float WindowHeight = 500.0f;

    TSharedRef<SWindow> Window = SNew(SWindow)
        .Title(FText::FromString(Title))
        .ClientSize(FVector2D(WindowWidth, WindowHeight))
        .SupportsMinimize(false)
        .SupportsMaximize(false)
        .SizingRule(ESizingRule::UserSized);

    Window->SetContent(
        SNew(SVerticalBox)
        // Log Area
        + SVerticalBox::Slot()
        .FillHeight(1.0f)
        .Padding(10.0f)
        [
            SNew(SBorder)
                .BorderImage(FAppStyle::GetBrush("ToolPanel.GroupBorder"))
                .Padding(5.0f)
                [
                    SNew(SScrollBox)
                        + SScrollBox::Slot()
                        [
                            SNew(STextBlock)
                                .Text(FText::FromString(LogContent))
                                .Font(FAppStyle::GetFontStyle("Log.Font")) // Monospaced font
                                .AutoWrapText(false) // Allow horizontal scrolling for long code/log lines
                        ]
                ]
        ]
    // Button Area
    + SVerticalBox::Slot()
        .AutoHeight()
        .HAlign(HAlign_Right)
        .Padding(10.0f)
        [
            SNew(SButton)
                .Text(NSLOCTEXT("Dink", "Close", "Close"))
                .OnClicked_Lambda([Window]() {
                Window->RequestDestroyWindow();
                return FReply::Handled();
                    })
                .ContentPadding(FMargin(20.0f, 5.0f))
        ]
        );

    // Present as a modal window blocking the editor until closed
    FSlateApplication::Get().AddModalWindow(Window, nullptr);
}

void FDinkEditorModule::StartupModule()
{
    UE_LOG(LogDinkEditor, Log, TEXT("DinkEditor module has started."));

    UToolMenus::RegisterStartupCallback(
        FSimpleMulticastDelegate::FDelegate::CreateRaw(this, &FDinkEditorModule::RegisterMenus));

    FPropertyEditorModule& PropertyModule =
        FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");
    PropertyModule.RegisterCustomClassLayout(
        UStringTable::StaticClass()->GetFName(),
        FOnGetDetailCustomizationInstance::CreateStatic(&FDinkStringTableCustomization::MakeInstance)
    );
    PropertyModule.NotifyCustomizationModuleChanged();
}

void FDinkEditorModule::ShutdownModule()
{
    UToolMenus::UnRegisterStartupCallback(this);
    if (UObjectInitialized())
    {
        UToolMenus::Get()->RemoveSection("MainFrame.MainMenu.Tools", "Dink");
        UToolMenus::Get()->RemoveSection("AssetEditor.StringTableEditor.ToolBar", "DinkBanner");
    }

    if (FModuleManager::Get().IsModuleLoaded("PropertyEditor"))
    {
        FPropertyEditorModule& PropertyModule =
            FModuleManager::GetModuleChecked<FPropertyEditorModule>("PropertyEditor");
        PropertyModule.UnregisterCustomClassLayout(UStringTable::StaticClass()->GetFName());
    }
}

void FDinkEditorModule::RegisterMenus()
{
    FToolMenuOwnerScoped OwnerScoped(this);

    UToolMenu* ToolsMenu = UToolMenus::Get()->ExtendMenu("MainFrame.MainMenu.Tools");
    FToolMenuSection& Section = ToolsMenu->FindOrAddSection("Dink");
    Section.Label = LOCTEXT("DinkSectionLabel", "Dink");

    Section.AddSubMenu(
        "DinkSubMenu",
        LOCTEXT("DinkSubMenuLabel", "Dink"),
        LOCTEXT("DinkSubMenuTooltip", "Dink tools"),
        FNewMenuDelegate::CreateLambda([](FMenuBuilder& MenuBuilder)
        {
            MenuBuilder.AddMenuEntry(
                LOCTEXT("OpenDinky", "Open Dinky"),
                LOCTEXT("OpenDinkyTooltip", "Open the Dinky editor with the project's .dinkproj file"),
                FSlateIcon(),
                FUIAction(FExecuteAction::CreateStatic(&FDinkEditorModule::OpenDinky, FString()))
            );
        })
    );

    UToolMenu* STToolbar = UToolMenus::Get()->ExtendMenu(TEXT("AssetEditor.StringTableEditor.ToolBar"));
    STToolbar->AddDynamicSection(TEXT("DinkBanner"), FNewToolMenuDelegate::CreateLambda([](UToolMenu* InMenu)
    {
        UAssetEditorToolkitMenuContext* Context = InMenu->FindContext<UAssetEditorToolkitMenuContext>();
        if (!Context) return;

        bool bIsDinkTable = false;
        for (UObject* Obj : Context->GetEditingObjects())
        {
            if (UStringTable* ST = Cast<UStringTable>(Obj))
            {
                if (ST->GetName().StartsWith(TEXT("DinkStrings_")))
                {
                    bIsDinkTable = true;
                    break;
                }
            }
        }
        if (!bIsDinkTable) return;

        FToolMenuSection& BannerSection = InMenu->FindOrAddSection(TEXT("DinkBanner"));
        BannerSection.AddEntry(FToolMenuEntry::InitWidget(
            "DinkBannerWidget",
            SNew(SBorder)
                .BorderImage(FAppStyle::GetBrush("NoBorder"))
                .Padding(FMargin(6.f, 2.f))
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Managed by Dink - do not edit manually."))
                    .ColorAndOpacity(FSlateColor(FLinearColor(1.f, 0.85f, 0.3f)))
                ],
            FText::GetEmpty(),
            true
        ));
    }));
}

void FDinkEditorModule::OpenDinky(FString GotoTarget)
{
    const UDinkEditorSettings* Settings = GetDefault<UDinkEditorSettings>();
    if (!Settings || Settings->ProjectFilePath.IsEmpty())
    {
        UE_LOG(LogDinkEditor, Warning, TEXT("OpenDinky: No project file path set in Dink Editor Settings."));
        return;
    }

    auto ResolveAbsolute = [](const FString& InPath) -> FString
    {
        return FPaths::IsRelative(InPath)
            ? FPaths::ConvertRelativePathToFull(FPaths::ProjectDir(), InPath)
            : InPath;
    };

    const FString AbsoluteProjectPath = ResolveAbsolute(Settings->ProjectFilePath);

    // Navigating to a target requires launching Dinky directly, since the OS
    // "open with" verb used below has no way to pass it "--goto". Dinky is a
    // normal installed app (not on the system PATH), so that means knowing
    // where its executable actually is.
    FString AbsoluteExePath;
    if (!GotoTarget.IsEmpty())
    {
        // Per-user Editor Preference, not a Project Setting - where Dinky is
        // installed varies machine to machine, so it must not live in the
        // shared, source-controlled project config (see UDinkyUserSettings).
        const UDinkyUserSettings* UserSettings = GetDefault<UDinkyUserSettings>();
        if (UserSettings && !UserSettings->DinkyExecutablePath.IsEmpty())
        {
            AbsoluteExePath = ResolveAbsolute(UserSettings->DinkyExecutablePath);
            if (!FPaths::FileExists(AbsoluteExePath))
            {
                UE_LOG(LogDinkEditor, Warning,
                    TEXT("OpenDinky: Dinky executable not found at '%s' (Editor Preferences -> Plugins -> Dinky) - trying a default install location instead."),
                    *AbsoluteExePath);
                AbsoluteExePath.Reset();
            }
        }

        // Nothing explicitly configured (or it didn't exist) - fall back to a
        // best-effort guess at a standard install location so this works with
        // zero setup on a fresh machine. Never overrides an explicit setting.
        if (AbsoluteExePath.IsEmpty())
        {
            FString DefaultPath;
            if (FindDefaultDinkyExecutable(DefaultPath))
            {
                UE_LOG(LogDinkEditor, Log,
                    TEXT("OpenDinky: Using auto-detected Dinky install at '%s'. Set Editor Preferences -> Plugins -> Dinky -> Dinky Executable Path to override."),
                    *DefaultPath);
                AbsoluteExePath = DefaultPath;
            }
        }

        if (AbsoluteExePath.IsEmpty())
        {
            UE_LOG(LogDinkEditor, Warning,
                TEXT("OpenDinky: Couldn't find a Dinky install (set Editor Preferences -> Plugins -> Dinky -> Dinky Executable Path), so '--goto %s' can't be applied - opening the project normally instead."),
                *GotoTarget);
            GotoTarget.Reset();
        }
    }

    if (GotoTarget.IsEmpty())
    {
        // Open the file with its OS-registered default application.
#if PLATFORM_WINDOWS
        FPlatformProcess::CreateProc(
            TEXT("cmd.exe"),
            *FString::Printf(TEXT("/c start \"\" \"%s\""), *AbsoluteProjectPath),
            true, true, false, nullptr, 0, nullptr, nullptr
        );
#elif PLATFORM_MAC
        FPlatformProcess::CreateProc(
            TEXT("/usr/bin/open"),
            *FString::Printf(TEXT("\"%s\""), *AbsoluteProjectPath),
            true, false, false, nullptr, 0, nullptr, nullptr
        );
#else
        UE_LOG(LogDinkEditor, Warning, TEXT("OpenDinky: unsupported platform"));
#endif
        return;
    }

    // Dinky is single-instance, so if it's already running this just focuses
    // it and navigates, rather than opening a second copy.
    UE_LOG(LogDinkEditor, Log, TEXT("OpenDinky: Launching '%s' \"%s\" --goto \"%s\""), *AbsoluteExePath, *AbsoluteProjectPath, *GotoTarget);
#if PLATFORM_WINDOWS
    FPlatformProcess::CreateProc(
        *AbsoluteExePath,
        *FString::Printf(TEXT("\"%s\" --goto \"%s\""), *AbsoluteProjectPath, *GotoTarget),
        true, false, false, nullptr, 0, nullptr, nullptr
    );
#elif PLATFORM_MAC
    // Support both a raw executable and a .app bundle in the settings path.
    if (AbsoluteExePath.EndsWith(TEXT(".app")))
    {
        FPlatformProcess::CreateProc(
            TEXT("/usr/bin/open"),
            *FString::Printf(TEXT("-a \"%s\" --args \"%s\" --goto \"%s\""), *AbsoluteExePath, *AbsoluteProjectPath, *GotoTarget),
            true, false, false, nullptr, 0, nullptr, nullptr
        );
    }
    else
    {
        FPlatformProcess::CreateProc(
            *AbsoluteExePath,
            *FString::Printf(TEXT("\"%s\" --goto \"%s\""), *AbsoluteProjectPath, *GotoTarget),
            true, false, false, nullptr, 0, nullptr, nullptr
        );
    }
#else
    UE_LOG(LogDinkEditor, Warning, TEXT("OpenDinky: unsupported platform"));
#endif
}

IMPLEMENT_MODULE(FDinkEditorModule, DinkEditor)

#undef LOCTEXT_NAMESPACE
