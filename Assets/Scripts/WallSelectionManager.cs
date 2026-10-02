using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý tương tác click vào từng đoạn tường đã xây trên Scene để nâng cấp riêng (hoặc nâng cả hàng)
/// chuẩn phong cách Clash of Clans, thiết kế tối ưu cho màn hình dọc (Mobile Portrait).
/// </summary>
public class WallSelectionManager : MonoBehaviour
{
    public static WallSelectionManager Instance { get; private set; }

    [Header("--- Tham Chiếu Hệ Thống ---")]
    public BuildingSystem buildingSystem;
    public BuildingShopUI shopUI;

    [Header("--- Tường Đang Được Chọn ---")]
    public WallSegment selectedWall;

    [Header("--- Giao Diện Nâng Cấp Tường (Clash of Clans Style) ---")]
    public GameObject wallCardPanel;
    public Text titleText;
    public Text subtitleText;
    public Button upgradeSingleButton;
    public Text upgradeSingleText;
    public Button upgradeRowButton;
    public Text upgradeRowText;
    public Button closeCardButton;

    // 3D Marker lơ lửng trên đoạn tường đang chọn
    private GameObject selectionMarker;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
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

        DeselectWall();
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
        // 1. Cập nhật vị trí và animation nhấp nhô của 3D Marker
        UpdateSelectionMarkerAnimation();

        // 2. Không nhận click chọn tường nếu đang trong chế độ kéo đặt công trình
        if (buildingSystem != null && buildingSystem.IsPlacing)
        {
            if (selectedWall != null) DeselectWall();
            return;
        }

        // 3. Nhận diện click chuột trái / chạm màn hình
        if (Input.GetMouseButtonDown(0))
        {
            // Bỏ qua nếu đang click trên giao diện UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Raycast từ Camera vào thế giới 3D
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 250f))
            {
                WallSegment clickedWall = hit.collider.GetComponentInParent<WallSegment>();
                if (clickedWall != null)
                {
                    SelectWall(clickedWall);
                    return;
                }
            }

            // Click vào khoảng trống hoặc mặt đất -> Bỏ chọn tường
            if (selectedWall != null)
            {
                DeselectWall();
            }
        }
    }

    /// <summary>
    /// Chọn một đoạn tường trên scene và hiển thị giao diện nâng cấp Clash of Clans
    /// </summary>
    public void SelectWall(WallSegment wall)
    {
        if (wall == null) return;

        selectedWall = wall;

        // Đóng menu Cửa hàng nếu đang mở để người chơi tập trung nâng cấp tường
        if (shopUI != null && shopUI.shopPanel != null && shopUI.shopPanel.activeSelf)
        {
            shopUI.CloseShopPanel();
        }

        if (wallCardPanel != null)
        {
            wallCardPanel.SetActive(true);
        }

        if (selectionMarker != null)
        {
            selectionMarker.SetActive(true);
            selectionMarker.transform.position = wall.transform.position + Vector3.up * 2.3f;
        }

        RefreshCardUI();
    }

    /// <summary>
    /// Bỏ chọn tường và ẩn giao diện
    /// </summary>
    public void DeselectWall()
    {
        selectedWall = null;

        if (wallCardPanel != null)
        {
            wallCardPanel.SetActive(false);
        }

        if (selectionMarker != null)
        {
            selectionMarker.SetActive(false);
        }
    }

    /// <summary>
    /// Cập nhật thông tin chi tiết trên Card nâng cấp tường
    /// </summary>
    public void RefreshCardUI()
    {
        if (selectedWall == null)
        {
            if (wallCardPanel != null && wallCardPanel.activeSelf) wallCardPanel.SetActive(false);
            if (selectionMarker != null && selectionMarker.activeSelf) selectionMarker.SetActive(false);
            return;
        }

        int lvl = selectedWall.currentLevel;
        string curName = (GameEconomy.WallLevelNames != null && lvl >= 1 && lvl <= GameEconomy.WallLevelNames.Length)
            ? GameEconomy.WallLevelNames[lvl - 1]
            : $"Cấp {lvl}";

        // Tiêu đề
        if (titleText != null)
        {
            titleText.text = $"🧱 <b>BỨC TƯỜNG (CẤP {lvl} / 6)</b>";
        }

        // Nâng cấp 1 đoạn đơn
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
                upgradeSingleText.text = $"⭐ <b>NÂNG CẤP</b>\n<color=#FFD700>{singleCost}🪙</color>";
            }

            if (upgradeSingleButton != null)
            {
                upgradeSingleButton.interactable = true;
            }
        }
        else
        {
            if (subtitleText != null)
            {
                subtitleText.text = $"👑 <color=#E0B0FF>ĐÃ ĐẠT CẤP ĐEN TITAN CỰC PHẨM (MAX)</color>";
            }

            if (upgradeSingleText != null)
            {
                upgradeSingleText.text = $"👑 <b>CẤP TỐI ĐA</b>\n<color=#AAAAAA>(MAX)</color>";
            }

            if (upgradeSingleButton != null)
            {
                upgradeSingleButton.interactable = false;
            }
        }

        // Nâng cấp cả hàng kết nối (Clash of Clans Row Upgrade)
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
                upgradeRowText.text = $"⚡ <b>NÂNG CẢ HÀNG ({upgradeableCount})</b>\n<color=#FFD700>{rowTotalCost}🪙</color>";
                if (upgradeRowButton != null) upgradeRowButton.interactable = true;
            }
            else
            {
                upgradeRowText.text = $"⚡ <b>CẢ HÀNG ({connectedRow.Count})</b>\n<color=#AAAAAA>(ĐÃ MAX)</color>";
                if (upgradeRowButton != null) upgradeRowButton.interactable = false;
            }
        }
    }

    private void OnUpgradeSingleClicked()
    {
        if (selectedWall == null) return;

        bool success = selectedWall.UpgradeSingle();
        if (success)
        {
            RefreshCardUI();
        }
    }

    private void OnUpgradeRowClicked()
    {
        if (selectedWall == null) return;

        bool success = selectedWall.UpgradeConnectedRow();
        if (success)
        {
            RefreshCardUI();
        }
    }

    private void UpdateSelectionMarkerAnimation()
    {
        if (selectionMarker != null && selectionMarker.activeSelf && selectedWall != null)
        {
            float bob = Mathf.Sin(Time.time * 5.5f) * 0.12f;
            selectionMarker.transform.position = selectedWall.transform.position + Vector3.up * (2.2f + bob);
            selectionMarker.transform.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
        }
    }

    private void CreateSelectionMarker()
    {
        if (selectionMarker != null) return;

        selectionMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        selectionMarker.name = "[Wall_Selection_Marker]";
        selectionMarker.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        selectionMarker.transform.rotation = Quaternion.Euler(45f, 45f, 45f);

        // Bỏ collider để không cản trở raycast chuột
        Collider col = selectionMarker.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Gán vật liệu phát sáng màu vàng/cyan
        Renderer rend = selectionMarker.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            rend.material = mat;
        }

        selectionMarker.SetActive(false);
    }

    /// <summary>
    /// Tự động khởi tạo Giao diện Card Nâng Cấp Tường (Mobile Portrait 9:16) nếu chưa có sẵn
    /// </summary>
    private void EnsureUIElementsCreated()
    {
        if (wallCardPanel != null)
        {
            HookButtonEvents();
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (safeFont == null) safeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Card Panel chính (Nằm ngay trên khu vực Thumb zone, kích thước vừa vặn chuẩn 9:16)
        wallCardPanel = new GameObject("Panel_WallUpgradeCard", typeof(RectTransform));
        wallCardPanel.layer = LayerMask.NameToLayer("UI");
        wallCardPanel.transform.SetParent(canvas.transform, false);

        RectTransform cardRect = wallCardPanel.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 150f);
        cardRect.sizeDelta = new Vector2(720f, 210f);

        Image cardBg = wallCardPanel.AddComponent<Image>();
        cardBg.color = new Color(0.11f, 0.13f, 0.18f, 0.96f);

        Outline cardOutline = wallCardPanel.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.75f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // 2. Tiêu đề (BỨC TƯỜNG CẤP X/6)
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform));
        titleObj.layer = LayerMask.NameToLayer("UI");
        titleObj.transform.SetParent(wallCardPanel.transform, false);
        RectTransform tRect = titleObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 1f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -12f);
        tRect.sizeDelta = new Vector2(-40f, 36f);

        titleText = titleObj.AddComponent<Text>();
        titleText.font = safeFont;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.2f, 1f);
        titleText.supportRichText = true;

        // 3. Phụ đề (Cấp hiện tại -> Cấp kế tiếp)
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform));
        subObj.layer = LayerMask.NameToLayer("UI");
        subObj.transform.SetParent(wallCardPanel.transform, false);
        RectTransform sRect = subObj.GetComponent<RectTransform>();
        sRect.anchorMin = new Vector2(0f, 1f);
        sRect.anchorMax = new Vector2(1f, 1f);
        sRect.pivot = new Vector2(0.5f, 1f);
        sRect.anchoredPosition = new Vector2(0f, -48f);
        sRect.sizeDelta = new Vector2(-40f, 28f);

        subtitleText = subObj.AddComponent<Text>();
        subtitleText.font = safeFont;
        subtitleText.fontSize = 17;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.color = new Color(0.85f, 0.95f, 1f, 0.95f);
        subtitleText.supportRichText = true;

        // 4. Hàng nút bấm (Row Container)
        GameObject btnRowObj = new GameObject("ButtonRow", typeof(RectTransform));
        btnRowObj.layer = LayerMask.NameToLayer("UI");
        btnRowObj.transform.SetParent(wallCardPanel.transform, false);
        RectTransform rowRect = btnRowObj.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 18f);
        rowRect.sizeDelta = new Vector2(670f, 95f);

        HorizontalLayoutGroup hlg = btnRowObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // Nút 1: ⭐ Nâng Cấp (Đoạn này)
        upgradeSingleButton = CreateCardActionButton(btnRowObj.transform, "Btn_UpgradeSingle", new Vector2(230f, 85f), new Color(0.85f, 0.55f, 0.12f), safeFont, out upgradeSingleText);

        // Nút 2: ⚡ Nâng Cả Hàng (Clash of Clans style)
        upgradeRowButton = CreateCardActionButton(btnRowObj.transform, "Btn_UpgradeRow", new Vector2(290f, 85f), new Color(0.15f, 0.55f, 0.88f), safeFont, out upgradeRowText);

        // Nút 3: ✕ Đóng / Bỏ chọn
        closeCardButton = CreateCardActionButton(btnRowObj.transform, "Btn_CloseCard", new Vector2(85f, 85f), new Color(0.55f, 0.2f, 0.2f), safeFont, out Text closeTxt);
        closeTxt.text = "<b>✕</b>";
        closeTxt.fontSize = 28;

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

        if (closeCardButton != null)
        {
            closeCardButton.onClick.RemoveAllListeners();
            closeCardButton.onClick.AddListener(DeselectWall);
        }
    }
}
