using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DayNightSystem - Hệ Thống Chu Kỳ Ngày & Đêm Tách Biệt Hoàn Toàn:
/// 1. Ánh sáng: Sử dụng 1 Light Directional MỚI ĐỘC LẬP [DayNight_DirectionalLight], 
///    tuyệt đối KHÔNG dùng chung với Light của Thunder hay bất kỳ vũ khí/hiệu ứng nào khác.
/// 2. Ban ngày: Quái vật xuất hiện rất ít (chu kỳ spawn chậm) để người chơi tự do thu thập vật phẩm,
///    robot đào mỏ (mineBotFarm) tự động đi đào tài nguyên bình thường.
/// 3. Ban đêm: Kích hoạt chế độ Phòng Thủ, quái vật spawn dồn dập tấn công căn cứ.
///    Người chơi có thể nhấn nút [🛡️ PHÒNG THỦ] trên UI để chủ động vào đêm ngay lập tức.
/// 4. Các robot farm (mineBotFarm) sẽ không tự ý đi farm khi trời tối mà sẽ rút về căn cứ ở nhà.
/// </summary>
public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    [Header("=== TRẠNG THÁI CHU KỲ (DAY / NIGHT) ===")]
    public bool isNight = false;
    public int currentDay = 1;

    [Tooltip("Thời lượng ban ngày (giây) - đủ dài để nhặt đồ và farm thoải mái")]
    public float dayDuration = 80.0f;

    [Tooltip("Thời lượng ban đêm (giây) - phòng thủ chống quái")]
    public float nightDuration = 60.0f;

    [Tooltip("Thời gian còn lại của chu kỳ hiện tại")]
    public float timeRemaining = 80.0f;

    [Header("=== TẦN SUẤT QUÁI VẬT SPAWN (INTERVAL) ===")]
    [Tooltip("Tần suất sinh quái ban ngày (rất ít quái để người chơi nhặt đồ)")]
    public float dayEnemyInterval = 18.0f;

    [Tooltip("Tần suất sinh quái ban đêm (quái dồn dập tấn công gấp 5 lần: 0.7s/con)")]
    public float nightEnemyInterval = 0.7f;

    [Header("=== ÁNH SÁNG NGÀY / ĐÊM (DEDICATED LIGHT) ===")]
    [Tooltip("Nguồn sáng Directional Light riêng biệt cho Ngày & Đêm (TÁCH BIỆT KHỎI THUNDER)")]
    public Light dayNightLight;

    // Cấu hình màu sắc & độ sáng
    public Color dayLightColor = new Color(1.0f, 0.96f, 0.88f);
    public float dayIntensity = 1.15f;
    public Vector3 dayRotationEuler = new Vector3(50f, -30f, 0f);
    public Color dayAmbientColor = new Color(0.55f, 0.58f, 0.65f);

    public Color nightLightColor = new Color(0.25f, 0.38f, 0.68f);
    public float nightIntensity = 0.28f;
    public Vector3 nightRotationEuler = new Vector3(40f, 145f, 0f);
    public Color nightAmbientColor = new Color(0.12f, 0.15f, 0.24f);

    [Header("=== GIAO DIỆN UI (HUD WIDGET) ===")]
    public GameObject dayNightHUDPanel;
    public Text statusTitleText;
    public Text timerCountdownText;
    public Button defenseActionButton;
    public Text actionButtonText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null && FindAnyObjectByType<DayNightSystem>() == null)
        {
            GameObject go = new GameObject("[Day_Night_System]");
            go.AddComponent<DayNightSystem>();
            Debug.Log("<color=#00FF88>[DayNightSystem]</color> Đã tự động kích hoạt Hệ Thống Ngày & Đêm!");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureDedicatedLightExists();
        timeRemaining = dayDuration;
        isNight = false;
        currentDay = 1;
    }

    private void Start()
    {
        EnsureDedicatedLightExists();
        CreateDayNightHUD();
        UpdateLightVisuals(true);
        RefreshHUDDisplay();
    }

    private void Update()
    {
        // 1. Đếm ngược thời gian chu kỳ
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            if (isNight)
            {
                TransitionToDay();
            }
            else
            {
                TransitionToNight();
            }
        }

        // 2. Chuyển đổi ánh sáng mượt mà (Smooth Lerp)
        UpdateLightVisuals(false);

        // 3. Cập nhật UI hiển thị
        RefreshHUDDisplay();
    }

    /// <summary>
    /// Đảm bảo luôn có 1 nguồn sáng Directional Light riêng biệt cho hệ thống Ngày/Đêm,
    /// hoàn toàn tách rời khỏi mọi hiệu ứng tia sét Thunder hay vụ nổ vũ khí.
    /// </summary>
    private void EnsureDedicatedLightExists()
    {
        if (dayNightLight != null) return;

        GameObject lightObj = GameObject.Find("[DayNight_DirectionalLight]");
        if (lightObj == null)
        {
            lightObj = new GameObject("[DayNight_DirectionalLight]");
            dayNightLight = lightObj.AddComponent<Light>();
            dayNightLight.type = LightType.Directional;
            dayNightLight.shadows = LightShadows.Soft;
            dayNightLight.shadowStrength = 0.65f;
            Debug.Log("<color=#FFD700>[DayNightSystem]</color> Đã tạo Light Directional mới độc lập: [DayNight_DirectionalLight]");
        }
        else
        {
            dayNightLight = lightObj.GetComponent<Light>();
            if (dayNightLight == null) dayNightLight = lightObj.AddComponent<Light>();
        }

        // Khởi tạo ánh sáng ban ngày ban đầu
        dayNightLight.color = dayLightColor;
        dayNightLight.intensity = dayIntensity;
        dayNightLight.transform.rotation = Quaternion.Euler(dayRotationEuler);
        RenderSettings.ambientLight = dayAmbientColor;
    }

    /// <summary>
    /// Cập nhật độ sáng, màu sắc và góc chiếu của mặt trời / mặt trăng
    /// </summary>
    private void UpdateLightVisuals(bool instant)
    {
        if (dayNightLight == null) return;

        Color targetColor = isNight ? nightLightColor : dayLightColor;
        float targetIntensity = isNight ? nightIntensity : dayIntensity;
        Quaternion targetRot = Quaternion.Euler(isNight ? nightRotationEuler : dayRotationEuler);
        Color targetAmbient = isNight ? nightAmbientColor : dayAmbientColor;

        if (instant)
        {
            dayNightLight.color = targetColor;
            dayNightLight.intensity = targetIntensity;
            dayNightLight.transform.rotation = targetRot;
            RenderSettings.ambientLight = targetAmbient;
        }
        else
        {
            float t = Time.deltaTime * 0.8f;
            dayNightLight.color = Color.Lerp(dayNightLight.color, targetColor, t);
            dayNightLight.intensity = Mathf.MoveTowards(dayNightLight.intensity, targetIntensity, Time.deltaTime * 0.35f);
            dayNightLight.transform.rotation = Quaternion.Slerp(dayNightLight.transform.rotation, targetRot, t);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, t);
        }
    }

    /// <summary>
    /// Chuyển sang ban ngày
    /// </summary>
    public void TransitionToDay()
    {
        isNight = false;
        currentDay++;
        timeRemaining = dayDuration;

        Debug.Log($"<color=#FFD700>[DayNightSystem]</color> ☀️ BÌNH MINH ĐÃ ĐẾN (NGÀY {currentDay})! Quái vật rút lui, Robot tiếp tục ra đồng farm.");
        ShowWorldBanner($"☀️ BÌNH MINH (NGÀY {currentDay})", "Quái vật giảm dần - Thu thập tài nguyên!");

        // Thông báo các robot farm tiếp tục tìm mỏ đá
        mineBotFarm[] bots = FindObjectsByType<mineBotFarm>(FindObjectsInactive.Exclude);
        foreach (var bot in bots)
        {
            if (bot != null) bot.FindBestMiningTarget();
        }
    }

    /// <summary>
    /// Chuyển sang ban đêm (Phòng thủ)
    /// </summary>
    public void TransitionToNight()
    {
        isNight = true;
        timeRemaining = nightDuration;

        Debug.Log($"<color=#00FFFF>[DayNightSystem]</color> 🌙 MÀN ĐÊM BUÔNG XUỐNG! Chế độ Phòng Thủ kích hoạt! Toàn bộ Robot rút về căn cứ.");
        ShowWorldBanner($"🌙 MÀN ĐÊM BUÔNG XUỐNG!", "Chế độ PHÒNG THỦ - Quái vật tấn công dồn dập!");
    }

    /// <summary>
    /// Người chơi nhấn nút [🛡️ PHÒNG THỦ] trên UI: Chủ động vào đêm để phòng thủ ngay
    /// </summary>
    public void OnDefenseButtonClicked()
    {
        if (!isNight)
        {
            // Vào phòng thủ ngay lập tức
            TransitionToNight();
        }
        else
        {
            // Nếu đang trong đêm và muốn chuyển sang ngày sớm
            TransitionToDay();
        }
        RefreshHUDDisplay();
    }

    private void ShowWorldBanner(string title, string sub)
    {
        FloatingDamageText.Spawn(Vector3.up * 3f, 0, isNight ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.9f, 0.2f));
    }

    // ================= TỰ ĐỘNG KHỞI TẠO GIAO DIỆN UI (HUD WIDGET) =================

    private void CreateDayNightHUD()
    {
        if (dayNightHUDPanel != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Panel góc trên bên phải (cạnh Top Resource HUD)
        dayNightHUDPanel = new GameObject("Panel_DayNightHUD", typeof(RectTransform));
        dayNightHUDPanel.layer = LayerMask.NameToLayer("UI");
        dayNightHUDPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = dayNightHUDPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-18f, -38f);
        rt.sizeDelta = new Vector2(250f, 68f);

        Image bg = dayNightHUDPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f);

        Outline outline = dayNightHUDPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.75f, 1f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Text Trạng Thái (Ngày / Đêm)
        GameObject titleObj = new GameObject("StatusTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(dayNightHUDPanel.transform, false);
        RectTransform tRt = titleObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 0.5f);
        tRt.anchorMax = new Vector2(0.58f, 1f);
        tRt.anchoredPosition = new Vector2(8f, -2f);
        tRt.sizeDelta = Vector2.zero;

        statusTitleText = titleObj.GetComponent<Text>();
        statusTitleText.font = safeFont;
        statusTitleText.fontSize = 12;
        statusTitleText.fontStyle = FontStyle.Bold;
        statusTitleText.alignment = TextAnchor.MiddleLeft;
        statusTitleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        statusTitleText.supportRichText = true;

        // Text Đồng Hồ Đếm Ngược
        GameObject timerObj = new GameObject("CountdownTimer", typeof(RectTransform), typeof(Text));
        timerObj.transform.SetParent(dayNightHUDPanel.transform, false);
        RectTransform tmRt = timerObj.GetComponent<RectTransform>();
        tmRt.anchorMin = new Vector2(0f, 0f);
        tmRt.anchorMax = new Vector2(0.58f, 0.5f);
        tmRt.anchoredPosition = new Vector2(8f, 2f);
        tmRt.sizeDelta = Vector2.zero;

        timerCountdownText = timerObj.GetComponent<Text>();
        timerCountdownText.font = safeFont;
        timerCountdownText.fontSize = 14;
        timerCountdownText.fontStyle = FontStyle.Bold;
        timerCountdownText.alignment = TextAnchor.MiddleLeft;
        timerCountdownText.color = Color.white;

        // Nút Thao Tác [🛡️ PHÒNG THỦ]
        GameObject btnObj = new GameObject("Btn_DefenseAction", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(dayNightHUDPanel.transform, false);
        RectTransform bRt = btnObj.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0.58f, 0.1f);
        bRt.anchorMax = new Vector2(0.96f, 0.9f);
        bRt.sizeDelta = Vector2.zero;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.35f, 1f);

        defenseActionButton = btnObj.GetComponent<Button>();
        defenseActionButton.onClick.AddListener(OnDefenseButtonClicked);

        GameObject btnTxtObj = new GameObject("BtnText", typeof(RectTransform), typeof(Text));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRt = btnTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.sizeDelta = Vector2.zero;

        actionButtonText = btnTxtObj.GetComponent<Text>();
        actionButtonText.font = safeFont;
        actionButtonText.fontSize = 11;
        actionButtonText.fontStyle = FontStyle.Bold;
        actionButtonText.alignment = TextAnchor.MiddleCenter;
        actionButtonText.color = Color.white;
        actionButtonText.supportRichText = true;
        actionButtonText.raycastTarget = false;
    }

    private void RefreshHUDDisplay()
    {
        int minutes = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) % 60f);

        if (isNight)
        {
            if (statusTitleText != null)
            {
                statusTitleText.text = $"🌙 <b>BAN ĐÊM</b>";
                statusTitleText.color = new Color(0.4f, 0.85f, 1f);
            }
            if (timerCountdownText != null)
            {
                timerCountdownText.text = $"⏱️ {minutes:D2}:{seconds:D2}";
                timerCountdownText.color = new Color(1f, 0.45f, 0.45f);
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "☀️ <b>SANG NGÀY</b>";
            }
            if (defenseActionButton != null)
            {
                defenseActionButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.85f);
            }
        }
        else
        {
            if (statusTitleText != null)
            {
                statusTitleText.text = $"☀️ <b>NGÀY {currentDay}</b>";
                statusTitleText.color = new Color(1f, 0.88f, 0.2f);
            }
            if (timerCountdownText != null)
            {
                timerCountdownText.text = $"⏱️ {minutes:D2}:{seconds:D2}";
                timerCountdownText.color = new Color(0.85f, 1f, 0.85f);
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "🛡️ <b>PHÒNG THỦ</b>";
            }
            if (defenseActionButton != null)
            {
                defenseActionButton.GetComponent<Image>().color = new Color(0.85f, 0.45f, 0.12f);
            }
        }
    }
}
