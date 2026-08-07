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
    private Sprite borderSprite;
    private Sprite greyTile; // flat grey placeholder shown for locked ("Soon") options
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

    // A white rounded-rectangle OUTLINE (transparent centre) for the selection highlight, with the
    // same corner rounding as the option tiles.
    private Sprite RoundBorder(int size, float radius, float thickness)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x - half + 0.5f, py = y - half + 0.5f;
                float outer = RoundRectSDF(px, py, half - 1f, half - 1f, radius);
                float inner = RoundRectSDF(px, py, half - 1f - thickness, half - 1f - thickness, Mathf.Max(1f, radius - thickness));
                float a = (outer <= 0f && inner > 0f)
                    ? Mathf.Min(Mathf.Clamp01(-outer / 1.5f), Mathf.Clamp01(inner / 1.5f)) : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // A flat grey rounded tile the same shape/size as the option icons — the placeholder for locked
    // options, so they read as "coming soon" without borrowing another option's art.
    private Sprite GreyTile(int size, float radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;
        Color fill = new Color(0.30f, 0.32f, 0.36f, 1f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x - half + 0.5f, py = y - half + 0.5f;
                float d = RoundRectSDF(px, py, half - 1f, half - 1f, radius);
                float a = Mathf.Clamp01(-d / 1.5f); // opaque inside, soft anti-aliased edge
                tex.SetPixel(x, y, new Color(fill.r, fill.g, fill.b, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static float RoundRectSDF(float px, float py, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(px) - (hw - r);
        float qy = Mathf.Abs(py) - (hh - r);
        float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
        float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
        return outside + inside - r;
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

        borderSprite = RoundBorder(128, 128 * 0.16f, 7f);
        greyTile = GreyTile(128, 128 * 0.16f);

        Panel(Vector2.zero, new Vector2(1300, 2200), SkyTop);

        Label("Walk Earth", new Vector2(0, 860), 84, Color.white, TextAnchor.MiddleCenter);

        globeImg = ImageAt(canvas.transform, Spr("globe"), new Vector2(0, 520), new Vector2(460, 460));

        RowSection("Biome", 150, new[]{
            new Opt("Forest","ic_Forest",false,()=>biome="Forest"),
            new Opt("Mountain","ic_Mountain",true,null),
            new Opt("Beach","ic_Beach",true,null),
            new Opt("River","ic_River",true,null),
            new Opt("Meadow","ic_Meadow",true,null),
        }, 0);

        RowSection("Weather", -100, new[]{
            new Opt("Sunny","ic_Sunny",false,()=>{weatherEnabled=true;weatherChance=0.05f;startStorm=false;}),
            new Opt("Stormy","ic_Rainy",false,()=>{weatherEnabled=true;startStorm=true;}),
            new Opt("Overcast","ic_Overcast",true,null),
            new Opt("Cloud","ic_Cloud",true,null),
            new Opt("Windy","ic_Windy",true,null),
        }, 1);

        RowSection("Time of Day", -350, new[]{
            new Opt("Morning","ic_Morning",false,()=>{period=DayPeriod.Dawn;TintGlobe();}),
            new Opt("Afternoon","ic_Afternoon",false,()=>{period=DayPeriod.Midday;TintGlobe();}),
            new Opt("Sunset","ic_Sunset",false,()=>{period=DayPeriod.Dusk;TintGlobe();}),
            new Opt("Night","ic_Night",false,()=>{period=DayPeriod.Night;TintGlobe();}),
            new Opt("LateNight","ic_LateNight",true,null),
        }, 2);

        var start = MakeButton("Start Journey", new Vector2(0, -720), new Vector2(780, 150), Green, Begin);
        start.GetComponentInChildren<Text>().fontSize = 52;

        // Restore the last-used choices (GameConfig persists across a return from the playing screen),
        // so re-opening the menu shows what you had picked instead of resetting to defaults.
        biome = GameConfig.Biome;
        period = GameConfig.Period;
        weatherEnabled = GameConfig.WeatherEnabled;
        weatherChance = GameConfig.WeatherChance;
        startStorm = GameConfig.StormLocked;
        Select(0, 0);                     // Forest (only unlocked biome for now)
        Select(1, startStorm ? 1 : 0);    // Rainy vs Sunny
        Select(2, TimeIndex(period));
        TintGlobe();
    }

    private static int TimeIndex(DayPeriod p)
    {
        switch (p)
        {
            case DayPeriod.Dawn: return 0;   // Morning
            case DayPeriod.Dusk: return 2;   // Sunset
            case DayPeriod.Night: return 3;  // Night
            default: return 1;               // Midday → Afternoon
        }
    }

    private void RowSection(string title, float y, Opt[] opts, int rowIdx)
    {
        Label(title, new Vector2(-430, y + 110), 34, Color.white, TextAnchor.MiddleLeft);
        var frames = new List<GameObject>();
        float tile = 150f, gap = 20f; // smaller + tighter so the 5 tiles sit well inside the screen edges
        float span = 4 * (tile + gap);
        for (int i = 0; i < opts.Length; i++)
        {
            int idx = i; var o = opts[i];
            float x = -span * 0.5f + i * (tile + gap);

            var cell = new GameObject("cell", typeof(RectTransform)); cell.transform.SetParent(canvas.transform, false);
            var crt = cell.GetComponent<RectTransform>(); crt.sizeDelta = new Vector2(tile, tile); crt.anchoredPosition = new Vector2(x, y);

            var frame = new GameObject("sel", typeof(RectTransform)); frame.transform.SetParent(cell.transform, false);
            var frt = frame.GetComponent<RectTransform>(); frt.sizeDelta = new Vector2(tile + 16, tile + 16); frt.anchoredPosition = Vector2.zero;
            var fimg = frame.AddComponent<Image>(); fimg.sprite = borderSprite; fimg.color = Color.white; fimg.raycastTarget = false; frame.SetActive(false);

            // Unlocked → its artwork; locked → a flat grey placeholder tile of the same size.
            var icon = ImageAt(cell.transform, o.locked ? greyTile : Spr(o.icon), Vector2.zero, new Vector2(tile, tile));

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
        txt.raycastTarget = false; // taps pass through the text to the button
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
