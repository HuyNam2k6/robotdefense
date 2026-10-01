using UnityEngine;
using UnityEngine.UI;

public class BuildingShopUI : MonoBehaviour
{
	public BuildingSystem buildingSystem;

	[Header("--- Các Nút Bấm Trên Giao Diện ---")]
	public Button turretButton;
	public Button wallButton;
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

		if (rotateButton != null)
			rotateButton.onClick.AddListener(OnRotate);

		if (cancelButton != null)
			cancelButton.onClick.AddListener(OnCancel);
	}

	void Update()
	{
		if (buildingSystem != null)
		{
			// Nút Xoay và nút Hủy chỉ hiện lên khi đang có bóng xem trước (Hologram)
			bool isPlacing = buildingSystem.IsPlacing;
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
