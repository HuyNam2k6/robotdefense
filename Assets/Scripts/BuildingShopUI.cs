using UnityEngine;
using UnityEngine.UI;

public class BuildingShopUI : MonoBehaviour
{
	public BuildingSystem buildingSystem;

	[Header("--- Nút Mua Hàng Trong Shop ---")]
	public Button turretButton;
	public Button wallButton;

	[Header("--- Bảng Điều Khiển Nổi Khi Đang Đặt (Placement HUD) ---")]
	[Tooltip("Khung chứa biểu tượng xoay và hủy, chỉ hiện khi đang chọn đặt công trình.")]
	public GameObject placementHUD;

	[Tooltip("Nút biểu tượng xoay (icon ↻)")]
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

			if (rotateButton != null && rotateButton.gameObject.activeSelf != isPlacing)
				rotateButton.gameObject.SetActive(isPlacing);

			if (cancelButton != null && cancelButton.gameObject.activeSelf != isPlacing)
				cancelButton.gameObject.SetActive(isPlacing);
		}
	}

	public void OnSelectTurret()
	{
		if (buildingSystem != null) buildingSystem.SelectItem(0);
	}

	public void OnSelectWall()
	{
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

