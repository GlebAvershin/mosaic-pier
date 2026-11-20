using UnityEngine;

public partial class Glasshouse {
  static readonly Color Ink = new Color(.058f, .12f, .15f);
  static readonly Color Enamel = new Color(.105f, .205f, .23f);
  static readonly Color Brass = new Color(.76f, .63f, .39f);
  static readonly Color Pale = new Color(.94f, .86f, .68f);
  Texture2D silvered;
  readonly Texture2D[] sockets = new Texture2D[3];
  Font displayFace;
  AudioSource voice;
  AudioClip[] notes;
  bool sound, quietMotion, exchanging, chapterClosed;
  int swapFrom, swapTo, rejected = -1, pressed = -1, revealFrom;
  float cueAt, panelAt, revealAt = -10;
  Vector2 pressPoint;

  void InitializeWorkshop() {
    coast = Resources.Load<Texture2D>("kingfisher");
    silvered = Resources.Load<Texture2D>("silvered");
    sockets[0] = Resources.Load<Texture2D>("enamel");
    sockets[1] = Resources.Load<Texture2D>("rime");
    sockets[2] = Resources.Load<Texture2D>("double-rime");
    displayFace = Resources.Load<Font>("CormorantGaramond");
    quietMotion = PlayerPrefs.GetInt("mp.quietMotion") == 1;
    sound = PlayerPrefs.GetInt("mp.sound", 1) == 1;
    var listener = gameObject.AddComponent<AudioListener>();
    listener.enabled = replayOutput == null;
    voice = gameObject.AddComponent<AudioSource>();
    float[] pitches = { 330, 392, 494, 587, 740, 880 };
    notes = new AudioClip[pitches.Length];
    for (int k = 0; k < pitches.Length; k++) {
      const int rate = 22050;
      var samples = new float[rate / 2];
      for (int i = 0; i < samples.Length; i++) {
        float t = i / (float)rate, a = t * pitches[k] * 2 * Mathf.PI;
        samples[i] = Mathf.Min(t * 190, 1) * Mathf.Exp(-t * 12) *
                     (.44f * Mathf.Sin(a) + .14f * Mathf.Sin(a * 2.76f) +
                      .06f * Mathf.Sin(a * 4.18f));
      }
      notes[k] = AudioClip.Create("Glass " + k, samples.Length, 1, rate, false);
      notes[k].SetData(samples, 0);
    }
  }
  void Chime(int note) {
    if (sound && replayOutput == null)
      voice.PlayOneShot(notes[note], .42f);
  }
  void Box(float x, float y, float w, float h, Color color) {
    Panel(new Rect(x, y, w, h), color);
  }
  void Text(float x, float y, float w, float h, string value, int size,
            Color color, bool display = false) {
    Caption(new Rect(x, y, w, h), value, size, color, display);
  }
  bool Button(float x, float y, float w, float h, string value,
              bool enabled = true, bool primary = false) {
    var r = new Rect(x, y, w, h);
    bool hover = enabled && r.Contains(Event.current.mousePosition);
    Panel(r, enabled ? Brass : new Color(.26f, .32f, .32f));
    Panel(new Rect(x + 1, y + 1, w - 2, h - 2),
          primary ? (hover ? Pale : Brass) : (hover ? Enamel : Ink));
    Text(x, y, w, h, value, 14,
         primary   ? Ink
         : enabled ? Pale
                   : new Color(.57f, .64f, .63f));
    return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
  }
  void DrawWorkshop() {
    if (title == null) {
      label =
          new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter,
                                         wordWrap = true };
      title = new GUIStyle(label) { font = displayFace,
                                    fontStyle = FontStyle.Bold };
    }
    bool wide = Screen.width >= Screen.height;
    float w = wide ? 1040 : 540, h = wide ? 660 : 836;
    float s = Mathf.Min(Screen.width / w, Screen.height / h);
    GUI.matrix = Matrix4x4.TRS(
        new Vector3((Screen.width - w * s) / 2, (Screen.height - h * s) / 2, 0),
        Quaternion.identity, new Vector3(s, s, 1));
    DrawWindow(wide ? new Rect(0, 0, 500, 660) : new Rect(0, 0, 540, 176),
               wide);
    GUI.BeginGroup(wide ? new Rect(500, 0, 540, 660)
                        : new Rect(0, 176, 540, 660));
    Box(0, 0, 540, 660, Ink);
    Box(12, 12, 516, 636, Brass);
    Box(13, 13, 514, 634, Enamel);
    Text(96, 20, 348, 25,
         (master ? "MASTERWORK   " : "CHAPTER   ") +
             (chapter + 1).ToString("00") + "   /   06",
         13, Brass);
    float intro =
        quietMotion ? 0 : Mathf.Max(0, 1 - (Time.unscaledTime - panelAt) * 3);
    Text(20, 41 + intro * 6, 500, 56, Titles[chapter], 46, cream, true);
    Stat(68, "MOVES", Rules.mp_moves());
    Stat(206, master ? "FROST LAYERS" : "FROST", Rules.mp_left());
    Stat(344, "SCORE", Rules.mp_score());
    DrawBoard();
    int state = Rules.mp_status();
    string status =
        chapterClosed ? "All six sections restored. The window is complete."
        : state == 2  ? "Every pane is clear. Fit the glass into the window."
        : state == 3  ? "Out of moves. Undo the last exchange or try again."
                      : message;
    Text(34, 540, 472, 40, status, 15, cream);
    bool canUndo = !busy && !chapterClosed && Rules.mp_can_rewind() != 0;
    if (state >= 2 && !busy) {
      if (Button(48, 585, 272, 42,
                 chapterClosed ? "Replay campaign"
                 : state == 2  ? "Restore " + Titles[chapter]
                               : "Try again",
                 true, true))
        ContinueChapter();
      if (Button(330, 585, 160, 42, "Undo move", canUndo))
        UndoExchange();
    } else {
      if (Button(46, 585, 142, 42, "Show hint  [H]", !busy, true))
        ShowHint();
      if (Button(198, 585, 144, 42, "Undo move  [Z]", canUndo))
        UndoExchange();
      if (Button(352, 585, 142, 42, "Restart", !busy))
        Begin(chapter);
    }
    GUI.EndGroup();
    GUI.matrix = Matrix4x4.identity;
  }
  void Stat(float x, string name, int value) {
    Box(x, 102, 128, 49, Ink);
    Box(x + 14, 149, 100, 1, Brass);
    Text(x, 102, 128, 29, value.ToString(), 23, Pale);
    Text(x, 130, 128, 16, name, 11, Brass);
  }
  void DrawWindow(Rect area, bool wide) {
    GUI.BeginGroup(area);
    float w = area.width, h = area.height;
    // Use the same cropped coordinates for silvered and restored bands.
    var frame = new Rect(0, 0, w, h);
    float aspect = w / h, source = 2f / 3f;
    float uw = Mathf.Min(1, aspect / source),
          vh = Mathf.Min(1, source / aspect);
    var uv = new Rect((1 - uw) / 2, (1 - vh) * (wide ? .5f : .82f), uw, vh);
    GUI.DrawTextureWithTexCoords(frame, silvered, uv);
    float reveal = unlock;
    if (!quietMotion && Time.unscaledTime - revealAt < 1.1f)
      reveal = Mathf.Lerp(
          revealFrom, unlock,
          Mathf.SmoothStep(0, 1, (Time.unscaledTime - revealAt) / 1.1f));
    float ratio = reveal / 6;
    if (ratio > 0)
      GUI.DrawTextureWithTexCoords(
          new Rect(0, h * (1 - ratio), w, h * ratio), coast,
          new Rect(uv.x, uv.y, uv.width, uv.height * ratio));
    float band = wide ? 184 : 95;
    Box(0, h - band, w, band, new Color(.035f, .09f, .11f, .91f));
    Box(0, h - band, w, 2, Brass);
    Text(10, h - band + 5, w - 20, wide ? 26 : 14, "M O S A I C    P I E R",
         wide ? 13 : 11, Brass);
    Text(10, h - band + (wide ? 30 : 15), w - 20, wide ? 57 : 28,
         "The Kingfisher Window", wide ? 41 : 24, cream, true);
    float start = (w - 169) / 2;
    for (int i = 0; i < 6; i++)
      Box(start + 29 * i, h - (wide ? 80 : 45), 24, 4,
          i < unlock ? Pale : new Color(.32f, .40f, .39f));
    Text(20, h - (wide ? 74 : 41), w - 40, wide ? 28 : 19,
         unlock + " of 6 sections restored", wide ? 15 : 12, cream);
    if (Button((w - 318) / 2, h - 31, 76, 22, sound ? "Sound on" : "Muted")) {
      sound = !sound;
      PlayerPrefs.SetInt("mp.sound", sound ? 1 : 0);
      PlayerPrefs.Save();
      Chime(1);
    }
    if (Button((w - 318) / 2 + 81, h - 31, 104, 22,
               quietMotion ? "Motion: low" : "Motion: full")) {
      quietMotion = !quietMotion;
      PlayerPrefs.SetInt("mp.quietMotion", quietMotion ? 1 : 0);
      PlayerPrefs.Save();
    }
    if (Button((w - 318) / 2 + 190, h - 31, 128, 22,
               master ? "Return to story" : "Play masterwork", !busy)) {
      LoadJournal(!master);
      PlayerPrefs.SetInt("mp.mode", master ? 1 : 0);
      PlayerPrefs.Save();
    }
    GUI.EndGroup();
  }
  void DrawBoard() {
    Box(80, 162, 380, 380, Ink);
    Box(83, 165, 374, 374, Brass);
    Box(86, 168, 368, 368, Ink);
    GUI.BeginGroup(new Rect(90, 172, 360, 360));
    var e = Event.current;
    var point = e.mousePosition;
    int cell = point.x >= 0 && point.y >= 0 && point.x < 360 && point.y < 360
                   ? (int)point.y / 60 * 6 + (int)point.x / 60
                   : -1;
    if (e.type == EventType.MouseDown && cell >= 0 && e.button == 0 && !busy) {
      pressed = cell;
      pressPoint = point;
      keyboardFocus = false;
      e.Use();
    } else if (e.type == EventType.MouseUp && pressed >= 0 && e.button == 0) {
      int origin = pressed;
      pressed = -1;
      if (cell >= 0 && !busy) {
        Vector2 d = point - pressPoint;
        if (d.magnitude > 22) {
          int target = origin + (Mathf.Abs(d.x) > Mathf.Abs(d.y)
                                     ? (int)Mathf.Sign(d.x)
                                     : 6 * (int)Mathf.Sign(d.y));
          if (target >= 0 && target < 36 &&
              Mathf.Abs(origin % 6 - target % 6) +
                      Mathf.Abs(origin / 6 - target / 6) ==
                  1) {
            selected = origin;
            Pick(target);
          }
        } else if (cell == origin)
          Pick(cell);
      }
      e.Use();
    }
    for (int i = 0; i < 36; i++) {
      float x = i % 6 * 60, y = i / 6 * 60;
      int layers = Rules.mp_frost(i);
      bool mark =
          selected == i || (hint >= 0 && (i == hint / 36 || i == hint % 36));
      GUI.DrawTexture(new Rect(x + 1, y + 1, 58, 58), sockets[layers]);
      if (mark) {
        Box(x + 5, y + 4, 50, 2, Pale);
        Box(x + 4, y + 6, 2, 46, Pale);
        Box(x + 54, y + 6, 2, 46, Pale);
      }
      if (mark || (keyboardFocus && i == cursor))
        Box(x + 8, y + 53, 44, 3, Pale);
    }
    for (int i = 0; i < 36; i++)
      DrawPiece(i);
    GUI.EndGroup();
  }
  void DrawPiece(int i) {
    float x = i % 6 * 60 + 30, y = i / 6 * 60 + 30, size = 51;
    float dt = Time.unscaledTime - phase;
    bool clearing = busy && !exchanging && !dropping && Rules.mp_clear(i) != 0;
    if (!quietMotion) {
      if (exchanging && (i == swapFrom || i == swapTo)) {
        int from = i == swapTo ? swapFrom : swapTo;
        float t = 1 - Mathf.SmoothStep(0, 1, dt / .15f);
        x += (from % 6 - i % 6) * 60 * t;
        y += (from / 6 - i / 6) * 60 * t;
      } else if (busy && dropping) {
        float t = Mathf.Clamp01(dt / .27f);
        y -= Rules.mp_fall(i) * 60 * (1 - t * t);
      } else if (clearing) {
        float t = Mathf.Clamp01(dt / .21f);
        size *= 1 + .2f * Mathf.Sin(t * Mathf.PI) - .8f * t * t;
        for (int n = 0; n < 5; n++) {
          float angle = n * 1.257f + i;
          Box(x + Mathf.Cos(angle) * 22 * t, y + Mathf.Sin(angle) * 22 * t, 3,
              3, Pale);
        }
      } else if (i == rejected && Time.unscaledTime - cueAt < .22f) {
        float t = (Time.unscaledTime - cueAt) / .22f;
        x += Mathf.Sin(t * 4 * Mathf.PI) * 5 * (1 - t);
      }
    }
    var r = new Rect(x - size / 2, y - size / 2, size, size);
    GUI.DrawTexture(r, pieces[Rules.mp_tile(i) - 1], ScaleMode.ScaleToFit);
  }
}
