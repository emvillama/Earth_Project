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

        Panel(canvas.transform, Vector2.zero, new Vector2(1400, 2400), new Color(0.05f, 0.07f, 0.06f, 1f));

        string w = !GameConfig.WeatherEnabled ? "clear skies"
                 : GameConfig.StormLocked ? "in a storm"
                 : "weather rolling through";
        Label(canvas.transform, GameConfig.Biome, new Vector2(0, 240), 84, new Color(0.9f, 0.95f, 0.9f, 1f));
        Label(canvas.transform, GameConfig.Period + " · " + w, new Vector2(0, 120), 40, new Color(1f, 1f, 1f, 0.6f));
        Label(canvas.transform, "close your eyes and walk", new Vector2(0, -30), 34, new Color(1f, 1f, 1f, 0.4f));

        MakeButton(canvas.transform, "Menu", new Vector2(0, 820), new Vector2(340, 110), Back);
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
