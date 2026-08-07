using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// The "playing" screen (FLEDGE 3.5). Layers, back to front: a solid sky-colour fill (recolours by
// time of day, and covers the forest image's transparent sky so the 3D world never shows through),
// the forest scene (tinted by time of day), then — on a canvas ABOVE the joysticks — the Menu
// button + selection pill. Built in code; created by MainMenu when the game starts.
public class PlayingScreen : MonoBehaviour
{
    private Font font;

    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Build();
    }

    private void Build()
    {
        // Background canvas — behind the joysticks (which sit at sortingOrder 100).
        var bg = MakeCanvas("PlayingBG", 40);
        FullImage(bg.transform, null, SkyColor(GameConfig.Period)); // solid sky colour, fills the sky
        var tex = Resources.Load<Texture2D>("UI/playbg_forest");
        var sprite = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        FullImage(bg.transform, sprite, tex != null ? SkyTint(GameConfig.Period) : new Color(0, 0, 0, 0));

        // UI canvas — ABOVE the joysticks, so its buttons render on top and stay tappable.
        var ui = MakeCanvas("PlayingUI", 150);
        string w = !GameConfig.WeatherEnabled ? "clear skies"
                 : GameConfig.StormLocked ? "in a storm" : "weather rolling through";
        MakeButton(ui.transform, "Menu", new Vector2(-340, 845), new Vector2(260, 150), Back);
        Panel(ui.transform, new Vector2(160, 845), new Vector2(560, 96), new Color(1f, 1f, 1f, 0.22f));
        Label(ui.transform, GameConfig.Biome + "   ·   " + GameConfig.Period + "   ·   " + w, new Vector2(160, 845), 30, Color.white);
        Label(ui.transform, "close your eyes and walk", new Vector2(0, 120), 36, new Color(1f, 1f, 1f, 0.9f));
    }

    private void Back()
    {
        GameConfig.Configured = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // The sky colour that fills the screen behind the forest (shows through its transparent sky).
    private static Color SkyColor(DayPeriod p)
    {
        switch (p)
        {
            case DayPeriod.Dawn: return new Color(1f, 0.72f, 0.6f);      // warm sunrise
            case DayPeriod.Dusk: return new Color(1f, 0.5f, 0.32f);      // orange sunset
            case DayPeriod.Night: return new Color(0.06f, 0.09f, 0.22f); // deep night
            default: return new Color(0.55f, 0.78f, 0.96f);             // midday blue
        }
    }

    // A gentler tint over the forest itself so the whole scene reads as that time of day.
    private static Color SkyTint(DayPeriod p)
    {
        switch (p)
        {
            case DayPeriod.Dawn: return new Color(1f, 0.88f, 0.78f);
            case DayPeriod.Dusk: return new Color(1f, 0.68f, 0.5f);
            case DayPeriod.Night: return new Color(0.42f, 0.5f, 0.75f);
            default: return Color.white;
        }
    }

    private Canvas MakeCanvas(string name, int order)
    {
        var go = new GameObject(name); go.transform.SetParent(transform, false);
        var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order;
        var sc = go.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private void FullImage(Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject("full", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>(); img.sprite = sprite; img.color = color; img.raycastTarget = false;
    }

    private void Panel(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = color;
    }

    private void Label(Transform parent, string text, Vector2 pos, int size, Color color)
    {
        var go = new GameObject("Lbl", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = new Vector2(1040, size + 24); rt.anchoredPosition = pos;
        var txt = go.AddComponent<Text>(); txt.text = text; txt.font = font; txt.fontSize = size; txt.alignment = TextAnchor.MiddleCenter; txt.color = color;
    }

    private void MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go = new GameObject("Btn_" + text, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0.32f); // visible rounded-ish tap target so the whole area is obviously clickable
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(() => onClick());
        var t = new GameObject("Text", typeof(RectTransform)); t.transform.SetParent(go.transform, false);
        var trt = t.GetComponent<RectTransform>(); trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var txt = t.AddComponent<Text>(); txt.text = text; txt.font = font; txt.fontSize = 40; txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white;
        txt.raycastTarget = false; // let taps pass through the text to the button behind it
    }
}
