#include "DinkStringTableCustomization.h"

#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "DetailCategoryBuilder.h"
#include "Widgets/Text/STextBlock.h"
#include "Widgets/Layout/SBorder.h"
#include "Styling/AppStyle.h"

TSharedRef<IDetailCustomization> FDinkStringTableCustomization::MakeInstance()
{
    return MakeShareable(new FDinkStringTableCustomization());
}

void FDinkStringTableCustomization::CustomizeDetails(IDetailLayoutBuilder& DetailBuilder)
{
    const TArray<TWeakObjectPtr<UObject>>& Objects = DetailBuilder.GetSelectedObjects();
    for (const TWeakObjectPtr<UObject>& WeakObj : Objects)
    {
        if (const UObject* Obj = WeakObj.Get())
        {
            if (!Obj->GetName().StartsWith(TEXT("DinkStrings_")))
                continue;

            IDetailCategoryBuilder& Category = DetailBuilder.EditCategory(
                "Dink", FText::FromString("Dink"), ECategoryPriority::Important);

            Category.AddCustomRow(FText::FromString("ManagedByDink"))
                .WholeRowContent()
                [
                    SNew(SBorder)
                    .BorderImage(FAppStyle::GetBrush("ToolPanel.GroupBorder"))
                    .Padding(FMargin(8.f, 6.f))
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString(
                            "This string table is managed by Dink and will be overwritten on import. Do not edit manually."))
                        .Font(IDetailLayoutBuilder::GetDetailFontItalic())
                        .ColorAndOpacity(FSlateColor(FLinearColor(1.f, 0.85f, 0.3f)))
                        .AutoWrapText(true)
                    ]
                ];
            return;
        }
    }
}
