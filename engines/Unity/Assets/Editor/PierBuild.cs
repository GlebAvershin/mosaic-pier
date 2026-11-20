using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PierBuild {
  public static void Verify() {
    VerifyChapters(null);
  }
  public static void VerifyChapters(Action<int, int> chapterComplete) {
    if (Rules.mp_count() != 6)
      throw new Exception("Unexpected campaign size");
    for (int ch = 0; ch < 6; ch++) {
      Rules.mp_start(ch);
      int movesAtStart = Rules.mp_moves();
      while (Rules.mp_status() == 0) {
        int p = Rules.mp_hint();
        if (p < 0 || Rules.mp_swap(p / 36, p % 36) == 0)
          throw new Exception("Invalid hint");
        while (Rules.mp_status() == 1)
          Rules.mp_step();
      }
      if (Rules.mp_status() != 2)
        throw new Exception("Unsolved chapter " + ch);
      chapterComplete?.Invoke(ch, movesAtStart - Rules.mp_moves());
    }
    Debug.Log("MOSAIC_NATIVE_REPLAY_OK");
  }
  public static void Mac() {
    Verify();
    PlayerSettings.companyName = "Gleb Avershin";
    PlayerSettings.productName = "Mosaic Pier";
    PlayerSettings.bundleVersion = "1.0.0";
    PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,
                                            "games.glebavershin.mosaicpier");
    PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
    PlayerSettings.runInBackground = true;
    PlayerSettings.defaultScreenWidth = 1000;
    PlayerSettings.defaultScreenHeight = 640;
    PlayerSettings.SetIcons(
        NamedBuildTarget.Unknown,
        new[] { AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Resources/lagoon.png") }, IconKind.Any);
    var target = Environment.GetEnvironmentVariable("MOSAIC_BUILD") ??
                 "Build/Mosaic Pier.app";
    var scenes = new[] { "Assets/Scenes/Pier.unity" };
    var report = BuildPipeline.BuildPlayer(
        scenes, target, BuildTarget.StandaloneOSX, BuildOptions.None);
    if (report.summary.result != BuildResult.Succeeded)
      throw new Exception(report.summary.result.ToString());
  }
}
