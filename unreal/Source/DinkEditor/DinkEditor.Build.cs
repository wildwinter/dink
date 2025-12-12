// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class DinkEditor : ModuleRules
{
    public DinkEditor(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;

        PublicIncludePaths.AddRange(
            new string[] {
            }
            );


        PrivateIncludePaths.AddRange(
            new string[] {
            }
            );


        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core",
                "CoreUObject",
                "EditorSubsystem",
                "Engine",
                "Slate",
                "SlateCore",
                "Json",
                "JsonUtilities"
            }
            );


        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "UnrealEd",
                "EditorStyle",
                "EditorSubsystem",
                "AssetRegistry",
                "Dink",
                "Projects"
            }
            );

        if (Target.bBuildEditor)
        {
            PrivateDependencyModuleNames.Add("EditorSubsystem");
        }


        DynamicallyLoadedModuleNames.AddRange(
            new string[]
            {
				// ... add any modules that your module loads dynamically here ...
			}
            );
    }
}
