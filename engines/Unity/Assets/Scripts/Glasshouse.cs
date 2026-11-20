using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public partial class Glasshouse : MonoBehaviour {
  static readonly string[] Titles = { "Tide Steps",     "Brass Arcade",
                                      "Citrus Court",   "Sea Lanterns",
                                      "The Sun Window", "Kingfisher Crown" };
  static readonly string[] Stones = { "lagoon", "honey", "coral", "orchid",
                                      "fern" };
  readonly Texture2D[] pieces = new Texture2D[5];
  Texture2D coast;
  int cursor;
  string replayOutput;
  bool master;
  string JournalKey(string field) {
    return (master ? "mp.master." : "mp.") + field;
  }
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
    InitializeWorkshop();
    for (int i = 0; i < pieces.Length; i++)
      pieces[i] = Resources.Load<Texture2D>(Stones[i]);
    if (replayOutput != null) {
      Directory.CreateDirectory(replayOutput);
      Begin(0);
      StartCoroutine(ReplayCampaign());
      return;
    }
    LoadJournal(PlayerPrefs.GetInt("mp.mode") == 1);
  }

  void LoadJournal(bool commission) {
    master = commission;
    revealAt = -10;
    unlock = Mathf.Clamp(PlayerPrefs.GetInt(JournalKey("restored")), 0, 6);
    Begin(Mathf.Clamp(PlayerPrefs.GetInt(JournalKey("chapter")), 0, 5), true);
    chapterClosed = chapter == 5 && unlock == 6 && Rules.mp_status() == 2 &&
                    PlayerPrefs.GetInt(JournalKey("completed")) == 1;
  }

  void Begin(int index, bool replay = false) {
    cursor = 0;
    chapter = index;
    if (master)
      Rules.mp_start_master(chapter);
    else
      Rules.mp_start(chapter);
    panelAt = Time.unscaledTime;
    chapterClosed = false;
    selected = hint = -1;
    message =
        master
            ? "Double rims mark two layers. A line sweep breaks both."
            : "Clear the striped panes. Four in a line sweeps the whole row " +
                  "or column.";
    if (replay)
      foreach (var item in PlayerPrefs.GetString(JournalKey("history"), "")
                   .Split(',')) {
        if (item == "undo" && Rules.mp_rewind() != 0)
          continue;
        if (!int.TryParse(item, out int pair) ||
            Rules.mp_swap(pair / 36, pair % 36) == 0)
          break;
        while (Rules.mp_status() == 1)
          Rules.mp_step();
      }
    else if (replayOutput == null) {
      PlayerPrefs.SetInt(JournalKey("completed"), 0);
      PlayerPrefs.SetString(JournalKey("history"), "");
      PlayerPrefs.SetInt(JournalKey("chapter"), chapter);
      PlayerPrefs.Save();
    }
  }

  void Pick(int i) {
    if (busy || Rules.mp_status() != 0)
      return;
    hint = -1;
    cueAt = Time.unscaledTime;
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
      rejected = i;
      message = "Try another swap. Look for three in a line.";
      return;
    }
    if (replayOutput == null)
      SaveEvent((old * 36 + i).ToString());
    swapFrom = old;
    swapTo = i;
    StartCoroutine(Resolve());
  }

  void SaveEvent(string action) {
    string prefix = PlayerPrefs.GetString(JournalKey("history"), "");
    PlayerPrefs.SetString(JournalKey("history"),
                          prefix + (prefix.Length > 0 ? "," : "") + action);
    PlayerPrefs.Save();
  }

  IEnumerator Resolve() {
    busy = true;
    exchanging = true;
    phase = Time.unscaledTime;
    Chime(0);
    yield return new WaitForSecondsRealtime(quietMotion ? .02f : .15f);
    exchanging = false;
    while (Rules.mp_status() == 1) {
      dropping = false;
      phase = Time.unscaledTime;
      message = Rules.mp_sweeps() > 0 ? "Line sweep!"
                                      : "Glass cascade ×" + Rules.mp_chain();
      Chime(Mathf.Min(4, Rules.mp_chain()));
      yield return new WaitForSecondsRealtime(quietMotion ? .04f : .21f);
      Rules.mp_step();
      dropping = true;
      phase = Time.unscaledTime;
      yield return new WaitForSecondsRealtime(quietMotion ? .04f : .27f);
    }
    dropping = busy = false;
    if (Rules.mp_status() == 2)
      Chime(5);
    if (Rules.mp_shuffle() != 0)
      message = "No exchanges remained. The glass has been reshuffled.";
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
    if (chapterClosed) {
      Begin(0);
      return;
    }
    revealFrom = unlock;
    revealAt = Time.unscaledTime;
    unlock = Mathf.Max(unlock, chapter + 1);
    if (replayOutput == null) {
      PlayerPrefs.SetInt(JournalKey("restored"), unlock);
      PlayerPrefs.Save();
    }
    if (chapter == 5 && replayOutput == null) {
      chapterClosed = true;
      PlayerPrefs.SetInt(JournalKey("completed"), 1);
      PlayerPrefs.Save();
      Chime(5);
    } else
      Begin((chapter + 1) % 6);
  }

  IEnumerator ReplayCampaign() {
    var report = new List<string>();
    yield return new WaitForEndOfFrame();
    ScreenCapture.CaptureScreenshot(Path.Combine(replayOutput, "player.png"));
    for (int journal = 0; journal < 2; journal++) {
      master = journal == 1;
      if (master) {
        unlock = 0;
        Begin(0);
      }
      report.Clear();
      report.Add("chapter,turns,spare,score");
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
      File.WriteAllLines(Path.Combine(replayOutput, master
                                                        ? "unity-master.csv"
                                                        : "unity-player.csv"),
                         report);
    }
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
      case KeyCode.Z:
        UndoExchange();
        break;
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
    DrawWorkshop();
  }

  void UndoExchange() {
    if (busy || chapterClosed || Rules.mp_rewind() == 0)
      return;
    selected = hint = rejected = -1;
    message = "Previous exchange undone. Your move is back.";
    if (replayOutput == null)
      SaveEvent("undo");
    Chime(0);
  }
}
