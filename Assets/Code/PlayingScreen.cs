using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        // Make sure an EventSystem exists so UI taps register even if the menu's was torn down.
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        string w = !GameConfig.WeatherEnabled ? "clear skies"
                 : GameConfig.StormLocked ? "in a storm" : "weather rolling through";

        // Menu: draw the "Menu" label, then a fully-transparent button IN FRONT of it (added last so it
        // renders on top). The text stays readable; a tap anywhere in the area hits the button and resets.
        Label(ui.transform, "Menu", new Vector2(-360, 845), 46, Color.white);
        MakeTapButton(ui.transform, new Vector2(-360, 845), new Vector2(340, 170), Back);

        Panel(ui.transform, new Vector2(170, 845), new Vector2(540, 96), new Color(1f, 1f, 1f, 0.22f));
        Label(ui.transform, GameConfig.Biome + "   ·   " + GameConfig.Period + "   ·   " + w, new Vector2(170, 845), 30, Color.white);
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
        txt.raycastTarget = false;
    }

    // A transparent button placed IN FRONT of its label (added last, so it renders on top). Any tap in
    // the rect fires onClick; the label behind stays readable because the button is see-through.
    private void MakeTapButton(Transform parent, Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go = new GameObject("MenuTapButton", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>(); img.color = new Color(1f, 1f, 1f, 0.01f); // see-through, still a raycast target
        img.raycastTarget = true;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img; btn.onClick.AddListener(() => onClick());
        go.transform.SetAsLastSibling(); // ensure it's in front of the "Menu" text
    }
}
