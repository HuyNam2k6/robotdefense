using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Bảng Chế Tạo Rô Bốt Chiến Đấu.
/// - Thiết kế đúng chuẩn theo hình ảnh giao diện Sci-Fi Cyber Mecha:
///   + Header: Ống ngắm tròn vàng + TRUNG TÂM CHẾ TẠO RÔ BỐT + Subtitle + Nút [X] đỏ + Quân số: X/4.
///   + 4 Thẻ Robot ngang (01 Stan, 02 Mike, 03 George, 04 Leela) với icon đúng 4 con robot đã nhặt về.
///   + Hiển thị giá Vàng, Gỗ, Kim Cương (mặc định = 0, có thể tùy biến qua Inspector).
///   + Nút bấm [⚡ SẢN XUẤT] to màu xanh lá cây gradient phát sáng.
///   + Hộp cảnh báo Footer màu vàng cam Sci-Fi.
/// </summary>
public class RobotCraftingUI : MonoBehaviour
{
    public static RobotCraftingUI Instance { get; private set; }

    [System.Serializable]
    public class RobotCostData
    {
        [Tooltip("Giá Vàng để chế tạo robot")]
        public int goldCost = 0;
        [Tooltip("Giá Gỗ để chế tạo robot")]
        public int woodCost = 0;
        [Tooltip("Giá Kim Cương để chế tạo robot")]
        public int diamondCost = 0;
    }

    [Header("--- Panel Chính (Căn Giữa Nửa Màn Hình) ---")]
    public GameObject mainCraftingPanel;
    public Button closePanelButton;
    public Text headerTitleText;
    public Text armyStatusText;

    [Header("--- Chi Phí Sản Xuất 4 Robot (Tùy Chỉnh Linh Hoạt Trong Inspector) ---")]
    public RobotCostData[] robotCosts = new RobotCostData[4]
    {
        new RobotCostData { goldCost = 0, woodCost = 0, diamondCost = 0 }, // 01 Stan
        new RobotCostData { goldCost = 0, woodCost = 0, diamondCost = 0 }, // 02 Mike
        new RobotCostData { goldCost = 0, woodCost = 0, diamondCost = 0 }, // 03 George
        new RobotCostData { goldCost = 0, woodCost = 0, diamondCost = 0 }  // 04 Leela
    };

    [Header("--- Icon Hình Ảnh 4 Robot Thực Tế ---")]
    public Sprite[] robotSprites = new Sprite[4];
    public Image[] robotIconImages;

    [Header("--- 4 Nút Chế Tạo & Nút [!] Thông Số ---")]
    public Button[] craftButtons; // Index 0: Stan, 1: Mike, 2: George, 3: Leela
    public Button[] infoButtons;  // Index 0: Stan, 1: Mike, 2: George, 3: Leela
    public Text[] robotNameTexts;
    public Text[] robotCostTexts; // Hiển thị: 🪙 0  🪵 0  💎 0
    public Text[] robotDescTexts;

    [Header("--- Modal Popup Thông Số Chi Tiết [!] ---")]
    public GameObject infoModalPanel;
    public Text infoModalTitleText;
    public Text infoModalContentText;
    public Button infoModalCloseButton;

    // Tham chiếu Nhà máy đang tương tác
    private RobotFactory currentFactory;

    // Dữ liệu chi tiết 4 Robot
    public struct RobotData
    {
        public string name;
        public string titleInCard;
        public string role;
        public string iconEmoji;
        public float hp;
        public float damage;
        public float range;
        public float speed;
        public string skills;
        public string desc;
    }

    public static readonly RobotData[] AllRobots = new RobotData[]
    {
        new RobotData
        {
            name = "Stan Pháo Nhện",
            titleInCard = "STAN PHÁO NHỆN",
            role = "Ụ Pháo Hỏa Lực Tầm Xa",
            iconEmoji = "💥",
            hp = 70f,
            damage = 30f,
            range = 12f,
            speed = 3.4f,
            skills = "• Pháo Cầu Nổ (Shoot - 70%)\n• Dậm Chân Đẩy Lùi (Kick - 30%)",
            desc = "Pháo nhện 4 chân hỏa lực kiên cố (70 HP / 30 DMG), xả đạn nổ chặn bước tiến của bầy quái từ khoảng cách an toàn."
        },
        new RobotData
        {
            name = "Mike Đấu Sĩ",
            titleInCard = "MIKE ĐẤU SĨ",
            role = "Tanker Hộ Pháp Tiền Tuyến",
            iconEmoji = "🛡️",
            hp = 80f,
            damage = 45f,
            range = 2.6f,
            speed = 3.6f,
            skills = "• Cú Đấm Móc Sấm Sét (Punch - 50%)\n• Đại Đao Quét Diện Rộng (SwordSlash - 50%)",
            desc = "Chiến binh giáp thép với lượng máu khủng (80 HP / 45 DMG), xông thẳng vào tiền tuyến chặn quái và vung kiếm càn quét."
        },
        new RobotData
        {
            name = "George Sát Thủ",
            titleInCard = "GEORGE SÁT THỦ",
            role = "Sát Thủ Cơ Động Lướt Nhanh",
            iconEmoji = "⚡",
            hp = 60f,
            damage = 35f,
            range = 3.0f,
            speed = 5.2f,
            skills = "• Song Kiếm Chém Lướt (SwordSlash - 60%)\n• Song Phi Quét Vòng (Kick - 40%)",
            desc = "Sát thủ lướt nhanh (60 HP / 35 DMG), chuyên lướt áp sát tiêu diệt nhanh các mục tiêu nguy hiểm."
        },
        new RobotData
        {
            name = "Leela Xạ Thủ",
            titleInCard = "LEELA XẠ THỦ",
            role = "Xạ Thủ Tầm Xa",
            iconEmoji = "🎯",
            hp = 60f,
            damage = 40f,
            range = 16f,
            speed = 4.0f,
            skills = "• Bắn Tỉa Laser Chuẩn Xác (Shoot - 80%)\n• Cú Đá Móc Tự Vệ Cận Chiến (Kick - 20%)",
            desc = "Xạ thủ bắn tỉa tầm xa (60 HP / 40 DMG), hạ gục quái vật trước khi chúng kịp tới gần căn cứ."
        }
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (mainCraftingPanel != null) mainCraftingPanel.SetActive(false);
        if (infoModalPanel != null) infoModalPanel.SetActive(false);

        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(ClosePanel);

        if (infoModalCloseButton != null)
            infoModalCloseButton.onClick.AddListener(CloseInfoModal);

        // Đấu nối 4 nút Chế tạo
        if (craftButtons != null)
        {
            for (int i = 0; i < craftButtons.Length; i++)
            {
                int index = i;
                if (craftButtons[i] != null)
                {
                    craftButtons[i].onClick.AddListener(() => OnCraftButtonClicked(index));
                }
            }
        }

        // Đấu nối 4 nút [!] Thông số
        if (infoButtons != null)
        {
            for (int i = 0; i < infoButtons.Length; i++)
            {
                int index = i;
                if (infoButtons[i] != null)
                {
                    infoButtons[i].onClick.AddListener(() => ShowRobotInfo(index));
                }
            }
        }

        UpdateCostDisplays();
    }

    void Update()
    {
        if (mainCraftingPanel != null && mainCraftingPanel.activeSelf)
        {
            UpdateLivePanelStatus();
        }
    }

    public void OpenPanel(RobotFactory factory = null)
    {
        if (factory != null) currentFactory = factory;
        else if (currentFactory == null) currentFactory = RobotFactory.Instance ?? FindAnyObjectByType<RobotFactory>();

        if (mainCraftingPanel != null)
        {
            mainCraftingPanel.SetActive(true);
            UpdateCostDisplays();
            UpdateLivePanelStatus();
        }
    }

    public void ClosePanel()
    {
        if (mainCraftingPanel != null) mainCraftingPanel.SetActive(false);
        if (infoModalPanel != null) infoModalPanel.SetActive(false);
    }

    public void UpdateCostDisplays()
    {
        if (robotCostTexts == null) return;
        for (int i = 0; i < robotCostTexts.Length; i++)
        {
            if (robotCostTexts[i] == null) continue;
            RobotCostData cost = (robotCosts != null && i < robotCosts.Length && robotCosts[i] != null) 
                ? robotCosts[i] 
                : new RobotCostData();

            robotCostTexts[i].text = $"🪙 <b>{cost.goldCost}</b>  🪵 <b>{cost.woodCost}</b>  💎 <b>{cost.diamondCost}</b>";
        }
    }

    private void OnCraftButtonClicked(int robotIndex)
    {
        if (currentFactory == null)
            currentFactory = RobotFactory.Instance ?? FindAnyObjectByType<RobotFactory>();

        if (currentFactory == null)
        {
            ShowWarningToast("⚠️ Chưa tìm thấy Nhà Máy trên đảo!");
            return;
        }

        // Kiểm tra chi phí nếu sau này người dùng cài đặt > 0 trong Inspector
        RobotCostData cost = (robotCosts != null && robotIndex < robotCosts.Length && robotCosts[robotIndex] != null) 
            ? robotCosts[robotIndex] 
            : new RobotCostData();

        if (cost.goldCost > 0 && GameEconomy.Instance != null && GameEconomy.Instance.coins < cost.goldCost)
        {
            ShowWarningToast($"⚠️ Không đủ Vàng! Cần {cost.goldCost} 🪙 để sản xuất.");
            return;
        }

        if (cost.woodCost > 0 && GameEconomy.Instance != null && GameEconomy.Instance.woodCount < cost.woodCost)
        {
            ShowWarningToast($"⚠️ Không đủ Gỗ! Cần {cost.woodCost} 🪵 để sản xuất.");
            return;
        }

        if (cost.diamondCost > 0 && GameEconomy.Instance != null && GameEconomy.Instance.gemCount < cost.diamondCost)
        {
            ShowWarningToast($"⚠️ Không đủ Kim Cương! Cần {cost.diamondCost} 💎 để sản xuất.");
            return;
        }

        // Khấu trừ tài nguyên nếu có chi phí
        if (cost.goldCost > 0 && GameEconomy.Instance != null)
        {
            GameEconomy.Instance.SpendCoins(cost.goldCost);
        }
        if (cost.woodCost > 0 && GameEconomy.Instance != null)
        {
            GameEconomy.Instance.woodCount -= cost.woodCost;
        }
        if (cost.diamondCost > 0 && GameEconomy.Instance != null)
        {
            GameEconomy.Instance.gemCount -= cost.diamondCost;
        }

        bool success = currentFactory.QueueCraftRobot(robotIndex);
        if (success)
        {
            string botName = (robotIndex >= 0 && robotIndex < AllRobots.Length) ? AllRobots[robotIndex].name : "Robot";
            Debug.Log($"<color=#00FF88>[Chế Tạo]</color> Đã bắt đầu chế tạo {botName} (15s)! Nhà máy đang vận hành cơ khí.");

            // Hiệu ứng nảy nút
            if (craftButtons != null && robotIndex < craftButtons.Length && craftButtons[robotIndex] != null)
            {
                StartCoroutine(AnimateButtonBounce(craftButtons[robotIndex].transform));
            }
        }
        else
        {
            ShowWarningToast("⚠️ Quân đội đã đạt tối đa (4/4)! Robot đang bảo vệ căn cứ.");
        }

        UpdateLivePanelStatus();
    }

    private void ShowRobotInfo(int robotIndex)
    {
        if (robotIndex < 0 || robotIndex >= AllRobots.Length) return;
        RobotData data = AllRobots[robotIndex];
        RobotCostData cost = (robotCosts != null && robotIndex < robotCosts.Length && robotCosts[robotIndex] != null) 
            ? robotCosts[robotIndex] 
            : new RobotCostData();

        if (infoModalPanel != null)
        {
            if (infoModalTitleText != null)
            {
                infoModalTitleText.text = $"{data.iconEmoji} <b>{data.name.ToUpper()}</b>\n<color=#00FFFF>✦ {data.role}</color>";
            }

            if (infoModalContentText != null)
            {
                infoModalContentText.text = 
                    $"❤️ <b>Máu (HP):</b> <color=#00FF88>{data.hp}</color>\n" +
                    $"⚔️ <b>Sát Thương (DMG):</b> <color=#FFD700>{data.damage}</color>\n" +
                    $"🎯 <b>Tầm Đánh:</b> <color=#00FFFF>{data.range} mét</color>\n" +
                    $"⚡ <b>Tốc Độ Di Chuyển:</b> <color=#FFA500>{data.speed} m/s</color>\n" +
                    $"⏱️ <b>Thời Gian Chế Tạo:</b> <color=#E0B0FF>15 Giây</color>\n\n" +
                    $"💰 <b>CHI PHÍ SẢN XUẤT:</b>\n" +
                    $"• Vàng: <color=#FFD700>{cost.goldCost} 🪙</color> | Gỗ: <color=#CD853F>{cost.woodCost} 🪵</color> | Kim Cương: <color=#00FFFF>{cost.diamondCost} 💎</color>\n\n" +
                    $"✨ <b>KỸ NĂNG TẤN CÔNG:</b>\n<color=#EEEEEE>{data.skills}</color>\n\n" +
                    $"📝 <i>{data.desc}</i>";
            }

            infoModalPanel.SetActive(true);
        }
    }

    public void CloseInfoModal()
    {
        if (infoModalPanel != null) infoModalPanel.SetActive(false);
    }

    private void UpdateLivePanelStatus()
    {
        if (currentFactory == null)
            currentFactory = RobotFactory.Instance ?? FindAnyObjectByType<RobotFactory>();

        if (currentFactory != null)
        {
            int army = currentFactory.GetCurrentArmyCount();
            int maxArmy = 4; // Tối đa 4 quân số theo thiết kế UI

            if (armyStatusText != null)
            {
                armyStatusText.text = $"⚡ <b>Quân số:</b> <color=#00FF88>{army}/{maxArmy}</color>";
            }
        }

        UpdateCostDisplays();
    }

    private void ShowWarningToast(string msg)
    {
        Debug.LogWarning($"<color=yellow>[RobotCraftingUI]</color> {msg}");
    }

    private IEnumerator AnimateButtonBounce(Transform btnT)
    {
        if (btnT == null) yield break;
        Vector3 orig = Vector3.one;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.88f, elapsed / 0.15f);
            if (btnT != null) btnT.localScale = orig * s;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(0.88f, 1f, elapsed / 0.15f);
            if (btnT != null) btnT.localScale = orig * s;
            yield return null;
        }
        if (btnT != null) btnT.localScale = orig;
    }
}
