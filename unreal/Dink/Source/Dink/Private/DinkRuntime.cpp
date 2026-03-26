#include "DinkRuntime.h"


FString FDinkBeat::ToString() const
{
    FString dump = FString::Printf(TEXT("[%s] "), *LineID.ToString());

    if (Type == EDinkBeatType::Line) {

        dump += FString::Printf(TEXT("Line | CharacterID: %s"), *CharacterID.ToString());

        if (!Qualifier.IsEmpty())
            dump += FString::Printf(TEXT(" | Qualifier: %s"), *Qualifier);
    }
    else if (Type == EDinkBeatType::Action)
    {
        dump += FString::Printf(TEXT("Action"), *LineID.ToString());
        dump += FString::Printf(TEXT(" | Text: \"%s\""), *Text);
    }

    return dump;
}
