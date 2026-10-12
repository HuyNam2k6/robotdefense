using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// BaseHQ - Nhà Chính / Lò Phản Ứng Năng Lượng Trung Tâm Căn Cứ:
/// - Mục tiêu chiến lược số 1 cần bảo vệ: Quái vật ban đêm sẽ kéo đến tấn công phá hủy Nhà Chính.
/// - Có thanh máu nổi World-Space lớn trên nóc hiển thị rõ ràng cho người chơi.
/// - Khi người chơi click/chạm vào Nhà Chính:
///   + Toàn bộ thân Nhà Chính nhấp nháy phát sáng (Flashing Pulse Highlight) cực kỳ nổi bật!
///   + Hiển thị 2 NÚT TO RÕ RÀNG:
///     1. NÚT [ℹ️ THÔNG TIN]: Bấm vào hiển thị popup thông tin chi tiết (Cấp độ, máu, giới hạn công nghệ).
///     2. NÚT [⭐ NÂNG CẤP]: Bấm vào nâng cấp Lò Năng Lượng lên cấp tiếp theo.
///   + Kèm nút sửa chữa (+350 HP) và nút đóng [✕].
/// - Khi các vũ khí nhấn nâng cấp mà cấp nhà chính chưa đạt yêu cầu -> CenterWarningBanner thông báo to giữa màn hình!
/// </summary>
public class BaseHQ : MonoBehaviour
{
    public static BaseHQ Instance { get; private set; }

    [Header("=== THÔNG SỐ NHÀ CHÍNH ===")]
    public string baseName = "🏛️ LÒ NĂNG LƯỢNG TRUNG TÂM [HQ CORE]";
    public float maxHealth = 2000f;
    public float currentHealth = 2000f;
    public int level = 1;
    public const int MaxLevel = 4;
    public bool isDestroyed = false;

    public static readonly float[] HealthPerLevel = new float[] { 2000f, 3500f, 5500f, 8000f };
    public static readonly int[] UpgradeCoinsCost = new int[] { 100, 250, 500 };
    public static readonly int[] UpgradeWoodCost = new int[] { 0, 0, 0 };
    public static readonly int[] UpgradeStoneCost = new int[] { 50, 120, 250 };

    public static int CurrentMapLevel => Instance != null ? Instance.level : 1;

    /// <summary>
    /// Giới hạn cấp độ tối đa mà các công trình khác được phép nâng lên dựa theo Lò Năng Lượng (BaseHQ).
    /// Quy tắc:
    /// - Khi Lõi Năng Lượng ở Cấp 1 -> Công trình tối đa Cấp 1
    /// - Khi Lõi Năng Lượng ở Cấp 2 -> Công trình tối đa Cấp 2
    /// - Khi Lõi Năng Lượng ở Cấp 3 -> Công trình tối đa Cấp 3
    /// - Khi Lõi Năng Lượng ở Cấp 4 (Cấp MAX của Lõi) -> Mở khóa toàn bộ công trình khác (Trụ, Tường) được nâng lên tối đa CẤP 6!
    /// </summary>
    public static int MaxAllowedBuildingLevel => CurrentMapLevel >= 4 ? 6 : CurrentMapLevel;

    public static bool CanUpgradeBuildingToLevel(int targetLevel, out string reason)
    {
        int mapLvl = CurrentMapLevel;
        int maxAllowed = MaxAllowedBuildingLevel;
        if (targetLevel > maxAllowed)
        {
            if (mapLvl < 4)
            {
                reason = $"⚠️ Lò năng lượng đang ở Cấp {mapLvl}! Vui lòng nâng cấp Lò Năng Lượng lên Cấp 4 để mở khóa công trình Cấp {targetLevel}!";
            }
            else
            {
                reason = $"⚠️ Đã đạt cấp độ tối đa của công trình (Cấp {maxAllowed})!";
            }
            return false;
        }
        reason = "";
        return true;
    }

    [Header("=== CHI PHÍ SỬA CHỮA CĂN CỨ ===")]
    public int repairCostCoins = 15;
    public int repairCostStone = 10;
    public int repairCostWood = 0;
    public float repairHealthAmount = 350f;

    [Header("=== CỐ ĐỊNH VỊ TRÍ BAN ĐẦU ===")]
    public bool lockToInitialPosition = true;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isPositionLocked = false;

    [Header("=== THANH MÁU NỔI (WORLD-SPACE HEALTHBAR) ===")]
    public float healthBarHeight = 7.5f;
    private GameObject healthBarObj;
    private RectTransform healthBarFill;
    private Image healthBarFillImage;
    private Text healthBarText;
    private Camera mainCamera;

    [Header("=== GIAO DIỆN TƯƠNG TÁC SỬA CHỮA (WORLD PROMPT CŨ) ===")]
    public float interactionDistance = 4.2f;

    [Header("=== CHI TIẾT CƠ KHÍ & LÕI NĂNG LƯỢNG ===")]
    public Transform energyCoreTransform;
    public Transform[] rotatingGears;
    public Transform[] counterRotatingGears;
    public float gearRotationSpeed = 30f;
    public float pulseSpeed = 2.5f;
    private Vector3 coreBaseScale = Vector3.one * 1.35f;

    public Light hqLight;
    private float lastDamageWarningTime = 0f;

    // --- HIỆU ỨNG NHẤP NHÁY TOÀN BỘ MÔ HÌNH KHI ĐƯỢC CHỌN (YÊU CẦU 4) ---
    private bool isHQSelected = false;
    private Renderer[] hqRenderers;
    private MaterialPropertyBlock hqPropBlock;

    // --- GIAO DIỆN 2 NÚT THÔNG TIN & NÂNG CẤP (SCREEN-SPACE UI) ---
    private GameObject hqActionPanel;
    private Text hqPanelTitleText;
    private Text hqPanelSubtitleText;
    private Button hqInfoButton;
    private Button hqUpgradeButton;
    private Text hqUpgradeButtonText;
    private Button hqRepairButton;
    private Text hqRepairButtonText;
    private Button hqCloseButton;

    // Bảng Modal Thông Tin Chi Tiết
    private GameObject hqInfoModalObj;
    private Text hqInfoModalDetailsText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBaseHQExistsInScene()
    {
        if (Instance != null || Object.FindAnyObjectByType<BaseHQ>() != null) return;

        GameObject prefab = Resources.Load<GameObject>("BaseHQ_Building");
#if UNITY_EDITOR
        if (prefab == null)
        {
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/BaseHQ_Building.prefab");
        }
#endif
        if (prefab == null) return;

        Vector3 defaultPos = new Vector3(0f, 9.6f, -31.5f);
        if (Terrain.activeTerrain != null)
        {
            float tY = Terrain.activeTerrain.SampleHeight(defaultPos) + Terrain.activeTerrain.transform.position.y;
            defaultPos.y = tY;
        }

        GameObject hqObj = Instantiate(prefab, defaultPos, Quaternion.Euler(0f, 180f, 0f));
        if (hqObj != null)
        {
            hqObj.name = "BaseHQ_Building";
            Debug.Log("<color=#00FF88>🏛️ [BaseHQ]</color> Đã nạp Nhà Chính từ Prefab tại vị trí: " + defaultPos);
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

        initialPosition = transform.position;
        initialRotation = transform.rotation;
        isPositionLocked = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        currentHealth = maxHealth;
        isDestroyed = false;

        // Đảm bảo không có Rigidbody (tuân thủ quy tắc CPU)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Đảm bảo có Collider bao phủ để nhận click chuột / chạm ngón tay 3D
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2.5f, 0f);
            box.size = new Vector3(6.5f, 6.0f, 6.5f);
        }

        CacheHQRenderers();
    }

    private void CacheHQRenderers()
    {
        hqRenderers = GetComponentsInChildren<Renderer>(true);
        hqPropBlock = new MaterialPropertyBlock();
    }

    private Material coreRuntimeMat;

    private void Start()
    {
        mainCamera = Camera.main;
        if (energyCoreTransform != null)
        {
            coreBaseScale = energyCoreTransform.localScale;
            SetupCoreGlowingMaterial();
        }
        CreateHealthBarUI();
        EnsureHQScreenUI();
        UpdateHealthBarVisual();
    }

    private void SetupCoreGlowingMaterial()
    {
        if (energyCoreTransform == null) return;
        Renderer rend = energyCoreTransform.GetComponent<Renderer>();
        if (rend == null) return;

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") 
                          ?? Shader.Find("Universal Render Pipeline/Lit") 
                          ?? Shader.Find("Unlit/Color");

        if (rend.sharedMaterial == null || rend.sharedMaterial.name.Contains("Factory") || rend.sharedMaterial.name.Contains("Default"))
        {
            coreRuntimeMat = new Material(unlitShader);
            coreRuntimeMat.name = "Runtime_Core_Glow";
            coreRuntimeMat.SetColor("_BaseColor", new Color(0.25f, 2.4f, 4.0f, 1f));
            rend.material = coreRuntimeMat;
        }
        else
        {
            coreRuntimeMat = rend.material;
        }

        if (coreRuntimeMat != null && coreRuntimeMat.HasProperty("_EmissionColor"))
        {
            coreRuntimeMat.EnableKeyword("_EMISSION");
            coreRuntimeMat.SetColor("_EmissionColor", new Color(0.25f, 2.4f, 4.0f, 1f));
        }

        if (hqLight != null)
        {
            hqLight.color = new Color(0.1f, 0.85f, 1f);
            hqLight.range = 16f;
            hqLight.intensity = 4.0f;
        }
    }

    private void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // 1. Chuyển động cơ khí: Xoay các bánh răng
        if (rotatingGears != null)
        {
            for (int i = 0; i < rotatingGears.Length; i++)
            {
                if (rotatingGears[i] != null)
                    rotatingGears[i].Rotate(Vector3.up, gearRotationSpeed * Time.deltaTime, Space.Self);
            }
        }
        if (counterRotatingGears != null)
        {
            for (int i = 0; i < counterRotatingGears.Length; i++)
            {
                if (counterRotatingGears[i] != null)
                    counterRotatingGears[i].Rotate(Vector3.up, -gearRotationSpeed * 0.85f * Time.deltaTime, Space.Self);
            }
        }

        // 2. Lõi Năng Lượng tự xoay và xung nhịp
        if (energyCoreTransform != null)
        {
            energyCoreTransform.Rotate(new Vector3(0.2f, 1f, 0.15f), 45f * Time.deltaTime, Space.Self);
            float pulse = 1f + 0.08f * Mathf.Sin(Time.time * pulseSpeed);
            energyCoreTransform.localScale = coreBaseScale * pulse;

            if (coreRuntimeMat != null)
            {
                float glowMultiplier = 2.4f + 1.2f * Mathf.Sin(Time.time * pulseSpeed);
                Color hdrGlow = new Color(0.15f, 0.85f, 1.0f) * glowMultiplier;
                if (coreRuntimeMat.HasProperty("_BaseColor")) coreRuntimeMat.SetColor("_BaseColor", hdrGlow);
                if (coreRuntimeMat.HasProperty("_EmissionColor")) coreRuntimeMat.SetColor("_EmissionColor", hdrGlow);
            }
        }

        // 3. Đèn Point Light nhấp nháy đồng bộ
        if (hqLight != null)
        {
            hqLight.intensity = 3.8f + 1.4f * Mathf.Sin(Time.time * pulseSpeed);
        }

        // 4. Billboard xoay thanh máu về Camera
        if (mainCamera != null && healthBarObj != null)
        {
            healthBarObj.transform.rotation = mainCamera.transform.rotation;
            healthBarObj.transform.position = transform.position + Vector3.up * healthBarHeight;
        }

        // 5. HIỆU ỨNG NHẤP NHÁY TOÀN BỘ MÔ HÌNH NHÀ CHÍNH KHI ĐƯỢC CHỌN (YÊU CẦU 4)
        UpdateHQFlashingHighlight();

        // 6. Nhận diện chạm tay trên màn hình cảm ứng di động (Mobile Touch)
        CheckMobileTouchRaycast();

        // 7. Phím tắt tiện lợi nếu đang mở menu
        if (isHQSelected)
        {
            if (Input.GetKeyDown(KeyCode.F)) UpgradeHQ();
            if (Input.GetKeyDown(KeyCode.E)) OnRepairButtonClicked();
            if (Input.GetKeyDown(KeyCode.Escape)) CloseHQInteractionMenu();
        }
    }

    private void LateUpdate()
    {
        if (lockToInitialPosition && isPositionLocked)
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }

    // ================= TƯƠNG TÁC CLICK & CHỌN NHÀ CHÍNH (YÊU CẦU 1 & 4) =================

    private void OnMouseDown()
    {
        // Khi WallSelectionManager đang hoạt động, toàn bộ quy trình nhấn giữ nhấp nháy, kiểm tra di chuyển và thả tay sẽ do WallSelectionManager điều phối thống nhất
        if (WallSelectionManager.Instance != null) return;

        if (WallSelectionManager.IsPointerOverUI()) return;

        BuildingSystem bs = FindAnyObjectByType<BuildingSystem>();
        if (bs != null && bs.IsPlacing) return;

        OpenHQInteractionMenu();
    }

    private void CheckMobileTouchRaycast()
    {
        // Nếu đã có WallSelectionManager điều phối chạm đa điểm và kéo thả -> nhường quyền xử lý
        if (WallSelectionManager.Instance != null) return;

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Ray ray = mainCamera.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit, 500f))
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            OpenHQInteractionMenu();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Mở Menu Tương Tác Nhà Chính: Hiển thị 2 nút to rõ ràng [THÔNG TIN] và [NÂNG CẤP]
    /// </summary>
    public void OpenHQInteractionMenu()
    {
        if (WallSelectionManager.Instance != null)
        {
            // Bỏ chọn các công trình khác nhưng DUY TRÌ NHẤP NHÁY LIÊN TỤC cho Nhà Chính
            WallSelectionManager.Instance.DeselectWall();
            WallSelectionManager.Instance.DeselectTurret();
            WallSelectionManager.Instance.DeselectFactory();
            WallSelectionManager.Instance.SetPersistentBlinkTarget(transform);
        }

        BuildingShopUI shop = FindAnyObjectByType<BuildingShopUI>();
        if (shop != null && shop.dashboardPanel != null && shop.dashboardPanel.activeSelf)
        {
            shop.CloseShopPanel();
        }

        isHQSelected = true;
        EnsureHQScreenUI();

        if (hqActionPanel != null)
        {
            hqActionPanel.SetActive(true);
            hqActionPanel.transform.SetAsLastSibling();
            UpdateHQPanelUI();
        }

        Debug.Log("<color=#00FFFF>🏛️ [BaseHQ]</color> Đã chọn Nhà Chính! Toàn bộ mô hình nhấp nháy liên tục cho đến khi nhấn vào vật khác.");
    }

    public void CloseHQInteractionMenu()
    {
        isHQSelected = false;

        if (WallSelectionManager.Instance != null && WallSelectionManager.Instance.currentBlinkTarget == transform)
        {
            WallSelectionManager.Instance.ClearPersistentBlinking();
        }

        if (hqActionPanel != null) hqActionPanel.SetActive(false);
        if (hqInfoModalObj != null) hqInfoModalObj.SetActive(false);

        ResetHQVisuals();
    }

    public void ResetHQVisuals()
    {
        if (hqRenderers != null)
        {
            for (int i = 0; i < hqRenderers.Length; i++)
            {
                if (hqRenderers[i] != null)
                {
                    hqRenderers[i].SetPropertyBlock(null);
                }
            }
        }
    }

    private void UpdateHQFlashingHighlight()
    {
        // Hiệu ứng nhấp nháy xanh dương -> màu cũ chỉ diễn ra khi người chơi nhấn giữ (do WallSelectionManager điều phối).
        // Khi người chơi thả tay ra -> trở về màu cũ nguyên bản, không nhấp nháy tiếp.
    }

    // ================= LOGIC NÂNG CẤP & SỬA CHỮA =================

    public int GetUpgradeCoins() => (level >= 1 && level < MaxLevel) ? UpgradeCoinsCost[level - 1] : 0;
    public int GetUpgradeWood() => (level >= 1 && level < MaxLevel) ? UpgradeWoodCost[level - 1] : 0;
    public int GetUpgradeStone() => (level >= 1 && level < MaxLevel) ? UpgradeStoneCost[level - 1] : 0;

    public bool UpgradeHQ()
    {
        if (level >= MaxLevel)
        {
            CenterWarningBanner.Show("👑 LÒ NĂNG LƯỢNG ĐÃ ĐẠT CẤP TỐI ĐA!", "Nhà Chính đã ở Cấp 4 (Cấp cao nhất). Không thể nâng thêm!", 2.5f);
            return false;
        }

        if (GameEconomy.Instance == null) return false;

        int reqCoins = GetUpgradeCoins();
        int reqStone = GetUpgradeStone();

        bool hasCoins = GameEconomy.Instance.coins >= reqCoins;
        bool hasStone = GameEconomy.Instance.stoneCount >= reqStone;

        if (!hasCoins || !hasStone)
        {
            string missing = $"Chưa đủ tài nguyên nâng Lò Năng Lượng Cấp {level + 1}!\nCần: ";
            if (!hasCoins) missing += $"<color=#FFD700>🪙 {reqCoins} Vàng</color>  ";
            if (!hasStone) missing += $"<color=#C0C0C0>🪨 {reqStone} Đá</color>  ";

            CenterWarningBanner.Show($"⚠️ CHƯA ĐỦ TÀI NGUYÊN NÂNG LÒ NĂNG LƯỢNG (CẤP {level + 1})!", missing, 3.2f);
            return false;
        }

        GameEconomy.Instance.SpendCoins(reqCoins);
        GameEconomy.Instance.SpendStone(reqStone);

        level++;
        maxHealth = HealthPerLevel[Mathf.Clamp(level - 1, 0, HealthPerLevel.Length - 1)];
        currentHealth = maxHealth;

        UpdateHealthBarVisual();
        UpdateHQPanelUI();

        if (level >= 4)
        {
            CenterWarningBanner.Show("🎉 ĐÃ NÂNG CẤP LÒ NĂNG LƯỢNG LÊN CẤP 4 (MAX)!", "🔓 Lõi năng lượng tối đa đã MỞ KHÓA TOÀN BỘ VŨ KHÍ & TƯỜNG LÊN CẤP 6 (TITAN MAX)!", 3.8f);
        }
        else
        {
            CenterWarningBanner.Show($"🎉 ĐÃ NÂNG CẤP LÒ NĂNG LƯỢNG LÊN CẤP {level}!", $"🔓 Giới hạn nâng cấp toàn bộ Vũ Khí, Bức Tường và Nhà Máy đã mở rộng lên Cấp {level}!", 3.5f);
        }
        Debug.Log($"<color=#00FF88><b>[BaseHQ]</b> Đã nâng cấp Nhà Chính lên Cấp {level} thành công!</color>");
        return true;
    }

    public void OnRepairButtonClicked()
    {
        if (currentHealth >= maxHealth || isDestroyed) return;
        if (GameEconomy.Instance == null) return;

        bool hasCoins = GameEconomy.Instance.coins >= repairCostCoins;
        bool hasStone = GameEconomy.Instance.stoneCount >= repairCostStone;

        if (!hasCoins || !hasStone)
        {
            string missing = "Thiếu tài nguyên sửa chữa Lò Năng Lượng!\nCần: ";
            if (!hasCoins) missing += $"<color=#FFD700>🪙 {repairCostCoins} Vàng</color>  ";
            if (!hasStone) missing += $"<color=#C0C0C0>🪨 {repairCostStone} Đá</color>  ";

            CenterWarningBanner.Show("⚠️ THIẾU TÀI NGUYÊN SỬA CHỮA!", missing, 2.5f);
            return;
        }

        GameEconomy.Instance.SpendCoins(repairCostCoins);
        GameEconomy.Instance.SpendStone(repairCostStone);

        currentHealth = Mathf.Min(maxHealth, currentHealth + repairHealthAmount);
        UpdateHealthBarVisual();
        UpdateHQPanelUI();

        FloatingDamageText.SpawnResource(transform.position + Vector3.up * 2.5f, $"+{repairHealthAmount:F0} HP 🔧 ĐÃ SỬA CHỮA LÒ NĂNG LƯỢNG!", new Color(0.2f, 1f, 0.3f, 1f), 0.1f);
    }

    public void OpenInfoModal()
    {
        EnsureHQScreenUI();
        if (hqInfoModalObj == null) return;

        hqInfoModalObj.SetActive(true);
        hqInfoModalObj.transform.SetAsLastSibling();

        if (hqInfoModalDetailsText != null)
        {
            string nextInfo = (level < MaxLevel)
                ? $"✦ <b>Cấp kế tiếp (Cấp {level + 1}):</b> Mở rộng giới hạn nâng cấp mọi vũ khí lên Cấp {level + 1}{(level + 1 == 4 ? " (Đồng thời mở khóa Cấp 6 cho Trụ & Tường!)" : "")}, tăng độ bền căn cứ lên <color=#00FF88>{HealthPerLevel[level]:F0} HP</color>.\n✦ <b>Tài nguyên cần để nâng cấp:</b>\n   • <color=#FFD700>🪙 {GetUpgradeCoins()} Vàng</color>\n   • <color=#DEB887>🪵 {GetUpgradeWood()} Gỗ</color>\n   • <color=#C0C0C0>🪨 {GetUpgradeStone()} Đá</color>"
                : "✦ <b>Cấp tối đa:</b> Lò Năng Lượng đã đạt công suất tối đa! Toàn bộ công trình đã được mở khóa lên Cấp 6 (Titan Max).";

            string maxCapStr = (level >= 4) ? "<color=#FFD700>Cấp 6 (Titan MAX)</color>" : $"Cấp {level}";
            hqInfoModalDetailsText.text = 
                $"✦ <b>Tên công trình:</b> LÒ NĂNG LƯỢNG TRUNG TÂM [HQ CORE]\n" +
                $"✦ <b>Cấp độ căn cứ:</b> <color=#00FFFF>CẤP {level} / {MaxLevel}</color>\n" +
                $"✦ <b>Độ bền kết cấu:</b> <color=#00FF88>{currentHealth:F0} / {maxHealth:F0} HP</color>\n" +
                $"✦ <b>Công nghệ căn cứ:</b> Cho phép nâng cấp Trụ Sét, Pháo Tự Hành, Súng Phun Lửa, Bức Tường và Nhà Máy tối đa đến <b>{maxCapStr}</b>.\n\n" +
                $"{nextInfo}";
        }
    }

    public void CloseInfoModal()
    {
        if (hqInfoModalObj != null) hqInfoModalObj.SetActive(false);
    }

    // ================= KHỞI TẠO GIAO DIỆN SCREEN-SPACE 2 NÚT TO RÕ =================

    private void EnsureHQScreenUI()
    {
        if (hqActionPanel != null && hqInfoModalObj != null) return;

        Canvas canvas = WallSelectionManager.GetMainScreenCanvas();
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. PANEL 2 NÚT THAO TÁC Ở NỬA DƯỚI MÀN HÌNH
        if (hqActionPanel == null)
        {
            hqActionPanel = new GameObject("Panel_BaseHQ_ActionCard", typeof(RectTransform));
            hqActionPanel.layer = LayerMask.NameToLayer("UI");
            hqActionPanel.transform.SetParent(canvas.transform, false);

            RectTransform cardRt = hqActionPanel.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0f);
            cardRt.anchorMax = new Vector2(0.5f, 0f);
            cardRt.pivot = new Vector2(0.5f, 0f);
            cardRt.anchoredPosition = new Vector2(0f, 130f);
            cardRt.sizeDelta = new Vector2(860f, 260f);

            Image cardBg = hqActionPanel.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.10f, 0.18f, 0.96f);

            Outline cardOutline = hqActionPanel.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.9f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Tiêu đề
            GameObject tObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
            tObj.transform.SetParent(hqActionPanel.transform, false);
            RectTransform tRt = tObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -10f);
            tRt.sizeDelta = new Vector2(-40f, 36f);

            hqPanelTitleText = tObj.GetComponent<Text>();
            hqPanelTitleText.font = safeFont;
            hqPanelTitleText.fontSize = 24;
            hqPanelTitleText.fontStyle = FontStyle.Bold;
            hqPanelTitleText.alignment = TextAnchor.MiddleCenter;
            hqPanelTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
            hqPanelTitleText.supportRichText = true;

            // Phụ đề
            GameObject sObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            sObj.transform.SetParent(hqActionPanel.transform, false);
            RectTransform sRt = sObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 1f);
            sRt.anchorMax = new Vector2(1f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.anchoredPosition = new Vector2(0f, -46f);
            sRt.sizeDelta = new Vector2(-40f, 28f);

            hqPanelSubtitleText = sObj.GetComponent<Text>();
            hqPanelSubtitleText.font = safeFont;
            hqPanelSubtitleText.fontSize = 17;
            hqPanelSubtitleText.alignment = TextAnchor.MiddleCenter;
            hqPanelSubtitleText.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            hqPanelSubtitleText.supportRichText = true;

            // HÀNG 2 NÚT CHÍNH TO RÕ RÀNG
            GameObject rowObj = new GameObject("ActionRow", typeof(RectTransform));
            rowObj.transform.SetParent(hqActionPanel.transform, false);
            RectTransform rowRt = rowObj.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0f);
            rowRt.anchorMax = new Vector2(0.5f, 0f);
            rowRt.pivot = new Vector2(0.5f, 0f);
            rowRt.anchoredPosition = new Vector2(0f, 20f);
            rowRt.sizeDelta = new Vector2(880f, 130f);

            HorizontalLayoutGroup hlg = rowObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // NÚT 1: [ℹ️ THÔNG TIN]
            hqInfoButton = CreateHQCardButton(rowObj.transform, "Btn_HQ_Info", new Vector2(145f, 110f), new Color(0.12f, 0.55f, 0.85f), safeFont, out Text infoTxt);
            infoTxt.text = "ℹ️ <b>THÔNG TIN</b>\n<color=#00FFFF>✦ Căn Cứ</color>";
            hqInfoButton.onClick.AddListener(OpenInfoModal);

            // NÚT 2: [🔬 VIỆN CÔNG NGHỆ]
            Button hqTechBtn = CreateHQCardButton(rowObj.transform, "Btn_HQ_Tech", new Vector2(185f, 110f), new Color(0.2f, 0.45f, 0.85f), safeFont, out Text techTxt);
            techTxt.text = "🔬 <b>CÔNG NGHỆ</b>\n<color=#00FF88>✦ Nâng Cấp Toàn Quân</color>";
            hqTechBtn.onClick.AddListener(() =>
            {
                if (GlobalTechTree.Instance != null)
                {
                    GlobalTechTree.Instance.OpenTechModal();
                }
            });

            // NÚT 3: [⭐ NÂNG CẤP LÒ NĂNG LƯỢNG] (360px rộng rãi ghi rõ Vàng, Gỗ, Đá)
            hqUpgradeButton = CreateHQCardButton(rowObj.transform, "Btn_HQ_Upgrade", new Vector2(360f, 110f), new Color(0.85f, 0.55f, 0.12f), safeFont, out hqUpgradeButtonText);
            hqUpgradeButton.onClick.AddListener(() => UpgradeHQ());

            // Nút 4: [🔧 SỬA CHỮA]
            hqRepairButton = CreateHQCardButton(rowObj.transform, "Btn_HQ_Repair", new Vector2(105f, 110f), new Color(0.18f, 0.65f, 0.35f), safeFont, out hqRepairButtonText);
            hqRepairButtonText.text = "🔧\n<b>SỬA</b>";
            hqRepairButton.onClick.AddListener(OnRepairButtonClicked);

            // Nút 5: [✕ ĐÓNG]
            hqCloseButton = CreateHQCardButton(rowObj.transform, "Btn_HQ_Close", new Vector2(45f, 110f), new Color(0.6f, 0.2f, 0.2f), safeFont, out Text closeTxt);
            closeTxt.text = "<b>✕</b>";
            closeTxt.fontSize = 24;
            hqCloseButton.onClick.AddListener(CloseHQInteractionMenu);

            hqActionPanel.SetActive(false);
        }

        // 2. MODAL THÔNG TIN CHI TIẾT TO Ở GIỮA MÀN HÌNH
        if (hqInfoModalObj == null)
        {
            hqInfoModalObj = new GameObject("Modal_BaseHQ_Info", typeof(RectTransform));
            hqInfoModalObj.layer = LayerMask.NameToLayer("UI");
            hqInfoModalObj.transform.SetParent(canvas.transform, false);

            RectTransform mRt = hqInfoModalObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.pivot = new Vector2(0.5f, 0.5f);
            mRt.anchoredPosition = new Vector2(0f, 30f);
            mRt.sizeDelta = new Vector2(860f, 520f);

            Image mBg = hqInfoModalObj.AddComponent<Image>();
            mBg.color = new Color(0.06f, 0.08f, 0.15f, 0.98f);

            Outline mOutline = hqInfoModalObj.AddComponent<Outline>();
            mOutline.effectColor = new Color(0.2f, 0.85f, 1f, 0.95f);
            mOutline.effectDistance = new Vector2(3f, -3f);

            // Tiêu đề modal
            GameObject mTitleObj = new GameObject("ModalTitle", typeof(RectTransform), typeof(Text));
            mTitleObj.transform.SetParent(hqInfoModalObj.transform, false);
            RectTransform mtRt = mTitleObj.GetComponent<RectTransform>();
            mtRt.anchorMin = new Vector2(0f, 1f);
            mtRt.anchorMax = new Vector2(1f, 1f);
            mtRt.pivot = new Vector2(0.5f, 1f);
            mtRt.anchoredPosition = new Vector2(0f, -16f);
            mtRt.sizeDelta = new Vector2(-40f, 40f);

            Text mtTxt = mTitleObj.GetComponent<Text>();
            mtTxt.font = safeFont;
            mtTxt.fontSize = 26;
            mtTxt.fontStyle = FontStyle.Bold;
            mtTxt.alignment = TextAnchor.MiddleCenter;
            mtTxt.color = new Color(1f, 0.85f, 0.2f, 1f);
            mtTxt.text = "🏛️ THÔNG SỐ LÒ NĂNG LƯỢNG TRUNG TÂM";

            // Khung nội dung chi tiết
            GameObject contentObj = new GameObject("ModalContent", typeof(RectTransform), typeof(Text));
            contentObj.transform.SetParent(hqInfoModalObj.transform, false);
            RectTransform cRt = contentObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f, 0f);
            cRt.anchorMax = new Vector2(1f, 1f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, -5f);
            cRt.sizeDelta = new Vector2(-60f, -160f);

            hqInfoModalDetailsText = contentObj.GetComponent<Text>();
            hqInfoModalDetailsText.font = safeFont;
            hqInfoModalDetailsText.fontSize = 20;
            hqInfoModalDetailsText.alignment = TextAnchor.MiddleLeft;
            hqInfoModalDetailsText.color = Color.white;
            hqInfoModalDetailsText.lineSpacing = 1.25f;
            hqInfoModalDetailsText.supportRichText = true;

            // Nút Đóng Modal
            Button okBtn = CreateHQCardButton(hqInfoModalObj.transform, "Btn_Modal_Close", new Vector2(300f, 65f), new Color(0.15f, 0.55f, 0.85f), safeFont, out Text okTxt);
            RectTransform okRt = okBtn.GetComponent<RectTransform>();
            okRt.anchorMin = new Vector2(0.5f, 0f);
            okRt.anchorMax = new Vector2(0.5f, 0f);
            okRt.pivot = new Vector2(0.5f, 0f);
            okRt.anchoredPosition = new Vector2(0f, 22f);
            okTxt.text = "<b>ĐÃ HIỂU / ĐÓNG</b>";
            okTxt.fontSize = 20;
            okBtn.onClick.AddListener(CloseInfoModal);

            hqInfoModalObj.SetActive(false);
        }
    }

    private Button CreateHQCardButton(Transform parent, string name, Vector2 size, Color bgColor, Font font, out Text label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.layer = LayerMask.NameToLayer("UI");
        btnObj.transform.SetParent(parent, false);

        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.4f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.GetComponent<Button>();

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.layer = LayerMask.NameToLayer("UI");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        label = txtObj.GetComponent<Text>();
        label.font = font;
        label.fontSize = 17;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.supportRichText = true;
        label.raycastTarget = false;

        return btn;
    }

    private void UpdateHQPanelUI()
    {
        if (hqPanelTitleText != null)
        {
            hqPanelTitleText.text = (level >= MaxLevel)
                ? $"🏛️ <b>LÒ NĂNG LƯỢNG (CẤP 4 - TỐI ĐA)</b>"
                : $"🏛️ <b>LÒ NĂNG LƯỢNG (CẤP {level} / {MaxLevel})</b>";
        }

        if (hqPanelSubtitleText != null)
        {
            hqPanelSubtitleText.text = $"Máu: <color=#00FF88>{currentHealth:F0}/{maxHealth:F0} HP</color>  |  Giới hạn công nghệ: <color=#00FFFF>Cấp {level}</color>";
        }

        if (hqUpgradeButtonText != null && hqUpgradeButton != null)
        {
            if (level < MaxLevel)
            {
                hqUpgradeButtonText.text = $"⭐ <b>NÂNG CẤP LÒ NĂNG LƯỢNG (CẤP {level + 1})</b>\n<size=15><color=#FFD700>🪙 {GetUpgradeCoins()} Vàng</color> • <color=#C0C0C0>🪨 {GetUpgradeStone()} Đá</color></size>";
                hqUpgradeButton.interactable = true;
            }
            else
            {
                hqUpgradeButtonText.text = $"👑 <b>CẤP {MaxLevel} - MAX</b>\n<color=#AAAAAA>(TỐI ĐA)</color>";
                hqUpgradeButton.interactable = false;
            }
        }

        if (hqRepairButton != null && hqRepairButtonText != null)
        {
            bool needRepair = currentHealth < maxHealth;
            hqRepairButton.interactable = needRepair;
            hqRepairButtonText.text = needRepair 
                ? $"🔧\n<b>SỬA CHỮA</b>\n<size=11><color=#FFD700>{repairCostCoins}🪙</color> <color=#C0C0C0>{repairCostStone}🪨</color></size>" 
                : "✅\n<b>ĐẦY MÁU</b>";
        }
    }

    // ================= THANH MÁU NỔI (WORLD-SPACE) =================

    private void CreateHealthBarUI()
    {
        healthBarObj = new GameObject("[BaseHQ_HealthBar]");
        healthBarObj.transform.SetParent(transform, false);
        healthBarObj.transform.position = transform.position + Vector3.up * healthBarHeight;

        Canvas canvas = healthBarObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        CanvasScaler cs = healthBarObj.AddComponent<CanvasScaler>();
        cs.dynamicPixelsPerUnit = 10;

        RectTransform rt = healthBarObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300f, 32f);
        rt.localScale = Vector3.one * 0.012f;

        GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(healthBarObj.transform, false);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.15f, 0.95f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(healthBarObj.transform, false);
        healthBarFill = fill.GetComponent<RectTransform>();
        healthBarFill.anchorMin = new Vector2(0.01f, 0.1f);
        healthBarFill.anchorMax = new Vector2(0.99f, 0.9f);
        healthBarFill.sizeDelta = Vector2.zero;
        healthBarFill.pivot = new Vector2(0f, 0.5f);
        healthBarFillImage = fill.GetComponent<Image>();
        healthBarFillImage.color = new Color(0.1f, 0.9f, 0.4f, 1f);

        GameObject textObj = new GameObject("HP_Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(healthBarObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        healthBarText = textObj.GetComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthBarText.font = font;
        healthBarText.fontSize = 15;
        healthBarText.fontStyle = FontStyle.Bold;
        healthBarText.alignment = TextAnchor.MiddleCenter;
        healthBarText.color = Color.white;
    }

    public void UpdateHealthBarVisual()
    {
        if (healthBarFill == null) return;

        float pct = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthBarFill.localScale = new Vector3(pct, 1f, 1f);

        if (healthBarFillImage != null)
        {
            if (pct > 0.5f)
                healthBarFillImage.color = Color.Lerp(new Color(1f, 0.8f, 0.1f), new Color(0.1f, 0.95f, 0.35f), (pct - 0.5f) * 2f);
            else
                healthBarFillImage.color = Color.Lerp(new Color(0.95f, 0.15f, 0.15f), new Color(1f, 0.8f, 0.1f), pct * 2f);
        }

        if (healthBarText != null)
        {
            healthBarText.text = $"🏛️ LÒ NĂNG LƯỢNG (CẤP {level}): {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed || amount <= 0f) return;

        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        FloatingDamageText.Spawn(transform.position + Vector3.up * 3.5f, amount, new Color(1f, 0.2f, 0.2f, 1f));

        if (currentHealth <= maxHealth * 0.35f && Time.time - lastDamageWarningTime > 6f)
        {
            lastDamageWarningTime = Time.time;
            CenterWarningBanner.Show("⚠️ NGUY CẤP: LÒ NĂNG LƯỢNG ĐANG BỊ CẮN PHÁ! ⚠️", "Quái vật đang tấn công Nhà Chính, hãy tập trung phòng thủ ngay!", 3f);
        }

        UpdateHealthBarVisual();
        if (isHQSelected) UpdateHQPanelUI();

        if (currentHealth <= 0f)
        {
            OnBaseDestroyed();
        }
    }

    public void TakeDamage(int amount)
    {
        TakeDamage((float)amount);
    }

    private GameObject defeatDialogObj;

    private void OnBaseDestroyed()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        int currentDay = (DayNightSystem.Instance != null) ? DayNightSystem.Instance.currentDay : 1;
        bool isNight = (DayNightSystem.Instance != null) && DayNightSystem.Instance.isNight;
        string timeStr = isNight ? $"Đêm thứ {currentDay}" : $"Ngày thứ {currentDay}";
        string defeatTitle = $"Lõi năng lượng bị phá hủy ở {timeStr}";

        FloatingDamageText.SpawnResource(transform.position + Vector3.up * 4.5f, $"☠️ {defeatTitle}!", new Color(1f, 0.2f, 0.2f, 1f), 0.16f);
        Debug.LogWarning($"<color=#FF2222>[BaseHQ]</color> 💥 {defeatTitle}! Đã tạm dừng toàn bộ trò chơi.");

        // ================= TOÀN BỘ GAME BỊ ĐỨNG LẠI (FREEZE GAME SCALE = 0) THEO YÊU CẦU =================
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Hiện Dialog Thất Bại với nút Chơi Lại để khôi phục hoạt động
        ShowDefeatDialog(currentDay, isNight);
    }

    private void ShowDefeatDialog(int currentDay, bool isNight)
    {
        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        foreach (var c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null && allCanvases.Length > 0) canvas = allCanvases[0];

        if (canvas == null)
        {
            GameObject cGo = new GameObject("[Defeat_Canvas]", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
        }

        if (defeatDialogObj != null) Destroy(defeatDialogObj);

        defeatDialogObj = new GameObject("[Defeat_Dialog_Modal]", typeof(RectTransform), typeof(Image));
        defeatDialogObj.transform.SetParent(canvas.transform, false);
        defeatDialogObj.transform.SetAsLastSibling();

        RectTransform rtModal = defeatDialogObj.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        // Nền đen mờ phủ toàn màn hình, chặn hoàn toàn click xuyên thấu
        Image imgModal = defeatDialogObj.GetComponent<Image>();
        imgModal.color = new Color(0.04f, 0.02f, 0.02f, 0.92f);
        imgModal.raycastTarget = true;

        // Hộp thông báo trung tâm
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(Outline));
        box.transform.SetParent(defeatDialogObj.transform, false);
        RectTransform rtBox = box.GetComponent<RectTransform>();
        rtBox.anchorMin = new Vector2(0.5f, 0.5f);
        rtBox.anchorMax = new Vector2(0.5f, 0.5f);
        rtBox.pivot = new Vector2(0.5f, 0.5f);
        rtBox.sizeDelta = new Vector2(560f, 290f);

        Image imgBox = box.GetComponent<Image>();
        imgBox.color = new Color(0.12f, 0.04f, 0.04f, 0.98f);

        Outline outline = box.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.2f, 0.2f, 1f);
        outline.effectDistance = new Vector2(3.5f, -3.5f);

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Tiêu đề: LÕI NĂNG LƯỢNG ĐÃ BỊ PHÁ HỦY!
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(box.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 1f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.pivot = new Vector2(0.5f, 1f);
        rtTitle.anchoredPosition = new Vector2(0f, -20f);
        rtTitle.sizeDelta = new Vector2(540f, 52f);
        Text txtTitle = titleObj.GetComponent<Text>();
        txtTitle.font = safeFont;
        txtTitle.text = "💥 <b>LÕI NĂNG LƯỢNG ĐÃ BỊ PHÁ HỦY!</b> 💥";
        txtTitle.fontSize = 24;
        txtTitle.alignment = TextAnchor.MiddleCenter;
        txtTitle.color = new Color(1f, 0.28f, 0.28f, 1f);

        // Mô tả chi tiết
        string timeContext = isNight ? $"Đêm thứ {currentDay}" : $"Ngày thứ {currentDay}";
        GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(Text));
        descObj.transform.SetParent(box.transform, false);
        RectTransform rtDesc = descObj.GetComponent<RectTransform>();
        rtDesc.anchorMin = new Vector2(0f, 1f);
        rtDesc.anchorMax = new Vector2(1f, 1f);
        rtDesc.pivot = new Vector2(0.5f, 1f);
        rtDesc.anchoredPosition = new Vector2(0f, -80f);
        rtDesc.sizeDelta = new Vector2(520f, 85f);
        Text txtDesc = descObj.GetComponent<Text>();
        txtDesc.font = safeFont;
        txtDesc.text = $"Căn cứ đã thất thủ tại <b><color=#FFD700>{timeContext}</color></b>!\nToàn bộ trò chơi đã tạm dừng hoạt động.\nNhấn nút <b>CHƠI LẠI</b> bên dưới để bắt đầu hoạt động tiếp tục.";
        txtDesc.fontSize = 17;
        txtDesc.lineSpacing = 1.15f;
        txtDesc.alignment = TextAnchor.MiddleCenter;
        txtDesc.color = new Color(0.92f, 0.92f, 0.92f, 1f);

        // Nút CHƠI LẠI (RESTART)
        GameObject btnObj = new GameObject("Btn_Restart", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(box.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0f);
        rtBtn.anchorMax = new Vector2(0.5f, 0f);
        rtBtn.pivot = new Vector2(0.5f, 0f);
        rtBtn.anchoredPosition = new Vector2(0f, 24f);
        rtBtn.sizeDelta = new Vector2(280f, 58f);

        Image imgBtn = btnObj.GetComponent<Image>();
        imgBtn.color = new Color(0.12f, 0.72f, 0.35f, 1f); // Nút màu xanh lá nổi bật

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.1f, 0.3f, 0.15f, 1f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            // KHÔI PHỤC HOẠT ĐỘNG TOÀN BỘ GAME KHI BẤM CHƠI LẠI
            AudioListener.pause = false;
            Time.timeScale = 1f;
            Debug.Log("<color=#00FF88>🔄 [BaseHQ]</color> Người chơi bấm Chơi Lại! Khôi phục Time.timeScale = 1 và tải lại ván chơi mới.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        });

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtBtnTxt = btnTxtObj.GetComponent<RectTransform>();
        rtBtnTxt.anchorMin = Vector2.zero;
        rtBtnTxt.anchorMax = Vector2.one;
        rtBtnTxt.sizeDelta = Vector2.zero;
        Text txtBtn = btnTxtObj.GetComponent<Text>();
        txtBtn.font = safeFont;
        txtBtn.text = "🔄 <b>CHƠI LẠI (BẮT ĐẦU TIẾP)</b>";
        txtBtn.fontSize = 19;
        txtBtn.alignment = TextAnchor.MiddleCenter;
        txtBtn.color = Color.white;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
