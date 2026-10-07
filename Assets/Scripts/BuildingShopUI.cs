using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BuildingShopUI : MonoBehaviour
{
	public BuildingSystem buildingSystem;

	[Header("--- Nút Icon Giỏ Hàng & Dashboard Ngang ---")]
	public Button shoppingCartButton; // Icon giỏ hàng 🛒
	public GameObject dashboardPanel;  // Bảng dashboard nằm ngang
	public Button closeDashboardButton; // Nút đóng ✕

	// Thuộc tính tương thích ngược
	public Button shopOpenButton { get => shoppingCartButton; set => shoppingCartButton = value; }
	public GameObject shopPanel { get => dashboardPanel; set => dashboardPanel = value; }
	public Button closeShopButton { get => closeDashboardButton; set => closeDashboardButton = value; }

	[Header("--- Thông Tin Tiền Tệ & Tài Nguyên ---")]
	public Text coinText;
	public Text stoneText;
	public Text woodText;

	[Header("--- Mục 1: Robot Đào Mỏ & Cúp Sắt ---")]
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

	void Awake()
	{
		// Đảm bảo Canvas không bị scale = 0
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

		if (closeShopButton != null)
			closeShopButton.onClick.AddListener(CloseShopPanel);

		// 1. Robot Đào & Cúp Sắt
		if (buyWorkerButton != null)
			buyWorkerButton.onClick.AddListener(OnBuyWorker);

		if (upgradePickaxeButton != null)
			upgradePickaxeButton.onClick.AddListener(OnUpgradePickaxe);

		// 2. Nhà Máy Sản Xuất & Chế Tạo Robot (15s)
		if (buildFactoryButton != null)
			buildFactoryButton.onClick.AddListener(OnBuildFactoryClicked);

		if (craftRobotButton != null)
			craftRobotButton.onClick.AddListener(OnCraftRobotClicked);
		else if (defenseRobotButton != null && defenseRobotButton != buildFactoryButton)
			defenseRobotButton.onClick.AddListener(OnCraftRobotClicked);

		// 3. Tường & Nâng Cấp Tường
		if (wallButton != null)
			wallButton.onClick.AddListener(OnSelectWall);

		if (upgradeWallButton != null)
			upgradeWallButton.onClick.AddListener(OnUpgradeWall);

		// Điều khiển xoay / hủy
		if (rotateIconButton != null)
			rotateIconButton.onClick.AddListener(OnRotate);

		if (cancelIconButton != null)
			cancelIconButton.onClick.AddListener(OnCancel);

		// Đăng ký event với GameEconomy
		if (GameEconomy.Instance != null)
		{
			GameEconomy.Instance.OnEconomyChanged += UpdateUI;
			GameEconomy.Instance.OnNotEnoughCoins += ShowNotEnoughCoinsWarning;
		}

		if (notEnoughCoinsBanner != null)
			notEnoughCoinsBanner.SetActive(false);

		// Mặc định ban đầu: Ẩn dashboard, chỉ hiện icon giỏ hàng 🛒 khi nhấn vào mới mở
		if (dashboardPanel != null)
			dashboardPanel.SetActive(false);

		if (shoppingCartButton != null)
			shoppingCartButton.gameObject.SetActive(true);

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
		if (buildingSystem != null)
		{
			bool isPlacing = buildingSystem.IsPlacing;

			if (placementHUD != null && placementHUD.activeSelf != isPlacing)
			{
				placementHUD.SetActive(isPlacing);
			}

			// Khi đang kéo đặt công trình: ẩn dashboard và icon giỏ hàng để dễ quan sát mặt đất
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

		// Cập nhật thời gian thực khi đang mở Shop để thấy đồng hồ đếm ngược 15s nhảy từng giây
		if (dashboardPanel != null && dashboardPanel.activeSelf)
		{
			if (Time.time >= nextLiveUiUpdate)
			{
				nextLiveUiUpdate = Time.time + 0.25f;
				UpdateLiveFactoryUI();
			}
		}
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
			coinText.text = $"🪙 <b>{GameEconomy.Instance.coins}</b> Vàng";

		if (stoneText != null)
			stoneText.text = $"🪨 <b>{GameEconomy.Instance.stoneCount}</b> Đá";

		if (woodText != null)
			woodText.text = $"🪵 <b>{GameEconomy.Instance.woodCount}</b> Gỗ";

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
			wallButtonText.text = $"🧱 <b>MUA TƯỜNG (KÉO TRÁI/PHẢI)</b>\n<color=#FFD700>🪙 {GameEconomy.Instance.wallSegmentCost} Vàng/Đoạn</color>";
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
		if (shopPanel != null)
		{
			shopPanel.SetActive(!shopPanel.activeSelf);
			if (shopPanel.activeSelf && WallSelectionManager.Instance != null)
			{
				WallSelectionManager.Instance.DeselectWall();
			}
			UpdateUI();
		}
	}

	public void CloseShopPanel()
	{
		if (shopPanel != null) shopPanel.SetActive(false);
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
		btnTransform.localScale = originalScale * 0.85f;
		yield return new WaitForSeconds(0.08f);
		btnTransform.localScale = originalScale * 1.12f;
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

	/// <summary>
	/// Bấm Xây Nhà Máy từ Cửa Hàng: Chuyển sang chế độ Hologram của BuildingSystem
	/// </summary>
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

			if (factoryIndex < 0) factoryIndex = 2; // fallback

			CloseShopPanel();
			buildingSystem.SelectItem(factoryIndex);
			Debug.Log("<color=#00FF88>[Xây Nhà Máy]</color> Chuyển sang chế độ đặt Nhà Máy! Nhấn chuột trái lên mặt đất để xây.");
		}
	}

	/// <summary>
	/// Bấm Chế Tạo Robot (15s) từ Cửa Hàng
	/// </summary>
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
			WallSelectionManager.Instance.DeselectWall();
		}

		CloseShopPanel();
		if (buildingSystem != null)
		{
			buildingSystem.SelectItem(1); // 1 là Tường trong BuildingSystem
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
