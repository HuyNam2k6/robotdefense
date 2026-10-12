using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// VirtualJoystick - Cần gạt di chuyển ảo công nghệ Cyber ở giữa màn game:
/// - Điều khiển nhân vật Player di chuyển linh hoạt 360 độ các hướng.
/// - Đặt tại vị trí giữa màn game (Horizontal Center, vị trí ngón tay bấm tiện lợi).
/// - Tự động liên kết mượt mà với PlayerController.
/// - Tự động sinh đồ họa hình tròn mượt mà sắc nét (Anti-aliased procedural circles).
/// - Tương thích 100% cảm ứng đa điểm Mobile và chuột kéo trên PC.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public static VirtualJoystick Instance { get; private set; }

    [Header("UI Tham Chiếu")]
    public RectTransform background;
    public RectTransform handle;

    private Vector2 inputVector = Vector2.zero;

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public Vector2 Direction => inputVector;

    private static Sprite cachedCircleSprite;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureSprites();
    }

    void Start()
    {
        BindToPlayer();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BindToPlayer()
    {
        PlayerController player = PlayerController.Instance ?? FindAnyObjectByType<PlayerController>();
        if (player != null && player.joystick == null)
        {
            player.joystick = this;
            Debug.Log("<color=#00FF88>[VirtualJoystick]</color> Đã tự động kết nối cần điều khiển di chuyển tới Player!");
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, eventData.pressEventCamera, out pos))
        {
            float radius = background.sizeDelta.x * 0.5f;
            if (radius <= 0.001f) radius = 100f;

            inputVector = pos / radius;
            if (inputVector.magnitude > 1.0f)
            {
                inputVector.Normalize();
            }

            // Khoảng di chuyển tối đa của núm điều khiển
            float maxHandleOffset = radius * 0.72f;
            handle.anchoredPosition = inputVector * maxHandleOffset;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    private void EnsureSprites()
    {
        Sprite circle = GetOrCreateCircleSprite();

        if (background != null)
        {
            Image bgImg = background.GetComponent<Image>();
            if (bgImg != null && bgImg.sprite == null)
            {
                bgImg.sprite = circle;
                bgImg.type = Image.Type.Simple;
            }
        }

        if (handle != null)
        {
            Image hImg = handle.GetComponent<Image>();
            if (hImg != null && hImg.sprite == null)
            {
                hImg.sprite = circle;
                hImg.type = Image.Type.Simple;
            }
        }
    }

    public static Sprite GetOrCreateCircleSprite(int size = 128)
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        float radius = center - 1.5f;
        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= radius - 1.2f)
                {
                    tex.SetPixel(x, y, white);
                }
                else if (dist <= radius)
                {
                    float alpha = 1f - (dist - (radius - 1.2f)) / 1.2f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
                }
                else
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
        }
        tex.Apply();
        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureJoystickRuntime()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<VirtualJoystick>() != null) return;

        Canvas canvas = null;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c != null && c.renderMode != RenderMode.WorldSpace)
            {
                canvas = c;
                break;
            }
        }

        if (canvas != null)
        {
            CreateJoystickHierarchy(canvas.gameObject);
        }
    }

    public static GameObject CreateJoystickHierarchy(GameObject canvasObj)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // Xóa joystick cũ nếu có trùng lặp
        Transform oldJoy = canvasObj.transform.Find("Joystick_Zone");
        if (oldJoy != null) Object.DestroyImmediate(oldJoy.gameObject);

        Sprite circleSprite = GetOrCreateCircleSprite();

        // 1. Vùng cảm ứng Joystick (Center-Bottom: Ngay giữa phía dưới màn hình)
        GameObject joyZone = new GameObject("Joystick_Zone", typeof(RectTransform));
        joyZone.layer = uiLayer;
        joyZone.transform.SetParent(canvasObj.transform, false);

        RectTransform zoneRt = joyZone.GetComponent<RectTransform>();
        zoneRt.anchorMin = new Vector2(0.5f, 0f);
        zoneRt.anchorMax = new Vector2(0.5f, 0f);
        zoneRt.pivot = new Vector2(0.5f, 0.5f);
        zoneRt.anchoredPosition = new Vector2(0f, 210f); // Ở GIỮA MÀN GAME (Horizontal Center)
        zoneRt.sizeDelta = new Vector2(300f, 300f);

        Image zoneImg = joyZone.AddComponent<Image>();
        zoneImg.color = new Color(0f, 0f, 0f, 0.001f); // Trong suốt nhận click cảm ứng rộng
        zoneImg.raycastTarget = true;

        // 2. Vòng tròn nền (Background)
        GameObject bgObj = new GameObject("Joystick_Background", typeof(RectTransform));
        bgObj.layer = uiLayer;
        bgObj.transform.SetParent(joyZone.transform, false);

        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(230f, 230f);

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color = new Color(0.06f, 0.12f, 0.20f, 0.72f); // Kính Cyber mờ cao cấp
        bgImg.raycastTarget = false;

        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0f, 0.85f, 1f, 0.75f); // Phát sáng viền neon xanh cyan
        bgOutline.effectDistance = new Vector2(2.5f, -2.5f);

        // Mũi tên 4 hướng tinh tế (▲, ▼, ◄, ►)
        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateDirectionAccents(bgObj.transform, safeFont);

        // 3. Núm gạt di chuyển (Handle)
        GameObject handleObj = new GameObject("Joystick_Handle", typeof(RectTransform));
        handleObj.layer = uiLayer;
        handleObj.transform.SetParent(bgObj.transform, false);

        RectTransform hRt = handleObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.5f, 0.5f);
        hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.anchoredPosition = Vector2.zero;
        hRt.sizeDelta = new Vector2(95f, 95f);

        Image hImg = handleObj.AddComponent<Image>();
        hImg.sprite = circleSprite;
        hImg.color = new Color(1f, 0.80f, 0.22f, 0.95f); // Màu vàng cam Amber rực rỡ
        hImg.raycastTarget = false;

        Outline hOutline = handleObj.AddComponent<Outline>();
        hOutline.effectColor = new Color(1f, 0.95f, 0.55f, 0.90f);
        hOutline.effectDistance = new Vector2(2f, -2f);

        // Điểm tâm lõi núm gạt (Center Dot)
        GameObject centerDot = new GameObject("Center_Dot", typeof(RectTransform));
        centerDot.layer = uiLayer;
        centerDot.transform.SetParent(handleObj.transform, false);
        RectTransform dotRt = centerDot.GetComponent<RectTransform>();
        dotRt.anchorMin = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.anchoredPosition = Vector2.zero;
        dotRt.sizeDelta = new Vector2(30f, 30f);
        Image dotImg = centerDot.AddComponent<Image>();
        dotImg.sprite = circleSprite;
        dotImg.color = new Color(0.12f, 0.12f, 0.14f, 0.85f);
        dotImg.raycastTarget = false;

        // 4. Gắn component VirtualJoystick
        VirtualJoystick vj = joyZone.AddComponent<VirtualJoystick>();
        vj.background = bgRt;
        vj.handle = hRt;

        vj.BindToPlayer();

        Debug.Log("<color=#00FF88>✅ [VirtualJoystick]</color> Đã tạo Joystick di chuyển ở giữa màn game (Center X = 0, Y = 210)! Sẵn sàng điều khiển Player các hướng.");
        return joyZone;
    }

    private static void CreateDirectionAccents(Transform parent, Font font)
    {
        string[] arrows = new string[] { "▲", "▼", "◄", "►" };
        Vector2[] positions = new Vector2[]
        {
            new Vector2(0f, 92f),
            new Vector2(0f, -92f),
            new Vector2(-92f, 0f),
            new Vector2(92f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject arrowObj = new GameObject($"Arrow_{i}", typeof(RectTransform));
            arrowObj.layer = parent.gameObject.layer;
            arrowObj.transform.SetParent(parent, false);
            RectTransform art = arrowObj.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0.5f, 0.5f);
            art.anchorMax = new Vector2(0.5f, 0.5f);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = positions[i];
            art.sizeDelta = new Vector2(24f, 24f);

            Text atxt = arrowObj.AddComponent<Text>();
            atxt.font = font;
            atxt.text = arrows[i];
            atxt.fontSize = 15;
            atxt.alignment = TextAnchor.MiddleCenter;
            atxt.color = new Color(0f, 0.85f, 1f, 0.45f);
            atxt.raycastTarget = false;
        }
    }
}
