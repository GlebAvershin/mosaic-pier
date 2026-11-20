using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class Glasshouse : MonoBehaviour {
  static readonly string[] Titles = { "Tide Steps",     "Brass Arcade",
                                      "Citrus Court",   "Sea Lanterns",
                                      "The Sun Window", "Kingfisher Crown" };
  static readonly string[] Stones = { "lagoon", "honey", "coral", "orchid",
                                      "fern" };
  readonly Texture2D[] pieces = new Texture2D[5];
  Texture2D coast;
  int cursor;
  string replayOutput;
  int chapter, selected = -1, hint = -1, unlock;
  bool busy, dropping, keyboardFocus;
  float phase;
  string message;
  GUIStyle title, label;
  readonly Color sea = new Color(.07f, .22f, .25f),
                 cream = new Color(.96f, .93f, .83f);

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot() {
    new GameObject("Glass workshop").AddComponent<Glasshouse>();
  }

  void Start() {
    Application.targetFrameRate = 60;
    string[] args = Environment.GetCommandLineArgs();
    int flag = Array.IndexOf(args, "-mosaic-replay");
    if (flag >= 0 && flag + 1 < args.Length)
      replayOutput = Path.GetFullPath(args[flag + 1]);
    var camera = gameObject.AddComponent<Camera>();
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = sea;
    coast = Resources.Load<Texture2D>("glasshouse");
    for (int i = 0; i < pieces.Length; i++)
      pieces[i] = Resources.Load<Texture2D>(Stones[i]);
    if (replayOutput != null) {
      Directory.CreateDirectory(replayOutput);
      Begin(0);
      StartCoroutine(ReplayCampaign());
      return;
    }
    unlock = Mathf.Clamp(PlayerPrefs.GetInt("mp.restored"), 0, 6);
    Begin(Mathf.Clamp(PlayerPrefs.GetInt("mp.chapter"), 0, 5), true);
  }

  void Begin(int index, bool replay = false) {
    cursor = 0;
    chapter = index;
    Rules.mp_start(chapter);
    selected = hint = -1;
    message = "Clear the striped panes. Four in a line sweeps the whole row " +
              "or column.";
    if (replay)
      foreach (var item in PlayerPrefs.GetString("mp.history", "").Split(',')) {
        if (!int.TryParse(item, out int pair) ||
            Rules.mp_swap(pair / 36, pair % 36) == 0)
          break;
        while (Rules.mp_status() == 1)
          Rules.mp_step();
      }
    else if (replayOutput == null) {
      PlayerPrefs.SetString("mp.history", "");
      PlayerPrefs.SetInt("mp.chapter", chapter);
      PlayerPrefs.Save();
    }
  }

  void Pick(int i) {
    if (busy || Rules.mp_status() != 0)
      return;
    hint = -1;
    if (selected == i) {
      selected = -1;
      return;
    }
    if (selected < 0 ||
        Math.Abs(selected / 6 - i / 6) + Math.Abs(selected % 6 - i % 6) != 1) {
      selected = i;
      return;
    }
    int old = selected;
    selected = -1;
    if (Rules.mp_swap(old, i) == 0) {
      message = "Try another swap. Look for three in a line.";
      return;
    }
    if (replayOutput == null)
      SaveMove(old * 36 + i);
    StartCoroutine(Resolve());
  }

  void SaveMove(int pair) {
    string prefix = PlayerPrefs.GetString("mp.history", "");
    PlayerPrefs.SetString("mp.history",
                          prefix + (prefix.Length > 0 ? "," : "") + pair);
    PlayerPrefs.Save();
  }

  IEnumerator Resolve() {
    busy = true;
    while (Rules.mp_status() == 1) {
      dropping = false;
      phase = Time.unscaledTime;
      message = Rules.mp_sweeps() > 0 ? "Line sweep!"
                                      : "Glass cascade ×" + Rules.mp_chain();
      yield return new WaitForSecondsRealtime(.18f);
      Rules.mp_step();
      dropping = true;
      phase = Time.unscaledTime;
      yield return new WaitForSecondsRealtime(.2f);
    }
    dropping = busy = false;
  }

  void Panel(Rect rect, Color color) {
    GUI.color = color;
    GUI.DrawTexture(rect, Texture2D.whiteTexture);
    GUI.color = Color.white;
  }

  void Caption(Rect rect, string text, int size, Color color,
               bool heading = false) {
    var style = heading ? title : label;
    style.fontSize = size;
    style.normal.textColor = color;
    GUI.Label(rect, text, style);
  }

  bool ActionButton(Rect rect, string text) {
    Panel(rect, sea);
    Caption(rect, text, 16, cream);
    return GUI.Button(rect, GUIContent.none, GUIStyle.none);
  }

  void ShowHint() {
    if (Rules.mp_status() != 0)
      return;
    hint = Rules.mp_hint();
    selected = -1;
    cursor = hint / 36;
    message = "Swap the two highlighted pieces.";
  }

  void ContinueChapter() {
    if (Rules.mp_status() == 3) {
      Begin(chapter);
      return;
    }
    unlock = Mathf.Max(unlock, chapter + 1);
    if (replayOutput == null) {
      PlayerPrefs.SetInt("mp.restored", unlock);
      PlayerPrefs.Save();
    }
    Begin((chapter + 1) % 6);
  }

  IEnumerator ReplayCampaign() {
    var report = new List<string> { "chapter,turns,spare,score" };
    yield return new WaitForEndOfFrame();
    ScreenCapture.CaptureScreenshot(Path.Combine(replayOutput, "player.png"));
    for (int ch = 0; ch < 6; ch++) {
      int turns = 0;
      while (Rules.mp_status() == 0 && turns < 30) {
        ShowHint();
        int pair = hint;
        Pick(pair / 36);
        Pick(pair % 36);
        while (busy)
          yield return null;
        turns++;
      }
      if (Rules.mp_status() != 2 || Rules.mp_left() != 0) {
        Debug.LogError("Campaign replay failed at chapter " + ch);
        Application.Quit(2);
        yield break;
      }
      report.Add(
          string.Join(",", ch, turns, Rules.mp_moves(), Rules.mp_score()));
      ContinueChapter();
    }
    File.WriteAllLines(Path.Combine(replayOutput, "unity-player.csv"), report);
    Debug.Log("MOSAIC_PLAYER_REPLAY_OK");
    Application.Quit();
  }

  void OnGUI() {
    if (!coast)
      return;
    if (Event.current.type == EventType.KeyDown && !busy &&
        replayOutput == null) {
      keyboardFocus = true;
      switch (Event.current.keyCode) {
      case KeyCode.H:
        ShowHint();
        break;
      case KeyCode.RightArrow:
        if (cursor % 6 < 5)
          cursor++;
        break;
      case KeyCode.LeftArrow:
        if (cursor % 6 > 0)
          cursor--;
        break;
      case KeyCode.DownArrow:
        if (cursor < 30)
          cursor += 6;
        break;
      case KeyCode.UpArrow:
        if (cursor >= 6)
          cursor -= 6;
        break;
      case KeyCode.Space:
      case KeyCode.Return:
        if (Rules.mp_status() >= 2)
          ContinueChapter();
        else
          Pick(cursor);
        break;
      default:
        break;
      }
    }
    if (title == null) {
      title =
          new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                                         fontStyle = FontStyle.Bold };
      label =
          new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                                         wordWrap = true };
    }
    bool wide = Screen.width >= Screen.height;
    float w = wide ? 1000 : 540, h = wide ? 640 : 810;
    float s = Mathf.Min(Screen.width / w, Screen.height / h);
    GUI.matrix = Matrix4x4.TRS(
        new Vector3((Screen.width - w * s) / 2, (Screen.height - h * s) / 2, 0),
        Quaternion.identity, new Vector3(s, s, 1));
    var art = wide ? new Rect(0, 0, 460, h) : new Rect(0, 0, w, 170);
    GUI.DrawTexture(art, coast, ScaleMode.ScaleAndCrop);
    Panel(new Rect(art.x, art.yMax - 95, art.width, 95),
          new Color(.04f, .16f, .18f, .86f));
    Caption(new Rect(art.x, art.yMax - 93, art.width, 54), "Mosaic Pier", 38,
            cream, true);
    Caption(new Rect(art.x, art.yMax - 43, art.width, 35),
            "The Pier Glasshouse   ·   " + unlock + " / 6 restored", 16, cream);
    GUI.BeginGroup(wide ? new Rect(460, 0, 540, 640)
                        : new Rect(0, 170, 540, 640));
    Panel(new Rect(0, 0, 540, 640), cream);
    Caption(new Rect(5, 9, 530, 24), "CHAPTER " + (chapter + 1) + " / 6", 14,
            sea);
    Caption(new Rect(5, 30, 530, 46), Titles[chapter], 32, sea, true);
    Caption(new Rect(0, 78, 540, 28),
            "Moves  " + Rules.mp_moves() + "      Frost  " + Rules.mp_left() +
                "      Score  " + Rules.mp_score(),
            19, sea);
    Panel(new Rect(74, 116, 392, 392), sea);
    for (int i = 0; i < 36; i++) {
      float x = 78 + (i % 6) * 64, y = 120 + (i / 6) * 64;
      var rect = new Rect(x, y, 61, 61);
      bool marked =
          i == selected || (hint >= 0 && (i == hint / 36 || i == hint % 36));
      Panel(rect, marked                  ? new Color(1, .82f, .45f)
                  : Rules.mp_frost(i) > 0 ? new Color(.68f, .86f, .82f)
                                          : new Color(.23f, .40f, .42f));
      if (keyboardFocus && i == cursor)
        Panel(new Rect(x + 4, y + 55, 53, 3), cream);
      if (Rules.mp_frost(i) > 0) {
        for (int stripe = 0; stripe < 3; stripe++)
          Panel(new Rect(x + 5 + stripe * 7, y + 3, 3, 8), Color.white);
      }
      float t = Mathf.Clamp01((Time.unscaledTime - phase) / .2f);
      float size =
          busy && !dropping && Rules.mp_clear(i) > 0 ? 52 * (1 - .55f * t) : 52;
      float lift =
          busy && dropping ? Mathf.Min(2, Rules.mp_fall(i)) * (1 - t) * 20 : 0;
      GUI.DrawTexture(
          new Rect(x + (61 - size) / 2, y + (61 - size) / 2 - lift, size, size),
          pieces[Rules.mp_tile(i) - 1], ScaleMode.ScaleToFit);
      if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
        Pick(i);
    }
    int status = Rules.mp_status();
    string info = status == 2 ? "All panes clear. The glass is ready to fit."
                  : status == 3
                      ? "Out of moves. Try another route through the frost."
                      : message;
    Caption(new Rect(40, 514, 460, 47), info, 16, sea);
    GUI.enabled = !busy;
    if (status >= 2 && !busy) {
      if (ActionButton(new Rect(126, 572, 288, 43),
                       status == 3 ? "Try again"
                       : chapter == 5 && unlock == 6
                           ? "Replay campaign"
                           : "Restore " + Titles[chapter])) {
        ContinueChapter();
      }
    } else {
      if (ActionButton(new Rect(82, 572, 176, 43), "Show hint  [H]"))
        ShowHint();
      if (ActionButton(new Rect(278, 572, 176, 43), "Restart level"))
        Begin(chapter);
    }
    GUI.enabled = true;
    GUI.EndGroup();
    GUI.matrix = Matrix4x4.identity;
  }
}
