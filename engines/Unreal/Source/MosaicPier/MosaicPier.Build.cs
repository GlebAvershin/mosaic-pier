using System.IO;
using UnrealBuildTool;
public class MosaicPier : ModuleRules
{
    public MosaicPier(ReadOnlyTargetRules target) : base(target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new[] {"Core", "CoreUObject", "Engine", "InputCore"});
        string core = Path.GetFullPath(Path.Combine(ModuleDirectory, "../../../../core"));
        PrivateIncludePaths.Add(core);
        foreach (string source in new[] {"pier.h", "pier.cpp", "campaign.inc"})
            ExternalDependencies.Add(Path.Combine(core, source));
    }
}
