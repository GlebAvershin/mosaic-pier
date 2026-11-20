using System;
using System.Collections;
using UnityEngine;

public class Glasshouse : MonoBehaviour
{
    static readonly string[] Titles = {"Tide Steps", "Brass Arcade", "Citrus Court", "Sea Lanterns", "The Sun Window", "Kingfisher Crown"};
    static readonly string[] Stones = {"lagoon", "honey", "coral", "orchid", "fern"};
    readonly Texture2D[] pieces = new Texture2D[5];
    Texture2D coast;
    int chapter, selected = -1, hint = -1, unlock;
    bool busy, dropping;
    float phase;
    string message;
    GUIStyle title, label, button;
    readonly Color sea = new Color(.07f,.22f,.25f), cream = new Color(.96f,.93f,.83f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { new GameObject("Glass workshop").AddComponent<Glasshouse>(); }

    void Start()
    {
        var camera = gameObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = sea;
        coast = Resources.Load<Texture2D>("glasshouse");
        for (int i=0; i<pieces.Length; i++) pieces[i]=Resources.Load<Texture2D>(Stones[i]);
        unlock = Mathf.Clamp(PlayerPrefs.GetInt("mp.restored"),0,6);
        Begin(Mathf.Clamp(PlayerPrefs.GetInt("mp.chapter"),0,5),true);
    }

    void Begin(int index, bool replay=false)
    {
        chapter=index; Rules.mp_start(chapter); selected=hint=-1;
        message="Clear the striped panes. Four in a line sweeps the whole row or column.";
        if (replay)
            foreach (var item in PlayerPrefs.GetString("mp.history","").Split(','))
            {
                if (!int.TryParse(item,out int pair) || Rules.mp_swap(pair/36,pair%36)==0) break;
                while (Rules.mp_status()==1) Rules.mp_step();
            }
        else { PlayerPrefs.SetString("mp.history",""); PlayerPrefs.SetInt("mp.chapter",chapter); PlayerPrefs.Save(); }
    }

    void Pick(int i)
    {
        if (busy || Rules.mp_status()!=0) return;
        hint=-1;
        if (selected==i) { selected=-1; return; }
        if (selected<0 || Math.Abs(selected/6-i/6)+Math.Abs(selected%6-i%6)!=1) { selected=i; return; }
        int old=selected; selected=-1;
        if (Rules.mp_swap(old,i)==0) { message="Try another swap. Look for three in a line."; return; }
        string prefix=PlayerPrefs.GetString("mp.history","");
        PlayerPrefs.SetString("mp.history",prefix+(prefix.Length>0?",":"")+(old*36+i));
        PlayerPrefs.Save();
        StartCoroutine(Resolve());
    }

    IEnumerator Resolve()
    {
        busy=true;
        while (Rules.mp_status()==1)
        {
            dropping=false; phase=Time.unscaledTime;
            message=Rules.mp_sweeps()>0?"Line sweep!":"Glass cascade ×"+Rules.mp_chain();
            yield return new WaitForSecondsRealtime(.18f);
            Rules.mp_step(); dropping=true; phase=Time.unscaledTime;
            yield return new WaitForSecondsRealtime(.2f);
        }
        dropping=busy=false;
    }

    void Panel(Rect rect, Color color)
    {
        GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=Color.white;
    }

    void Caption(Rect rect, string text, int size, Color color, bool heading=false)
    {
        var style=heading?title:label; style.fontSize=size; style.normal.textColor=color;
        GUI.Label(rect,text,style);
    }

    void OnGUI()
    {
        if (!coast) return;
        if (title==null)
        {
            title=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            label=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true};
            button=new GUIStyle(GUI.skin.button){fontSize=16};
        }
        bool wide=Screen.width>=Screen.height;
        float w=wide?1000:540, h=wide?640:810;
        float s=Mathf.Min(Screen.width/w,Screen.height/h);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-w*s)/2,(Screen.height-h*s)/2,0),Quaternion.identity,new Vector3(s,s,1));
        var art=wide?new Rect(0,0,460,h):new Rect(0,0,w,170);
        GUI.DrawTexture(art,coast,ScaleMode.ScaleAndCrop);
        Panel(new Rect(art.x,art.yMax-95,art.width,95),new Color(.04f,.16f,.18f,.86f));
        Caption(new Rect(art.x,art.yMax-93,art.width,54),"Mosaic Pier",38,cream,true);
        Caption(new Rect(art.x,art.yMax-43,art.width,35),"The Pier Glasshouse   ·   "+unlock+" / 6 restored",16,cream);
        GUI.BeginGroup(wide?new Rect(460,0,540,640):new Rect(0,170,540,640));
        Panel(new Rect(0,0,540,640),cream);
        Caption(new Rect(5,9,530,24),"CHAPTER "+(chapter+1)+" / 6",14,sea);
        Caption(new Rect(5,30,530,46),Titles[chapter],32,sea,true);
        Caption(new Rect(0,78,540,28),"Moves  "+Rules.mp_moves()+"      Frost  "+Rules.mp_left()+"      Score  "+Rules.mp_score(),19,sea);
        Panel(new Rect(74,116,392,392),sea);
        for (int i=0;i<36;i++)
        {
            float x=78+(i%6)*64, y=120+(i/6)*64;
            var rect=new Rect(x,y,61,61);
            bool marked=i==selected || (hint>=0 && (i==hint/36 || i==hint%36));
            Panel(rect,marked?new Color(1,.82f,.45f):Rules.mp_frost(i)>0?new Color(.68f,.86f,.82f):new Color(.23f,.40f,.42f));
            if (Rules.mp_frost(i)>0) { for (int stripe=0;stripe<3;stripe++) Panel(new Rect(x+5+stripe*7,y+3,3,8),Color.white); }
            float t=Mathf.Clamp01((Time.unscaledTime-phase)/.2f);
            float size=busy && !dropping && Rules.mp_clear(i)>0?52*(1-.55f*t):52;
            float lift=busy && dropping?Mathf.Min(2,Rules.mp_fall(i))*(1-t)*20:0;
            GUI.DrawTexture(new Rect(x+(61-size)/2,y+(61-size)/2-lift,size,size),pieces[Rules.mp_tile(i)-1],ScaleMode.ScaleToFit);
            if (GUI.Button(rect,GUIContent.none,GUIStyle.none)) Pick(i);
        }
        int status=Rules.mp_status();
        string info=status==2?"All panes clear. The glass is ready to fit.":status==3?"Out of moves. Try another route through the frost.":message;
        Caption(new Rect(40,514,460,47),info,16,sea);
        GUI.enabled=!busy;
        if (status>=2 && !busy)
        {
            if (GUI.Button(new Rect(126,572,288,43),status==3?"Try again":chapter==5 && unlock==6?"Replay campaign":"Restore "+Titles[chapter],button))
            {
                if (status==3) Begin(chapter);
                else {unlock=Mathf.Max(unlock,chapter+1);PlayerPrefs.SetInt("mp.restored",unlock);PlayerPrefs.Save();Begin((chapter+1)%6);}
            }
        }
        else
        {
            if (GUI.Button(new Rect(82,572,176,43),"Show hint",button)) {hint=Rules.mp_hint();selected=-1;message="Swap the two highlighted pieces.";}
            if (GUI.Button(new Rect(278,572,176,43),"Restart level",button)) Begin(chapter);
        }
        GUI.enabled=true; GUI.EndGroup(); GUI.matrix=Matrix4x4.identity;
    }
}
