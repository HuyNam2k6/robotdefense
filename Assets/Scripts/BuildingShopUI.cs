using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildingShopUI : MonoBehaviour
{
	public BuildingSystem buildingSystem;

	[Header("--- Nút Icon Mở Cửa Hàng & Bảng Phòng Thủ ---")]
	public Button shoppingCartButton;  // Nút mở bảng shop / phòng thủ
	public GameObject dashboardPanel;   // Bảng điều hành phòng thủ chính
	public Button closeDashboardButton; // Nút đóng ✕

	// Thuộc tính tương thích ngược với các script khác
	public Button shopOpenButton { get => shoppingCartButton; set => shoppingCartButton = value; }
	public GameObject shopPanel { get => dashboardPanel; set => dashboardPanel = value; }
	public Button closeShopButton { get => closeDashboardButton; set => closeDashboardButton = value; }

	[Header("--- Hệ Thống Tab Thanh Bên Trái (Left Navigation) ---")]
	public Button tabDefenseButton;   // Tab 0: Phòng Thủ (Mặc định)
	public Button tabWarehouseButton; // Tab 1: Kho Hàng
	public Button tabMachineryButton; // Tab 2: Máy Móc
	public Button tabRobotButton;     // Tab 3: Robot
	public Button tabSpecialButton;   // Tab 4: Đặc Biệt

	public Image tabDefenseBg;
	public Image tabWarehouseBg;
	public Image tabMachineryBg;
	public Image tabRobotBg;
	public Image tabSpecialBg;

	[Header("--- Các Trang Nội Dung Tương Ứng Với Tab ---")]
	public GameObject pageDefense;
	public GameObject pageWarehouse;
	public GameObject pageMachinery;
	public GameObject pageRobot;
	public GameObject pageSpecial;

	[Header("--- Các Thẻ Vũ Khí Phòng Thủ (Defense Items) ---")]
	public Button buyPhaotuhanhButton;     // 1. Pháo Tự Hành (1,500)
	public Button buyThunderButton;         // 2. Trụ Sét Thunder (2,500)
	public Button buyFlamethrowerButton;    // 3. Súng Phun Lửa (3,200)
	public Button buyWallItemButton;        // 4. Bức Tường (100)
	public Button buyStarterPackButton;     // Gói khởi đầu (chuyển vào Kho Hàng)

	[Header("--- Header Bar & Đồng Hồ Đếm Ngược ---")]
	public Text countdownText;
	public Button refreshShopButton;

	[Header("--- Thông Tin Tiền Tệ & Tài Nguyên ---")]
	public Text coinText;
	public Text stoneText;
	public Text woodText;

	[Header("--- Mục 1: Robot Đào Mỏ & Cúp Sắt (Tương thích ngược) ---")]
	public Button buyWorkerButton;
	public Text buyWorkerText;
	public Button upgradePickaxeButton;
	public Text upgradePickaxeText;

	[Header("--- Mục 2: Nhà Máy Sản Xuất & Chế Tạo Robot (15s) ---")]
	public Button buildFactoryButton;   // Nút Xây Nhà Máy
	public Text buildFactoryText;
	public Button craftRobotButton;     // Nút Chế Tạo Robot (15s)
	public Text craftRobotText;

	// Tương thích ngược với defenseRobotButton cũ
	public Button defenseRobotButton { get => craftRobotButton != null ? craftRobotButton : buildFactoryButton; set => craftRobotButton = value; }
	public Text defenseRobotText { get => craftRobotText != null ? craftRobotText : buildFactoryText; set => craftRobotText = value; }

	[Header("--- Mục 3: Bức Tường (Kéo Trái/Phải & Nâng Cấp) ---")]
	public Button wallButton;
	public Text wallButtonText;
	public Button upgradeWallButton;
	public Text upgradeWallText;

	[Header("--- Thông Báo Cảnh Báo Không Đủ Vàng ---")]
	public GameObject notEnoughCoinsBanner;
	public Text notEnoughCoinsText;

	[Header("--- Bảng Điều Khiển Nổi Khi Đang Đặt (Placement HUD) ---")]
	public GameObject placementHUD;
	public Button rotateIconButton;
	public Button cancelIconButton;

	private Coroutine warningCoroutine;
	private float nextLiveUiUpdate = 0f;
	private float refreshCountdown = 272f; // 04:32 ban đầu
	private int currentTab = 0; // 0: Phòng thủ, 1: Kho hàng, 2: Máy móc, 3: Robot, 4: Đặc biệt

	private readonly Color activeTabColor = new Color(0.96f, 0.65f, 0.12f, 1f);     // Màu vàng hổ phách nổi bật
	private readonly Color inactiveTabColor = new Color(0.08f, 0.14f, 0.24f, 0.95f); // Màu xanh thẫm công nghệ

	void Awake()
	{
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas != null)
		{
			RectTransform crt = canvas.GetComponent<RectTransform>();
			if (crt != null && (crt.localScale.x == 0f || crt.localScale.y == 0f))
			{
				crt.localScale = Vector3.one;
			}
		}

		if (shoppingCartButton != null)
		{
			shoppingCartButton.gameObject.SetActive(true);
		}
	}

	void Start()
	{
		if (buildingSystem == null)
			buildingSystem = FindAnyObjectByType<BuildingSystem>();

		if (shoppingCartButton != null)
			shoppingCartButton.onClick.AddListener(ToggleShopPanel);

		if (closeDashboardButton != null)
			closeDashboardButton.onClick.AddListener(CloseShopPanel);

		// Tab listeners
		if (tabDefenseButton != null) tabDefenseButton.onClick.AddListener(() => SwitchTab(0));
		if (tabWarehouseButton != null) tabWarehouseButton.onClick.AddListener(() => SwitchTab(1));
		if (tabMachineryButton != null) tabMachineryButton.onClick.AddListener(() => SwitchTab(2));
		if (tabRobotButton != null) tabRobotButton.onClick.AddListener(() => SwitchTab(3));
		if (tabSpecialButton != null) tabSpecialButton.onClick.AddListener(() => SwitchTab(4));

		// Vũ khí phòng thủ
		if (buyPhaotuhanhButton != null) buyPhaotuhanhButton.onClick.AddListener(OnSelectPhaotuhanh);
		if (buyThunderButton != null) buyThunderButton.onClick.AddListener(OnSelectThunder);
		if (buyFlamethrowerButton != null) buyFlamethrowerButton.onClick.AddListener(OnSelectFlamethrower);
		if (buyWallItemButton != null) buyWallItemButton.onClick.AddListener(OnSelectWall);
		if (buyStarterPackButton != null) buyStarterPackButton.onClick.AddListener(OnBuyStarterPack);

		if (refreshShopButton != null) refreshShopButton.onClick.AddListener(OnRefreshShop);

		// Backward compatibility listeners
		if (buyWorkerButton != null) buyWorkerButton.onClick.AddListener(OnBuyWorker);
		if (upgradePickaxeButton != null) upgradePickaxeButton.onClick.AddListener(OnUpgradePickaxe);
		if (buildFactoryButton != null) buildFactoryButton.onClick.AddListener(OnBuildFactoryClicked);
		if (craftRobotButton != null) craftRobotButton.onClick.AddListener(OnCraftRobotClicked);
		if (wallButton != null) wallButton.onClick.AddListener(OnSelectWall);
		if (upgradeWallButton != null) upgradeWallButton.onClick.AddListener(OnUpgradeWall);

		// Placement HUD
		if (rotateIconButton != null) rotateIconButton.onClick.AddListener(OnRotate);
		if (cancelIconButton != null) cancelIconButton.onClick.AddListener(OnCancel);

		if (GameEconomy.Instance != null)
		{
			GameEconomy.Instance.OnEconomyChanged += UpdateUI;
			GameEconomy.Instance.OnNotEnoughCoins += ShowNotEnoughCoinsWarning;
		}

		if (notEnoughCoinsBanner != null)
			notEnoughCoinsBanner.SetActive(false);

		// Mặc định ban đầu: Ẩn dashboard, chọn tab Phòng Thủ (0)
		if (dashboardPanel != null)
			dashboardPanel.SetActive(false);

		SwitchTab(0);
		UpdateUI();
	}

	void OnDestroy()
	{
		if (GameEconomy.Instance != null)
		{
			GameEconomy.Instance.OnEconomyChanged -= UpdateUI;
			GameEconomy.Instance.OnNotEnoughCoins -= ShowNotEnoughCoinsWarning;
		}
	}

	void Update()
	{
		// Phím tắt B: Bật/Tắt Bảng Phòng Thủ
		if (Input.GetKeyDown(KeyCode.B))
		{
			ToggleShopPanel();
		}

		if (buildingSystem != null)
		{
			bool isPlacing = buildingSystem.IsPlacing;

			if (placementHUD != null && placementHUD.activeSelf != isPlacing)
			{
				placementHUD.SetActive(isPlacing);
			}

			// Khi đang kéo đặt công trình: ẩn dashboard để dễ quan sát mặt đất
			if (isPlacing)
			{
				if (dashboardPanel != null && dashboardPanel.activeSelf)
				{
					dashboardPanel.SetActive(false);
				}
				if (shoppingCartButton != null && shoppingCartButton.gameObject.activeSelf)
				{
					shoppingCartButton.gameObject.SetActive(false);
				}
			}
			else
			{
				if (shoppingCartButton != null && !shoppingCartButton.gameObject.activeSelf)
				{
					shoppingCartButton.gameObject.SetActive(true);
				}
			}
		}

		// Cập nhật đồng hồ đếm ngược và live UI khi mở panel
		if (dashboardPanel != null && dashboardPanel.activeSelf)
		{
			UpdateCountdown();

			if (Time.time >= nextLiveUiUpdate)
			{
				nextLiveUiUpdate = Time.time + 0.25f;
				UpdateLiveFactoryUI();
			}
		}
	}

	private void UpdateCountdown()
	{
		refreshCountdown -= Time.deltaTime;
		if (refreshCountdown <= 0f) refreshCountdown = 300f;

		if (countdownText != null)
		{
			int minutes = Mathf.FloorToInt(refreshCountdown / 60f);
			int seconds = Mathf.FloorToInt(refreshCountdown % 60f);
			countdownText.text = $"⏳ Làm mới sau: <color=#00FFFF><b>{minutes:00}:{seconds:00}</b></color>";
		}
	}

	public void SwitchTab(int tabIndex)
	{
		currentTab = tabIndex;

		if (pageDefense != null) pageDefense.SetActive(tabIndex == 0);
		if (pageWarehouse != null) pageWarehouse.SetActive(tabIndex == 1);
		if (pageMachinery != null) pageMachinery.SetActive(tabIndex == 2);
		if (pageRobot != null) pageRobot.SetActive(tabIndex == 3);
		if (pageSpecial != null) pageSpecial.SetActive(tabIndex == 4);

		// Cập nhật màu sắc nút tab
		if (tabDefenseBg != null) tabDefenseBg.color = tabIndex == 0 ? activeTabColor : inactiveTabColor;
		if (tabWarehouseBg != null) tabWarehouseBg.color = tabIndex == 1 ? activeTabColor : inactiveTabColor;
		if (tabMachineryBg != null) tabMachineryBg.color = tabIndex == 2 ? activeTabColor : inactiveTabColor;
		if (tabRobotBg != null) tabRobotBg.color = tabIndex == 3 ? activeTabColor : inactiveTabColor;
		if (tabSpecialBg != null) tabSpecialBg.color = tabIndex == 4 ? activeTabColor : inactiveTabColor;
	}

	// ================= CHỌN VŨ KHÍ PHÒNG THỦ =================

	public void OnSelectPhaotuhanh()
	{
		SelectItemWithCostCheck(0, "Pháo Tự Hành");
	}

	public void OnSelectThunder()
	{
		SelectItemWithCostCheck(1, "Trụ Sét");
	}

	public void OnSelectFlamethrower()
	{
		SelectItemWithCostCheck(2, "Súng Phun Lửa");
	}

	private void SelectItemWithCostCheck(int itemIndex, string displayName)
	{
		if (buildingSystem == null || buildingSystem.items == null || itemIndex >= buildingSystem.items.Length)
		{
			ShowNotEnoughCoinsWarning($"⚠️ Chưa tìm thấy prefab {displayName}!");
			return;
		}

		int cost = buildingSystem.items[itemIndex].cost;
		if (cost > 0 && GameEconomy.Instance != null && GameEconomy.Instance.coins < cost)
		{
			ShowNotEnoughCoinsWarning($"⚠️ Không đủ vàng để mua {displayName}! (Cần {cost}🪙)");
			return;
		}

		if (WallSelectionManager.Instance != null)
		{
			WallSelectionManager.Instance.DeselectAll();
		}

		CloseShopPanel();
		buildingSystem.SelectItem(itemIndex);
		Debug.Log($"<color=#00FF88>[Phòng Thủ]</color> Đã chọn <b>{displayName}</b>! Nhấp chuột trái lên mặt đất để đặt vị trí.");
	}

	public void OnBuyStarterPack()
	{
		const int packCost = 4990;
		if (GameEconomy.Instance != null && GameEconomy.Instance.coins < packCost)
		{
			ShowNotEnoughCoinsWarning($"⚠️ Không đủ vàng để mua Gói Khởi Đầu! (Cần {packCost}🪙)");
			return;
		}

		if (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(packCost))
		{
			CloseShopPanel();
			// Bắt đầu đặt Pháo Tự Hành trước
			if (buildingSystem != null) buildingSystem.SelectItem(0);
			Debug.Log("<color=#FFD700><b>[Kho Hàng - Gói Khởi Đầu]</b></color> Đã mở Gói Khởi Đầu! Bắt đầu bố trí Pháo Tự Hành.");
		}
	}

	public void OnRefreshShop()
	{
		refreshCountdown = 300f; // Reset về 05:00
		UpdateCountdown();
		StartCoroutine(AnimateButtonBounce(refreshShopButton != null ? refreshShopButton.transform : null));
		Debug.Log("<color=#00FFFF>[Làm Mới]</color> Đã làm mới danh mục phòng thủ!");
	}

	private void UpdateLiveFactoryUI()
	{
		if (craftRobotText == null && defenseRobotText == null) return;
		Text targetText = craftRobotText != null ? craftRobotText : defenseRobotText;

		if (RobotFactory.Instance == null)
		{
			targetText.text = "🤖 <b>CHẾ TẠO ROBOT</b>\n<color=#FF7777>⚠️ Cần Xây Nhà Máy Trước</color>";
			return;
		}

		int army = RobotFactory.Instance.GetCurrentArmyCount();
		int maxArmy = RobotFactory.Instance.GetMaxArmySize();

		if (RobotFactory.Instance.IsProducing)
		{
			float rem = RobotFactory.Instance.RemainingProductionTime;
			int q = RobotFactory.Instance.QueuedCount;
			string qText = q > 0 ? $" (+{q})" : "";
			targetText.text = $"⏳ <b>ĐANG ĐÚC: {rem:F0}s{qText}</b>\n<color=#00FFFF>Quân: {army}/{maxArmy} • Stan/Mike/George/Leela</color>";
		}
		else if (army >= maxArmy)
		{
			targetText.text = $"👑 <b>QUÂN ĐỘI TỐI ĐA ({army}/{maxArmy})</b>\n<color=#00FF88>✦ Đang Tuần Tra Phòng Thủ</color>";
		}
		else
		{
			targetText.text = $"🤖 <b>CHẾ TẠO ROBOT (15s)</b>\n<color=#00FF88>Quân: {army}/{maxArmy} • Bấm Để Chế Tạo</color>";
		}
	}

	public void UpdateUI()
	{
		if (GameEconomy.Instance == null) return;

		// 1. Tiền tệ
		if (coinText != null)
			coinText.text = $"🪙 <b>{GameEconomy.Instance.coins:N0}</b>";

		if (stoneText != null)
			stoneText.text = $"🪨 <b>{GameEconomy.Instance.stoneCount:N0}</b>";

		if (woodText != null)
			woodText.text = $"🪵 <b>{GameEconomy.Instance.woodCount:N0}</b>";

		// 2. Robot Đào Mỏ
		if (buyWorkerText != null)
		{
			buyWorkerText.text = $"🤖 <b>MUA ROBOT ĐÀO</b>\n<color=#FFD700>🪙 {GameEconomy.Instance.workerRobotCost} Vàng</color>";
		}

		// 2.1. Nút Xây Nhà Máy
		if (buildFactoryText != null)
		{
			bool hasFactory = (RobotFactory.Instance != null);
			if (hasFactory)
			{
				buildFactoryText.text = "🏭 <b>XÂY NHÀ MÁY ROBOT</b>\n<color=#00FF88>✦ Đang Hoạt Động (Xây thêm)</color>";
			}
			else
			{
				buildFactoryText.text = "🏭 <b>XÂY NHÀ MÁY ROBOT</b>\n<color=#FFD700>🪙 0 Vàng (Bấm Để Đặt)</color>";
			}
		}

		// 2.2. Nút Chế Tạo Robot (15s)
		UpdateLiveFactoryUI();

		// 3. Nâng Cấp Cúp Sắt
		if (upgradePickaxeText != null)
		{
			int pCost = GameEconomy.Instance.GetPickaxeUpgradeCost();
			upgradePickaxeText.text = $"⛏️ <b>CÚP SẮT (CẤP {GameEconomy.Instance.pickaxeLevel})</b>\n<color=#FFD700>⭐ Nâng Cấp: {pCost}🪙</color>";
		}

		// 4. Mua Tường
		if (wallButtonText != null)
		{
			wallButtonText.text = $"🧱 <b>MUA TƯỜNG (KÉO DÀI)</b>\n<color=#FFD700>🪙 {GameEconomy.Instance.wallSegmentCost} Vàng/Đoạn</color>";
		}

		// 5. Nâng Cấp Tường
		if (upgradeWallText != null)
		{
			int wLevel = GameEconomy.Instance.wallLevel;
			if (wLevel < 6)
			{
				int wCost = GameEconomy.Instance.GetWallUpgradeCost();
				string nextName = GameEconomy.WallLevelNames[wLevel];
				upgradeWallText.text = $"⭐ <b>NÂNG CẤP TƯỜNG (LÊN CẤP {wLevel + 1})</b>\n<color=#00FFFF>✦ {nextName}</color> • <color=#FFD700>{wCost}🪙</color>";
				if (upgradeWallButton != null) upgradeWallButton.interactable = true;
			}
			else
			{
				upgradeWallText.text = $"👑 <b>TƯỜNG CẤP 6 - MAX (TỐI ĐA)</b>\n<color=#E0B0FF>✦ ĐEN TITAN (ĐÃ ĐẠT TỐI ĐA)</color>";
				if (upgradeWallButton != null) upgradeWallButton.interactable = false;
			}
		}
	}

	public void ToggleShopPanel()
	{
		if (dashboardPanel != null)
		{
			dashboardPanel.SetActive(!dashboardPanel.activeSelf);
			if (dashboardPanel.activeSelf && WallSelectionManager.Instance != null)
			{
				WallSelectionManager.Instance.DeselectAll();
			}
			UpdateUI();
		}
	}

	public void CloseShopPanel()
	{
		if (dashboardPanel != null) dashboardPanel.SetActive(false);
	}

	public void OnMiningRobotClicked()
	{
		if (shoppingCartButton != null)
		{
			StartCoroutine(AnimateButtonBounce(shoppingCartButton.transform));
		}
		OnBuyWorker();
	}

	private IEnumerator AnimateButtonBounce(Transform btnTransform)
	{
		if (btnTransform == null) yield break;
		Vector3 originalScale = Vector3.one;
		btnTransform.localScale = originalScale * 0.88f;
		yield return new WaitForSeconds(0.08f);
		btnTransform.localScale = originalScale * 1.10f;
		yield return new WaitForSeconds(0.08f);
		btnTransform.localScale = originalScale;
	}

	public void OnBuyWorker()
	{
		if (GameEconomy.Instance != null)
		{
			if (GameEconomy.Instance.BuyWorkerRobot())
			{
				CloseShopPanel();
			}
		}
	}

	public void OnBuildFactoryClicked()
	{
		if (buildingSystem != null)
		{
			int factoryIndex = -1;
			for (int i = 0; i < buildingSystem.items.Length; i++)
			{
				if (buildingSystem.items[i] != null && buildingSystem.items[i].itemName.ToLower().Contains("nhà máy"))
				{
					factoryIndex = i;
					break;
				}
			}

			if (factoryIndex < 0) factoryIndex = 4; // fallback

			CloseShopPanel();
			buildingSystem.SelectItem(factoryIndex);
			Debug.Log("<color=#00FF88>[Xây Nhà Máy]</color> Chuyển sang chế độ đặt Nhà Máy! Nhấn chuột trái lên mặt đất để xây.");
		}
	}

	public void OnCraftRobotClicked()
	{
		if (RobotFactory.Instance == null)
		{
			ShowNotEnoughCoinsWarning("⚠️ Chưa có Nhà Máy! Hãy bấm 'XÂY NHÀ MÁY ROBOT' trước.");
			return;
		}

		bool queued = RobotFactory.Instance.QueueCraftRobot();
		if (queued)
		{
			UpdateUI();
			Transform btnT = craftRobotButton != null ? craftRobotButton.transform : (defenseRobotButton != null ? defenseRobotButton.transform : null);
			if (btnT != null)
			{
				StartCoroutine(AnimateButtonBounce(btnT));
			}
		}
		else
		{
			ShowNotEnoughCoinsWarning("⚠️ Quân đội đã đạt tối đa (8/8)! Robot đang bảo vệ căn cứ.");
		}
	}

	public void OnDefenseRobotClicked()
	{
		OnCraftRobotClicked();
	}

	public void OnUpgradePickaxe()
	{
		if (GameEconomy.Instance != null)
		{
			if (GameEconomy.Instance.UpgradePickaxe())
			{
				CloseShopPanel();
			}
		}
	}

	public void OnSelectWall()
	{
		if (GameEconomy.Instance != null && GameEconomy.Instance.wallSegmentCost > 0 && GameEconomy.Instance.coins < GameEconomy.Instance.wallSegmentCost)
		{
			ShowNotEnoughCoinsWarning("Không đủ vàng để mua tường!");
			return;
		}

		if (WallSelectionManager.Instance != null)
		{
			WallSelectionManager.Instance.DeselectAll();
		}

		CloseShopPanel();
		if (buildingSystem != null)
		{
			// Tìm index của Wall
			int wallIdx = 3;
			for (int i = 0; i < buildingSystem.items.Length; i++)
			{
				if (buildingSystem.items[i] != null && buildingSystem.items[i].isWall)
				{
					wallIdx = i;
					break;
				}
			}
			buildingSystem.SelectItem(wallIdx);
		}
	}

	public void OnUpgradeWall()
	{
		if (GameEconomy.Instance != null)
		{
			if (GameEconomy.Instance.UpgradeGlobalWall())
			{
				CloseShopPanel();
			}
		}
	}

	public void OnRotate()
	{
		if (buildingSystem != null) buildingSystem.RotatePreview(90f);
	}

	public void OnCancel()
	{
		if (buildingSystem != null) buildingSystem.CancelPlacement();
	}

	public void ShowNotEnoughCoinsWarning(string message = "⚠️ Không đủ vàng!")
	{
		if (notEnoughCoinsBanner == null) return;

		if (notEnoughCoinsText != null)
			notEnoughCoinsText.text = message;

		if (warningCoroutine != null)
			StopCoroutine(warningCoroutine);

		warningCoroutine = StartCoroutine(DoShowWarning());
	}

	private IEnumerator DoShowWarning()
	{
		notEnoughCoinsBanner.SetActive(true);
		yield return new WaitForSeconds(1.8f);
		notEnoughCoinsBanner.SetActive(false);
	}
}
