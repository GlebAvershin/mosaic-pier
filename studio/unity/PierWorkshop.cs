using System;
using System.Collections.Generic;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

static class PierWorkshop
{
    [CliGroupCommand("mosaic", "Replay the shipped campaign through the native rules plug-in in Edit Mode.")]
    internal static ExitCode VerifyCampaign(CommandContext cmd)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before replaying the campaign.");

        var chapters = Run(false);
        var commissions = Run(true);
        cmd.Result.Write(new { editor = Application.unityVersion, chapters, commissions });
        return ExitCode.Success;
    }

    static List<object> Run(bool master)
    {
        var chapters = new List<object>();
        PierBuild.VerifyChapters((chapter, turns) => chapters.Add(new {
            chapter, turns, spare = Rules.mp_moves(), score = Rules.mp_score(),
            panes_left = Rules.mp_left()
        }), master);
        return chapters;
    }
}
