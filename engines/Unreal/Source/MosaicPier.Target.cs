using UnrealBuildTool;
public class MosaicPierTarget : TargetRules
{
    public MosaicPierTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        ExtraModuleNames.Add("MosaicPier");
    }
}
