using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GlobalTechTree - Viện Nghiên Cứu Công Nghệ Toàn Căn Cứ (Idle Tech Upgrades):
/// - Quản lý các chỉ số nâng cấp toàn cục tăng trưởng sức mạnh cho toàn bộ phòng tuyến:
///   1. Sát thương toàn quân (Trụ + Robot chiến đấu)
///   2. Hiệu suất khai thác (Bot mỏ khai thác nhanh hơn)
///   3. Sản lượng vàng thụ động (+% Vàng/giây)
///   4. Độ bền giáp căn cứ (+Máu Nhà Chính và Tường)
/// - Hỗ trợ chế độ nâng cấp nhanh: [x1] / [x5] / [MAX] chuẩn phong cách Game Idle!
/// </summary>
public class GlobalTechTree : MonoBehaviour
{
    public static GlobalTechTree Instance { get; private set; }

    [Header("=== CẤP ĐỘ CÔNG NGHỆ (TECH LEVELS) ===")]
    public int techDamageLevel = 1;
    public int techMiningLevel = 1;
    public int techGoldLevel = 1;
    public int techDefenseLevel = 1;

    public const int MaxTechLevel = 50;

    // Chỉ số hiệu lực thực tế
    public float damageBonusPercent => (techDamageLevel - 1) * 8f;     // Mỗi cấp +8% Damage
    public float miningEfficiencyPercent => (techMiningLevel - 1) * 10f; // Mỗi cấp +10% Tốc độ đào
    public float goldBonusPercent => (techGoldLevel - 1) * 12f;         // Mỗi cấp +12% Vàng/s
    public float baseHpBonus => (techDefenseLevel - 1) * 350f;          // Mỗi cấp +350 HP

    public static float DamageMultiplier => (Instance != null) ? (1f + Instance.damageBonusPercent / 100f) : 1f;
    public static float MiningSpeedMultiplier => (Instance != null) ? (1f + Instance.miningEfficiencyPercent / 100f) : 1f;

    // Chế độ nâng cấp: 1 = x1, 5 = x5, 0 = MAX
    public int upgradeMultiplierMode = 1;

    private GameObject techModalObj;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("[System_GlobalTechTree]");
            go.AddComponent<GlobalTechTree>();
            DontDestroyOnLoad(go);
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
        DontDestroyOnLoad(gameObject);
        LoadTechLevels();
    }

    private void LoadTechLevels()
    {
        techDamageLevel = Mathf.Clamp(PlayerPrefs.GetInt("Tech_Damage", 1), 1, MaxTechLevel);
        techMiningLevel = Mathf.Clamp(PlayerPrefs.GetInt("Tech_Mining", 1), 1, MaxTechLevel);
        techGoldLevel = Mathf.Clamp(PlayerPrefs.GetInt("Tech_Gold", 1), 1, MaxTechLevel);
        techDefenseLevel = Mathf.Clamp(PlayerPrefs.GetInt("Tech_Defense", 1), 1, MaxTechLevel);
    }

    private void SaveTechLevels()
    {
        PlayerPrefs.SetInt("Tech_Damage", techDamageLevel);
        PlayerPrefs.SetInt("Tech_Mining", techMiningLevel);
        PlayerPrefs.SetInt("Tech_Gold", techGoldLevel);
        PlayerPrefs.SetInt("Tech_Defense", techDefenseLevel);
        PlayerPrefs.Save();
    }

    public int GetCost(int currentLvl)
    {
        return Mathf.RoundToInt(60f * Mathf.Pow(1.22f, currentLvl - 1));
    }

    public void ToggleTechModal()
    {
        if (techModalObj != null && techModalObj.activeSelf)
        {
            techModalObj.SetActive(false);
        }
        else
        {
            OpenTechModal();
        }
    }

    public void OpenTechModal()
    {
        EnsureTechModalUI();
        if (techModalObj != null)
        {
            techModalObj.SetActive(true);
            RefreshTechModalContent();
        }
    }

    private void EnsureTechModalUI()
    {
        if (techModalObj != null)
        {
            Destroy(techModalObj);
            techModalObj = null;
        }

        Canvas canvas = WallSelectionManager.GetMainScreenCanvas();
        if (canvas == null) return;

        Font safeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // 1. Overlay
        techModalObj = new GameObject("Modal_GlobalTechTree", typeof(RectTransform));
        techModalObj.layer = LayerMask.NameToLayer("UI");
        techModalObj.transform.SetParent(canvas.transform, false);

        RectTransform overlayRt = techModalObj.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImg = techModalObj.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.72f);

        // 2. Box Chính (Phóng to rộng rãi, cân đối hoàn hảo trên màn hình)
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(techModalObj.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(880f, 760f);

        Image boxImg = box.GetComponent<Image>();
        boxImg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);

        Outline bo = box.AddComponent<Outline>();
        bo.effectColor = new Color(0f, 0.9f, 1f, 0.9f);
        bo.effectDistance = new Vector2(2.5f, -2.5f);

        // Header: Tiêu đề ở giữa, nút Cơ Chế bên trái, nút Đóng bên phải (Tách biệt hoàn toàn, không đè nhau)
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        tObj.transform.SetParent(box.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0.5f, 1f);
        tRt.anchorMax = new Vector2(0.5f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(0f, -22f);
        tRt.sizeDelta = new Vector2(560f, 40f);

        Text titleText = tObj.GetComponent<Text>();
        titleText.font = safeFont;
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.85f, 0.2f, 1f);
        titleText.text = "🔬 <b>VIỆN CÔNG NGHỆ TOÀN CĂN CỨ</b>";

        // Subtitle
        GameObject sObj = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
        sObj.transform.SetParent(box.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.5f, 1f);
        sRt.anchorMax = new Vector2(0.5f, 1f);
        sRt.pivot = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, -64f);
        sRt.sizeDelta = new Vector2(720f, 28f);

        Text subText = sObj.GetComponent<Text>();
        subText.font = safeFont;
        subText.fontSize = 16;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.65f, 0.82f, 1f, 0.95f);
        subText.text = "Nâng cấp vĩnh viễn: Sát thương toàn quân • Tốc độ đào • Vàng thụ động • Giáp căn cứ";

        // Nút Đóng [X] ở góc trên bên phải
        GameObject closeBtnObj = new GameObject("Btn_Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(box.transform, false);
        RectTransform cRt = closeBtnObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1f, 1f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.pivot = new Vector2(1f, 1f);
        cRt.anchoredPosition = new Vector2(-24f, -20f);
        cRt.sizeDelta = new Vector2(46f, 46f);

        Image cImg = closeBtnObj.GetComponent<Image>();
        cImg.color = new Color(0.85f, 0.22f, 0.22f, 0.95f);

        Outline co = closeBtnObj.AddComponent<Outline>();
        co.effectColor = new Color(0.4f, 0.05f, 0.05f, 0.9f);
        co.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject cTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        cTxtObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform ctRt = cTxtObj.GetComponent<RectTransform>();
        ctRt.anchorMin = Vector2.zero;
        ctRt.anchorMax = Vector2.one;
        ctRt.offsetMin = Vector2.zero;
        ctRt.offsetMax = Vector2.zero;
        Text cTxt = cTxtObj.GetComponent<Text>();
        cTxt.font = safeFont;
        cTxt.fontSize = 24;
        cTxt.fontStyle = FontStyle.Bold;
        cTxt.alignment = TextAnchor.MiddleCenter;
        cTxt.color = Color.white;
        cTxt.text = "✕";

        closeBtnObj.GetComponent<Button>().onClick.AddListener(() => techModalObj.SetActive(false));

        // Nút chuyển chế độ x1 / x5 / MAX ở góc trên bên trái
        GameObject modeBtnObj = new GameObject("Btn_Mode", typeof(RectTransform), typeof(Image), typeof(Button));
        modeBtnObj.transform.SetParent(box.transform, false);
        RectTransform mRt = modeBtnObj.GetComponent<RectTransform>();
        mRt.anchorMin = new Vector2(0f, 1f);
        mRt.anchorMax = new Vector2(0f, 1f);
        mRt.pivot = new Vector2(0f, 1f);
        mRt.anchoredPosition = new Vector2(24f, -20f);
        mRt.sizeDelta = new Vector2(136f, 46f);

        Image mImg = modeBtnObj.GetComponent<Image>();
        mImg.color = new Color(0.12f, 0.45f, 0.85f, 0.98f);

        Outline mo = modeBtnObj.AddComponent<Outline>();
        mo.effectColor = new Color(0.4f, 0.75f, 1f, 0.8f);
        mo.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject mTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        mTxtObj.transform.SetParent(modeBtnObj.transform, false);
        RectTransform mtRt = mTxtObj.GetComponent<RectTransform>();
        mtRt.anchorMin = Vector2.zero;
        mtRt.anchorMax = Vector2.one;
        mtRt.offsetMin = Vector2.zero;
        mtRt.offsetMax = Vector2.zero;
        Text mTxt = mTxtObj.GetComponent<Text>();
        mTxt.font = safeFont;
        mTxt.fontSize = 16;
        mTxt.fontStyle = FontStyle.Bold;
        mTxt.alignment = TextAnchor.MiddleCenter;
        mTxt.color = Color.white;
        mTxt.text = "CƠ CHẾ: x1";

        modeBtnObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            if (upgradeMultiplierMode == 1) upgradeMultiplierMode = 5;
            else if (upgradeMultiplierMode == 5) upgradeMultiplierMode = 0; // 0 = MAX
            else upgradeMultiplierMode = 1;

            mTxt.text = (upgradeMultiplierMode == 0) ? "CƠ CHẾ: MAX" : $"CƠ CHẾ: x{upgradeMultiplierMode}";
            RefreshTechModalContent();
        });

        // Container 4 dòng công nghệ
        GameObject listContainer = new GameObject("ListContainer", typeof(RectTransform));
        listContainer.transform.SetParent(box.transform, false);
        RectTransform lRt = listContainer.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0.5f, 0f);
        lRt.anchorMax = new Vector2(0.5f, 0f);
        lRt.pivot = new Vector2(0.5f, 0f);
        lRt.anchoredPosition = new Vector2(0f, 22f);
        lRt.sizeDelta = new Vector2(836f, 610f);

        VerticalLayoutGroup vlg = listContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        CreateTechRow(listContainer.transform, 0, "⚡ SÁT THƯƠNG TOÀN QUÂN", "Tăng hỏa lực cho toàn bộ Súng, Pháo & Robot Đồng Minh", safeFont);
        CreateTechRow(listContainer.transform, 1, "⛏️ TỐC ĐỘ KHAI THÁC", "Bot mỏ di chuyển nhanh hơn & đập đá nhận nhiều tài nguyên hơn", safeFont);
        CreateTechRow(listContainer.transform, 2, "💰 THU NHẬP THỤ ĐỘNG", "Tăng lượng Vàng sản xuất tự động mỗi giây khi AFK", safeFont);
        CreateTechRow(listContainer.transform, 3, "🛡️ GIÁP TRUNG TÂM & TƯỜNG", "Gia cố thêm máu tối đa cho Nhà Chính và toàn bộ Bức Tường", safeFont);
    }

    private Text[] techStatTexts = new Text[4];
    private Text[] techCostTexts = new Text[4];
    private Button[] techUpgradeButtons = new Button[4];
    private Image[] techUpgradeButtonImages = new Image[4];

    private void CreateTechRow(Transform parent, int index, string name, string desc, Font font)
    {
        GameObject row = new GameObject("Row_" + index, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        RectTransform rRt = row.GetComponent<RectTransform>();
        rRt.sizeDelta = new Vector2(836f, 136f);

        Image img = row.GetComponent<Image>();
        img.color = new Color(0.10f, 0.14f, 0.22f, 0.98f);

        Outline ol = row.AddComponent<Outline>();
        ol.effectColor = new Color(0.18f, 0.45f, 0.75f, 0.85f);
        ol.effectDistance = new Vector2(2f, -2f);

        // Cột trái: Tên, Mô tả & Chỉ số
        GameObject leftCol = new GameObject("LeftCol", typeof(RectTransform));
        leftCol.transform.SetParent(row.transform, false);
        RectTransform leftRt = leftCol.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(1f, 1f);
        leftRt.offsetMin = new Vector2(22f, 10f);
        leftRt.offsetMax = new Vector2(-260f, -10f);

        // 1. Tiêu đề mục (fontSize 22, Bold, Vàng sáng)
        GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        tObj.transform.SetParent(leftCol.transform, false);
        RectTransform tRt = tObj.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0f, 1f);
        tRt.anchoredPosition = new Vector2(0f, 0f);
        tRt.sizeDelta = new Vector2(0f, 32f);

        Text t = tObj.GetComponent<Text>();
        t.font = font;
        t.fontSize = 22;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 0.88f, 0.4f);
        t.text = name;

        // 2. Mô tả (fontSize 15, Xám sáng)
        GameObject dObj = new GameObject("Desc", typeof(RectTransform), typeof(Text));
        dObj.transform.SetParent(leftCol.transform, false);
        RectTransform dRt = dObj.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 1f);
        dRt.anchorMax = new Vector2(1f, 1f);
        dRt.pivot = new Vector2(0f, 1f);
        dRt.anchoredPosition = new Vector2(0f, -34f);
        dRt.sizeDelta = new Vector2(0f, 26f);

        Text d = dObj.GetComponent<Text>();
        d.font = font;
        d.fontSize = 15;
        d.color = new Color(0.72f, 0.82f, 0.9f);
        d.text = desc;

        // 3. Chỉ số Cấp & Hiệu quả (fontSize 18, Bold, Rõ ràng)
        GameObject sObj = new GameObject("Stat", typeof(RectTransform), typeof(Text));
        sObj.transform.SetParent(leftCol.transform, false);
        RectTransform sRt = sObj.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f);
        sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 4f);
        sRt.sizeDelta = new Vector2(0f, 34f);

        Text s = sObj.GetComponent<Text>();
        s.font = font;
        s.fontSize = 18;
        s.fontStyle = FontStyle.Bold;
        s.color = Color.white;
        techStatTexts[index] = s;

        // Cột phải: Nút Nâng Cấp To Rõ Ràng (230px x 84px)
        GameObject btnObj = new GameObject("Btn_Upgrade", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(row.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-18f, 0f);
        btnRt.sizeDelta = new Vector2(230f, 84f);

        Image bImg = btnObj.GetComponent<Image>();
        bImg.color = new Color(0.08f, 0.68f, 0.32f, 1f);
        techUpgradeButtonImages[index] = bImg;

        Outline bOutline = btnObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(0f, 0.35f, 0.15f, 0.95f);
        bOutline.effectDistance = new Vector2(2f, -2f);

        GameObject bTxtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        bTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRt = bTxtObj.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero;
        btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero;
        btRt.offsetMax = Vector2.zero;

        Text bTxt = bTxtObj.GetComponent<Text>();
        bTxt.font = font;
        bTxt.fontSize = 18;
        bTxt.fontStyle = FontStyle.Bold;
        bTxt.alignment = TextAnchor.MiddleCenter;
        bTxt.color = Color.white;
        techCostTexts[index] = bTxt;

        Button btn = btnObj.GetComponent<Button>();
        techUpgradeButtons[index] = btn;

        int rowIdx = index;
        btn.onClick.AddListener(() => OnUpgradeTechClicked(rowIdx));
    }

    private void RefreshTechModalContent()
    {
        for (int i = 0; i < 4; i++)
        {
            int lvl = GetTechLevel(i);
            int cost = CalculateBulkCost(i, out int steps);

            if (techStatTexts[i] != null)
            {
                string statDesc = "";
                switch (i)
                {
                    case 0: statDesc = $"CẤP <b>{lvl}/{MaxTechLevel}</b>  •  HIỆU LỰC: <color=#00FF88>+{damageBonusPercent:F0}% SÁT THƯƠNG</color>"; break;
                    case 1: statDesc = $"CẤP <b>{lvl}/{MaxTechLevel}</b>  •  HIỆU LỰC: <color=#00FF88>+{miningEfficiencyPercent:F0}% TỐC ĐỘ ĐÀO</color>"; break;
                    case 2: statDesc = $"CẤP <b>{lvl}/{MaxTechLevel}</b>  •  HIỆU LỰC: <color=#00FF88>+{goldBonusPercent:F0}% VÀNG/S</color>"; break;
                    case 3: statDesc = $"CẤP <b>{lvl}/{MaxTechLevel}</b>  •  HIỆU LỰC: <color=#00FF88>+{baseHpBonus:F0} MÁU CĂN CỨ</color>"; break;
                }
                techStatTexts[i].text = statDesc;
            }

            if (techCostTexts[i] != null && techUpgradeButtons[i] != null)
            {
                if (lvl >= MaxTechLevel)
                {
                    techCostTexts[i].text = "👑 <b>ĐÃ ĐẠT CẤP MAX</b>";
                    techUpgradeButtons[i].interactable = false;
                    if (techUpgradeButtonImages[i] != null)
                        techUpgradeButtonImages[i].color = new Color(0.35f, 0.38f, 0.44f, 1f);
                }
                else
                {
                    bool canAfford = GameEconomy.Instance != null && GameEconomy.Instance.coins >= cost;
                    string stepStr = (steps > 1) ? $"NÂNG (+{steps} CẤP)" : "NÂNG CẤP (+1)";

                    if (canAfford)
                    {
                        techCostTexts[i].text = $"<b>{stepStr}</b>\n<color=#FFEB3B><b>{cost:#,##0} VÀNG</b></color>";
                        techUpgradeButtons[i].interactable = true;
                        if (techUpgradeButtonImages[i] != null)
                            techUpgradeButtonImages[i].color = new Color(0.08f, 0.68f, 0.32f, 1f);
                    }
                    else
                    {
                        techCostTexts[i].text = $"<b>{stepStr}</b>\n<color=#FF8A80><b>{cost:#,##0} VÀNG</b></color>";
                        techUpgradeButtons[i].interactable = false;
                        if (techUpgradeButtonImages[i] != null)
                            techUpgradeButtonImages[i].color = new Color(0.24f, 0.28f, 0.35f, 1f);
                    }
                }
            }
        }
    }

    private int GetTechLevel(int index)
    {
        switch (index)
        {
            case 0: return techDamageLevel;
            case 1: return techMiningLevel;
            case 2: return techGoldLevel;
            case 3: return techDefenseLevel;
            default: return 1;
        }
    }

    private void SetTechLevel(int index, int lvl)
    {
        switch (index)
        {
            case 0: techDamageLevel = lvl; break;
            case 1: techMiningLevel = lvl; break;
            case 2: techGoldLevel = lvl; break;
            case 3: techDefenseLevel = lvl; break;
        }
    }

    private int CalculateBulkCost(int index, out int actualSteps)
    {
        int curLvl = GetTechLevel(index);
        actualSteps = 0;
        int totalCost = 0;

        if (curLvl >= MaxTechLevel) return 0;

        int userCoins = (GameEconomy.Instance != null) ? GameEconomy.Instance.coins : 0;
        int maxPossibleSteps = MaxTechLevel - curLvl;

        if (upgradeMultiplierMode == 1)
        {
            actualSteps = 1;
            return GetCost(curLvl);
        }
        else if (upgradeMultiplierMode == 5)
        {
            int target = Mathf.Min(5, maxPossibleSteps);
            for (int i = 0; i < target; i++)
            {
                totalCost += GetCost(curLvl + i);
                actualSteps++;
            }
            return totalCost;
        }
        else // Mode MAX
        {
            for (int i = 0; i < maxPossibleSteps; i++)
            {
                int nextCost = GetCost(curLvl + i);
                if (totalCost + nextCost <= userCoins)
                {
                    totalCost += nextCost;
                    actualSteps++;
                }
                else break;
            }
            if (actualSteps == 0)
            {
                actualSteps = 1;
                totalCost = GetCost(curLvl);
            }
            return totalCost;
        }
    }

    private void OnUpgradeTechClicked(int index)
    {
        int cost = CalculateBulkCost(index, out int steps);
        if (steps <= 0) return;

        if (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(cost))
        {
            int newLvl = Mathf.Min(MaxTechLevel, GetTechLevel(index) + steps);
            SetTechLevel(index, newLvl);
            SaveTechLevels();
            RefreshTechModalContent();

            // Hiệu ứng nổi
            if (PlayerController.Instance != null)
            {
                FloatingDamageText.SpawnResource(PlayerController.Instance.transform.position + Vector3.up * 2f, $"⭐ Nâng cấp thành công (+{steps})!", new Color(0f, 0.9f, 1f), 0.15f);
            }

            Debug.Log($"<color=#00FF88>[GlobalTechTree]</color> Đã nâng cấp công nghệ {index} lên cấp {newLvl} (+{steps} cấp)!");
        }
    }
}
