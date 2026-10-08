using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh công cụ hiển thị tài nguyên thường trực ở đỉnh màn hình (Top Resource HUD Bar):
/// Gồm 5 Capsule tài nguyên (Vàng, Đá/Sắt, Gỗ/Gạch, Năng lượng, Kim cương) + 1 Nút Cài đặt (Bánh răng).
/// Thiết kế giao diện cao cấp, sắc nét, đồng bộ 100% với bức ảnh yêu cầu.
/// </summary>
public class TopResourceHUD : MonoBehaviour
{
    public static TopResourceHUD Instance { get; private set; }

    [Header("--- Tham Chiếu Text Số Lượng ---")]
    public Text coinText;
    public Text stoneText;
    public Text woodText;
    public Text energyText;
    public Text gemText;

    [Header("--- Tham Chiếu Panels ---")]
    public GameObject topBarPanel;
    public GameObject settingsPopup;

    private static Font safeFont;

    // Procedural Sprites sắc nét cho từng loại tài nguyên
    private static Sprite coinSprite;
    private static Sprite stoneSprite;
    private static Sprite woodSprite;
    private static Sprite energySprite;
    private static Sprite gemSprite;
    private static Sprite gearSprite;
    private static Sprite plusSprite;
    private static Sprite capsuleBgSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponentInChildren<TopResourceHUD>() == null)
            {
                GameObject hudObj = new GameObject("[Top_Resource_HUD]", typeof(RectTransform));
                hudObj.transform.SetParent(canvas.transform, false);
                hudObj.AddComponent<TopResourceHUD>();
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        EnsureSpritesCreated();
        CreateTopBarUI();

        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.OnEconomyChanged += RefreshAllValues;
        }

        RefreshAllValues();
    }

    void OnDestroy()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.OnEconomyChanged -= RefreshAllValues;
        }
    }

    /// <summary>
    /// Cập nhật hiển thị số lượng tài nguyên có dấu phẩy phân cách hàng nghìn (ví dụ 12,450)
    /// </summary>
    public void RefreshAllValues()
    {
        if (GameEconomy.Instance == null) return;

        if (coinText != null) coinText.text = GameEconomy.Instance.coins.ToString("#,##0");
        if (stoneText != null) stoneText.text = GameEconomy.Instance.stoneCount.ToString("#,##0");
        if (woodText != null) woodText.text = GameEconomy.Instance.woodCount.ToString("#,##0");
        if (energyText != null) energyText.text = GameEconomy.Instance.energyCount.ToString("#,##0");
        if (gemText != null) gemText.text = GameEconomy.Instance.gemCount.ToString("#,##0");
    }

    // ================= XỬ LÝ CLICK NÚT CỘNG TÀI NGUYÊN (+) =================

    private void OnAddCoinsClicked()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddCoins(1000);
            PunchScale(coinText);
            Debug.Log("<color=#FFD700>[Nạp Tài Nguyên]</color> +1,000 Vàng!");
        }
    }

    private void OnAddStoneClicked()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddStone(500);
            PunchScale(stoneText);
            Debug.Log("<color=#C0C0C0>[Nạp Tài Nguyên]</color> +500 Đá / Sắt!");
        }
    }

    private void OnAddWoodClicked()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddWood(500);
            PunchScale(woodText);
            Debug.Log("<color=#CD7F32>[Nạp Tài Nguyên]</color> +500 Gỗ / Gạch!");
        }
    }

    private void OnAddEnergyClicked()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddEnergy(50);
            PunchScale(energyText);
            Debug.Log("<color=#00FFFF>[Nạp Tài Nguyên]</color> +50 Năng lượng!");
        }
    }

    private void OnAddGemsClicked()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddGems(20);
            PunchScale(gemText);
            Debug.Log("<color=#E066FF>[Nạp Tài Nguyên]</color> +20 Kim cương tím!");
        }
    }

    private void PunchScale(Text targetText)
    {
        if (targetText != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(DoPunchScale(targetText.transform));
        }
    }

    private IEnumerator DoPunchScale(Transform tr)
    {
        Vector3 orig = Vector3.one;
        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
            tr.localScale = orig * s;
            yield return null;
        }

        tr.localScale = orig;
    }

    // ================= XỬ LÝ NÚT CÀI ĐẶT (BÁNH RĂNG) =================

    private void OnSettingsClicked()
    {
        if (settingsPopup != null)
        {
            bool willOpen = !settingsPopup.activeSelf;
            settingsPopup.SetActive(willOpen);
            if (willOpen) settingsPopup.transform.SetAsLastSibling();
        }
    }

    // ================= TỰ ĐỘNG KHỞI TẠO GIAO DIỆN UI CHUẨN ĐẸP =================

    private void CreateTopBarUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Tăng độ phân giải raster font trên thiết bị di động (tránh mờ nhòe chữ)
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 2.5f;
            scaler.referencePixelsPerUnit = 100;
        }

        safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. THANH CHỨA TOP HUD BAR Ở ĐỈNH MÀN HÌNH
        topBarPanel = new GameObject("Panel_TopResourceHUD", typeof(RectTransform));
        topBarPanel.layer = LayerMask.NameToLayer("UI");
        topBarPanel.transform.SetParent(canvas.transform, false);

        RectTransform topRect = topBarPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -38f); // Cách mép trên 38px
        topRect.sizeDelta = new Vector2(1040f, 72f);

        HorizontalLayoutGroup hlg = topBarPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 2. TẠO 5 CAPSULE TÀI NGUYÊN (THEO ĐÚNG HÌNH ẢNH)
        Vector2 capsuleSize = new Vector2(174f, 62f);

        // Capsule 1: VÀNG (12,450)
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Coins", capsuleSize, coinSprite, out coinText, OnAddCoinsClicked);

        // Capsule 2: ĐÁ / SẮT (8,230)
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Stone", capsuleSize, stoneSprite, out stoneText, OnAddStoneClicked);

        // Capsule 3: GỖ / GẠCH (5,180)
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Wood", capsuleSize, woodSprite, out woodText, OnAddWoodClicked);

        // Capsule 4: NĂNG LƯỢNG (320)
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Energy", capsuleSize, energySprite, out energyText, OnAddEnergyClicked);

        // Capsule 5: KIM CƯƠNG TÍM (120)
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Gems", capsuleSize, gemSprite, out gemText, OnAddGemsClicked);

        // 3. NÚT CÀI ĐẶT (BÁNH RĂNG) Ở GÓC PHẢI
        CreateSettingsButton(topBarPanel.transform, new Vector2(62f, 62f));

        // 4. POPUP CÀI ĐẶT NHỎ GỌN
        CreateSettingsPopup(canvas.transform);
    }

    private void CreateResourceCapsule(Transform parent, string name, Vector2 size, Sprite icon, out Text countText, UnityEngine.Events.UnityAction onAdd)
    {
        GameObject capsuleObj = new GameObject(name, typeof(RectTransform));
        capsuleObj.layer = LayerMask.NameToLayer("UI");
        capsuleObj.transform.SetParent(parent, false);

        RectTransform r = capsuleObj.GetComponent<RectTransform>();
        r.sizeDelta = size;

        Image bg = capsuleObj.AddComponent<Image>();
        bg.sprite = capsuleBgSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.10f, 0.16f, 0.94f); // Xanh đen công nghệ

        Outline ol = capsuleObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.12f, 0.38f, 0.62f, 0.85f); // Viền xanh navy cyan
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon bên trái
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.layer = LayerMask.NameToLayer("UI");
        iconObj.transform.SetParent(capsuleObj.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(8f, 0f);
        iconRect.sizeDelta = new Vector2(38f, 38f);

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = icon;
        iconImg.raycastTarget = false;

        // Nút cộng (+) ở bên phải
        GameObject plusObj = new GameObject("Btn_Plus", typeof(RectTransform));
        plusObj.layer = LayerMask.NameToLayer("UI");
        plusObj.transform.SetParent(capsuleObj.transform, false);
        RectTransform plusRect = plusObj.GetComponent<RectTransform>();
        plusRect.anchorMin = new Vector2(1f, 0.5f);
        plusRect.anchorMax = new Vector2(1f, 0.5f);
        plusRect.pivot = new Vector2(1f, 0.5f);
        plusRect.anchoredPosition = new Vector2(-6f, 0f);
        plusRect.sizeDelta = new Vector2(26f, 26f);

        Image plusBg = plusObj.AddComponent<Image>();
        plusBg.color = new Color(0.08f, 0.18f, 0.28f, 0.85f);

        Button plusBtn = plusObj.AddComponent<Button>();
        ColorBlock cb = plusBtn.colors;
        cb.highlightedColor = new Color(0.15f, 0.45f, 0.65f, 1f);
        cb.pressedColor = new Color(0.05f, 0.12f, 0.20f, 1f);
        plusBtn.colors = cb;
        plusBtn.onClick.AddListener(onAdd);

        // Icon dấu + màu cyan sáng
        GameObject plusIconObj = new GameObject("PlusIcon", typeof(RectTransform));
        plusIconObj.layer = LayerMask.NameToLayer("UI");
        plusIconObj.transform.SetParent(plusObj.transform, false);
        RectTransform piRect = plusIconObj.GetComponent<RectTransform>();
        piRect.anchorMin = Vector2.zero;
        piRect.anchorMax = Vector2.one;
        piRect.sizeDelta = Vector2.zero;

        Text piText = plusIconObj.AddComponent<Text>();
        piText.font = safeFont;
        piText.text = "+";
        piText.fontSize = 21;
        piText.fontStyle = FontStyle.Bold;
        piText.alignment = TextAnchor.MiddleCenter;
        piText.color = new Color(0.15f, 0.85f, 1.0f, 1f); // Cyan neon
        piText.raycastTarget = false;

        // Text số lượng ở giữa
        GameObject txtObj = new GameObject("CountText", typeof(RectTransform));
        txtObj.layer = LayerMask.NameToLayer("UI");
        txtObj.transform.SetParent(capsuleObj.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(48f, 0f);
        tr.offsetMax = new Vector2(-34f, 0f);

        countText = txtObj.AddComponent<Text>();
        countText.font = safeFont;
        countText.fontSize = 22;
        countText.fontStyle = FontStyle.Bold;
        countText.alignment = TextAnchor.MiddleRight;
        countText.color = new Color(0.98f, 0.99f, 1.0f, 1f); // Trắng sáng nổi bật
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Overflow;
        countText.raycastTarget = false;
        countText.text = "0";

        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void CreateSettingsButton(Transform parent, Vector2 size)
    {
        GameObject setObj = new GameObject("Btn_Settings", typeof(RectTransform));
        setObj.layer = LayerMask.NameToLayer("UI");
        setObj.transform.SetParent(parent, false);

        RectTransform r = setObj.GetComponent<RectTransform>();
        r.sizeDelta = size;

        Image bg = setObj.AddComponent<Image>();
        bg.sprite = capsuleBgSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.10f, 0.16f, 0.94f);

        Outline ol = setObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.12f, 0.38f, 0.62f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = setObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.15f, 0.45f, 0.65f, 1f);
        cb.pressedColor = new Color(0.05f, 0.12f, 0.20f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(OnSettingsClicked);

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.layer = LayerMask.NameToLayer("UI");
        iconObj.transform.SetParent(setObj.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;

        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = gearSprite;
        iconImg.raycastTarget = false;
    }

    private void CreateSettingsPopup(Transform canvasTr)
    {
        settingsPopup = new GameObject("Popup_SettingsDialog", typeof(RectTransform));
        settingsPopup.layer = LayerMask.NameToLayer("UI");
        settingsPopup.transform.SetParent(canvasTr, false);

        RectTransform rect = settingsPopup.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(500f, 320f);

        Image bg = settingsPopup.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.18f, 0.98f);

        Outline ol = settingsPopup.AddComponent<Outline>();
        ol.effectColor = new Color(0.15f, 0.65f, 1f, 0.85f);
        ol.effectDistance = new Vector2(2f, -2f);

        // Tiêu đề
        GameObject tObj = new GameObject("Title", typeof(RectTransform));
        tObj.layer = LayerMask.NameToLayer("UI");
        tObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -15f);
        tr.sizeDelta = new Vector2(460f, 36f);

        Text titleText = tObj.AddComponent<Text>();
        titleText.font = safeFont;
        titleText.text = "⚙️ <b>CÀI ĐẶT TRÒ CHƠI</b>";
        titleText.fontSize = 22;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        titleText.raycastTarget = false;

        // Nút cứu hộ Player Reset vị trí
        GameObject resetObj = new GameObject("Btn_ResetPlayerPos", typeof(RectTransform));
        resetObj.layer = LayerMask.NameToLayer("UI");
        resetObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform resetRect = resetObj.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.5f, 0.5f);
        resetRect.anchorMax = new Vector2(0.5f, 0.5f);
        resetRect.pivot = new Vector2(0.5f, 0.5f);
        resetRect.anchoredPosition = new Vector2(0f, 25f);
        resetRect.sizeDelta = new Vector2(380f, 55f);

        Image resetBg = resetObj.AddComponent<Image>();
        resetBg.color = new Color(0.18f, 0.55f, 0.75f, 1f);

        Button resetBtn = resetObj.AddComponent<Button>();
        resetBtn.onClick.AddListener(() =>
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = new Vector3(0f, 10.5f, -32f);
                if (cc != null) cc.enabled = true;
                Debug.Log("<color=#00FF88>[Cài Đặt]</color> Đã giải cứu đưa Player về vị trí an toàn trung tâm!");
            }
            settingsPopup.SetActive(false);
        });

        GameObject resetTxtObj = new GameObject("Text", typeof(RectTransform));
        resetTxtObj.layer = LayerMask.NameToLayer("UI");
        resetTxtObj.transform.SetParent(resetObj.transform, false);
        RectTransform rtr = resetTxtObj.GetComponent<RectTransform>();
        rtr.anchorMin = Vector2.zero;
        rtr.anchorMax = Vector2.one;
        rtr.sizeDelta = Vector2.zero;

        Text resetTxt = resetTxtObj.AddComponent<Text>();
        resetTxt.font = safeFont;
        resetTxt.text = "🚀 <b>CỨU HỘ PLAYER VỀ TRUNG TÂM</b>";
        resetTxt.fontSize = 17;
        resetTxt.alignment = TextAnchor.MiddleCenter;
        resetTxt.color = Color.white;
        resetTxt.raycastTarget = false;

        // Nút Đóng
        GameObject closeObj = new GameObject("Btn_Close", typeof(RectTransform));
        closeObj.layer = LayerMask.NameToLayer("UI");
        closeObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 20f);
        closeRect.sizeDelta = new Vector2(200f, 50f);

        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(0.65f, 0.22f, 0.22f, 1f);

        Button closeBtn = closeObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => settingsPopup.SetActive(false));

        GameObject closeTxtObj = new GameObject("Text", typeof(RectTransform));
        closeTxtObj.layer = LayerMask.NameToLayer("UI");
        closeTxtObj.transform.SetParent(closeObj.transform, false);
        RectTransform ctr = closeTxtObj.GetComponent<RectTransform>();
        ctr.anchorMin = Vector2.zero;
        ctr.anchorMax = Vector2.one;
        ctr.sizeDelta = Vector2.zero;

        Text closeTxt = closeTxtObj.AddComponent<Text>();
        closeTxt.font = safeFont;
        closeTxt.text = "<b>ĐÓNG</b>";
        closeTxt.fontSize = 18;
        closeTxt.alignment = TextAnchor.MiddleCenter;
        closeTxt.color = Color.white;
        closeTxt.raycastTarget = false;

        settingsPopup.SetActive(false);
    }

    // ================= TẠO PROCEDURAL SPRITES SẮC NÉT (PIXEL PERFECT) =================

    private static void EnsureSpritesCreated()
    {
        if (coinSprite != null) return;

        // 1. Khung Capsule Bo Góc Độ Phân Giải Cao (128x128 9-slice)
        Texture2D capTex = CreateRoundedBoxTexture(128, 128, 28, new Color(1f, 1f, 1f, 1f));
        capsuleBgSprite = Sprite.Create(capTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(32, 32, 32, 32));

        // 2-7. Bộ Icon Tài Nguyên 128x128 Siêu Sắc Nét Cho Màn Hình Retina/FHD+
        coinSprite = CreateSpriteFromTexture(CreateCoinTexture(128));
        stoneSprite = CreateSpriteFromTexture(CreateStoneTexture(128));
        woodSprite = CreateSpriteFromTexture(CreateBrickTexture(128));
        energySprite = CreateSpriteFromTexture(CreateLightningTexture(128));
        gemSprite = CreateSpriteFromTexture(CreateGemTexture(128));
        gearSprite = CreateSpriteFromTexture(CreateGearTexture(128));
    }

    private static Sprite CreateSpriteFromTexture(Texture2D tex)
    {
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
    }

    private static Texture2D CreateRoundedBoxTexture(int w, int h, int radius, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (w - 1 - radius)));
                int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (h - 1 - radius)));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01(1f - (dist - (radius - 1f)));
                cols[y * w + x] = new Color(color.r, color.g, color.b, alpha * color.a);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateCoinTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float center = size * 0.5f;
        float rOut = size * 0.44f;
        float rIn = size * 0.35f;

        Color[] cols = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (d > rOut + 1f)
                {
                    cols[y * size + x] = Color.clear;
                }
                else
                {
                    float aa = Mathf.Clamp01(rOut + 1f - d);
                    Color c;
                    if (d > rIn)
                    {
                        // Viền vàng cam kim loại
                        float rimT = (d - rIn) / (rOut - rIn);
                        c = Color.Lerp(new Color(1.0f, 0.72f, 0.05f), new Color(0.85f, 0.50f, 0.02f), rimT);
                    }
                    else
                    {
                        // Mặt trong đồng tiền vàng sáng bóng
                        float grad = (y - (center - rIn)) / (rIn * 2f);
                        c = Color.Lerp(new Color(1.0f, 0.88f, 0.20f), new Color(1.0f, 0.78f, 0.10f), grad);

                        // Biểu tượng chữ $ hoặc ngôi sao ở giữa
                        float dx = Mathf.Abs(x - center);
                        float dy = Mathf.Abs(y - center);
                        if (dx < 3.5f && dy < rIn * 0.65f) c = new Color(0.82f, 0.45f, 0.02f);
                        if (dy < 3.5f && dx < rIn * 0.45f) c = new Color(0.82f, 0.45f, 0.02f);
                    }
                    cols[y * size + x] = new Color(c.r, c.g, c.b, aa);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateStoneTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        float c = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Hình thỏi bạc / tảng đá 3D đa giác
                float nx = (x - c) / (size * 0.42f);
                float ny = (y - c) / (size * 0.38f);
                float boxDist = Mathf.Max(Mathf.Abs(nx) + Mathf.Abs(ny) * 0.4f, Mathf.Abs(ny));

                if (boxDist > 1.05f)
                {
                    cols[y * size + x] = Color.clear;
                }
                else
                {
                    float aa = Mathf.Clamp01(1.05f - boxDist);
                    // Mặt trên sáng bạc, mặt bên bóng đổ
                    Color col = (ny > 0.1f) ? new Color(0.82f, 0.86f, 0.94f) : new Color(0.55f, 0.58f, 0.68f);
                    if (nx < 0f) col *= 1.15f; // Ánh sáng từ bên trái
                    cols[y * size + x] = new Color(col.r, col.g, col.b, aa);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateBrickTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        float c = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / (size * 0.44f);
                float ny = (y - c) / (size * 0.40f);
                float dist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                if (dist > 1.0f)
                {
                    cols[y * size + x] = Color.clear;
                }
                else
                {
                    float aa = Mathf.Clamp01(1.0f - dist);
                    // Màu gạch nung đỏ cam 3D cao cấp
                    Color col = (ny > 0f) ? new Color(0.92f, 0.48f, 0.22f) : new Color(0.72f, 0.32f, 0.12f);
                    if (nx < 0f) col *= 1.1f;
                    cols[y * size + x] = new Color(col.r, col.g, col.b, aa);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateLightningTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Vẽ tia sét ⚡ zigzag sắc nét
                float u = (float)x / size;
                float v = (float)y / size;

                float boltX = 0.5f;
                if (v < 0.45f) boltX = Mathf.Lerp(0.35f, 0.65f, v / 0.45f);
                else if (v < 0.55f) boltX = Mathf.Lerp(0.65f, 0.38f, (v - 0.45f) / 0.1f);
                else boltX = Mathf.Lerp(0.38f, 0.62f, (v - 0.55f) / 0.45f);

                float dist = Mathf.Abs(u - boltX);
                if (dist < 0.14f && v > 0.1f && v < 0.9f)
                {
                    float core = Mathf.Clamp01(1f - dist / 0.14f);
                    Color neonCyan = Color.Lerp(new Color(0.0f, 0.85f, 1.0f), Color.white, core * core);
                    cols[y * size + x] = new Color(neonCyan.r, neonCyan.g, neonCyan.b, core);
                }
                else
                {
                    cols[y * size + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateGemTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        float c = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Hình viên kim cương tím đa giác 💎
                float nx = Mathf.Abs((x - c) / (size * 0.40f));
                float ny = (y - c) / (size * 0.42f);

                bool inside = false;
                if (ny >= 0f) inside = (nx + ny * 0.6f <= 0.85f);
                else inside = (nx <= 0.85f + ny * 0.95f);

                if (!inside)
                {
                    cols[y * size + x] = Color.clear;
                }
                else
                {
                    // Màu tím pha lê phát sáng
                    Color col = (ny > 0.1f) ? new Color(0.85f, 0.50f, 1.0f) : new Color(0.60f, 0.15f, 0.90f);
                    if (x < c) col *= 1.2f;
                    cols[y * size + x] = new Color(col.r, col.g, col.b, 1f);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateGearTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] cols = new Color[size * size];
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);

                // 8 răng cưa bánh răng ⚙️
                float teeth = Mathf.Sin(angle * 8f) * (size * 0.08f);
                float rOuter = size * 0.38f + teeth;
                float rHole = size * 0.14f;

                if (dist > rOuter || dist < rHole)
                {
                    cols[y * size + x] = Color.clear;
                }
                else
                {
                    float aa = Mathf.Clamp01(rOuter - dist);
                    Color c = Color.Lerp(new Color(0.70f, 0.82f, 0.95f), new Color(0.40f, 0.55f, 0.75f), (float)y / size);
                    cols[y * size + x] = new Color(c.r, c.g, c.b, aa);
                }
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }
}
