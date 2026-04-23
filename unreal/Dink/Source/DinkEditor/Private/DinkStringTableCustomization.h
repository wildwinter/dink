#pragma once

#include "IDetailCustomization.h"

// Shows a "Managed by Dink" banner at the top of any UStringTable asset whose
// name starts with "DinkStrings_", discouraging manual editing.
class FDinkStringTableCustomization : public IDetailCustomization
{
public:
    static TSharedRef<IDetailCustomization> MakeInstance();
    virtual void CustomizeDetails(IDetailLayoutBuilder& DetailBuilder) override;
};
