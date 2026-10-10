using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DayCyclePhase
{
    Day,        // ☀️ Ban Ngày
    Afternoon,  // 🌅 Buổi Chiều (Hoàng Hôn - Hiệu ứng Thunder rực rỡ)
    Night       // 🌙 Ban Đêm (Phòng Thủ)
}

/// <summary>
/// DayNightSystem - Hệ Thống Chu Kỳ Ngày & Đêm & Buổi Chiều (3 Pha Hoàn Chỉnh):
/// 1. Ánh sáng: Sử dụng 1 Light Directional ĐỘC LẬP [DayNight_DirectionalLight].
/// 2. Ban ngày: Trời sáng trong xanh, quái ít, robot đi khai thác tài nguyên.
/// 3. Buổi chiều (Afternoon): Ánh sáng hoàng hôn vàng cam 5000K chiếu xiên góc thấp 8.5 độ giống Thunder,
///    bóng đổ dài nghệ thuật lung linh khắp hòn đảo.
/// 4. Ban đêm: Trời tối tím thẫm, quái vật dồn dập tấn công Nhà chính, robot rút về nhà trú ẩn.
/// </summary>
public class DayNightSystem : MonoBehaviour
{
    public static DayNightSystem Instance { get; private set; }

    [Header("=== TRẠNG THÁI CHU KỲ (DAY / AFTERNOON / NIGHT) ===")]
    public DayCyclePhase currentPhase = DayCyclePhase.Day;
    public bool isNight => currentPhase == DayCyclePhase.Night;
    public bool isAfternoon => currentPhase == DayCyclePhase.Afternoon;
    public bool isDay => currentPhase == DayCyclePhase.Day;
    public int currentDay = 1;

    [Tooltip("Thời lượng ban ngày (giây)")]
    public float dayDuration = 55.0f;

    [Tooltip("Thời lượng buổi chiều hoàng hôn (giây)")]
    public float afternoonDuration = 35.0f;

    [Tooltip("Thời lượng ban đêm phòng thủ (giây)")]
    public float nightDuration = 60.0f;

    [Tooltip("Thời gian còn lại của pha hiện tại")]
    public float timeRemaining = 55.0f;

    [Header("=== TẦN SUẤT QUÁI VẬT SPAWN (INTERVAL) ===")]
    [Tooltip("Tần suất sinh quái ban ngày (rất ít quái để người chơi nhặt đồ)")]
    public float dayEnemyInterval = 18.0f;

    [Tooltip("Tần suất sinh quái ban đêm (quái dồn dập tấn công gấp 5 lần: 0.7s/con)")]
    public float nightEnemyInterval = 0.7f;

    [Header("=== ÁNH SÁNG NGÀY / CHIỀU / ĐÊM (DEDICATED LIGHT) ===")]
    [Tooltip("Nguồn sáng Directional Light riêng biệt cho Ngày & Đêm & Buổi Chiều")]
    public Light dayNightLight;

    // A. Cấu hình Ban Ngày
    public Color dayLightColor = new Color(1.0f, 0.96f, 0.88f);
    public float dayIntensity = 1.15f;
    public Vector3 dayRotationEuler = new Vector3(50f, -30f, 0f);
    public Color dayAmbientColor = new Color(0.55f, 0.58f, 0.65f);

    // B. Cấu hình Buổi Chiều (Chuẩn ánh sáng Thunder: Vàng cam ấm áp 5000K, góc chiếu xiên thấp ~8.5 độ, bóng đổ dài)
    public Color afternoonLightColor = new Color(1.0f, 0.82f, 0.55f);
    public float afternoonIntensity = 1.95f;
    public Vector3 afternoonRotationEuler = new Vector3(8.5f, -30f, 0f);
    public Color afternoonAmbientColor = new Color(0.68f, 0.48f, 0.38f);

    // C. Cấu hình Ban Đêm
    public Color nightLightColor = new Color(0.25f, 0.38f, 0.68f);
    public float nightIntensity = 0.28f;
    public Vector3 nightRotationEuler = new Vector3(40f, 145f, 0f);
    public Color nightAmbientColor = new Color(0.12f, 0.15f, 0.24f);

    [Header("=== ẨN / HIỆN GIAO DIỆN (HIERARCHY & INSPECTOR) ===")]
    [Tooltip("Tích chọn để HIỆN, bỏ tích để ẨN Panel Ngày Đêm trên màn hình.")]
    public bool showDayNightHUD = true;

    [ContextMenu("👁️ Hiện Panel Ngày Đêm")]
    public void ShowHUD()
    {
        showDayNightHUD = true;
        if (dayNightHUDPanel != null) dayNightHUDPanel.SetActive(true);
    }

    [ContextMenu("🙈 Ẩn Panel Ngày Đêm")]
    public void HideHUD()
    {
        showDayNightHUD = false;
        if (dayNightHUDPanel != null) dayNightHUDPanel.SetActive(false);
    }

    private void OnValidate()
    {
        if (dayNightHUDPanel != null)
        {
            dayNightHUDPanel.SetActive(showDayNightHUD);
        }
    }

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
        currentPhase = DayCyclePhase.Day;
        timeRemaining = dayDuration;
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
        // 1. Đồng bộ trạng thái ẩn / hiện của Panel với showDayNightHUD
        if (dayNightHUDPanel != null && dayNightHUDPanel.activeSelf != showDayNightHUD)
        {
            dayNightHUDPanel.SetActive(showDayNightHUD);
        }

        // 2. Đếm ngược thời gian chu kỳ (Ngày -> Chiều -> Đêm)
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f)
        {
            switch (currentPhase)
            {
                case DayCyclePhase.Day:
                    TransitionToAfternoon();
                    break;
                case DayCyclePhase.Afternoon:
                    TransitionToNight();
                    break;
                case DayCyclePhase.Night:
                    TransitionToDay();
                    break;
            }
        }

        // 3. Chuyển đổi ánh sáng mượt mà (Smooth Lerp)
        UpdateLightVisuals(false);

        // 4. Cập nhật UI hiển thị
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
    /// Cập nhật độ sáng, màu sắc và góc chiếu của mặt trời / hoàng hôn / mặt trăng
    /// </summary>
    private void UpdateLightVisuals(bool instant)
    {
        if (dayNightLight == null) return;

        Color targetColor;
        float targetIntensity;
        Quaternion targetRot;
        Color targetAmbient;

        switch (currentPhase)
        {
            case DayCyclePhase.Afternoon:
                targetColor = afternoonLightColor;
                targetIntensity = afternoonIntensity;
                targetRot = Quaternion.Euler(afternoonRotationEuler);
                targetAmbient = afternoonAmbientColor;
                break;
            case DayCyclePhase.Night:
                targetColor = nightLightColor;
                targetIntensity = nightIntensity;
                targetRot = Quaternion.Euler(nightRotationEuler);
                targetAmbient = nightAmbientColor;
                break;
            default:
                targetColor = dayLightColor;
                targetIntensity = dayIntensity;
                targetRot = Quaternion.Euler(dayRotationEuler);
                targetAmbient = dayAmbientColor;
                break;
        }

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
    /// Chuyển sang buổi chiều (Ánh sáng hoàng hôn rực rỡ giống Thunder)
    /// </summary>
    public void TransitionToAfternoon()
    {
        currentPhase = DayCyclePhase.Afternoon;
        timeRemaining = afternoonDuration;

        Debug.Log($"<color=#FFA500>[DayNightSystem]</color> 🌅 BUỔI CHIỀU HOÀNG HÔN ĐÃ ĐẾN (NGÀY {currentDay})! Ánh nắng chiều tà rực rỡ chiếu xiên thấp.");

        Transform pTrans = PlayerController.Instance != null ? PlayerController.Instance.transform : (BaseHQ.Instance != null ? BaseHQ.Instance.transform : null);
        Vector3 bannerPos = pTrans != null ? (pTrans.position + Vector3.up * 3.5f) : Vector3.up * 4f;
        FloatingDamageText.SpawnResource(bannerPos, $"🌅 BUỔI CHIỀU HOÀNG HÔN (NGÀY {currentDay})!\n⚠️ ÁNH CHIỀU TÀ RỰC RỠ, CHUẨN BỊ PHÒNG THỦ!", new Color(1f, 0.75f, 0.25f, 1f), 0.12f);
    }

    /// <summary>
    /// Chuyển sang ban ngày
    /// </summary>
    public void TransitionToDay()
    {
        currentPhase = DayCyclePhase.Day;
        currentDay++;
        timeRemaining = dayDuration;

        Debug.Log($"<color=#FFD700>[DayNightSystem]</color> ☀️ BÌNH MINH ĐÃ ĐẾN (NGÀY {currentDay})! Ánh mặt trời thiêu đốt quái vật, Robot tiếp tục ra đồng farm.");
        
        // Tiêu diệt toàn bộ quái vật đêm còn sót lại (chúng bốc cháy và rớt vàng rải rác)
        Enemy[] remainingEnemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        foreach (var enemy in remainingEnemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                enemy.Die();
            }
        }

        // Hiện chữ thông báo lớn trên đầu Player / Căn cứ
        Transform pTrans = PlayerController.Instance != null ? PlayerController.Instance.transform : (BaseHQ.Instance != null ? BaseHQ.Instance.transform : null);
        Vector3 bannerPos = pTrans != null ? (pTrans.position + Vector3.up * 3.5f) : Vector3.up * 4f;
        FloatingDamageText.SpawnResource(bannerPos, $"☀️ BÌNH MINH NGÀY {currentDay}!\n🏆 ĐÃ SỐNG SÓT! HÃY NHẶT VÀNG & SỬA NHÀ CHÍNH!", new Color(1f, 0.9f, 0.2f, 1f), 0.12f);

        // Thông báo các robot farm tiếp tục tìm mỏ đá
        mineBotFarm[] bots = Object.FindObjectsByType<mineBotFarm>(FindObjectsInactive.Exclude);
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
        currentPhase = DayCyclePhase.Night;
        timeRemaining = nightDuration;

        Debug.Log($"<color=#00FFFF>[DayNightSystem]</color> 🌙 MÀN ĐÊM BUÔNG XUỐNG! Chế độ Phòng Thủ kích hoạt! Quái vật tấn công Nhà Chính!");
        
        Transform pTrans = PlayerController.Instance != null ? PlayerController.Instance.transform : (BaseHQ.Instance != null ? BaseHQ.Instance.transform : null);
        Vector3 bannerPos = pTrans != null ? (pTrans.position + Vector3.up * 3.5f) : Vector3.up * 4f;
        FloatingDamageText.SpawnResource(bannerPos, "🌙 MÀN ĐÊM BUÔNG XUỐNG!\n⚠️ QUÁI VẬT TẤN CÔNG NHÀ CHÍNH!", new Color(1f, 0.25f, 0.25f, 1f), 0.12f);
    }

    /// <summary>
    /// Người chơi nhấn nút [🛡️ PHÒNG THỦ] trên UI: Chủ động vào đêm để phòng thủ ngay
    /// </summary>
    public void OnDefenseButtonClicked()
    {
        if (currentPhase != DayCyclePhase.Night)
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

    private void CreateDayNightHUD()
    {
        Canvas canvas = GetMainScreenCanvas();
        if (canvas == null) return;

        // Ưu tiên tìm Panel_DayNightHUD đã có sẵn trong Hierarchy dưới canvas
        if (dayNightHUDPanel == null)
        {
            Transform found = canvas.transform.Find("Panel_DayNightHUD");
            if (found != null)
            {
                dayNightHUDPanel = found.gameObject;
            }
        }

        if (dayNightHUDPanel != null)
        {
            if (dayNightHUDPanel.transform.parent != canvas.transform)
            {
                dayNightHUDPanel.transform.SetParent(canvas.transform, false);
            }

            if (statusTitleText == null) statusTitleText = dayNightHUDPanel.transform.Find("StatusTitle")?.GetComponent<Text>();
            if (timerCountdownText == null) timerCountdownText = dayNightHUDPanel.transform.Find("CountdownTimer")?.GetComponent<Text>();
            Transform btnTr = dayNightHUDPanel.transform.Find("Btn_DefenseAction");
            if (btnTr != null)
            {
                if (defenseActionButton == null) defenseActionButton = btnTr.GetComponent<Button>();
                if (actionButtonText == null) actionButtonText = btnTr.Find("BtnText")?.GetComponent<Text>();
            }

            if (defenseActionButton != null)
            {
                defenseActionButton.onClick.RemoveListener(OnDefenseButtonClicked);
                defenseActionButton.onClick.AddListener(OnDefenseButtonClicked);
            }

            dayNightHUDPanel.SetActive(showDayNightHUD);
            return;
        }

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

        dayNightHUDPanel.SetActive(showDayNightHUD);
    }

    private void RefreshHUDDisplay()
    {
        if (dayNightHUDPanel != null && !dayNightHUDPanel.activeInHierarchy)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(0f, timeRemaining) % 60f);

        if (currentPhase == DayCyclePhase.Night)
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
        else if (currentPhase == DayCyclePhase.Afternoon)
        {
            if (statusTitleText != null)
            {
                statusTitleText.text = $"🌅 <b>BUỔI CHIỀU</b>";
                statusTitleText.color = new Color(1f, 0.72f, 0.15f);
            }
            if (timerCountdownText != null)
            {
                timerCountdownText.text = $"⏱️ {minutes:D2}:{seconds:D2}";
                timerCountdownText.color = new Color(1f, 0.82f, 0.35f);
            }
            if (actionButtonText != null)
            {
                actionButtonText.text = "🛡️ <b>PHÒNG THỦ</b>";
            }
            if (defenseActionButton != null)
            {
                defenseActionButton.GetComponent<Image>().color = new Color(0.95f, 0.48f, 0.1f);
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
