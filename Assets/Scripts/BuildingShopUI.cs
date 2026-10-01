using UnityEngine;
using UnityEngine.UI;

public class BuildingShopUI : MonoBehaviour
{
	public BuildingSystem buildingSystem;

	[Header("--- Nút Mua Hàng Trong Shop ---")]
	[Header("--- Nút Mở Cửa Hàng ---")]
	[Tooltip("Nút có hình Cửa Hàng hiển thị trên màn hình")]
	public Button shopOpenButton;

	[Header("--- Bảng Cửa Hàng Popup (Shop Panel) ---")]
	[Tooltip("Bảng danh sách công trình (Ụ pháo & Tường), xuất hiện sau khi nhấn nút Cửa Hàng")]
	public GameObject shopPanel;
	public Button closeShopButton;

	[Header("--- Nút Chọn Công Trình Trong Bảng ---")]
	public Button turretButton;
	public Button wallButton;

	[Header("--- Bảng Điều Khiển Nổi Khi Đang Đặt (Placement HUD) ---")]
	[Tooltip("Khung chứa biểu tượng xoay và hủy, chỉ hiện khi đang chọn đặt công trình.")]
	public GameObject placementHUD;

	[Tooltip("Nút biểu tượng xoay (icon ⟳)")]
	public Button rotateIconButton;

	[Tooltip("Nút biểu tượng hủy (icon ✕)")]
	public Button cancelIconButton;

	// Tương thích ngược nếu còn nút cũ
	public Button rotateButton;
	public Button cancelButton;

	void Start()
	{
		if (buildingSystem == null)
			buildingSystem = FindAnyObjectByType<BuildingSystem>();

		if (shopOpenButton != null)
			shopOpenButton.onClick.AddListener(ToggleShopPanel);

		if (closeShopButton != null)
			closeShopButton.onClick.AddListener(CloseShopPanel);

		if (turretButton != null)
			turretButton.onClick.AddListener(OnSelectTurret);

		if (wallButton != null)
			wallButton.onClick.AddListener(OnSelectWall);

		if (rotateIconButton != null)
			rotateIconButton.onClick.AddListener(OnRotate);
		else if (rotateButton != null)
			rotateButton.onClick.AddListener(OnRotate);

		if (cancelIconButton != null)
			cancelIconButton.onClick.AddListener(OnCancel);
		else if (cancelButton != null)
			cancelButton.onClick.AddListener(OnCancel);

		// Mặc định lúc bắt đầu game: Bảng Shop đóng, chỉ hiển thị nút Cửa Hàng
		if (shopPanel != null)
			shopPanel.SetActive(false);
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

			if (cancelButton != null && cancelButton.gameObject.activeSelf != isPlacing)
				cancelButton.gameObject.SetActive(isPlacing);

			// Khi đang ở chế độ xem trước đặt công trình thì ẩn nút mở shop để màn hình thông thoáng
			if (shopOpenButton != null && shopOpenButton.gameObject.activeSelf == isPlacing)
			{
				shopOpenButton.gameObject.SetActive(!isPlacing);
			}
		}
	}

	public void ToggleShopPanel()
	{
		if (shopPanel != null)
		{
			shopPanel.SetActive(!shopPanel.activeSelf);
		}
	}

	public void OpenShopPanel()
	{
		if (shopPanel != null) shopPanel.SetActive(true);
	}

	public void CloseShopPanel()
	{
		if (shopPanel != null) shopPanel.SetActive(false);
	}

	public void OnSelectTurret()
	{
		CloseShopPanel();
		if (buildingSystem != null) buildingSystem.SelectItem(0);
	}

	public void OnSelectWall()
	{
		CloseShopPanel();
		if (buildingSystem != null) buildingSystem.SelectItem(1);
	}

	public void OnRotate()
	{
		if (buildingSystem != null) buildingSystem.RotatePreview(45f);
	}

	public void OnCancel()
	{
		if (buildingSystem != null) buildingSystem.CancelPlacement();
	}
}

