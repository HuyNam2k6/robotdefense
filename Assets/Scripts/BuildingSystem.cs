using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSystem : MonoBehaviour
{
	[System.Serializable]
	public class PlaceableItem
	{
		public string itemName = "Công trình";
		public GameObject prefab;
		public int cost = 0; // Giá mua 0đ
		public Sprite icon;
		public bool isWall = false; // Phân biệt công trình kéo dài (Tường) và công trình đơn chiếc (Pháo)
		public float segmentLength = 1f; // Chiều dài mỗi đoạn khi kéo dài
	}

	[Header("--- Danh Sách Công Trình Trong Shop ---")]
	public PlaceableItem[] items;

	[Header("--- Cài Đặt Mặt Đất & Hologram ---")]
	[Tooltip("Layer mặt đất được phép đặt công trình (Terrain, Ground).")]
	public LayerMask groundLayer = ~0;

	[Tooltip("Material màu xanh dương bán trong suốt dùng cho hình bóng xem trước (Hologram).")]
	public Material blueHologramMaterial;

	[Tooltip("Bật nếu muốn tiếp tục đặt thêm sau khi vừa đặt xong 1 cái đối với công trình đơn.")]
	public bool continuousPlacement = false;

	// Trạng thái nội bộ
	private int selectedIndex = -1;
	private float currentYRotation = 0f;
	private Camera mainCamera;

	// Danh sách các hologram preview (hỗ trợ kéo dài tường gồm nhiều đoạn)
	private List<GameObject> previewPool = new List<GameObject>();

	// Biến hỗ trợ kéo dài tường
	private bool isDraggingWall = false;
	private Vector3 wallDragStart;
	private Vector3 wallDragCurrent;

	public bool IsPlacing => selectedIndex >= 0 && previewPool.Count > 0;
	public int SelectedIndex => selectedIndex;
	public bool IsCurrentItemWall => selectedIndex >= 0 && selectedIndex < items.Length && items[selectedIndex].isWall;

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
		// Phím tắt nhanh 1 (Pháo) và 2 (Tường)
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			SelectItem(0);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			SelectItem(1);
		}

		if (selectedIndex < 0) return;

		// Phím R: Xoay công trình 45 độ trên PC
		if (Input.GetKeyDown(KeyCode.R))
		{
			RotatePreview(45f);
		}

		// Click chuột phải hoặc phím Escape: HỦY ĐẶT
		if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
		{
			CancelPlacement();
			return;
		}

		// Cập nhật vị trí và xử lý kéo thả
		HandlePlacementLogic();
	}

	void HandlePlacementLogic()
	{
		if (mainCamera == null) mainCamera = Camera.main;
		if (mainCamera == null) return;

		Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
		bool hasGroundHit = Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer);

		bool isCurrentWall = items[selectedIndex].isWall;

		// 1. XỬ LÝ CHO BỨC TƯỜNG (KÉO DÀI THÀNH HÀNG TƯỜNG THEO PHONG CÁCH CLASH OF CLANS)
		if (isCurrentWall)
		{
			if (hasGroundHit)
			{
				wallDragCurrent = hit.point;

				// Bắt đầu kéo khi nhấn chuột trái xuống
				if (Input.GetMouseButtonDown(0))
				{
					if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
					{
						isDraggingWall = true;
						wallDragStart = hit.point;
					}
				}

				float segLen = Mathf.Max(0.5f, items[selectedIndex].segmentLength);
				Vector3 startSnap = SnapToGrid(wallDragStart, segLen);
				Vector3 curSnap = SnapToGrid(wallDragCurrent, segLen);

				float dx = curSnap.x - startSnap.x;
				float dz = curSnap.z - startSnap.z;
				bool isHorizontal = Mathf.Abs(dx) >= Mathf.Abs(dz);

				int count = isHorizontal 
					? Mathf.RoundToInt(Mathf.Abs(dx) / segLen) + 1 
					: Mathf.RoundToInt(Mathf.Abs(dz) / segLen) + 1;

				float dirSign = isHorizontal ? (dx >= 0 ? 1f : -1f) : (dz >= 0 ? 1f : -1f);
				// Tường prefab dài 1m theo trục X cục bộ:
				// Nếu kéo ngang (X): rotation = identity (chiều dài nối tiếp nhau theo trục X)
				// Nếu kéo dọc (Z): rotation = Euler(0, 90, 0) (xoay 90 độ để chiều dài nối tiếp nhau theo trục Z)
				Quaternion wallRot = isHorizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);

				int wallCost = (GameEconomy.Instance != null) ? GameEconomy.Instance.wallSegmentCost : 0;
				int currentCoins = (GameEconomy.Instance != null) ? GameEconomy.Instance.coins : 0;
				int maxCanAfford = (wallCost <= 0) ? 99999 : Mathf.Max(0, currentCoins / wallCost);

				// Trong lúc giữ chuột kéo
				if (isDraggingWall)
				{
					if (wallCost > 0 && maxCanAfford <= 0)
					{
						SetAllPreviewsActive(false);
						if (GameEconomy.Instance != null)
						{
							GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để mua tường!");
						}
					}
					else
					{
						// Giới hạn số đoạn kéo theo số vàng (nếu giá > 0)
						if (wallCost > 0 && count > maxCanAfford)
						{
							count = maxCanAfford;
							if (GameEconomy.Instance != null)
							{
								GameEconomy.Instance.TriggerNotEnoughCoins("Đã hết vàng! Không thể kéo dài thêm tường!");
							}
						}

						EnsurePreviewCount(count);
						for (int i = 0; i < count; i++)
						{
							float x = isHorizontal ? startSnap.x + i * segLen * dirSign : startSnap.x;
							float z = isHorizontal ? startSnap.z : startSnap.z + i * segLen * dirSign;
							float y = SampleGroundY(x, z, startSnap.y);

							previewPool[i].SetActive(true);
							previewPool[i].transform.position = new Vector3(x, y, z);
							previewPool[i].transform.rotation = wallRot;
						}
					}
				}
				else
				{
					// Chưa nhấn chuột: 1 đoạn tường hologram di chuyển theo chuột, snap nhẹ theo grid
					Vector3 hoverSnap = SnapToGrid(hit.point, segLen);
					hoverSnap.y = SampleGroundY(hoverSnap.x, hoverSnap.z, hit.point.y);

					EnsurePreviewCount(1);
					previewPool[0].SetActive(true);
					previewPool[0].transform.position = hoverSnap;
					previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
				}

				// Thả chuột trái: XÂY DỰNG TOÀN BỘ CÁC ĐOẠN TƯỜNG CLASH OF CLANS
				if (Input.GetMouseButtonUp(0) && isDraggingWall)
				{
					isDraggingWall = false;

					if (wallCost > 0 && maxCanAfford <= 0)
					{
						if (GameEconomy.Instance != null)
						{
							GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để mua tường!");
						}
					}
					else
					{
						if (wallCost > 0 && count > maxCanAfford) count = maxCanAfford;
						int totalCost = count * wallCost;

						if (wallCost <= 0 || GameEconomy.Instance == null || GameEconomy.Instance.SpendCoins(totalCost))
						{
							for (int i = 0; i < count; i++)
							{
								float x = isHorizontal ? startSnap.x + i * segLen * dirSign : startSnap.x;
								float z = isHorizontal ? startSnap.z : startSnap.z + i * segLen * dirSign;
								float y = SampleGroundY(x, z, startSnap.y);

								SpawnRealObject(items[selectedIndex].prefab, new Vector3(x, y, z), wallRot);
							}

							Debug.Log($"<color=cyan>[Tường Clash of Clans]</color> Đã kéo và xây thành công {count} đoạn tường chuẩn lưới!");
						}
					}

					// Giữ chế độ để người chơi tiếp tục kéo đoạn tiếp theo
					EnsurePreviewCount(1);
				}
			}
			else
			{
				SetAllPreviewsActive(false);
			}
		}
		// 2. XỬ LÝ CHO CÔNG TRÌNH ĐƠN (Ụ PHÁO)
		else
		{
			EnsurePreviewCount(1);

			if (hasGroundHit)
			{
				previewPool[0].SetActive(true);
				previewPool[0].transform.position = hit.point;
				previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);

				if (Input.GetMouseButtonDown(0))
				{
					if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
					{
						SpawnRealObject(items[selectedIndex].prefab, hit.point, Quaternion.Euler(0f, currentYRotation, 0f));

						if (!continuousPlacement)
						{
							CancelPlacement();
						}
					}
				}
			}
			else
			{
				previewPool[0].SetActive(false);
			}
		}
	}

	private Vector3 SnapToGrid(Vector3 pos, float grid)
	{
		return new Vector3(
			Mathf.Round(pos.x / grid) * grid,
			pos.y,
			Mathf.Round(pos.z / grid) * grid
		);
	}

	private float SampleGroundY(float x, float z, float defaultY)
	{
		if (Physics.Raycast(new Vector3(x, 100f, z), Vector3.down, out RaycastHit hit, 200f, groundLayer))
		{
			return hit.point.y;
		}
		return defaultY;
	}

	GameObject SpawnRealObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GameObject realObj = Instantiate(prefab, position, rotation);
		realObj.name = prefab.name;

		if (selectedIndex >= 0 && selectedIndex < items.Length && items[selectedIndex].isWall)
		{
			WallSegment ws = realObj.GetComponent<WallSegment>();
			if (ws == null) ws = realObj.AddComponent<WallSegment>();
			if (GameEconomy.Instance != null)
			{
				ws.ApplyLevel(GameEconomy.Instance.wallLevel);
			}
		}

		return realObj;
	}

	// Gọi từ nút bấm trên UI Shop
	public void SelectItem(int index)
	{
		if (items == null || index < 0 || index >= items.Length || items[index].prefab == null) return;

		if (selectedIndex == index)
		{
			CancelPlacement();
			return;
		}

		CancelPlacement();
		selectedIndex = index;
		EnsurePreviewCount(1);
	}

	// Đảm bảo số lượng Hologram Preview đáp ứng đủ số đoạn tường đang kéo
	void EnsurePreviewCount(int count)
	{
		if (selectedIndex < 0 || selectedIndex >= items.Length || items[selectedIndex].prefab == null) return;

		GameObject prefab = items[selectedIndex].prefab;

		// Tạo thêm nếu thiếu
		while (previewPool.Count < count)
		{
			GameObject p = Instantiate(prefab);
			p.name = $"[Hologram_Preview_{previewPool.Count}]";

			foreach (Collider col in p.GetComponentsInChildren<Collider>()) col.enabled = false;
			foreach (MonoBehaviour mb in p.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;

			if (blueHologramMaterial != null)
			{
				foreach (Renderer rend in p.GetComponentsInChildren<Renderer>())
				{
					Material[] mats = new Material[rend.sharedMaterials.Length];
					for (int i = 0; i < mats.Length; i++) mats[i] = blueHologramMaterial;
					rend.sharedMaterials = mats;
				}
			}

			previewPool.Add(p);
		}

		// Kích hoạt đủ số lượng và ẩn phần thừa
		for (int i = 0; i < previewPool.Count; i++)
		{
			previewPool[i].SetActive(i < count);
		}
	}

	void SetAllPreviewsActive(bool active)
	{
		for (int i = 0; i < previewPool.Count; i++)
		{
			if (previewPool[i] != null) previewPool[i].SetActive(active);
		}
	}

	public void RotatePreview(float angle = 45f)
	{
		currentYRotation = (currentYRotation + angle) % 360f;
		if (previewPool.Count > 0 && previewPool[0] != null && !isDraggingWall)
		{
			previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
		}
	}

	public void CancelPlacement()
	{
		selectedIndex = -1;
		isDraggingWall = false;

		for (int i = 0; i < previewPool.Count; i++)
		{
			if (previewPool[i] != null) Destroy(previewPool[i]);
		}
		previewPool.Clear();
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

