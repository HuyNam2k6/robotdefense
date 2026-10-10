using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý tương tác click và kéo thả (Drag & Drop) vào Bức Tường hoặc Ụ Pháo:
/// 1. Nhấn giữ để di chuyển trên mặt đất:
///    - Chỉ di chuyển được đúng trên mặt đất hợp lệ.
///    - Nếu vị trí KHÔNG PHÙ HỢP (đè lên vật thể khác hoặc ngoài rìa đảo) -> Hiển thị MÀU ĐỎ cảnh báo.
///    - Thả tay ra ở vị trí HỢP LỆ -> Đứng yên tại vị trí mới.
///    - Thả tay ra ở vị trí KHÔNG HỢP LỆ -> Tự động quay về vị trí lúc chưa di chuyển.
/// 2. Hỗ trợ xoay hướng 90 độ tại chỗ hoặc khi đang di chuyển (nút UI hoặc phím R).
/// 3. Dời riêng 1 đoạn tường hoặc dời toàn bộ hàng tường kết nối phẳng lì chuẩn lưới.
/// </summary>
public class WallSelectionManager : MonoBehaviour
{
    public static WallSelectionManager Instance { get; private set; }

    [Header("--- Tham Chiếu Hệ Thống ---")]
    public BuildingSystem buildingSystem;
    public BuildingShopUI shopUI;

    [Header("--- Công Trình / Trụ Đang Được Chọn ---")]
    public WallSegment selectedWall;
    public UpgradableTurret selectedTurret;
    public RobotFactory selectedFactory;

    // Quản lý trạng thái nhấn giữ vào công trình (Vũ khí, Tường, Nhà chính, Nhà máy)
    private bool isPressHolding = false;
    private bool isDragStarted = false;
    private Vector3 pressStartMousePos;
    private float nextHQWarningTime = 0f;

    private WallSegment pressedWall;
    private UpgradableTurret pressedTurret;
    private BaseHQ pressedHQ;
    private RobotFactory pressedFactory;

    private struct PressRendererInfo
    {
        public Renderer renderer;
        public Color originalBaseColor;
        public Color originalEmission;
    }
    private readonly List<PressRendererInfo> activePressRenderers = new List<PressRendererInfo>();
    private MaterialPropertyBlock pressBlinkPropBlock;

    // Quản lý Renderers của vật thể đang chọn
    private readonly List<Renderer> activeSelectedRenderers = new List<Renderer>();
    private MaterialPropertyBlock selectionPulsePropBlock;

    [Header("--- Giao Diện Card Thông Tin & Thao Tác ---")]
    public GameObject wallCardPanel;
    public Text titleText;
    public Text subtitleText;

    // Hàng nút trên (Nâng cấp, Sửa chữa & Đóng)
    public Button upgradeSingleButton;
    public Text upgradeSingleText;
    public Button upgradeRowButton;
    public Text upgradeRowText;
    public Button repairButton;
    public Text repairText;
    public Button closeCardButton;

    // Hàng nút dưới (Di chuyển & Xoay)
    public GameObject actionRowObj;
    public Button moveSingleButton;
    public Text moveSingleText;
    public Button moveRowButton;
    public Text moveRowText;
    public Button rotateButton;
    public Text rotateText;

    [Header("--- Giao Diện Chế Độ Di Chuyển (Move Mode) ---")]
    public GameObject moveControlPanel;
    public Text moveInstructionText;
    public Button rotateInMoveButton;
    public Button cancelMoveButton;

    // 3D Marker lơ lửng trên đoạn tường / trụ đang chọn
    private GameObject selectionMarker;

    // --- Quản Lý Trạng Thái Di Chuyển (Drag & Drop System) ---
    private bool isMoveMode = false;
    private bool isDragging = false;
    private bool isMovingRow = false;
    private bool isPlacementValid = true;

    private Transform movePivot;
    private Vector3 movePivotOrigPos;
    private Quaternion movePivotOrigRot;

    private struct MovingItemBackup
    {
        public Transform transform;
        public Vector3 origPos;
        public Quaternion origRot;
        public Vector3 offsetFromPivot;
        public Collider[] colliders;
    }
    private readonly List<MovingItemBackup> activeMovingItems = new List<MovingItemBackup>();

    // MaterialPropertyBlock màu đỏ cảnh báo không phù hợp
    private static MaterialPropertyBlock redWarningPropBlock;

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
            if (canvas != null && canvas.GetComponent<WallSelectionManager>() == null)
            {
                canvas.gameObject.AddComponent<WallSelectionManager>();
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
        if (buildingSystem == null) buildingSystem = FindAnyObjectByType<BuildingSystem>();
        if (shopUI == null) shopUI = FindAnyObjectByType<BuildingShopUI>();

        EnsureUIElementsCreated();
        CreateSelectionMarker();

        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.OnEconomyChanged += RefreshCardUI;
        }

        DeselectAll();
    }

    void OnDestroy()
    {
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.OnEconomyChanged -= RefreshCardUI;
        }
        if (selectionMarker != null)
        {
            Destroy(selectionMarker);
        }
    }

    void Update()
    {
        // 1. Nếu đang ở chế độ di chuyển đồ vật -> xử lý logic nhấn giữ kéo thả
        if (isMoveMode)
        {
            UpdateMoveMode();
            return;
        }

        // 2. Không nhận click chọn nếu đang trong chế độ kéo đặt công trình mới của shop
        if (buildingSystem != null && buildingSystem.IsPlacing)
        {
            if (isPressHolding) ClearPressBlinking();
            if (selectedWall != null || selectedTurret != null || selectedFactory != null) DeselectAll();
            return;
        }

        // 3. Nhận diện nhấn giữ, nhấp nháy xanh dương và kéo trực tiếp
        HandleWorldSelectionAndDragDetection();
    }

    /// <summary>
    /// Kiểm tra toàn diện xem con trỏ chuột hoặc ngón tay cảm ứng có đang chạm vào UI không (tránh click xuyên thấu)
    /// </summary>
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }

    private void HandleWorldSelectionAndDragDetection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressStartMousePos = Input.mousePosition;
            isPressHolding = false;
            isDragStarted = false;

            // Nếu click hoặc chạm vào bất kỳ phần tử UI nào -> KHÔNG xử lý 3D
            if (IsPointerOverUI())
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 350f))
            {
                WallSegment clickedWall = hit.collider.GetComponentInParent<WallSegment>();
                UpgradableTurret clickedTurret = hit.collider.GetComponentInParent<UpgradableTurret>();
                BaseHQ clickedHQ = hit.collider.GetComponentInParent<BaseHQ>();
                RobotFactory clickedFactory = hit.collider.GetComponentInParent<RobotFactory>();

                if (clickedWall != null || clickedTurret != null || clickedHQ != null || clickedFactory != null)
                {
                    pressedWall = clickedWall;
                    pressedTurret = clickedTurret;
                    pressedHQ = clickedHQ;
                    pressedFactory = clickedFactory;
                    isPressHolding = true;

                    Transform targetT = (clickedWall != null) ? clickedWall.transform :
                                        (clickedTurret != null) ? clickedTurret.transform :
                                        (clickedHQ != null) ? clickedHQ.transform :
                                        clickedFactory.transform;

                    CollectPressRenderers(targetT);
                    return;
                }
            }

            // Click vào khoảng trống mặt đất: Bỏ chọn và đóng tất cả card/menu
            DeselectAll();
            if (BaseHQ.Instance != null) BaseHQ.Instance.CloseHQInteractionMenu();
        }
        else if (Input.GetMouseButton(0) && isPressHolding)
        {
            // Trong lúc đang nhấn giữ: Nhấp nháy màu xanh dương rồi trở về màu cũ liên tục
            UpdatePressBlinking();

            // Kiểm tra kéo chuột / ngón tay vượt quá 14 pixel
            if (Vector3.Distance(Input.mousePosition, pressStartMousePos) > 14f)
            {
                // A. RIÊNG NHÀ CHÍNH (BaseHQ): KHÔNG THỂ DI CHUYỂN, BÁO "LÕI KHÔNG THỂ DI CHUYỂN"
                if (pressedHQ != null)
                {
                    isDragStarted = true;
                    if (Time.time >= nextHQWarningTime)
                    {
                        nextHQWarningTime = Time.time + 1.2f;
                        CenterWarningBanner.Show("⚠️ LÕI KHÔNG THỂ DI CHUYỂN!", "Lõi năng lượng là công trình căn cứ cố định, không thể di dời vị trí!", 2.0f);
                        FloatingDamageText.SpawnResource(pressedHQ.transform.position + Vector3.up * 4.0f, "⚠️ Lõi không thể di chuyển!", new Color(1f, 0.35f, 0.35f), 0.15f);
                        Debug.LogWarning("<color=#FF5555>[BaseHQ]</color> Lõi không thể di chuyển!");
                    }
                }
                // B. VŨ KHÍ, TƯỜNG, HOẶC NHÀ MÁY: KÍCH HOẠT CHẾ ĐỘ DI CHUYỂN KÉO THẢ
                else if (!isMoveMode)
                {
                    isDragStarted = true;
                    if (pressedWall != null)
                    {
                        SelectWall(pressedWall);
                        StartMoveSingle();
                    }
                    else if (pressedTurret != null)
                    {
                        SelectTurret(pressedTurret);
                        StartMoveSingle();
                    }
                    else if (pressedFactory != null)
                    {
                        SelectFactory(pressedFactory);
                        StartMoveSingle();
                    }
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isPressHolding)
            {
                isPressHolding = false;

                // Thả tay ra: Dừng ngay nhấp nháy và trả về màu cũ nguyên bản
                ClearPressBlinking();

                // Nếu không phải đang kéo di chuyển (chỉ nhấn/thả tại chỗ) -> Mở bảng thông tin/nâng cấp
                if (!isDragStarted)
                {
                    if (pressedHQ != null)
                    {
                        pressedHQ.OpenHQInteractionMenu();
                    }
                    else if (pressedWall != null)
                    {
                        SelectWall(pressedWall);
                    }
                    else if (pressedTurret != null)
                    {
                        SelectTurret(pressedTurret);
                    }
                    else if (pressedFactory != null)
                    {
                        SelectFactory(pressedFactory);
                    }
                }

                pressedWall = null;
                pressedTurret = null;
                pressedHQ = null;
                pressedFactory = null;
                isDragStarted = false;
            }
        }
    }

    // ================= HIỆU ỨNG NHẤP NHÁY XANH DƯƠNG -> MÀU CŨ KHI NHẤN GIỮ =================

    public void CollectPressRenderers(Transform root)
    {
        ClearPressBlinking();
        if (root == null) return;

        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null || r is LineRenderer || r is ParticleSystemRenderer) continue;
            if (r.GetComponent<CanvasRenderer>() != null) continue;

            Color origBase = Color.white;
            Color origEmission = Color.black;

            if (r.sharedMaterial != null)
            {
                if (r.sharedMaterial.HasProperty("_BaseColor"))
                    origBase = r.sharedMaterial.GetColor("_BaseColor");
                else if (r.sharedMaterial.HasProperty("_Color"))
                    origBase = r.sharedMaterial.GetColor("_Color");

                if (r.sharedMaterial.HasProperty("_EmissionColor"))
                    origEmission = r.sharedMaterial.GetColor("_EmissionColor");
            }

            activePressRenderers.Add(new PressRendererInfo
            {
                renderer = r,
                originalBaseColor = origBase,
                originalEmission = origEmission
            });
        }
    }

    public void UpdatePressBlinking()
    {
        if (activePressRenderers.Count == 0) return;

        if (pressBlinkPropBlock == null) pressBlinkPropBlock = new MaterialPropertyBlock();

        // Tần số nhấp nháy ~1.8 chu kỳ / giây: Đảo nhịp liên tục giữa màu xanh dương và màu cũ nguyên bản
        float pulse = (Mathf.Sin(Time.time * 11f) + 1f) * 0.5f;

        Color blueNeon = new Color(0.08f, 0.60f, 1.0f, 1f);     // Xanh dương neon công nghệ
        Color blueEmission = new Color(0.12f, 0.70f, 1.40f, 1f); // Phát sáng xanh dương HDR

        for (int i = 0; i < activePressRenderers.Count; i++)
        {
            var info = activePressRenderers[i];
            if (info.renderer == null) continue;

            info.renderer.GetPropertyBlock(pressBlinkPropBlock);

            // Chuyển đổi qua lại liên tục giữa Màu Xanh Dương và Màu Cũ ban đầu
            Color curBase = Color.Lerp(info.originalBaseColor, blueNeon, pulse * 0.85f);
            Color curEmission = Color.Lerp(info.originalEmission, blueEmission, pulse);

            pressBlinkPropBlock.SetColor("_BaseColor", curBase);
            pressBlinkPropBlock.SetColor("_Color", curBase);
            pressBlinkPropBlock.SetColor("_EmissionColor", curEmission);

            info.renderer.SetPropertyBlock(pressBlinkPropBlock);
        }
    }

    public void ClearPressBlinking()
    {
        for (int i = 0; i < activePressRenderers.Count; i++)
        {
            var info = activePressRenderers[i];
            if (info.renderer != null)
            {
                info.renderer.SetPropertyBlock(null);
            }
        }
        activePressRenderers.Clear();

        // Phục hồi lại hoàn toàn visual chuẩn của cấp độ
        if (pressedWall != null) pressedWall.ApplyLevel(pressedWall.currentLevel);
        else if (selectedWall != null) selectedWall.ApplyLevel(selectedWall.currentLevel);

        if (pressedTurret != null) pressedTurret.ApplyTurretVisuals(pressedTurret.currentLevel);
        else if (selectedTurret != null) selectedTurret.ApplyTurretVisuals(selectedTurret.currentLevel);

        if (pressedHQ != null) pressedHQ.ResetHQVisuals();
        else if (BaseHQ.Instance != null) BaseHQ.Instance.ResetHQVisuals();

        if (pressedFactory != null)
        {
            Renderer[] rends = pressedFactory.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends) if (r != null) r.SetPropertyBlock(null);
        }
    }

    private void CollectSelectedRenderers(Transform root)
    {
        ClearSelectedRenderers();
        if (root == null) return;

        Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            if (r == null || r is LineRenderer || r is ParticleSystemRenderer) continue;
            if (r.GetComponent<CanvasRenderer>() != null) continue;
            activeSelectedRenderers.Add(r);
        }
    }

    private void ClearSelectedRenderers()
    {
        for (int i = 0; i < activeSelectedRenderers.Count; i++)
        {
            if (activeSelectedRenderers[i] != null)
            {
                activeSelectedRenderers[i].SetPropertyBlock(null);
            }
        }
        activeSelectedRenderers.Clear();

        if (selectedWall != null) selectedWall.ApplyLevel(selectedWall.currentLevel);
        if (selectedTurret != null) selectedTurret.ApplyTurretVisuals(selectedTurret.currentLevel);
    }

    private void UpdateSelectionPulsingHighlight()
    {
        // Hiệu ứng nhấp nháy xanh dương chỉ diễn ra khi người chơi đang nhấn giữ (theo yêu cầu).
        // Khi người chơi thả tay ra -> trở về màu cũ của công trình.
    }

    // ================= CHỌN / BỎ CHỌN VẬT THỂ =================

    public void SelectWall(WallSegment wall)
    {
        if (wall == null || isMoveMode) return;

        if (selectedTurret != null) DeselectTurret();
        if (selectedFactory != null) DeselectFactory();
        if (selectedWall != null && selectedWall != wall) selectedWall.SetSelected(false);

        selectedWall = wall;
        selectedWall.SetSelected(true);
        selectedWall.TriggerSelectHop(0f, 0.45f);

        CollectSelectedRenderers(wall.transform);

        // Hiệu ứng sóng lan tỏa: Các đoạn tường kết nối trong hàng nảy sóng nhẹ
        List<WallSegment> connectedRow = selectedWall.GetConnectedRow();
        for (int i = 0; i < connectedRow.Count; i++)
        {
            WallSegment otherWall = connectedRow[i];
            if (otherWall != null && otherWall != selectedWall)
            {
                float dist = Vector3.Distance(selectedWall.transform.position, otherWall.transform.position);
                otherWall.TriggerSelectHop(dist * 0.035f, 0.22f);
            }
        }

        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            shopUI.CloseShopPanel();
        }

        if (wallCardPanel != null)
        {
            wallCardPanel.SetActive(true);
            wallCardPanel.transform.SetAsLastSibling();
        }
        if (selectionMarker != null) selectionMarker.SetActive(false);

        RefreshCardUI();
    }

    public void SelectTurret(UpgradableTurret turret)
    {
        if (turret == null || isMoveMode) return;

        if (selectedWall != null) DeselectWall();
        if (selectedFactory != null) DeselectFactory();
        if (selectedTurret != null && selectedTurret != turret) selectedTurret.SetSelected(false);

        selectedTurret = turret;
        selectedTurret.SetSelected(true);

        CollectSelectedRenderers(turret.transform);

        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            shopUI.CloseShopPanel();
        }

        if (wallCardPanel != null)
        {
            wallCardPanel.SetActive(true);
            wallCardPanel.transform.SetAsLastSibling();
        }
        if (selectionMarker != null) selectionMarker.SetActive(false);

        RefreshCardUI();
    }

    public void SelectFactory(RobotFactory factory)
    {
        if (factory == null || isMoveMode) return;

        if (selectedWall != null) DeselectWall();
        if (selectedTurret != null) DeselectTurret();
        if (selectedFactory != null && selectedFactory != factory) DeselectFactory();

        selectedFactory = factory;
        CollectSelectedRenderers(factory.transform);

        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            shopUI.CloseShopPanel();
        }

        if (wallCardPanel != null)
        {
            wallCardPanel.SetActive(true);
            wallCardPanel.transform.SetAsLastSibling();
        }
        if (selectionMarker != null) selectionMarker.SetActive(false);

        RefreshCardUI();
        Debug.Log("<color=#00FF88>🏭 [WallSelectionManager]</color> Đã chọn Trại Huấn Luyện Quân! Toàn thân nhấp nháy phát sáng & mở bảng điều khiển.");
    }

    public void DeselectWall()
    {
        if (selectedWall != null)
        {
            selectedWall.SetSelected(false);
            selectedWall = null;
        }

        ClearSelectedRenderers();

        if (selectedTurret == null && selectedFactory == null)
        {
            if (wallCardPanel != null) wallCardPanel.SetActive(false);
            if (selectionMarker != null) selectionMarker.SetActive(false);
        }
    }

    public void DeselectTurret()
    {
        if (selectedTurret != null)
        {
            selectedTurret.SetSelected(false);
            selectedTurret = null;
        }

        ClearSelectedRenderers();

        if (selectedWall == null && selectedFactory == null)
        {
            if (wallCardPanel != null) wallCardPanel.SetActive(false);
            if (selectionMarker != null) selectionMarker.SetActive(false);
        }
    }

    public void DeselectFactory()
    {
        if (selectedFactory != null)
        {
            selectedFactory = null;
        }

        ClearSelectedRenderers();

        if (selectedWall == null && selectedTurret == null)
        {
            if (wallCardPanel != null) wallCardPanel.SetActive(false);
            if (selectionMarker != null) selectionMarker.SetActive(false);
        }
    }

    public void DeselectAll()
    {
        if (isMoveMode) CancelMove();
        if (isPressHolding) ClearPressBlinking();
        DeselectWall();
        DeselectTurret();
        DeselectFactory();
        if (BaseHQ.Instance != null) BaseHQ.Instance.CloseHQInteractionMenu();
    }

    // ================= CẬP NHẬT GIAO DIỆN NÂNG CẤP & THAO TÁC =================

    public void RefreshCardUI()
    {
        if (isMoveMode) return;

        if (selectedWall == null && selectedTurret == null && selectedFactory == null)
        {
            if (wallCardPanel != null && wallCardPanel.activeSelf) wallCardPanel.SetActive(false);
            if (selectionMarker != null && selectionMarker.activeSelf) selectionMarker.SetActive(false);
            return;
        }

        // ================= 1. NẾU ĐANG CHỌN BỨC TƯỜNG =================
        if (selectedWall != null)
        {
            if (upgradeRowButton != null) upgradeRowButton.gameObject.SetActive(true);
            if (moveRowButton != null) moveRowButton.gameObject.SetActive(true);
            if (moveSingleText != null) moveSingleText.text = "📦 <b>DỜI 1 ĐOẠN</b>";

            int lvl = selectedWall.currentLevel;
            string curName = (GameEconomy.WallLevelNames != null && lvl >= 1 && lvl <= GameEconomy.WallLevelNames.Length)
                ? GameEconomy.WallLevelNames[lvl - 1]
                : $"Cấp {lvl}";

            if (titleText != null)
            {
                titleText.text = (lvl >= 6)
                    ? $"🧱 <b>BỨC TƯỜNG (CẤP 6 - TITAN TỐI ĐA)</b>"
                    : $"🧱 <b>BỨC TƯỜNG (CẤP {lvl} / 6)</b>";
            }

            if (lvl < 6)
            {
                int singleCost = selectedWall.GetUpgradeCost();
                string nextName = (GameEconomy.WallLevelNames != null && lvl < GameEconomy.WallLevelNames.Length)
                    ? GameEconomy.WallLevelNames[lvl]
                    : $"Cấp {lvl + 1}";

                if (subtitleText != null)
                {
                    subtitleText.text = $"✦ {curName}  ➔  <color=#00FFFF>{nextName}</color>";
                }

                if (upgradeSingleText != null)
                {
                    string costStr = singleCost == 0 ? "MIỄN PHÍ (0 Vàng)" : $"🪙 {singleCost} Vàng";
                    upgradeSingleText.text = $"⭐ <b>NÂNG CẤP</b>\n<color=#00FF88>{costStr}</color>";
                }

                if (upgradeSingleButton != null) upgradeSingleButton.interactable = true;
            }
            else
            {
                if (subtitleText != null)
                {
                    subtitleText.text = $"👑 <color=#E0B0FF>ĐÃ ĐẠT CẤP 6 - TITAN (TỐI ĐA)</color>";
                }

                if (upgradeSingleText != null)
                {
                    upgradeSingleText.text = $"👑 <b>CẤP 6 - MAX</b>\n<color=#AAAAAA>(TỐI ĐA)</color>";
                }

                if (upgradeSingleButton != null) upgradeSingleButton.interactable = false;
            }

            // Nâng cấp cả hàng kết nối
            List<WallSegment> connectedRow = selectedWall.GetConnectedRow();
            int upgradeableCount = 0;
            int rowTotalCost = 0;

            for (int i = 0; i < connectedRow.Count; i++)
            {
                if (connectedRow[i] != null && connectedRow[i].currentLevel < 6)
                {
                    upgradeableCount++;
                    rowTotalCost += connectedRow[i].GetUpgradeCost();
                }
            }

            if (upgradeRowText != null)
            {
                if (upgradeableCount > 0)
                {
                    string rowCostStr = rowTotalCost == 0 ? "MIỄN PHÍ (0 Vàng)" : $"🪙 {rowTotalCost} Vàng";
                    upgradeRowText.text = $"⚡ <b>NÂNG CẢ HÀNG ({upgradeableCount})</b>\n<color=#00FF88>{rowCostStr}</color>";
                    if (upgradeRowButton != null) upgradeRowButton.interactable = true;
                }
                else
                {
                    upgradeRowText.text = $"⚡ <b>CẢ HÀNG ({connectedRow.Count})</b>\n<color=#AAAAAA>MAX TỐI ĐA</color>";
                    if (upgradeRowButton != null) upgradeRowButton.interactable = false;
                }
            }

            if (moveRowText != null)
            {
                moveRowText.text = $"🚛 <b>DỜI CẢ HÀNG ({connectedRow.Count})</b>";
            }

            if (repairButton != null && repairText != null)
            {
                repairButton.gameObject.SetActive(true);
                int repairCost = selectedWall.GetRepairCost();
                float curHp = selectedWall.currentHealth;
                float maxHp = selectedWall.maxHealth;
                if (curHp >= maxHp)
                {
                    repairText.text = $"🔧 <b>SỬA CHỮA</b>\n<color=#00FF88>ĐẦY MÁU ({Mathf.CeilToInt(curHp)}/{Mathf.CeilToInt(maxHp)})</color>";
                    repairButton.interactable = false;
                }
                else
                {
                    string costStr = repairCost == 0 ? "0 Vàng (MIỄN PHÍ)" : $"🪙 {repairCost} Vàng";
                    repairText.text = $"🔧 <b>SỬA CHỮA</b>\n<color=#00FFFF>{costStr}</color>";
                    repairButton.interactable = true;
                }
            }
        }
        // ================= 2. NẾU ĐANG CHỌN Ụ PHÁO =================
        else if (selectedTurret != null)
        {
            if (upgradeRowButton != null) upgradeRowButton.gameObject.SetActive(false);
            if (moveRowButton != null) moveRowButton.gameObject.SetActive(false);
            if (moveSingleText != null) moveSingleText.text = "📦 <b>DI CHUYỂN TRỤ</b>";

            int lvl = selectedTurret.currentLevel;
            int maxLvl = UpgradableTurret.MaxLevel;

            string curTurretName = (UpgradableTurret.TurretLevelNames != null && lvl >= 1 && lvl <= UpgradableTurret.TurretLevelNames.Length)
                ? UpgradableTurret.TurretLevelNames[lvl - 1]
                : $"Cấp {lvl}";

            if (titleText != null)
            {
                titleText.text = (lvl >= maxLvl)
                    ? $"<b>{selectedTurret.turretDisplayName}</b> (CẤP {lvl} - MAX {curTurretName.ToUpper()})"
                    : $"<b>{selectedTurret.turretDisplayName}</b> (CẤP {lvl} / {maxLvl} - {curTurretName})";
            }

            if (subtitleText != null)
            {
                subtitleText.text = selectedTurret.GetStatsDescription();
            }

            if (lvl < maxLvl)
            {
                int cost = selectedTurret.GetUpgradeCost();
                if (upgradeSingleText != null)
                {
                    string costStr = cost == 0 ? "MIỄN PHÍ (0 Vàng)" : $"🪙 {cost} Vàng";
                    upgradeSingleText.text = $"⭐ <b>NÂNG CẤP</b>\n<color=#00FF88>{costStr}</color>";
                }
                if (upgradeSingleButton != null) upgradeSingleButton.interactable = true;
            }
            else
            {
                if (upgradeSingleText != null)
                {
                    upgradeSingleText.text = $"👑 <b>CẤP {maxLvl} - MAX</b>\n<color=#AAAAAA>(TỐI ĐA)</color>";
                }
                if (upgradeSingleButton != null) upgradeSingleButton.interactable = false;
            }

            if (repairButton != null && repairText != null)
            {
                repairButton.gameObject.SetActive(true);
                int repairCost = selectedTurret.GetRepairCost();
                float curHp = selectedTurret.currentHealth;
                float maxHp = selectedTurret.maxHealth;
                if (curHp >= maxHp)
                {
                    repairText.text = $"🔧 <b>SỬA CHỮA</b>\n<color=#00FF88>ĐẦY MÁU ({Mathf.CeilToInt(curHp)}/{Mathf.CeilToInt(maxHp)})</color>";
                    repairButton.interactable = false;
                }
                else
                {
                    string costStr = repairCost == 0 ? "0 Vàng (MIỄN PHÍ)" : $"🪙 {repairCost} Vàng";
                    repairText.text = $"🔧 <b>SỬA CHỮA</b>\n<color=#00FFFF>{costStr}</color>";
                    repairButton.interactable = true;
                }
            }
        }
        // ================= 3. NẾU ĐANG CHỌN TRẠI HUẤN LUYỆN QUÂN =================
        else if (selectedFactory != null)
        {
            if (upgradeRowButton != null) upgradeRowButton.gameObject.SetActive(false);
            if (moveRowButton != null) moveRowButton.gameObject.SetActive(false);
            if (moveSingleText != null) moveSingleText.text = "📦 <b>DI CHUYỂN TRẠI LÍNH</b>";

            int fLvl = selectedFactory.factoryLevel;
            int maxFLvl = RobotFactory.MaxFactoryLevel;

            if (titleText != null)
            {
                titleText.text = (fLvl >= maxFLvl)
                    ? $"🏭 <b>TRẠI LÍNH (CẤP {fLvl} - MAX TỐI ĐA)</b>"
                    : $"🏭 <b>TRẠI HUẤN LUYỆN QUÂN (CẤP {fLvl} / {maxFLvl})</b>";
            }

            int curArmy = selectedFactory.GetCurrentArmyCount();
            int maxArmy = selectedFactory.maxArmySize;

            if (subtitleText != null)
            {
                subtitleText.text = $"Quân số: <color=#00FF88><b>{curArmy}/{maxArmy}</b></color>  |  Mở khóa: Stan, Mike, George, Leela";
            }

            if (fLvl < maxFLvl)
            {
                int c = selectedFactory.GetUpgradeCoins();
                int w = selectedFactory.GetUpgradeWood();
                int s = selectedFactory.GetUpgradeStone();

                if (upgradeSingleText != null)
                {
                    upgradeSingleText.text = $"⭐ <b>NÂNG CẤP (LÊN CẤP {fLvl + 1})</b>\n<color=#FFD700>🪙 {c} Vàng</color> • <color=#DEB887>🪵 {w} Gỗ</color> • <color=#C0C0C0>🪨 {s} Đá</color>";
                }
                if (upgradeSingleButton != null) upgradeSingleButton.interactable = true;
            }
            else
            {
                if (upgradeSingleText != null)
                {
                    upgradeSingleText.text = $"👑 <b>CẤP {maxFLvl} - MAX</b>\n<color=#AAAAAA>(TỐI ĐA)</color>";
                }
                if (upgradeSingleButton != null) upgradeSingleButton.interactable = false;
            }

            if (repairButton != null && repairText != null)
            {
                repairButton.gameObject.SetActive(true);
                repairText.text = "🤖 <b>CHẾ TẠO ROBOT</b>\n<color=#00FFFF>(MỞ BẢNG 15s)</color>";
                repairButton.interactable = true;
            }
        }
    }

    private void OnRepairClicked()
    {
        if (selectedWall != null)
        {
            bool success = selectedWall.Repair();
            Debug.Log($"[WallSelectionManager] Sửa chữa Tường: success={success}");
            if (success) RefreshCardUI();
        }
        else if (selectedTurret != null)
        {
            bool success = selectedTurret.Repair();
            Debug.Log($"[WallSelectionManager] Sửa chữa Ụ Pháo ({selectedTurret.turretDisplayName}): success={success}");
            if (success) RefreshCardUI();
        }
        else if (selectedFactory != null)
        {
            selectedFactory.TryOpenCraftingPanel();
        }
    }

    private void OnUpgradeSingleClicked()
    {
        Debug.Log($"<color=#00FFFF>[WallSelectionManager]</color> Nhấn nút Nâng Cấp! selectedWall={selectedWall != null}, selectedTurret={selectedTurret != null}, selectedFactory={selectedFactory != null}");
        if (selectedWall != null)
        {
            bool success = selectedWall.UpgradeSingle();
            Debug.Log($"[WallSelectionManager] Nâng cấp Tường: success={success}");
            if (success) RefreshCardUI();
        }
        else if (selectedTurret != null)
        {
            bool success = selectedTurret.Upgrade();
            Debug.Log($"[WallSelectionManager] Nâng cấp Ụ Pháo ({selectedTurret.turretDisplayName}): success={success}");
            if (success)
            {
                selectedTurret.ApplyTurretVisuals(selectedTurret.currentLevel);
                RefreshCardUI();
            }
        }
        else if (selectedFactory != null)
        {
            bool success = selectedFactory.UpgradeFactory();
            Debug.Log($"[WallSelectionManager] Nâng cấp Trại Lính: success={success}");
            if (success) RefreshCardUI();
        }
    }

    private void OnUpgradeRowClicked()
    {
        if (selectedWall == null) return;

        bool success = selectedWall.UpgradeConnectedRow();
        if (success) RefreshCardUI();
    }

    // ================= CHẾ ĐỘ DI CHUYỂN (NHẤN GIỮ KÉO THẢ CHUẨN XÁC) =================

    public void StartMoveSingle()
    {
        if (selectedWall != null)
        {
            StartMoveMode(selectedWall.transform, false);
        }
        else if (selectedTurret != null)
        {
            StartMoveMode(selectedTurret.transform, false);
        }
        else if (selectedFactory != null)
        {
            StartMoveMode(selectedFactory.transform, false);
        }
    }

    public void StartMoveRow()
    {
        if (selectedWall != null)
        {
            StartMoveMode(selectedWall.transform, true);
        }
    }

    private void StartMoveMode(Transform pivot, bool isRow)
    {
        if (pivot == null) return;

        isMoveMode = true;
        isDragging = true;
        isMovingRow = isRow;
        isPlacementValid = true;

        movePivot = pivot;
        movePivotOrigPos = pivot.position;
        movePivotOrigRot = pivot.rotation;

        activeMovingItems.Clear();
        CollectPressRenderers(pivot);

        if (isRow && selectedWall != null)
        {
            List<WallSegment> row = selectedWall.GetConnectedRow();
            for (int i = 0; i < row.Count; i++)
            {
                WallSegment ws = row[i];
                if (ws == null) continue;

                MovingItemBackup item = new MovingItemBackup
                {
                    transform = ws.transform,
                    origPos = ws.transform.position,
                    origRot = ws.transform.rotation,
                    offsetFromPivot = ws.transform.position - pivot.position,
                    colliders = ws.GetComponentsInChildren<Collider>()
                };

                // Tạm tắt collider để tia raycast rơi chuẩn vào mặt đất bên dưới
                if (item.colliders != null)
                {
                    foreach (var c in item.colliders) if (c != null) c.enabled = false;
                }

                activeMovingItems.Add(item);
            }
        }
        else
        {
            MovingItemBackup item = new MovingItemBackup
            {
                transform = pivot,
                origPos = pivot.position,
                origRot = pivot.rotation,
                offsetFromPivot = Vector3.zero,
                colliders = pivot.GetComponentsInChildren<Collider>()
            };

            if (item.colliders != null)
            {
                foreach (var c in item.colliders) if (c != null) c.enabled = false;
            }

            activeMovingItems.Add(item);
        }

        // Tạm ẩn Card Panel và 3D Marker
        if (wallCardPanel != null) wallCardPanel.SetActive(false);
        if (selectionMarker != null) selectionMarker.SetActive(false);

        // Bật thanh thông báo di chuyển
        if (moveControlPanel != null)
        {
            moveControlPanel.SetActive(true);
            UpdateInstructionText(true);
        }
    }

    private void UpdateMoveMode()
    {
        if (movePivot == null)
        {
            CancelMove();
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        // Phím R: Xoay nhanh 90 độ
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateInMoveMode();
        }

        // 1. TRONG LÚC ĐANG NHẤN GIỮ CHUỘT / CHẠM TAY: Cập nhật vị trí bám đúng mặt đất
        if (Input.GetMouseButton(0) || isDragging)
        {
            isDragging = true;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 groundPoint = GetGroundPlacementPoint(ray, out bool hitGroundValid);

            if (hitGroundValid)
            {
                if (selectedWall != null)
                {
                    // Snap lưới tường bước 1.425m (đã giảm 2 lần) và hút khít vào các tường khác
                    float segLen = 1.425f;
                    Vector3 snappedPos = GetSnappedWallPosition(groundPoint, segLen);

                    // Khóa độ cao mặt đất cố định cho cả hàng tường (Tránh bậc thang)
                    float fixedY = SampleGroundY(snappedPos.x, snappedPos.z, groundPoint.y);

                    Vector3 targetPivotPos = new Vector3(snappedPos.x, fixedY, snappedPos.z);
                    movePivot.position = targetPivotPos;

                    // Di chuyển các đoạn tường khác trong hàng theo offset tương đối và cùng fixedY
                    for (int i = 0; i < activeMovingItems.Count; i++)
                    {
                        var item = activeMovingItems[i];
                        if (item.transform == null || item.transform == movePivot) continue;

                        Vector3 newPos = new Vector3(
                            targetPivotPos.x + item.offsetFromPivot.x,
                            fixedY,
                            targetPivotPos.z + item.offsetFromPivot.z
                        );
                        item.transform.position = newPos;
                    }
                }
                else if (selectedTurret != null)
                {
                    // Snap nhẹ ụ pháo theo lưới 1.0m
                    float snapX = Mathf.Round(groundPoint.x);
                    float snapZ = Mathf.Round(groundPoint.z);
                    float groundY = SampleGroundY(snapX, snapZ, groundPoint.y);

                    movePivot.position = new Vector3(snapX, groundY, snapZ);
                }
                else if (selectedFactory != null)
                {
                    // Snap nhẹ Trại Huấn Luyện Quân theo lưới 1.0m
                    float snapX = Mathf.Round(groundPoint.x);
                    float snapZ = Mathf.Round(groundPoint.z);
                    float groundY = SampleGroundY(snapX, snapZ, groundPoint.y);

                    movePivot.position = new Vector3(snapX, groundY, snapZ);
                }
            }

            // Kiểm tra tính hợp lệ: Phải trúng mặt đất đảo VÀ không đè lên vật khác
            isPlacementValid = hitGroundValid && CheckPlacementValidity();

            // Hiển thị màu đỏ nếu vị trí KHÔNG PHÙ HỢP, hoặc hiển thị màu bình thường nếu HỢP LỆ
            ApplyMovingVisuals(isPlacementValid);
            UpdateInstructionText(isPlacementValid);
        }

        // 2. KHI THẢ TAY RA (MOUSE UP):
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            if (isPlacementValid)
            {
                // ĐẶT THÀNH CÔNG -> ĐỨNG YÊN TẠI VỊ TRÍ NÀY
                ConfirmMove();
            }
            else
            {
                // VỊ TRÍ KHÔNG PHÙ HỢP -> TỰ ĐỘNG QUAY VỀ VỊ TRÍ BAN ĐẦU
                CancelMove();
            }
        }
    }

    private void UpdateInstructionText(bool isValid)
    {
        if (moveInstructionText == null) return;

        if (isValid)
        {
            moveInstructionText.text = isMovingRow
                ? $"<color=#00FF88><b>✅ VỊ TRÍ HỢP LỆ (HÀNG TƯỜNG)</b></color>\n<i>Thả tay ra để đặt đứng yên | Bấm [XOAY] hoặc phím R để xoay hướng</i>"
                : $"<color=#00FF88><b>✅ VỊ TRÍ HỢP LỆ</b></color>\n<i>Thả tay ra để đặt công trình đứng yên | Bấm [XOAY] hoặc phím R để xoay</i>";
        }
        else
        {
            moveInstructionText.text = $"<color=#FF3333><b>❌ VỊ TRÍ KHÔNG PHÙ HỢP (MÀU ĐỎ)</b></color>\n<i>Thả tay ra sẽ tự động quay về vị trí ban đầu!</i>";
        }
    }

    /// <summary>
    /// Kiểm tra xem vị trí đặt hiện tại có hợp lệ 100% không:
    /// - Phải nằm trên mặt đất cao nguyên đảo (Y >= 7.0m, dốc phẳng).
    /// - Không đè lên các bức tường khác, ụ pháo khác, Player, robot.
    /// </summary>
    private bool CheckPlacementValidity()
    {
        if (activeMovingItems.Count == 0 || movePivot == null) return false;

        for (int i = 0; i < activeMovingItems.Count; i++)
        {
            var item = activeMovingItems[i];
            if (item.transform == null) continue;

            Vector3 pos = item.transform.position;

            // 1. Kiểm tra mặt đất: Bắn tia thẳng đứng từ cao xuống
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(pos.x, 100f, pos.z), Vector3.down, 200f);
            bool hasValidGround = false;
            float gY = 0f;
            Vector3 gNorm = Vector3.up;

            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
                foreach (var h in hits)
                {
                    if (h.collider == null || h.collider.isTrigger) continue;
                    if (IsItemBeingMoved(h.collider.transform)) continue;
                    if (h.collider.GetComponentInParent<WorkerBot>() != null) continue;

                    if (h.collider is TerrainCollider || h.collider.GetComponent<Terrain>() != null ||
                        h.collider.gameObject.name.ToLower().Contains("terrain") ||
                        h.collider.gameObject.name.ToLower().Contains("ground") ||
                        h.collider.gameObject.name.ToLower().Contains("island"))
                    {
                        hasValidGround = true;
                        gY = h.point.y;
                        gNorm = h.normal;
                        break;
                    }
                }
            }

            // Nếu không có đất hoặc rơi xuống biển (dưới 7m) -> KHÔNG HỢP LỆ
            if (!hasValidGround || gY < 7.0f)
            {
                return false;
            }

            // Nếu mặt đất dốc quá mức (vực dốc đứng) -> KHÔNG HỢP LỆ
            if (gNorm.y < 0.82f)
            {
                return false;
            }

            // Độ chênh lệch giữa vị trí vật thể và mặt đất (không được bay lơ lửng)
            if (Mathf.Abs(pos.y - gY) > 1.35f)
            {
                return false;
            }

            // 2. Kiểm tra đè lên công trình khác:
            // A. Đè lên Bức Tường KHÁC (cố định trên bản đồ)
            for (int w = 0; w < WallSegment.AllWalls.Count; w++)
            {
                WallSegment otherWall = WallSegment.AllWalls[w];
                if (otherWall == null || IsItemBeingMoved(otherWall.transform)) continue;

                float distXZ = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(otherWall.transform.position.x, otherWall.transform.position.z));
                // Nếu khoảng cách tâm < 0.98m là đang đè trùng lên nhau (đã giảm tỷ lệ cho tường nhỏ)
                if (distXZ < 0.98f)
                {
                    return false;
                }
            }

            // B. Đè lên Ụ Pháo KHÁC
            for (int tIdx = 0; tIdx < UpgradableTurret.AllTurrets.Count; tIdx++)
            {
                UpgradableTurret otherTurret = UpgradableTurret.AllTurrets[tIdx];
                if (otherTurret == null || IsItemBeingMoved(otherTurret.transform)) continue;

                float distXZ = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(otherTurret.transform.position.x, otherTurret.transform.position.z));
                if (distXZ < 2.3f)
                {
                    return false;
                }
            }

            // C. Đè lên Player hoặc chạm gần Player (Phát hiện siêu nhạy đa tầng)
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                float distXZ = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(player.transform.position.x, player.transform.position.z));
                float playerSafetyRadius = (selectedWall != null) ? 1.5f : (selectedFactory != null ? 3.0f : 2.6f);
                if (distXZ < playerSafetyRadius)
                {
                    return false; // DÍNH PLAYER -> BÁO LỖI (MÀU ĐỎ)!
                }
            }

            // Quét kiểm tra va chạm hộp 3D thực tế quanh từng đoạn tường / trụ / trại lính với Player
            Vector3 boxCenter = pos + Vector3.up * 1.2f;
            Vector3 halfExtents = (selectedWall != null) ? new Vector3(0.95f, 1.1f, 0.95f) :
                                  (selectedFactory != null) ? new Vector3(1.8f, 1.8f, 2.0f) :
                                  new Vector3(1.6f, 2.2f, 1.6f);
            Collider[] hitCols = Physics.OverlapBox(boxCenter, halfExtents, item.transform.rotation);
            if (hitCols != null)
            {
                for (int cIdx = 0; cIdx < hitCols.Length; cIdx++)
                {
                    Collider col = hitCols[cIdx];
                    if (col == null || col.isTrigger) continue;
                    if (col.CompareTag("Player") || col.GetComponentInParent<PlayerController>() != null || col is CharacterController)
                    {
                        return false; // DÍNH PLAYER -> BÁO LỖI (MÀU ĐỎ)!
                    }
                }
            }

            // Quét kiểm tra tất cả GameObject có Tag "Player"
            GameObject[] pObjs = GameObject.FindGameObjectsWithTag("Player");
            if (pObjs != null)
            {
                for (int pIdx = 0; pIdx < pObjs.Length; pIdx++)
                {
                    if (pObjs[pIdx] == null) continue;
                    float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(pObjs[pIdx].transform.position.x, pObjs[pIdx].transform.position.z));
                    float r = (selectedWall != null) ? 1.5f : (selectedFactory != null ? 3.0f : 2.6f);
                    if (d < r)
                    {
                        return false; // DÍNH PLAYER -> BÁO LỖI (MÀU ĐỎ)!
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Đổi màu sắc hiển thị của công trình: MÀU ĐỎ nếu không phù hợp, MÀU BÌNH THƯỜNG nếu hợp lệ
    /// </summary>
    private void ApplyMovingVisuals(bool isValid)
    {
        if (!isValid)
        {
            if (redWarningPropBlock == null)
            {
                redWarningPropBlock = new MaterialPropertyBlock();
                Color warningRed = new Color(1.0f, 0.12f, 0.12f, 0.95f);
                redWarningPropBlock.SetColor("_BaseColor", warningRed);
                redWarningPropBlock.SetColor("_Color", warningRed);
                redWarningPropBlock.SetColor("_EmissionColor", new Color(0.85f, 0.05f, 0.05f, 1f));
                redWarningPropBlock.SetFloat("_Metallic", 0.1f);
                redWarningPropBlock.SetFloat("_Smoothness", 0.4f);
            }

            for (int i = 0; i < activeMovingItems.Count; i++)
            {
                var item = activeMovingItems[i];
                if (item.transform == null) continue;

                Renderer[] rends = item.transform.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (r == null || r is LineRenderer || r is ParticleSystemRenderer) continue;
                    r.SetPropertyBlock(redWarningPropBlock);
                }
            }
        }
        else
        {
            // Vị trí hợp lệ: Nhấp nháy màu xanh dương rồi trở về màu cũ liên tục đến khi thả tay ra
            UpdatePressBlinking();
        }
    }

    public void RotateInMoveMode()
    {
        if (movePivot == null || !isMoveMode) return;

        movePivot.Rotate(0f, 90f, 0f, Space.World);

        if (isMovingRow)
        {
            Quaternion rot90 = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < activeMovingItems.Count; i++)
            {
                var item = activeMovingItems[i];
                if (item.transform == null) continue;

                item.offsetFromPivot = rot90 * item.offsetFromPivot;
                activeMovingItems[i] = item;

                if (item.transform != movePivot)
                {
                    item.transform.Rotate(0f, 90f, 0f, Space.World);
                    item.transform.position = new Vector3(
                        movePivot.position.x + item.offsetFromPivot.x,
                        movePivot.position.y,
                        movePivot.position.z + item.offsetFromPivot.z
                    );
                }
            }
        }

        // Cập nhật lại tính hợp lệ và màu sắc sau khi xoay
        isPlacementValid = CheckPlacementValidity();
        ApplyMovingVisuals(isPlacementValid);
        UpdateInstructionText(isPlacementValid);
    }

    public void ConfirmMove()
    {
        if (!isMoveMode) return;

        ClearPressBlinking();

        // Bật lại Collider và khôi phục màu sắc ban đầu của cấp độ
        for (int i = 0; i < activeMovingItems.Count; i++)
        {
            var item = activeMovingItems[i];
            if (item.colliders != null)
            {
                foreach (var c in item.colliders) if (c != null) c.enabled = true;
            }

            if (item.transform != null)
            {
                WallSegment ws = item.transform.GetComponent<WallSegment>();
                if (ws != null)
                {
                    ws.ApplyLevel(ws.currentLevel);
                    ws.TriggerSelectHop(i * 0.03f, 0.35f);
                }

                UpgradableTurret ut = item.transform.GetComponent<UpgradableTurret>();
                if (ut != null)
                {
                    ut.ApplyTurretVisuals(ut.currentLevel);
                    ut.TriggerPunchHop();
                }

                RobotFactory rf = item.transform.GetComponent<RobotFactory>();
                if (rf != null)
                {
                    if (rf.rallyPoint != null)
                    {
                        rf.rallyPoint.position = rf.transform.position + rf.transform.forward * 4.5f;
                    }
                }
            }
        }

        isMoveMode = false;
        isDragging = false;
        activeMovingItems.Clear();

        if (moveControlPanel != null) moveControlPanel.SetActive(false);

        // Mở lại Card UI tại vị trí mới
        if (wallCardPanel != null) wallCardPanel.SetActive(true);
        if (selectionMarker != null) selectionMarker.SetActive(false); // Ẩn hoàn toàn cube marker trên đầu theo Yêu Cầu 4

        RefreshCardUI();
        Debug.Log("<color=#00FF88>[Di Chuyển]</color> Đã đặt công trình đứng yên tại vị trí mới thành công!");
    }

    public void CancelMove()
    {
        if (!isMoveMode) return;

        ClearPressBlinking();

        // Tự động quay về vị trí lúc chưa di chuyển
        for (int i = 0; i < activeMovingItems.Count; i++)
        {
            var item = activeMovingItems[i];
            if (item.transform != null)
            {
                item.transform.position = item.origPos;
                item.transform.rotation = item.origRot;

                WallSegment ws = item.transform.GetComponent<WallSegment>();
                if (ws != null)
                {
                    ws.ApplyLevel(ws.currentLevel);
                    ws.TriggerSelectHop(0f, 0.25f);
                }

                UpgradableTurret ut = item.transform.GetComponent<UpgradableTurret>();
                if (ut != null)
                {
                    ut.ApplyTurretVisuals(ut.currentLevel);
                    ut.TriggerPunchHop();
                }
            }

            if (item.colliders != null)
            {
                foreach (var c in item.colliders) if (c != null) c.enabled = true;
            }
        }

        isMoveMode = false;
        isDragging = false;
        activeMovingItems.Clear();

        if (moveControlPanel != null) moveControlPanel.SetActive(false);

        if (wallCardPanel != null) wallCardPanel.SetActive(true);
        if (selectionMarker != null) selectionMarker.SetActive(false);

        RefreshCardUI();
        Debug.Log("<color=yellow>[Di Chuyển]</color> Vị trí không phù hợp! Công trình đã tự động quay về vị trí ban đầu.");
    }

    public void OnRotateClicked()
    {
        if (selectedWall != null)
        {
            selectedWall.Rotate90Degrees();
        }
        else if (selectedTurret != null)
        {
            selectedTurret.Rotate90Degrees();
        }
        else if (selectedFactory != null)
        {
            selectedFactory.transform.Rotate(0f, 90f, 0f, Space.World);
        }
    }

    // ================= TIỆN ÍCH TÍNH TOÁN RAYCAST & SNAP =================

    private Vector3 GetGroundPlacementPoint(Ray ray, out bool hasHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 350f);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (IsItemBeingMoved(h.collider.transform)) continue;
                if (h.collider.GetComponentInParent<WorkerBot>() != null) continue;

                // Chỉ nhận khi va chạm trúng Terrain hoặc mặt đất hợp lệ
                if (h.collider is TerrainCollider || h.collider.GetComponent<Terrain>() != null ||
                    h.collider.gameObject.name.ToLower().Contains("terrain") ||
                    h.collider.gameObject.name.ToLower().Contains("ground") ||
                    h.collider.gameObject.name.ToLower().Contains("island"))
                {
                    // Mặt đất đảo phải cao từ 7m trở lên (tránh đáy biển và mép vực)
                    if (h.point.y >= 7.0f)
                    {
                        hasHit = true;
                        return h.point;
                    }
                }
            }
        }
        hasHit = false;
        return Vector3.zero;
    }

    private bool IsItemBeingMoved(Transform t)
    {
        for (int i = 0; i < activeMovingItems.Count; i++)
        {
            if (activeMovingItems[i].transform == t || t.IsChildOf(activeMovingItems[i].transform))
                return true;
        }
        return false;
    }

    private Vector3 GetSnappedWallPosition(Vector3 rawPoint, float segLen)
    {
        WallSegment nearest = null;
        float minDist = 1.8f;

        for (int i = 0; i < WallSegment.AllWalls.Count; i++)
        {
            WallSegment ws = WallSegment.AllWalls[i];
            if (ws == null || IsItemBeingMoved(ws.transform)) continue;

            float dist = Vector3.Distance(rawPoint, ws.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = ws;
            }
        }

        if (nearest != null)
        {
            Vector3 nearPos = nearest.transform.position;
            Vector3 delta = rawPoint - nearPos;
            float nearRotY = nearest.transform.eulerAngles.y;
            bool nearIsHorizontal = (Mathf.Abs(nearRotY - 0f) < 25f || Mathf.Abs(nearRotY - 180f) < 25f);

            Vector3 forwardDir = nearIsHorizontal ? Vector3.right : Vector3.forward;
            Vector3 sideDir = nearIsHorizontal ? Vector3.forward : Vector3.right;

            float forwardProj = Vector3.Dot(delta, forwardDir);
            float sideProj = Vector3.Dot(delta, sideDir);

            if (Mathf.Abs(forwardProj) >= Mathf.Abs(sideProj))
            {
                float sign = Mathf.Sign(forwardProj);
                return new Vector3(nearPos.x + forwardDir.x * segLen * sign, nearPos.y, nearPos.z + forwardDir.z * segLen * sign);
            }
            else
            {
                float sign = Mathf.Sign(sideProj);
                return new Vector3(nearPos.x + sideDir.x * segLen * sign, nearPos.y, nearPos.z + sideDir.z * sign * segLen);
            }
        }

        float sx = Mathf.Round(rawPoint.x / segLen) * segLen;
        float sz = Mathf.Round(rawPoint.z / segLen) * segLen;
        return new Vector3(sx, rawPoint.y, sz);
    }

    private float SampleGroundY(float x, float z, float defaultY)
    {
        Terrain t = Terrain.activeTerrain ?? FindAnyObjectByType<Terrain>();
        if (t != null)
        {
            Vector3 samplePos = new Vector3(x, 0, z);
            Vector3 tPos = t.transform.position;
            Vector3 tSize = t.terrainData.size;
            if (x >= tPos.x && x <= tPos.x + tSize.x && z >= tPos.z && z <= tPos.z + tSize.z)
            {
                return t.SampleHeight(samplePos) + tPos.y;
            }
        }

        RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 100f, z), Vector3.down, 200f);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (IsItemBeingMoved(h.collider.transform)) continue;
                if (h.collider.GetComponentInParent<WorkerBot>() != null) continue;

                return h.point.y;
            }
        }
        return defaultY;
    }

    private void UpdateSelectionMarkerAnimation()
    {
        if (selectionMarker != null && selectionMarker.activeSelf)
        {
            Vector3 targetPos = Vector3.zero;
            if (selectedWall != null)
            {
                targetPos = selectedWall.transform.position + Vector3.up * 2.2f;
            }
            else if (selectedTurret != null)
            {
                targetPos = selectedTurret.GetMarkerPosition();
            }

            if (targetPos != Vector3.zero)
            {
                float bob = Mathf.Sin(Time.time * 5.5f) * 0.14f;
                selectionMarker.transform.position = targetPos + Vector3.up * bob;
                selectionMarker.transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
            }
        }
    }

    private void CreateSelectionMarker()
    {
        if (selectionMarker != null) return;

        selectionMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectionMarker.name = "[Building_Selection_Marker]";
        selectionMarker.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        selectionMarker.transform.rotation = Quaternion.Euler(45f, 45f, 45f);

        Collider col = selectionMarker.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = selectionMarker.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = new Color(0.15f, 0.85f, 1f, 0.95f);
            rend.material = mat;
        }

        selectionMarker.SetActive(false);
    }

    // ================= TỰ ĐỘNG KHỞI TẠO GIAO DIỆN UI (CANVAS) =================

    private void EnsureUIElementsCreated()
    {
        if (wallCardPanel != null && moveControlPanel != null)
        {
            HookButtonEvents();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = GetMainScreenCanvas();
        }
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // ---------------- 1. CARD NÂNG CẤP & THAO TÁC CHÍNH ----------------
        if (wallCardPanel == null)
        {
            wallCardPanel = new GameObject("Panel_WallUpgradeCard", typeof(RectTransform));
            wallCardPanel.layer = LayerMask.NameToLayer("UI");
            wallCardPanel.transform.SetParent(canvas.transform, false);

            RectTransform cardRect = wallCardPanel.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0f);
            cardRect.anchorMax = new Vector2(0.5f, 0f);
            cardRect.pivot = new Vector2(0.5f, 0f);
            cardRect.anchoredPosition = new Vector2(0f, 130f);
            cardRect.sizeDelta = new Vector2(780f, 275f);

            Image cardBg = wallCardPanel.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.12f, 0.17f, 0.96f);

            Outline cardOutline = wallCardPanel.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.18f, 0.75f, 1f, 0.85f);
            cardOutline.effectDistance = new Vector2(2f, -2f);

            // Tiêu đề
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
            titleObj.layer = LayerMask.NameToLayer("UI");
            titleObj.transform.SetParent(wallCardPanel.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 1f);
            tRect.anchorMax = new Vector2(1f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0f, -10f);
            tRect.sizeDelta = new Vector2(-40f, 34f);

            titleText = titleObj.AddComponent<Text>();
            titleText.font = safeFont;
            titleText.fontSize = 23;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 0.88f, 0.2f, 1f);
            titleText.supportRichText = true;
            titleText.raycastTarget = false;

            // Phụ đề
            GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform));
            subObj.layer = LayerMask.NameToLayer("UI");
            subObj.transform.SetParent(wallCardPanel.transform, false);
            RectTransform sRect = subObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -44f);
            sRect.sizeDelta = new Vector2(-40f, 28f);

            subtitleText = subObj.AddComponent<Text>();
            subtitleText.font = safeFont;
            subtitleText.fontSize = 16;
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            subtitleText.supportRichText = true;
            subtitleText.raycastTarget = false;

            // HÀNG NÚT TRÊN (NÂNG CẤP, SỬA CHỮA & ĐÓNG)
            GameObject topRowObj = new GameObject("ButtonRowTop", typeof(RectTransform));
            topRowObj.layer = LayerMask.NameToLayer("UI");
            topRowObj.transform.SetParent(wallCardPanel.transform, false);
            RectTransform topRowRect = topRowObj.GetComponent<RectTransform>();
            topRowRect.anchorMin = new Vector2(0.5f, 0f);
            topRowRect.anchorMax = new Vector2(0.5f, 0f);
            topRowRect.pivot = new Vector2(0.5f, 0f);
            topRowRect.anchoredPosition = new Vector2(0f, 96f);
            topRowRect.sizeDelta = new Vector2(750f, 85f);

            HorizontalLayoutGroup topHlg = topRowObj.AddComponent<HorizontalLayoutGroup>();
            topHlg.spacing = 10f;
            topHlg.childAlignment = TextAnchor.MiddleCenter;
            topHlg.childControlWidth = false;
            topHlg.childControlHeight = false;
            topHlg.childForceExpandWidth = false;
            topHlg.childForceExpandHeight = false;

            upgradeSingleButton = CreateCardActionButton(topRowObj.transform, "Btn_UpgradeSingle", new Vector2(210f, 80f), new Color(0.85f, 0.55f, 0.12f), safeFont, out upgradeSingleText);
            upgradeRowButton = CreateCardActionButton(topRowObj.transform, "Btn_UpgradeRow", new Vector2(240f, 80f), new Color(0.15f, 0.55f, 0.88f), safeFont, out upgradeRowText);
            repairButton = CreateCardActionButton(topRowObj.transform, "Btn_Repair", new Vector2(200f, 80f), new Color(0.18f, 0.65f, 0.38f), safeFont, out repairText);
            closeCardButton = CreateCardActionButton(topRowObj.transform, "Btn_CloseCard", new Vector2(65f, 80f), new Color(0.55f, 0.2f, 0.2f), safeFont, out Text closeTxt);
            closeTxt.text = "<b>✕</b>";
            closeTxt.fontSize = 28;

            // HÀNG NÚT DƯỚI (DI CHUYỂN & XOAY)
            actionRowObj = new GameObject("ButtonRowBottom", typeof(RectTransform));
            actionRowObj.layer = LayerMask.NameToLayer("UI");
            actionRowObj.transform.SetParent(wallCardPanel.transform, false);
            RectTransform btmRowRect = actionRowObj.GetComponent<RectTransform>();
            btmRowRect.anchorMin = new Vector2(0.5f, 0f);
            btmRowRect.anchorMax = new Vector2(0.5f, 0f);
            btmRowRect.pivot = new Vector2(0.5f, 0f);
            btmRowRect.anchoredPosition = new Vector2(0f, 14f);
            btmRowRect.sizeDelta = new Vector2(700f, 72f);

            HorizontalLayoutGroup btmHlg = actionRowObj.AddComponent<HorizontalLayoutGroup>();
            btmHlg.spacing = 12f;
            btmHlg.childAlignment = TextAnchor.MiddleCenter;
            btmHlg.childControlWidth = false;
            btmHlg.childControlHeight = false;
            btmHlg.childForceExpandWidth = false;
            btmHlg.childForceExpandHeight = false;

            moveSingleButton = CreateCardActionButton(actionRowObj.transform, "Btn_MoveSingle", new Vector2(210f, 68f), new Color(0.18f, 0.65f, 0.55f), safeFont, out moveSingleText);
            moveSingleText.text = "📦 <b>DỜI 1 ĐOẠN</b>";

            moveRowButton = CreateCardActionButton(actionRowObj.transform, "Btn_MoveRow", new Vector2(260f, 68f), new Color(0.20f, 0.45f, 0.85f), safeFont, out moveRowText);
            moveRowText.text = "🚛 <b>DỜI CẢ HÀNG</b>";

            rotateButton = CreateCardActionButton(actionRowObj.transform, "Btn_Rotate", new Vector2(170f, 68f), new Color(0.55f, 0.35f, 0.80f), safeFont, out rotateText);
            rotateText.text = "🔄 <b>XOAY 90°</b>";
        }

        // ---------------- 2. PANEL ĐIỀU KHIỂN CHẾ ĐỘ DI CHUYỂN (DRAG & DROP) ----------------
        if (moveControlPanel == null)
        {
            moveControlPanel = new GameObject("Panel_MoveControls", typeof(RectTransform));
            moveControlPanel.layer = LayerMask.NameToLayer("UI");
            moveControlPanel.transform.SetParent(canvas.transform, false);

            RectTransform moveRect = moveControlPanel.GetComponent<RectTransform>();
            moveRect.anchorMin = new Vector2(0.5f, 0f);
            moveRect.anchorMax = new Vector2(0.5f, 0f);
            moveRect.pivot = new Vector2(0.5f, 0f);
            moveRect.anchoredPosition = new Vector2(0f, 130f);
            moveRect.sizeDelta = new Vector2(740f, 165f);

            Image moveBg = moveControlPanel.AddComponent<Image>();
            moveBg.color = new Color(0.08f, 0.10f, 0.15f, 0.96f);

            Outline moveOutline = moveControlPanel.AddComponent<Outline>();
            moveOutline.effectColor = new Color(0.15f, 0.95f, 0.65f, 0.85f);
            moveOutline.effectDistance = new Vector2(2f, -2f);

            // Nhãn hướng dẫn di chuyển & trạng thái màu sắc
            GameObject instrObj = new GameObject("InstructionText", typeof(RectTransform));
            instrObj.layer = LayerMask.NameToLayer("UI");
            instrObj.transform.SetParent(moveControlPanel.transform, false);
            RectTransform inRect = instrObj.GetComponent<RectTransform>();
            inRect.anchorMin = new Vector2(0f, 1f);
            inRect.anchorMax = new Vector2(1f, 1f);
            inRect.pivot = new Vector2(0.5f, 1f);
            inRect.anchoredPosition = new Vector2(0f, -8f);
            inRect.sizeDelta = new Vector2(-40f, 64f);

            moveInstructionText = instrObj.AddComponent<Text>();
            moveInstructionText.font = safeFont;
            moveInstructionText.fontSize = 17;
            moveInstructionText.alignment = TextAnchor.MiddleCenter;
            moveInstructionText.color = new Color(1f, 0.95f, 0.7f, 1f);
            moveInstructionText.supportRichText = true;
            moveInstructionText.raycastTarget = false;

            // Hàng nút Xoay và Hủy
            GameObject ctrlRowObj = new GameObject("MoveControlRow", typeof(RectTransform));
            ctrlRowObj.layer = LayerMask.NameToLayer("UI");
            ctrlRowObj.transform.SetParent(moveControlPanel.transform, false);
            RectTransform ctrlRowRect = ctrlRowObj.GetComponent<RectTransform>();
            ctrlRowRect.anchorMin = new Vector2(0.5f, 0f);
            ctrlRowRect.anchorMax = new Vector2(0.5f, 0f);
            ctrlRowRect.pivot = new Vector2(0.5f, 0f);
            ctrlRowRect.anchoredPosition = new Vector2(0f, 12f);
            ctrlRowRect.sizeDelta = new Vector2(700f, 75f);

            HorizontalLayoutGroup ctrlHlg = ctrlRowObj.AddComponent<HorizontalLayoutGroup>();
            ctrlHlg.spacing = 20f;
            ctrlHlg.childAlignment = TextAnchor.MiddleCenter;
            ctrlHlg.childControlWidth = false;
            ctrlHlg.childControlHeight = false;
            ctrlHlg.childForceExpandWidth = false;
            ctrlHlg.childForceExpandHeight = false;

            rotateInMoveButton = CreateCardActionButton(ctrlRowObj.transform, "Btn_RotateInMove", new Vector2(320f, 70f), new Color(0.15f, 0.55f, 0.88f), safeFont, out Text rotTxt);
            rotTxt.text = "🔄 <b>XOAY 90° (Phím R)</b>";
            rotTxt.fontSize = 19;

            cancelMoveButton = CreateCardActionButton(ctrlRowObj.transform, "Btn_CancelMove", new Vector2(240f, 70f), new Color(0.68f, 0.22f, 0.22f), safeFont, out Text cancelTxt);
            cancelTxt.text = "❌ <b>HỦY BỎ</b>";
            cancelTxt.fontSize = 19;

            moveControlPanel.SetActive(false);
        }

        HookButtonEvents();
    }

    private Button CreateCardActionButton(Transform parent, string name, Vector2 size, Color bgColor, Font font, out Text label)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.layer = LayerMask.NameToLayer("UI");
        btnObj.transform.SetParent(parent, false);

        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = bgColor;

        Outline ol = btnObj.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.4f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.15f;
        cb.pressedColor = bgColor * 0.8f;
        cb.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.layer = LayerMask.NameToLayer("UI");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tr = txtObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        label = txtObj.AddComponent<Text>();
        label.font = font;
        label.fontSize = 17;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.supportRichText = true;
        label.raycastTarget = false; // Đảm bảo sự kiện click chuột/chạm luôn rơi thẳng vào Button

        return btn;
    }

    private void HookButtonEvents()
    {
        if (upgradeSingleButton != null)
        {
            upgradeSingleButton.onClick.RemoveAllListeners();
            upgradeSingleButton.onClick.AddListener(OnUpgradeSingleClicked);
        }

        if (upgradeRowButton != null)
        {
            upgradeRowButton.onClick.RemoveAllListeners();
            upgradeRowButton.onClick.AddListener(OnUpgradeRowClicked);
        }

        if (repairButton != null)
        {
            repairButton.onClick.RemoveAllListeners();
            repairButton.onClick.AddListener(OnRepairClicked);
        }

        if (closeCardButton != null)
        {
            closeCardButton.onClick.RemoveAllListeners();
            closeCardButton.onClick.AddListener(DeselectAll);
        }

        if (moveSingleButton != null)
        {
            moveSingleButton.onClick.RemoveAllListeners();
            moveSingleButton.onClick.AddListener(StartMoveSingle);
        }

        if (moveRowButton != null)
        {
            moveRowButton.onClick.RemoveAllListeners();
            moveRowButton.onClick.AddListener(StartMoveRow);
        }

        if (rotateButton != null)
        {
            rotateButton.onClick.RemoveAllListeners();
            rotateButton.onClick.AddListener(OnRotateClicked);
        }

        if (rotateInMoveButton != null)
        {
            rotateInMoveButton.onClick.RemoveAllListeners();
            rotateInMoveButton.onClick.AddListener(RotateInMoveMode);
        }

        if (cancelMoveButton != null)
        {
            cancelMoveButton.onClick.RemoveAllListeners();
            cancelMoveButton.onClick.AddListener(CancelMove);
        }
    }
}
