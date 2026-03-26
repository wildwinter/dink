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
}

void FDinkEditorModule::ShutdownModule()
{
}

IMPLEMENT_MODULE(FDinkEditorModule, DinkEditor)

#undef LOCTEXT_NAMESPACE
