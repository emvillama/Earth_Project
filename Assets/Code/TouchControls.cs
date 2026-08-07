using UnityEngine;
using UnityEngine.UI;

// Interim mobile controls (FLEDGE 3.4): a left-side virtual joystick to move + right-side drag to
// look. Implements IInputSource, so the Cubie controller consumes it identically to keyboard/mouse.
// Builds its own on-screen UI at runtime (no scene setup) and reads raw touches (no EventSystem
// needed). Placeholder for testing until Stage 4 (WAYFINDER) swaps in real phone motion/GPS.
public class TouchControls : MonoBehaviour, IInputSource
{
    [Tooltip("Joystick radius as a fraction of the screen's short side (drag this far = full speed).")]
    [Range(0.08f, 0.3f)] public float radiusFraction = 0.16f;
    [Tooltip("Look sensitivity — screen pixels dragged converted to look-delta.")]
    public float lookScale = 0.06f;
    [Tooltip("Flip if dragging up looks down (or vice versa).")]
    public bool invertLookY = false;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public float LookX { get; private set; }
    public float LookY { get; private set; }

    private RectTransform baseRt, knobRt;
    private Vector2 baseCenter;   // screen-pixel centre of the active joystick
    private float radius;
    private int moveFinger = -1, lookFinger = -1;

    void Start()
    {
        radius = Mathf.Min(Screen.width, Screen.height) * radiusFraction;
        BuildUI();
    }

    void Update()
    {
        if (!GameConfig.Configured) return; // stay dormant while the main menu is up

        LookX = 0f; LookY = 0f; // look is a per-frame drag delta

        bool moving = false;
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.phase == TouchPhase.Began)
            {
                if (t.position.x < Screen.width * 0.5f && moveFinger == -1)
                {
                    moveFinger = t.fingerId;
                    baseCenter = t.position;          // floating stick: appears where you press
                    baseRt.position = baseCenter;
                    baseRt.gameObject.SetActive(true);
                }
                else if (t.position.x >= Screen.width * 0.5f && lookFinger == -1)
                {
                    lookFinger = t.fingerId;
                }
            }

            if (t.fingerId == moveFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) moveFinger = -1;
                else
                {
                    Vector2 off = Vector2.ClampMagnitude(t.position - baseCenter, radius);
                    knobRt.anchoredPosition = off;
                    Horizontal = off.x / radius;
                    Vertical = off.y / radius;
                    moving = true;
                }
            }
            else if (t.fingerId == lookFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) lookFinger = -1;
                else if (t.phase == TouchPhase.Moved)
                {
                    LookX = t.deltaPosition.x * lookScale;
                    LookY = t.deltaPosition.y * lookScale * (invertLookY ? -1f : 1f);
                }
            }
        }

        if (!moving)
        {
            Horizontal = 0f; Vertical = 0f;
            if (moveFinger == -1 && baseRt != null)
            {
                knobRt.anchoredPosition = Vector2.zero;
                baseRt.gameObject.SetActive(false);
            }
        }
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("TouchControlsCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        Sprite ring = CircleSprite(128, new Color(1f, 1f, 1f, 0.18f));
        Sprite dot = CircleSprite(96, new Color(1f, 1f, 1f, 0.45f));

        baseRt = MakeImage(canvasGo.transform, ring, radius * 2f);
        knobRt = MakeImage(baseRt, dot, radius * 0.9f);
        knobRt.anchoredPosition = Vector2.zero;
        baseRt.gameObject.SetActive(false); // floating: shown only while pressed
    }

    private static RectTransform MakeImage(Transform parent, Sprite sprite, float size)
    {
        var go = new GameObject("img", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        return rt;
    }

    // Soft-edged filled circle, generated so we need no sprite assets.
    private static Sprite CircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                float a = Mathf.Clamp01((r - d) / 2f); // 2px soft edge
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
