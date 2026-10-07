using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý Bảng Chế Tạo Robot Chiến Đấu.
/// - Hiển thị to nửa màn hình, căn chỉnh chính giữa màn hình khi nhấn vào Nhà Máy.
/// - Chứa đầy đủ 4 Icon / Thẻ của 4 con robot phòng thủ: Stan, Mike, George, Leela.
/// - Mỗi Icon có nút [!] để xem chi tiết thông số: Máu, Sát thương, Tầm đánh, Tốc độ, Kỹ năng.
/// - Người chơi tự chọn con robot muốn sản xuất (thời gian làm 15s/con kèm animation cơ khí).
/// </summary>
public class RobotCraftingUI : MonoBehaviour
{
    public static RobotCraftingUI Instance { get; private set; }

    [Header("--- Panel Chính (To Nửa Màn Hình Giữa Màn Hình) ---")]
    public GameObject mainCraftingPanel;
    public Button closePanelButton;
    public Text headerTitleText;
    public Text armyStatusText;

    [Header("--- Thanh Tiến Độ Sản Xuất Hiện Tại ---")]
    public GameObject currentProductionBar;
    public Text currentProductionText;
    public Image currentProgressBarFill;

    [Header("--- 4 Nút Chế Tạo & Nút [!] Thông Số ---")]
    public Button[] craftButtons; // Index 0: Stan, 1: Mike, 2: George, 3: Leela
    public Button[] infoButtons;  // Index 0: Stan, 1: Mike, 2: George, 3: Leela
    public Text[] robotNameTexts;

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
            role = "Ụ Pháo Hỏa Lực Tầm Xa",
            iconEmoji = "💥",
            hp = 220f,
            damage = 32f,
            range = 12f,
            speed = 3.4f,
            skills = "• Pháo Cầu Nổ (Shoot - 70%)\n• Dậm Chân Đẩy Lùi (Kick - 30%)",
            desc = "Pháo nhện 4 chân hỏa lực kiên cố, xả đạn nổ chặn bước tiến của bầy quái từ khoảng cách an toàn."
        },
        new RobotData
        {
            name = "Mike Đấu Sĩ",
            role = "Tanker Hộ Pháp Tiền Tuyến",
            iconEmoji = "🛡️",
            hp = 320f,
            damage = 45f,
            range = 2.6f,
            speed = 3.6f,
            skills = "• Cú Đấm Móc Sấm Sét (Punch - 50%)\n• Đại Đao Quét Diện Rộng (SwordSlash - 50%)",
            desc = "Chiến binh giáp thép với lượng máu khủng nhất, xông thẳng vào tiền tuyến chặn quái và vung kiếm càn quét."
        },
        new RobotData
        {
            name = "George Sát Thủ",
            role = "Sát Thủ Cơ Động Siêu Tốc",
            iconEmoji = "⚡",
            hp = 160f,
            damage = 35f,
            range = 3.0f,
            speed = 5.4f,
            skills = "• Song Kiếm Chém Lướt (SwordSlash - 60%)\n• Song Phi Quét Vòng (Kick - 40%)",
            desc = "Tốc độ di chuyển và tốc độ ra đòn nhanh nhất (0.85s/đòn), chuyên lướt áp sát tiêu diệt nhanh các mục tiêu nguy hiểm."
        },
        new RobotData
        {
            name = "Leela Xạ Thủ",
            role = "Xạ Thủ Bắn Tỉa Tầm Xa",
            iconEmoji = "🎯",
            hp = 140f,
            damage = 50f,
            range = 18f,
            speed = 4.0f,
            skills = "• Bắn Tỉa Laser Chuẩn Xác (Shoot - 80%)\n• Cú Đá Móc Tự Vệ Cận Chiến (Kick - 20%)",
            desc = "Tầm bắn xa nhất chiến trường (18m) với sát thương xuyên giáp 50 điểm, hạ gục quái vật trước khi chúng kịp tới gần."
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
            UpdateLivePanelStatus();
        }
    }

    public void ClosePanel()
    {
        if (mainCraftingPanel != null) mainCraftingPanel.SetActive(false);
        if (infoModalPanel != null) infoModalPanel.SetActive(false);
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
            ShowWarningToast("⚠️ Quân đội đã đạt tối đa (8/8)! Robot đang bảo vệ căn cứ.");
        }

        UpdateLivePanelStatus();
    }

    private void ShowRobotInfo(int robotIndex)
    {
        if (robotIndex < 0 || robotIndex >= AllRobots.Length) return;
        RobotData data = AllRobots[robotIndex];

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
                    $"⚔️ <b>Sát Thương:</b> <color=#FFD700>{data.damage}</color>\n" +
                    $"🎯 <b>Tầm Đánh:</b> <color=#00FFFF>{data.range} mét</color>\n" +
                    $"⚡ <b>Tốc Độ Chạy:</b> <color=#FFA500>{data.speed} m/s</color>\n" +
                    $"⏱️ <b>Thời Gian Chế Tạo:</b> <color=#E0B0FF>15 Giây</color>\n\n" +
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
            int maxArmy = currentFactory.GetMaxArmySize();
            int queued = currentFactory.QueuedCount;

            if (armyStatusText != null)
            {
                string qStr = queued > 0 ? $" • Đang chờ: <color=#FFA500>{queued}</color>" : "";
                armyStatusText.text = $"🛡️ Quân số: <b>{army}/{maxArmy}</b>{qStr}";
            }

            // Thanh tiến độ sản xuất hiện tại
            if (currentProductionBar != null)
            {
                if (currentFactory.IsProducing)
                {
                    currentProductionBar.SetActive(true);
                    float rem = currentFactory.RemainingProductionTime;
                    int craftIdx = currentFactory.CurrentCraftingIndex;
                    string craftName = (craftIdx >= 0 && craftIdx < AllRobots.Length) ? AllRobots[craftIdx].name : "Robot";

                    if (currentProductionText != null)
                    {
                        currentProductionText.text = $"⏳ Đang lắp ráp <b>{craftName}</b>: <color=#FFD700>{rem:F0}s</color>";
                    }

                    if (currentProgressBarFill != null)
                    {
                        currentProgressBarFill.fillAmount = currentFactory.GetProductionProgress();
                    }
                }
                else
                {
                    if (currentProductionText != null)
                    {
                        currentProductionText.text = "⚡ Nhà máy sẵn sàng nhận lệnh chế tạo (15s)";
                    }
                    if (currentProgressBarFill != null)
                    {
                        currentProgressBarFill.fillAmount = 0f;
                    }
                }
            }
        }
        else
        {
            if (armyStatusText != null)
            {
                armyStatusText.text = "<color=#FF6666>⚠️ Chưa tìm thấy Nhà Máy trên đảo</color>";
            }
        }
    }

    private void ShowWarningToast(string msg)
    {
        BuildingShopUI shop = FindAnyObjectByType<BuildingShopUI>();
        if (shop != null) shop.ShowNotEnoughCoinsWarning(msg);
        else Debug.LogWarning(msg);
    }

    private IEnumerator AnimateButtonBounce(Transform btnT)
    {
        if (btnT == null) yield break;
        Vector3 orig = Vector3.one;
        btnT.localScale = orig * 0.88f;
        yield return new WaitForSeconds(0.08f);
        btnT.localScale = orig * 1.08f;
        yield return new WaitForSeconds(0.08f);
        btnT.localScale = orig;
    }
}
