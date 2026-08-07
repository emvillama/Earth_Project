using UnityEngine;
using UnityEngine.UI;

// Interim mobile controls (FLEDGE 3.4): twin on-screen joysticks — MOVE (left, walk/strafe) and
// LOOK (right, turn/pitch at a rate while held). Both are always visible and labelled. Implements
// IInputSource so the Cubie controller consumes them like keyboard/mouse. Builds its own UI at
// runtime and reads raw touches (no EventSystem). Placeholder until Stage 4 swaps in phone motion.
public class TouchControls : MonoBehaviour, IInputSource
{
    [Tooltip("Stick radius as a fraction of the screen's short side.")]
    [Range(0.08f, 0.3f)] public float radiusFraction = 0.15f;
    [Tooltip("Look turn speed at full deflection.")]
    public float lookRate = 0.5f;
    [Tooltip("Flip if pushing up on the Look stick looks down.")]
    public bool invertLookY = false;

    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public float LookX { get; private set; }
    public float LookY { get; private set; }

    private RectTransform leftKnob, rightKnob;
    private Vector2 leftCenter, rightCenter; // screen px
    private float radius;
    private int leftFinger = -1, rightFinger = -1;
    private int mouseSide = -1; // editor mouse-as-touch: 0 = left stick, 1 = right stick

    void Start()
    {
        radius = Mathf.Min(Screen.width, Screen.height) * radiusFraction;
        float margin = radius * 1.5f;
        leftCenter = new Vector2(margin, margin);
        rightCenter = new Vector2(Screen.width - margin, margin);
        BuildUI();
    }

    void Update()
    {
        if (!GameConfig.Configured) return; // dormant while the main menu is up

        Horizontal = Vertical = LookX = LookY = 0f;
        Vector2 leftOff = Vector2.zero, rightOff = Vector2.zero;
        bool leftHeld = false, rightHeld = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            bool leftSide = t.position.x < Screen.width * 0.5f;
            if (t.phase == TouchPhase.Began)
            {
                if (leftSide && leftFinger == -1) leftFinger = t.fingerId;
                else if (!leftSide && rightFinger == -1) rightFinger = t.fingerId;
            }
            if (t.fingerId == leftFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) leftFinger = -1;
                else { leftOff = Vector2.ClampMagnitude(t.position - leftCenter, radius); leftHeld = true; }
            }
            else if (t.fingerId == rightFinger)
            {
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) rightFinger = -1;
                else { rightOff = Vector2.ClampMagnitude(t.position - rightCenter, radius); rightHeld = true; }
            }
        }

#if UNITY_EDITOR
        if (Input.touchCount == 0) EditorMouse(ref leftHeld, ref rightHeld, ref leftOff, ref rightOff);
#endif

        if (leftHeld)
        {
            Horizontal = leftOff.x / radius;
            Vertical = leftOff.y / radius;
        }
        if (rightHeld)
        {
            float dtNorm = Time.deltaTime * 60f; // frame-rate-independent turn rate
            LookX = (rightOff.x / radius) * lookRate * dtNorm;
            LookY = (rightOff.y / radius) * lookRate * dtNorm * (invertLookY ? -1f : 1f);
        }

        if (leftKnob != null) leftKnob.anchoredPosition = leftHeld ? leftOff : Vector2.zero;
        if (rightKnob != null) rightKnob.anchoredPosition = rightHeld ? rightOff : Vector2.zero;
    }

#if UNITY_EDITOR
    // Mouse-as-touch so the Unity Game view / Device Simulator is a full preview without a phone:
    // hold the mouse in the left half to drive Move, the right half to drive Look.
    private void EditorMouse(ref bool leftHeld, ref bool rightHeld, ref Vector2 leftOff, ref Vector2 rightOff)
    {
        if (Input.GetMouseButtonDown(0))
            mouseSide = Input.mousePosition.x < Screen.width * 0.5f ? 0 : 1;
        if (Input.GetMouseButton(0) && mouseSide >= 0)
        {
            Vector2 mp = Input.mousePosition;
            if (mouseSide == 0) { leftOff = Vector2.ClampMagnitude(mp - leftCenter, radius); leftHeld = true; }
            else { rightOff = Vector2.ClampMagnitude(mp - rightCenter, radius); rightHeld = true; }
        }
        if (Input.GetMouseButtonUp(0)) mouseSide = -1;
    }
#endif

    private void BuildUI()
    {
        var cgo = new GameObject("TouchControlsCanvas");
        cgo.transform.SetParent(transform, false);
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above the playing screen (50)

        Sprite ring = CircleSprite(128, new Color(1f, 1f, 1f, 0.16f));
        Sprite dot = CircleSprite(96, new Color(1f, 1f, 1f, 0.42f));
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        leftKnob = Stick(cgo.transform, leftCenter, ring, dot, "Move", font);
        rightKnob = Stick(cgo.transform, rightCenter, ring, dot, "Look", font);
    }

    // A stick = base ring at `center` (screen px) + a knob (returned) + a label under it.
    private RectTransform Stick(Transform parent, Vector2 center, Sprite ring, Sprite dot, string label, Font font)
    {
        var baseGo = new GameObject("Base_" + label, typeof(RectTransform));
        baseGo.transform.SetParent(parent, false);
        var brt = baseGo.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = Vector2.zero; // bottom-left → anchoredPosition is screen px
        brt.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        brt.anchoredPosition = center;
        Img(baseGo, ring);

        var knobGo = new GameObject("Knob", typeof(RectTransform));
        knobGo.transform.SetParent(baseGo.transform, false);
        var krt = knobGo.GetComponent<RectTransform>();
        krt.sizeDelta = new Vector2(radius * 0.9f, radius * 0.9f);
        krt.anchoredPosition = Vector2.zero;
        Img(knobGo, dot);

        var lblGo = new GameObject("Lbl_" + label, typeof(RectTransform));
        lblGo.transform.SetParent(parent, false);
        var lrt = lblGo.GetComponent<RectTransform>();
        lrt.anchorMin = lrt.anchorMax = Vector2.zero;
        lrt.sizeDelta = new Vector2(radius * 2f, 50f);
        lrt.anchoredPosition = new Vector2(center.x, center.y - radius - 34f);
        var txt = lblGo.AddComponent<Text>();
        txt.text = label; txt.font = font; txt.fontSize = 34; txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 1f, 1f, 0.6f);

        return krt;
    }

    private static void Img(GameObject go, Sprite s)
    {
        var img = go.AddComponent<Image>();
        img.sprite = s; img.raycastTarget = false;
    }

    private static Sprite CircleSprite(int size, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(r, r));
                float a = Mathf.Clamp01((r - d) / 2f);
                tex.SetPixel(x, y, new Color(color.r, color.g, color.b, color.a * a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
