using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// FLEDGE 3.5 — the main menu shown before the soundscape loads. The player picks time of day,
// weather, and biome; Start writes GameConfig and tells the ItemSpawner to build the world. Built
// entirely in code (Canvas + uGUI, no scene/prefab setup) so it works with our runtime-created
// systems. Created by ItemSpawner when GameConfig isn't configured yet.
public class MainMenu : MonoBehaviour
{
    public ItemSpawner spawner;

    private DayPeriod period = DayPeriod.Midday;
    private bool weatherEnabled = true;
    private float weatherChance = 0.10f;
    private bool startStorm = false;
    private string biome = "Forest";

    private Font font;
    private Canvas canvas;
    private static readonly Color Normal = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color Chosen = new Color(0.32f, 0.72f, 0.5f, 0.95f);

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    private void Build()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var cgo = new GameObject("MainMenuCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        Panel(new Vector2(0, 0), new Vector2(1200, 2100), new Color(0.05f, 0.07f, 0.06f, 0.97f));
        Label("EARTH", new Vector2(0, 770), 96, new Color(0.9f, 0.95f, 0.9f, 1f));
        Label("a walk you can hear", new Vector2(0, 690), 34, new Color(1f, 1f, 1f, 0.45f));

        Label("Time of Day", new Vector2(0, 520), 40);
        var tod = new List<Button>();
        Row(tod, new[] { "Dawn", "Midday", "Dusk", "Night" }, 430, i => period = (DayPeriod)i);

        Label("Weather", new Vector2(0, 250), 40);
        var wx = new List<Button>();
        Row(wx, new[] { "Off", "Low", "Normal", "Stormy" }, 160, i =>
        {
            weatherEnabled = i != 0;
            weatherChance = i == 1 ? 0.05f : i == 2 ? 0.10f : i == 3 ? 0.25f : 0f;
            startStorm = i == 3; // "Stormy" → spawn straight into a storm
        });

        Label("Biome", new Vector2(0, -20), 40);
        var bm = new List<Button>();
        Row(bm, new[] { "Forest" }, -110, _ => biome = "Forest");

        var start = MakeButton(canvas.transform, "START", new Vector2(0, -430), new Vector2(560, 150), Begin);
        start.GetComponent<Image>().color = Chosen;
        start.GetComponentInChildren<Text>().fontSize = 56;

        Select(tod, 1);  // Midday
        Select(wx, 2);   // Normal
        Select(bm, 0);   // Forest
    }

    private void Begin()
    {
        GameConfig.Period = period;
        GameConfig.WeatherEnabled = weatherEnabled;
        GameConfig.WeatherChance = weatherChance;
        GameConfig.StartStorm = startStorm;
        GameConfig.Biome = biome;
        GameConfig.Configured = true;

        // Cover the (visual-less) 3D world with the "playing" screen — you listen, not look.
        new GameObject("PlayingScreen").AddComponent<PlayingScreen>();

        if (canvas != null) Destroy(canvas.gameObject);
        if (spawner != null) spawner.BeginWorld();
        Destroy(gameObject);
    }

    // --- tiny uGUI builders ---

    private void Row(List<Button> group, string[] labels, float y, Action<int> onPick)
    {
        int n = labels.Length;
        float w = Mathf.Min(250f, 1040f / n - 20f);
        float span = (n - 1) * (w + 20f);
        for (int i = 0; i < n; i++)
        {
            int idx = i;
            float x = -span * 0.5f + i * (w + 20f);
            var b = MakeButton(canvas.transform, labels[i], new Vector2(x, y), new Vector2(w, 120f),
                               () => { Select(group, idx); onPick(idx); });
            group.Add(b);
        }
    }

    private void Select(List<Button> group, int idx)
    {
        for (int i = 0; i < group.Count; i++)
            group[i].GetComponent<Image>().color = (i == idx) ? Chosen : Normal;
    }

    private Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, Action onClick)
    {
        var go = new GameObject("Btn_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = Normal;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick());

        var t = new GameObject("Text", typeof(RectTransform));
        t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var txt = t.AddComponent<Text>();
        txt.text = text; txt.font = font; txt.fontSize = 40; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        return btn;
    }

    private void Label(string text, Vector2 pos, int size, Color? color = null)
    {
        var go = new GameObject("Lbl_" + text, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1040, size + 24); rt.anchoredPosition = pos;
        var txt = go.AddComponent<Text>();
        txt.text = text; txt.font = font; txt.fontSize = size; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color ?? new Color(1f, 1f, 1f, 0.85f);
    }

    private void Panel(Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = color;
    }
}
