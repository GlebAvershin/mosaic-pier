using UnrealBuildTool;
public class MosaicPierEditorTarget : TargetRules
{
    public MosaicPierEditorTarget(TargetInfo target) : base(target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        ExtraModuleNames.Add("MosaicPier");
    }
}
