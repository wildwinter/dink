// Copyright Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;

public class Dink : ModuleRules
{
	public Dink(ReadOnlyTargetRules Target) : base(Target)
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
                "Engine"
            }
            );
			
		
		PrivateDependencyModuleNames.AddRange(
			new string[]
			{
                "Json",
                "JsonUtilities"
            }
            );

        if (Target.bBuildEditor)
        {
            PrivateDependencyModuleNames.AddRange(new string[] {
				"UnrealEd"
            }
			);
		}


        DynamicallyLoadedModuleNames.AddRange(
			new string[]
			{
				// ... add any modules that your module loads dynamically here ...
			}
			);
	}
}
