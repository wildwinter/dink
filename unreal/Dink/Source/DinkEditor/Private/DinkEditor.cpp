#include "DinkEditor.h"
#include "Modules/ModuleManager.h"
#include "Logging/LogMacros.h"
#include "DinkEditorSettings.h"
#include "ToolMenus.h"
#include "Misc/Paths.h"
#include "HAL/PlatformProcess.h"

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
}

void FDinkEditorModule::ShutdownModule()
{
    UToolMenus::UnRegisterStartupCallback(this);
    if (UObjectInitialized())
    {
        UToolMenus::Get()->RemoveSection("MainFrame.MainMenu.Tools", "Dink");
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
                FUIAction(FExecuteAction::CreateStatic(&FDinkEditorModule::OpenDinky))
            );
        })
    );
}

void FDinkEditorModule::OpenDinky()
{
    const UDinkEditorSettings* Settings = GetDefault<UDinkEditorSettings>();
    if (!Settings || Settings->ProjectFilePath.IsEmpty())
    {
        UE_LOG(LogDinkEditor, Warning, TEXT("OpenDinky: No project file path set in Dink Editor Settings."));
        return;
    }

    FString AbsolutePath = FPaths::IsRelative(Settings->ProjectFilePath)
        ? FPaths::ConvertRelativePathToFull(FPaths::ProjectDir(), Settings->ProjectFilePath)
        : Settings->ProjectFilePath;

    // Open the file with its OS-registered default application
#if PLATFORM_WINDOWS
    FPlatformProcess::CreateProc(
        TEXT("cmd.exe"),
        *FString::Printf(TEXT("/c start \"\" \"%s\""), *AbsolutePath),
        true, true, false, nullptr, 0, nullptr, nullptr
    );
#elif PLATFORM_MAC
    FPlatformProcess::CreateProc(
        TEXT("/usr/bin/open"),
        *FString::Printf(TEXT("\"%s\""), *AbsolutePath),
        true, false, false, nullptr, 0, nullptr, nullptr
    );
#else
    UE_LOG(LogDinkEditor, Warning, TEXT("OpenDinky: unsupported platform"));
#endif
}

IMPLEMENT_MODULE(FDinkEditorModule, DinkEditor)

#undef LOCTEXT_NAMESPACE
