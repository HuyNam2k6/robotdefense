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

    public static Canvas GetMainScreenCanvas()
    {
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>();
        foreach (var c in allCanvases)
        {
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay) return c;
        }
        foreach (var c in allCanvases)
        {
            if (c != null && c.renderMode != RenderMode.WorldSpace) return c;
        }
        foreach (var c in allCanvases)
        {
            if (c != null && (c.name.Contains("Shop_Canvas") || c.name.Contains("Mobile_Canvas"))) return c;
        }
        return allCanvases.Length > 0 ? allCanvases[0] : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            Canvas canvas = GetMainScreenCanvas();
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
        EnsureEventSystemExists();
        EnsureSpritesCreated();
        CreateTopBarUI();
        RefreshAudioSettingsUI();

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

    private int lastStoneCount = -1;

    /// <summary>
    /// Cập nhật hiển thị số lượng tài nguyên có dấu phẩy phân cách hàng nghìn (ví dụ 12,450)
    /// </summary>
    public void RefreshAllValues()
    {
        if (GameEconomy.Instance == null) return;

        if (coinText != null) coinText.text = GameEconomy.Instance.coins.ToString("#,##0");
        if (stoneText != null)
        {
            stoneText.text = GameEconomy.Instance.stoneCount.ToString("#,##0");
            if (lastStoneCount != -1 && GameEconomy.Instance.stoneCount > lastStoneCount)
            {
                PunchScale(stoneText);
            }
            lastStoneCount = GameEconomy.Instance.stoneCount;
        }
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
            if (willOpen)
            {
                settingsPopup.transform.SetAsLastSibling();
                RefreshAudioSettingsUI();
            }
        }
    }

    // ================= TỰ ĐỘNG KHỞI TẠO GIAO DIỆN UI CHUẨN ĐẸP =================

    private void CreateTopBarUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = GetMainScreenCanvas();
        }
        if (canvas == null) return;

        // Bật pixelPerfect và tỷ lệ 1:1 chuẩn để font rasterizer luôn siêu nét trên mọi độ phân giải
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 1f; // 1:1 chuẩn chống mờ nhòe bilinear
            scaler.referencePixelsPerUnit = 100;
        }

        safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Dọn dẹp thanh cũ nếu đã tồn tại để tránh render đè 2 lớp làm tối chữ
        Transform oldPanel = canvas.transform.Find("Panel_TopResourceHUD");
        if (oldPanel != null) Destroy(oldPanel.gameObject);

        // 1. THANH CHỨA TOP HUD BAR Ở ĐỈNH MÀN HÌNH
        topBarPanel = new GameObject("Panel_TopResourceHUD", typeof(RectTransform));
        topBarPanel.layer = LayerMask.NameToLayer("UI");
        topBarPanel.transform.SetParent(canvas.transform, false);

        RectTransform topRect = topBarPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -38f); // Cách mép trên 38px
        topRect.sizeDelta = new Vector2(924f, 72f);

        HorizontalLayoutGroup hlg = topBarPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // 2. TẠO 4 CAPSULE TÀI NGUYÊN (VÀNG, ĐÁ/SẮT, NĂNG LƯỢNG, KIM CƯƠNG)
        Vector2 capsuleSize = new Vector2(182f, 64f);

        // Capsule 1: VÀNG
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Coins", capsuleSize, coinSprite, out coinText, OnAddCoinsClicked);

        // Capsule 2: ĐÁ / SẮT
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Stone", capsuleSize, stoneSprite, out stoneText, OnAddStoneClicked);

        // Capsule 3: NĂNG LƯỢNG
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Energy", capsuleSize, energySprite, out energyText, OnAddEnergyClicked);

        // Capsule 4: KIM CƯƠNG TÍM
        CreateResourceCapsule(topBarPanel.transform, "Capsule_Gems", capsuleSize, gemSprite, out gemText, OnAddGemsClicked);

        // 3. NÚT ÂM THANH NHANH (QUICK MUTE/UNMUTE)
        CreateQuickAudioButton(topBarPanel.transform, new Vector2(64f, 64f));

        // 4. NÚT CÀI ĐẶT (BÁNH RĂNG) Ở GÓC PHẢI
        CreateSettingsButton(topBarPanel.transform, new Vector2(64f, 64f));

        // 5. POPUP CÀI ĐẶT NHỎ GỌN
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
        bg.color = new Color(0.06f, 0.10f, 0.16f, 0.96f); // Xanh đen công nghệ

        Outline ol = capsuleObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.12f, 0.45f, 0.75f, 0.85f); // Viền xanh navy cyan
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
        iconRect.sizeDelta = new Vector2(40f, 40f);

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
        plusRect.sizeDelta = new Vector2(28f, 28f);

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
        piText.fontSize = 22;
        piText.fontStyle = FontStyle.Bold;
        piText.alignment = TextAnchor.MiddleCenter;
        piText.color = new Color(0.0f, 0.92f, 1.0f, 1f); // Cyan neon
        piText.raycastTarget = false;

        // Text số lượng ở giữa (Cỡ chữ 28 đậm, không dùng bóng đổ đen đặc gây lem chữ)
        GameObject txtObj = new GameObject("CountText", typeof(RectTransform));
        txtObj.layer = LayerMask.NameToLayer("UI");
        txtObj.transform.SetParent(capsuleObj.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(46f, 0f);
        tr.offsetMax = new Vector2(-36f, 0f);

        countText = txtObj.AddComponent<Text>();
        countText.font = safeFont;
        countText.fontSize = 28;
        countText.fontStyle = FontStyle.Bold;
        countText.alignment = TextAnchor.MiddleRight;
        countText.color = new Color(0.98f, 0.99f, 1.0f, 1f); // Trắng sáng nổi bật
        countText.horizontalOverflow = HorizontalWrapMode.Overflow;
        countText.verticalOverflow = VerticalWrapMode.Overflow;
        countText.raycastTarget = false;
        countText.text = "0";

        // Outline mảnh tinh tế để chữ sắc cạnh, không bị lem mực
        Outline txtOutline = txtObj.AddComponent<Outline>();
        txtOutline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        txtOutline.effectDistance = new Vector2(0.8f, -0.8f);
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

    private void CreateQuickAudioButton(Transform parent, Vector2 size)
    {
        GameObject audioBtnObj = new GameObject("Btn_QuickAudio", typeof(RectTransform));
        audioBtnObj.layer = LayerMask.NameToLayer("UI");
        audioBtnObj.transform.SetParent(parent, false);

        RectTransform r = audioBtnObj.GetComponent<RectTransform>();
        r.sizeDelta = size;

        Image bg = audioBtnObj.AddComponent<Image>();
        bg.sprite = capsuleBgSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.06f, 0.10f, 0.16f, 0.94f);

        Outline ol = audioBtnObj.AddComponent<Outline>();
        ol.effectColor = new Color(0.12f, 0.38f, 0.62f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = audioBtnObj.AddComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (GameAudioManager.Instance != null)
            {
                GameAudioManager.Instance.ToggleMute();
                RefreshAudioSettingsUI();
            }
        });

        GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
        iconObj.layer = LayerMask.NameToLayer("UI");
        iconObj.transform.SetParent(audioBtnObj.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;

        quickAudioBtnText = iconObj.AddComponent<Text>();
        quickAudioBtnText.font = safeFont;
        quickAudioBtnText.fontSize = 26;
        quickAudioBtnText.alignment = TextAnchor.MiddleCenter;
        quickAudioBtnText.text = "🔊";
        quickAudioBtnText.color = new Color(0f, 0.95f, 1f, 1f);
        quickAudioBtnText.raycastTarget = false;
    }

    private Slider bgmSlider;
    private Text bgmPercentText;
    private Text bgmMuteBtnText;

    private Slider sfxSlider;
    private Text sfxPercentText;
    private Text sfxMuteBtnText;

    private Text quickAudioBtnText;

    private void EnsureEventSystemExists()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }

    public void RefreshAudioSettingsUI()
    {
        if (GameAudioManager.Instance == null) return;

        float bgmVol = GameAudioManager.Instance.bgmVolume;
        bool bgmMuted = GameAudioManager.Instance.isBgmMuted;

        float sfxVol = GameAudioManager.Instance.sfxVolume;
        bool sfxMuted = GameAudioManager.Instance.isSfxMuted;

        // 1. Nhạc Nền (BGM): Giữ nguyên vị trí thanh trượt đã kéo
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(bgmVol);
        if (bgmPercentText != null) bgmPercentText.text = (bgmMuted || bgmVol <= 0.01f) ? "TẮT" : $"{Mathf.RoundToInt(bgmVol * 100)}%";
        if (bgmMuteBtnText != null) bgmMuteBtnText.text = (!bgmMuted && bgmVol > 0.01f) ? "🔊" : "🔇";

        // 2. Hiệu Ứng (SFX): Giữ nguyên vị trí thanh trượt đã kéo
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfxVol);
        if (sfxPercentText != null) sfxPercentText.text = (sfxMuted || sfxVol <= 0.01f) ? "TẮT" : $"{Mathf.RoundToInt(sfxVol * 100)}%";
        if (sfxMuteBtnText != null) sfxMuteBtnText.text = (!sfxMuted && sfxVol > 0.01f) ? "🔊" : "🔇";

        // 3. Nút Quick Audio góc trên TopBar
        UpdateQuickAudioButtonState();
    }

    private void UpdateQuickAudioButtonState()
    {
        if (quickAudioBtnText == null || GameAudioManager.Instance == null) return;
        bool hasSound = (!GameAudioManager.Instance.isBgmMuted && GameAudioManager.Instance.bgmVolume > 0.01f) ||
                        (!GameAudioManager.Instance.isSfxMuted && GameAudioManager.Instance.sfxVolume > 0.01f);
        quickAudioBtnText.text = hasSound ? "🔊" : "🔇";
        quickAudioBtnText.color = hasSound ? new Color(0f, 0.95f, 1f, 1f) : new Color(1f, 0.35f, 0.35f, 1f);
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
        rect.sizeDelta = new Vector2(520f, 490f);

        Image bg = settingsPopup.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);

        Outline ol = settingsPopup.AddComponent<Outline>();
        ol.effectColor = new Color(0.15f, 0.75f, 1f, 0.9f);
        ol.effectDistance = new Vector2(2.5f, -2.5f);

        // 1. Tiêu đề Cài Đặt
        GameObject tObj = new GameObject("Title", typeof(RectTransform));
        tObj.layer = LayerMask.NameToLayer("UI");
        tObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform tr = tObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 1f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -14f);
        tr.sizeDelta = new Vector2(480f, 38f);

        Text titleText = tObj.AddComponent<Text>();
        titleText.font = safeFont;
        titleText.text = "⚙️ <b>CÀI ĐẶT TRÒ CHƠI</b>";
        titleText.fontSize = 22;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        titleText.raycastTarget = false;

        // 2. KHUNG ÂM THANH: NHẠC NỀN RIÊNG VÀ HIỆU ỨNG RIÊNG (SLIDER KÉO THẢ ĐỨNG YÊN)
        GameObject audioBox = new GameObject("Audio_Section_Box", typeof(RectTransform));
        audioBox.layer = LayerMask.NameToLayer("UI");
        audioBox.transform.SetParent(settingsPopup.transform, false);
        RectTransform abRt = audioBox.GetComponent<RectTransform>();
        abRt.anchorMin = new Vector2(0.5f, 1f);
        abRt.anchorMax = new Vector2(0.5f, 1f);
        abRt.pivot = new Vector2(0.5f, 1f);
        abRt.anchoredPosition = new Vector2(0f, -62f);
        abRt.sizeDelta = new Vector2(470f, 210f);

        Image abBg = audioBox.AddComponent<Image>();
        abBg.color = new Color(0.03f, 0.07f, 0.12f, 0.95f);
        Outline abOl = audioBox.AddComponent<Outline>();
        abOl.effectColor = new Color(0f, 0.65f, 0.9f, 0.6f);
        abOl.effectDistance = new Vector2(1.5f, -1.5f);

        // Tiêu đề khung âm thanh
        GameObject aHeadObj = new GameObject("AudioHeader", typeof(RectTransform), typeof(Text));
        aHeadObj.layer = LayerMask.NameToLayer("UI");
        aHeadObj.transform.SetParent(audioBox.transform, false);
        RectTransform ahRt = aHeadObj.GetComponent<RectTransform>();
        ahRt.anchorMin = new Vector2(0f, 1f);
        ahRt.anchorMax = new Vector2(1f, 1f);
        ahRt.pivot = new Vector2(0.5f, 1f);
        ahRt.anchoredPosition = new Vector2(0f, -10f);
        ahRt.sizeDelta = new Vector2(440f, 24f);
        Text ahTxt = aHeadObj.GetComponent<Text>();
        ahTxt.font = safeFont;
        ahTxt.text = "🎵 <b>CÀI ĐẶT ÂM THANH</b>";
        ahTxt.fontSize = 15;
        ahTxt.alignment = TextAnchor.MiddleCenter;
        ahTxt.color = new Color(0f, 0.92f, 1f, 1f);

        // THANH 1: ÂM THANH NỀN (BGM)
        bgmSlider = CreateAudioSliderRow(
            audioBox.transform,
            "🎵 Âm Thanh Nền",
            -40f,
            out bgmPercentText,
            out bgmMuteBtnText,
            () =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.ToggleBGMMute();
                RefreshAudioSettingsUI();
            },
            (val) =>
            {
                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.SetBGMVolume(val);
                    if (val > 0.01f && GameAudioManager.Instance.isBgmMuted)
                    {
                        GameAudioManager.Instance.SetBGMMuted(false);
                    }
                }
                if (bgmPercentText != null)
                {
                    bool isMuted = GameAudioManager.Instance != null && GameAudioManager.Instance.isBgmMuted;
                    bgmPercentText.text = (isMuted || val <= 0.01f) ? "TẮT" : $"{Mathf.RoundToInt(val * 100)}%";
                }
                if (bgmMuteBtnText != null)
                {
                    bool isMuted = GameAudioManager.Instance != null && GameAudioManager.Instance.isBgmMuted;
                    bgmMuteBtnText.text = (!isMuted && val > 0.01f) ? "🔊" : "🔇";
                }
                UpdateQuickAudioButtonState();
            }
        );

        // THANH 2: ÂM THANH HIỆU ỨNG (SFX)
        sfxSlider = CreateAudioSliderRow(
            audioBox.transform,
            "💥 Âm Thanh Hiệu Ứng",
            -116f,
            out sfxPercentText,
            out sfxMuteBtnText,
            () =>
            {
                if (GameAudioManager.Instance != null) GameAudioManager.Instance.ToggleSFXMute();
                RefreshAudioSettingsUI();
            },
            (val) =>
            {
                if (GameAudioManager.Instance != null)
                {
                    GameAudioManager.Instance.SetSFXVolume(val);
                    if (val > 0.01f && GameAudioManager.Instance.isSfxMuted)
                    {
                        GameAudioManager.Instance.SetSFXMuted(false);
                    }
                }
                if (sfxPercentText != null)
                {
                    bool isMuted = GameAudioManager.Instance != null && GameAudioManager.Instance.isSfxMuted;
                    sfxPercentText.text = (isMuted || val <= 0.01f) ? "TẮT" : $"{Mathf.RoundToInt(val * 100)}%";
                }
                if (sfxMuteBtnText != null)
                {
                    bool isMuted = GameAudioManager.Instance != null && GameAudioManager.Instance.isSfxMuted;
                    sfxMuteBtnText.text = (!isMuted && val > 0.01f) ? "🔊" : "🔇";
                }
                UpdateQuickAudioButtonState();
            }
        );

        // 3. NÚT CỨU HỘ PLAYER RESET VỊ TRÍ
        GameObject resetObj = new GameObject("Btn_ResetPlayerPos", typeof(RectTransform));
        resetObj.layer = LayerMask.NameToLayer("UI");
        resetObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform resetRect = resetObj.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0.5f, 0f);
        resetRect.anchorMax = new Vector2(0.5f, 0f);
        resetRect.pivot = new Vector2(0.5f, 0f);
        resetRect.anchoredPosition = new Vector2(0f, 85f);
        resetRect.sizeDelta = new Vector2(400f, 48f);

        Image resetBg = resetObj.AddComponent<Image>();
        resetBg.color = new Color(0.18f, 0.52f, 0.72f, 1f);

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
        resetTxt.fontSize = 16;
        resetTxt.alignment = TextAnchor.MiddleCenter;
        resetTxt.color = Color.white;
        resetTxt.raycastTarget = false;

        // 4. NÚT ĐÓNG
        GameObject closeObj = new GameObject("Btn_Close", typeof(RectTransform));
        closeObj.layer = LayerMask.NameToLayer("UI");
        closeObj.transform.SetParent(settingsPopup.transform, false);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 20f);
        closeRect.sizeDelta = new Vector2(220f, 48f);

        Image closeBg = closeObj.AddComponent<Image>();
        closeBg.color = new Color(0.68f, 0.2f, 0.2f, 1f);

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

    private Slider CreateAudioSliderRow(
        Transform parent,
        string titleText,
        float posY,
        out Text percentText,
        out Text muteBtnText,
        UnityEngine.Events.UnityAction onMuteClick,
        UnityEngine.Events.UnityAction<float> onSliderChange)
    {
        bool isBgm = titleText.Contains("Nền");
        Color themeColor = isBgm ? new Color(0f, 0.88f, 1f, 1f) : new Color(1f, 0.75f, 0.2f, 1f);

        // 1. Container của từng dòng cài đặt âm thanh
        GameObject rowObj = new GameObject("SliderRow_" + (isBgm ? "BGM" : "SFX"), typeof(RectTransform));
        rowObj.layer = LayerMask.NameToLayer("UI");
        rowObj.transform.SetParent(parent, false);

        RectTransform rowRt = rowObj.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 1f);
        rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, posY);
        rowRt.sizeDelta = new Vector2(440f, 68f);

        // 2. Tên phân loại âm thanh (bên trái phía trên)
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.layer = LayerMask.NameToLayer("UI");
        labelObj.transform.SetParent(rowObj.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(0.65f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.anchoredPosition = new Vector2(6f, -2f);
        labelRt.sizeDelta = new Vector2(250f, 22f);

        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font = safeFont;
        labelTxt.text = "<b>" + titleText + "</b>";
        labelTxt.fontSize = 15;
        labelTxt.alignment = TextAnchor.MiddleLeft;
        labelTxt.color = themeColor;
        labelTxt.raycastTarget = false;

        // 3. Số % âm lượng trực quan (bên phải phía trên)
        GameObject pctObj = new GameObject("Percent", typeof(RectTransform), typeof(Text));
        pctObj.layer = LayerMask.NameToLayer("UI");
        pctObj.transform.SetParent(rowObj.transform, false);
        RectTransform pctRt = pctObj.GetComponent<RectTransform>();
        pctRt.anchorMin = new Vector2(0.65f, 1f);
        pctRt.anchorMax = new Vector2(1f, 1f);
        pctRt.pivot = new Vector2(1f, 1f);
        pctRt.anchoredPosition = new Vector2(-6f, -2f);
        pctRt.sizeDelta = new Vector2(140f, 22f);

        percentText = pctObj.GetComponent<Text>();
        percentText.font = safeFont;
        percentText.text = "100%";
        percentText.fontSize = 15;
        percentText.alignment = TextAnchor.MiddleRight;
        percentText.color = Color.white;
        percentText.raycastTarget = false;

        // 4. Nút Bật/Tắt âm thanh riêng biệt (Mute/Unmute)
        GameObject muteBtnObj = new GameObject("Btn_Mute", typeof(RectTransform), typeof(Image), typeof(Button));
        muteBtnObj.layer = LayerMask.NameToLayer("UI");
        muteBtnObj.transform.SetParent(rowObj.transform, false);
        RectTransform muteRt = muteBtnObj.GetComponent<RectTransform>();
        muteRt.anchorMin = new Vector2(0f, 0f);
        muteRt.anchorMax = new Vector2(0f, 0f);
        muteRt.pivot = new Vector2(0f, 0f);
        muteRt.anchoredPosition = new Vector2(6f, 6f);
        muteRt.sizeDelta = new Vector2(40f, 32f);

        Image muteBg = muteBtnObj.GetComponent<Image>();
        muteBg.color = new Color(0.08f, 0.16f, 0.25f, 0.95f);
        Outline mOl = muteBtnObj.AddComponent<Outline>();
        mOl.effectColor = new Color(0.15f, 0.45f, 0.7f, 0.8f);
        mOl.effectDistance = new Vector2(1f, -1f);

        Button muteBtn = muteBtnObj.GetComponent<Button>();
        muteBtn.onClick.AddListener(onMuteClick);

        GameObject muteTxtObj = new GameObject("Icon", typeof(RectTransform), typeof(Text));
        muteTxtObj.layer = LayerMask.NameToLayer("UI");
        muteTxtObj.transform.SetParent(muteBtnObj.transform, false);
        RectTransform mtRt = muteTxtObj.GetComponent<RectTransform>();
        mtRt.anchorMin = Vector2.zero;
        mtRt.anchorMax = Vector2.one;
        mtRt.sizeDelta = Vector2.zero;

        muteBtnText = muteTxtObj.GetComponent<Text>();
        muteBtnText.font = safeFont;
        muteBtnText.text = "🔊";
        muteBtnText.fontSize = 18;
        muteBtnText.alignment = TextAnchor.MiddleCenter;
        muteBtnText.color = Color.white;
        muteBtnText.raycastTarget = false;

        // 5. Thanh trượt Slider tương tác thực tế (Kéo - Thả đứng yên tại chỗ)
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.layer = LayerMask.NameToLayer("UI");
        sliderObj.transform.SetParent(rowObj.transform, false);
        RectTransform sRt = sliderObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0f, 0f);
        sRt.anchoredPosition = new Vector2(54f, 6f);
        sRt.sizeDelta = new Vector2(-60f, 32f);

        Slider slider = sliderObj.GetComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        // 5a. Rãnh trượt (Track Background)
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.layer = LayerMask.NameToLayer("UI");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.30f);
        bgRt.anchorMax = new Vector2(1f, 0.70f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.12f, 0.18f, 1f);
        bgImg.raycastTarget = true; // Bấm trực tiếp lên thanh rãnh trượt để nhảy giá trị

        Outline bgOl = bgObj.AddComponent<Outline>();
        bgOl.effectColor = new Color(0.15f, 0.3f, 0.45f, 0.8f);
        bgOl.effectDistance = new Vector2(1f, -1f);

        // 5b. Thanh mức âm lượng (Fill Bar)
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.layer = LayerMask.NameToLayer("UI");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.30f);
        faRt.anchorMax = new Vector2(1f, 0.70f);
        faRt.offsetMin = new Vector2(5f, 0f);
        faRt.offsetMax = new Vector2(-5f, 0f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.layer = LayerMask.NameToLayer("UI");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.sizeDelta = Vector2.zero;

        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = themeColor;
        fillImg.raycastTarget = false;
        slider.fillRect = fillRt;

        // 5c. Cục kéo (Handle Knob) - Cầm kéo thả mượt mà, thả ra đứng yên
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.layer = LayerMask.NameToLayer("UI");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = new Vector2(0f, 0f);
        haRt.anchorMax = new Vector2(1f, 1f);
        haRt.offsetMin = new Vector2(14f, 0f);
        haRt.offsetMax = new Vector2(-14f, 0f);

        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObj.layer = LayerMask.NameToLayer("UI");
        handleObj.transform.SetParent(handleArea.transform, false);
        RectTransform handleRt = handleObj.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(28f, 30f);

        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.color = Color.white;
        handleImg.raycastTarget = true; // Nhận bắt sự kiện chuột và chạm màn hình

        Outline hOl = handleObj.AddComponent<Outline>();
        hOl.effectColor = themeColor;
        hOl.effectDistance = new Vector2(1.5f, -1.5f);

        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;

        // Đăng ký nhận sự kiện kéo thả giá trị
        slider.onValueChanged.AddListener(onSliderChange);

        return slider;
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
