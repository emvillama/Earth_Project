using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// FLEDGE 3.5 — main menu, styled to Harry's mockup: a floating globe over sky-blue, with Biome /
// Weather / Time-of-day rows of image tiles (locked ones dimmed "Soon"), and a Start Journey button.
// Art loads from Resources/UI. Options we don't have audio for yet are locked. Built in code.
public class MainMenu : MonoBehaviour
{
    public ItemSpawner spawner;

    private string biome = "Forest";
    private DayPeriod period = DayPeriod.Midday;
    private bool weatherEnabled = true;
    private float weatherChance = 0.05f;
    private bool startStorm = false;

    private Font font;
    private Canvas canvas;
    private Image globeImg;
    private readonly List<List<GameObject>> rowFrames = new List<List<GameObject>>();

    private static readonly Color SkyTop = new Color(0.55f, 0.78f, 0.96f, 1f);
    private static readonly Color Green = new Color(0.33f, 0.74f, 0.45f, 1f);

    private struct Opt { public string label, icon; public bool locked; public Action apply;
        public Opt(string l, string i, bool lk, Action a){ label=l; icon=i; locked=lk; apply=a; } }

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    private Sprite Spr(string n)
    {
        var t = Resources.Load<Texture2D>("UI/" + n);
        return t == null ? null : Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
    }

    private void Build()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var cgo = new GameObject("MainMenuCanvas"); cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 200;
        var sc = cgo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        Panel(Vector2.zero, new Vector2(1300, 2200), SkyTop);

        Label("Walk Earth", new Vector2(0, 840), 84, Color.white, TextAnchor.MiddleCenter);

        globeImg = ImageAt(canvas.transform, Spr("globe"), new Vector2(0, 430), new Vector2(600, 600));

        RowSection("Biome", 150, new[]{
            new Opt("Forest","ic_Forest",false,()=>biome="Forest"),
            new Opt("Mountain","ic_Mountain",true,null),
            new Opt("Beach","ic_Beach",true,null),
            new Opt("River","ic_River",true,null),
            new Opt("Meadow","ic_Meadow",true,null),
        }, 0);

        RowSection("Weather", -120, new[]{
            new Opt("Sunny","ic_Sunny",false,()=>{weatherEnabled=true;weatherChance=0.05f;startStorm=false;}),
            new Opt("Rainy","ic_Rainy",false,()=>{weatherEnabled=true;startStorm=true;}),
            new Opt("Overcast","ic_Overcast",true,null),
            new Opt("Cloud","ic_Cloud",true,null),
            new Opt("Windy","ic_Windy",true,null),
        }, 1);

        RowSection("Time of Day", -390, new[]{
            new Opt("Morning","ic_Morning",false,()=>{period=DayPeriod.Dawn;TintGlobe();}),
            new Opt("Afternoon","ic_Afternoon",false,()=>{period=DayPeriod.Midday;TintGlobe();}),
            new Opt("Sunset","ic_Sunset",false,()=>{period=DayPeriod.Dusk;TintGlobe();}),
            new Opt("Night","ic_Night",false,()=>{period=DayPeriod.Night;TintGlobe();}),
            new Opt("LateNight","ic_LateNight",true,null),
        }, 2);

        var start = MakeButton("Start Journey", new Vector2(0, -760), new Vector2(780, 150), Green, Begin);
        start.GetComponentInChildren<Text>().fontSize = 52;

        Select(0, 0); Select(1, 0); Select(2, 1); // Forest, Sunny, Afternoon(Midday)
        TintGlobe();
    }

    private void RowSection(string title, float y, Opt[] opts, int rowIdx)
    {
        Label(title, new Vector2(-430, y + 110), 34, Color.white, TextAnchor.MiddleLeft);
        var frames = new List<GameObject>();
        float tile = 168f, gap = 34f;
        float span = 4 * (tile + gap);
        for (int i = 0; i < opts.Length; i++)
        {
            int idx = i; var o = opts[i];
            float x = -span * 0.5f + i * (tile + gap);

            var cell = new GameObject("cell", typeof(RectTransform)); cell.transform.SetParent(canvas.transform, false);
            var crt = cell.GetComponent<RectTransform>(); crt.sizeDelta = new Vector2(tile, tile); crt.anchoredPosition = new Vector2(x, y);

            var frame = new GameObject("sel", typeof(RectTransform)); frame.transform.SetParent(cell.transform, false);
            var frt = frame.GetComponent<RectTransform>(); frt.sizeDelta = new Vector2(tile + 18, tile + 18); frt.anchoredPosition = Vector2.zero;
            var fimg = frame.AddComponent<Image>(); fimg.color = Green; fimg.raycastTarget = false; frame.SetActive(false);

            var icon = ImageAt(cell.transform, Spr(o.icon), Vector2.zero, new Vector2(tile, tile));
            if (o.locked) icon.color = new Color(1, 1, 1, 0.32f);

            Label(o.locked ? "Soon" : o.label, new Vector2(x, y - tile * 0.5f - 34f), 26,
                  o.locked ? new Color(1, 1, 1, 0.5f) : Color.white, TextAnchor.MiddleCenter);

            var btn = cell.AddComponent<Button>(); btn.targetGraphic = icon;
            btn.interactable = !o.locked;
            if (!o.locked) btn.onClick.AddListener(() => { Select(rowIdx, idx); o.apply?.Invoke(); });
            frames.Add(frame);
        }
        rowFrames.Add(frames);
    }

    private void Select(int row, int idx)
    {
        if (row >= rowFrames.Count) return;
        var frames = rowFrames[row];
        for (int i = 0; i < frames.Count; i++) frames[i].SetActive(i == idx);
    }

    private void TintGlobe()
    {
        if (globeImg == null) return;
        switch (period)
        {
            case DayPeriod.Dawn: globeImg.color = new Color(1f, 0.9f, 0.82f); break;
            case DayPeriod.Midday: globeImg.color = Color.white; break;
            case DayPeriod.Dusk: globeImg.color = new Color(1f, 0.78f, 0.6f); break;
            case DayPeriod.Night: globeImg.color = new Color(0.55f, 0.62f, 0.85f); break;
        }
    }

    private void Begin()
    {
        GameConfig.Period = period;
        GameConfig.WeatherEnabled = weatherEnabled;
        GameConfig.WeatherChance = weatherChance;
        GameConfig.StormLocked = startStorm;
        GameConfig.Biome = biome;
        GameConfig.Configured = true;

        new GameObject("PlayingScreen").AddComponent<PlayingScreen>();
        if (canvas != null) Destroy(canvas.gameObject);
        if (spawner != null) spawner.BeginWorld();
        Destroy(gameObject);
    }

    // --- builders ---
    private Image ImageAt(Transform parent, Sprite s, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("img", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>(); img.sprite = s; img.preserveAspect = true;
        return img;
    }

    private Button MakeButton(string text, Vector2 pos, Vector2 size, Color color, Action onClick)
    {
        var go = new GameObject("Btn", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>(); img.color = color;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(() => onClick());
        var t = new GameObject("Text", typeof(RectTransform)); t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var txt = t.AddComponent<Text>(); txt.text = text; txt.font = font; txt.fontSize = 40; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
        return btn;
    }

    private void Label(string text, Vector2 pos, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("Lbl", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(980, size + 22); rt.anchoredPosition = pos;
        var txt = go.AddComponent<Text>(); txt.text = text; txt.font = font; txt.fontSize = size; txt.alignment = anchor; txt.color = color;
    }

    private void Panel(Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = color;
    }
}
