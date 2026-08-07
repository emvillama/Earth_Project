using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// The "playing" screen (FLEDGE 3.5). Earth has no visual world worth showing, so while you walk we
// cover the camera view with a simple full-screen panel: what you picked + a Back button that
// returns to the main menu to reselect. Built in code; created by MainMenu when the game starts.
// Sits above the 3D view but below the joystick canvas, so the joystick still shows.
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
        var cgo = new GameObject("PlayingCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the 3D view, below the joystick (100)
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Full-screen forest scene covers the visual-less 3D world.
        var bg = new GameObject("BG", typeof(RectTransform));
        bg.transform.SetParent(canvas.transform, false);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        var bimg = bg.AddComponent<Image>();
        var tex = Resources.Load<Texture2D>("UI/playbg_forest");
        if (tex != null)
        {
            bimg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            bimg.color = SkyTint(GameConfig.Period); // shift the whole scene (sky included) by time of day
        }
        else bimg.color = new Color(0.05f, 0.07f, 0.06f, 1f);

        string w = !GameConfig.WeatherEnabled ? "clear skies"
                 : GameConfig.StormLocked ? "in a storm"
                 : "weather rolling through";

        // Top: back-to-menu button + a selection pill.
        MakeButton(canvas.transform, "Menu", new Vector2(-390, 850), new Vector2(210, 92), Back);
        Panel(canvas.transform, new Vector2(80, 850), new Vector2(680, 96), new Color(1f, 1f, 1f, 0.22f));
        Label(canvas.transform, GameConfig.Biome + "   ·   " + GameConfig.Period + "   ·   " + w, new Vector2(80, 850), 32, Color.white);

        Label(canvas.transform, "close your eyes and walk", new Vector2(0, 120), 36, new Color(1f, 1f, 1f, 0.9f));
    }

    // Tints the whole scene (and its sky) to match the selected time of day.
    private static Color SkyTint(DayPeriod p)
    {
        switch (p)
        {
            case DayPeriod.Dawn: return new Color(1f, 0.88f, 0.78f);   // warm sunrise
            case DayPeriod.Dusk: return new Color(1f, 0.68f, 0.5f);    // orange sunset
            case DayPeriod.Night: return new Color(0.42f, 0.5f, 0.75f);// dim cool night
            default: return Color.white;                               // midday
        }
    }

    private void Back()
    {
        // Full reset back to the main menu so preferences can be re-picked.
        GameConfig.Configured = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Panel(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = color;
    }

    private void Label(Transform parent, string text, Vector2 pos, int size, Color color)
    {
        var go = new GameObject("Lbl", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1040, size + 24); rt.anchoredPosition = pos;
        var txt = go.AddComponent<Text>();
        txt.text = text; txt.font = font; txt.fontSize = size; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
    }

    private void MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go = new GameObject("Btn_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.14f);
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
    }
}
