using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSystem : MonoBehaviour
{
	[System.Serializable]
	public class PlaceableItem
	{
		public string itemName;
		public GameObject prefab;
		public int cost = 10;
		public Sprite icon;
	}

	[Header("--- Danh Sách Công Trình Trong Shop ---")]
	public PlaceableItem[] items;

	[Header("--- Cài Đặt Mặt Đất & Hologram ---")]
	[Tooltip("Layer mặt đất được phép đặt công trình (Terrain, Ground). Mặc định là Default.")]
	public LayerMask groundLayer = ~0;

	[Tooltip("Material màu xanh dương bán trong suốt dùng cho hình bóng xem trước (Hologram).")]
	public Material blueHologramMaterial;

	[Tooltip("Bật nếu muốn tiếp tục đặt thêm sau khi vừa đặt xong 1 cái.")]
	public bool continuousPlacement = false;

	// Trạng thái nội bộ
	private int selectedIndex = -1;
	private GameObject previewInstance;
	private float currentYRotation = 0f;
	private Camera mainCamera;

	public bool IsPlacing => selectedIndex >= 0 && previewInstance != null;

	void Start()
	{
		mainCamera = Camera.main;
		if (blueHologramMaterial == null)
		{
			CreateDefaultHologramMaterial();
		}
	}

	void Update()
	{
		if (selectedIndex < 0 || previewInstance == null) return;

		UpdatePreviewPosition();

		// Phím R: Xoay công trình 45 độ trên PC
		if (Input.GetKeyDown(KeyCode.R))
		{
			RotatePreview(45f);
		}

		// Click chuột trái / chạm màn hình: ĐẶT CÔNG TRÌNH
		if (Input.GetMouseButtonDown(0))
		{
			// Không đặt nếu ngón tay/chuột đang bấm đè lên các nút UI
			if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
				return;

			ConfirmPlacement();
		}

		// Click chuột phải hoặc phím Escape: HỦY ĐẶT
		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
		{
			CancelPlacement();
		}
	}

	// Gọi từ nút bấm trên UI Shop
	public void SelectItem(int index)
	{
		if (items == null || index < 0 || index >= items.Length || items[index].prefab == null) return;

		// Nếu đang chọn đúng cái đó thì tắt đi
		if (selectedIndex == index)
		{
			CancelPlacement();
			return;
		}

		CancelPlacement();
		selectedIndex = index;
		CreatePreviewObject();
	}

	void CreatePreviewObject()
	{
		GameObject prefab = items[selectedIndex].prefab;
		previewInstance = Instantiate(prefab);
		previewInstance.name = "[Preview_Ghost]";

		// Tắt toàn bộ Collider trên Preview để không cản trở tia ngắm
		foreach (Collider col in previewInstance.GetComponentsInChildren<Collider>())
		{
			col.enabled = false;
		}

		// Tắt các script chiến đấu / đào mỏ trên Preview
		foreach (MonoBehaviour mb in previewInstance.GetComponentsInChildren<MonoBehaviour>())
		{
			mb.enabled = false;
		}

		// Áp dụng Material màu xanh dương (Hologram) cho toàn bộ mô hình Preview
		if (blueHologramMaterial != null)
		{
			foreach (Renderer rend in previewInstance.GetComponentsInChildren<Renderer>())
			{
				Material[] mats = new Material[rend.sharedMaterials.Length];
				for (int i = 0; i < mats.Length; i++)
				{
					mats[i] = blueHologramMaterial;
				}
				rend.sharedMaterials = mats;
			}
		}
	}

	void UpdatePreviewPosition()
	{
		if (mainCamera == null) mainCamera = Camera.main;
		if (mainCamera == null) return;

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
		{
			previewInstance.SetActive(true);
			previewInstance.transform.position = hit.point;
			previewInstance.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
		}
		else
		{
			previewInstance.SetActive(false);
		}
	}

	public void RotatePreview(float angle = 45f)
	{
		currentYRotation = (currentYRotation + angle) % 360f;
		if (previewInstance != null)
		{
			previewInstance.transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
		}
	}

	void ConfirmPlacement()
	{
		if (previewInstance == null || !previewInstance.activeSelf) return;

		Vector3 placePos = previewInstance.transform.position;
		Quaternion placeRot = previewInstance.transform.rotation;

		// 1. SINH RA CÔNG TRÌNH THẬT: Đúng màu sắc nguyên bản, hoạt động đầy đủ tính năng!
		GameObject realObj = Instantiate(items[selectedIndex].prefab, placePos, placeRot);
		realObj.name = items[selectedIndex].prefab.name;

		Debug.Log($"<color=cyan>[Shop]</color> Đã xây dựng: <b>{items[selectedIndex].itemName}</b> đúng màu gốc tại vị trí {placePos}!");

		// 2. Nếu không bật đặt liên tiếp thì hủy chế độ xem trước
		if (!continuousPlacement)
		{
			CancelPlacement();
		}
	}

	public void CancelPlacement()
	{
		selectedIndex = -1;
		if (previewInstance != null)
		{
			Destroy(previewInstance);
			previewInstance = null;
		}
	}

	void CreateDefaultHologramMaterial()
	{
		Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
		if (shader == null) shader = Shader.Find("Unlit/Color");
		if (shader == null) shader = Shader.Find("Standard");

		if (shader != null)
		{
			blueHologramMaterial = new Material(shader);
			blueHologramMaterial.color = new Color(0f, 0.75f, 1f, 0.65f); // Màu xanh dương neon công nghệ
		}
	}
}
