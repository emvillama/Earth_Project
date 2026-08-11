using UnityEngine;
using UnityEngine.UI;

// FLEDGE 3.5 — one-time "wear headphones" prompt. Earth is a binaural 3D soundscape, so headphones
// (or earbuds) matter a lot. Shown once per app run, over the main menu, dismissed with a tap.
// Built in code; created by MainMenu. Uses a static flag so returning to the menu doesn't re-show it.
public class HeadphonePrompt : MonoBehaviour
{
    private static bool shown = false;

    public static void ShowOnce(Font font)
    {
        if (shown) return;
        shown = true;
        var go = new GameObject("HeadphonePrompt");
        go.AddComponent<HeadphonePrompt>().Build(font);
    }

    private void Build(Font font)
    {
        var cgo = new GameObject("HeadphoneCanvas"); cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // above the menu (200)
        var sc = cgo.AddComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1080, 1920); sc.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        // Dim scrim — raycastTarget on, so it blocks the menu behind while the prompt is up.
        var scrim = new GameObject("Scrim", typeof(RectTransform)); scrim.transform.SetParent(cgo.transform, false);
        var srt = scrim.GetComponent<RectTransform>();
        srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = srt.offsetMax = Vector2.zero;
        scrim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var card = new GameObject("Card", typeof(RectTransform)); card.transform.SetParent(cgo.transform, false);
        var crt = card.GetComponent<RectTransform>(); crt.sizeDelta = new Vector2(880, 600); crt.anchoredPosition = Vector2.zero;
        card.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 0.98f);

        Label(card.transform, "Headphones\nrecommended", new Vector2(0, 170), new Vector2(800, 230), 66, Color.white, FontStyle.Bold, font);
        Label(card.transform, "Earth is a 3D binaural soundscape. Pop in headphones, close your eyes, and let the world move around you.",
              new Vector2(0, -30), new Vector2(740, 250), 34, new Color(1f, 1f, 1f, 0.85f), FontStyle.Normal, font);

        var btn = new GameObject("GotIt", typeof(RectTransform)); btn.transform.SetParent(card.transform, false);
        var brt = btn.GetComponent<RectTransform>(); brt.sizeDelta = new Vector2(380, 116); brt.anchoredPosition = new Vector2(0, -220);
        var bimg = btn.AddComponent<Image>(); bimg.color = new Color(0.33f, 0.74f, 0.45f, 1f);
        var b = btn.AddComponent<Button>(); b.targetGraphic = bimg; b.onClick.AddListener(() => Destroy(gameObject));
        Label(btn.transform, "Got it", Vector2.zero, new Vector2(380, 116), 46, Color.white, FontStyle.Bold, font);
    }

    private void Label(Transform parent, string text, Vector2 pos, Vector2 size, int fs, Color color, FontStyle style, Font font)
    {
        var go = new GameObject("Lbl", typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size; rt.anchoredPosition = pos;
        var t = go.AddComponent<Text>(); t.text = text; t.font = font; t.fontSize = fs; t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
    }
}
