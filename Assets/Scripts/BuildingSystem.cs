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

		// 1. XỬ LÝ CHO BỨC TƯỜNG (KÉO DÀI THÀNH HÀNG TƯỜNG HOẶC CLICK ĐẶT LIÊN TỤC)
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

				// Trong lúc giữ chuột kéo
				if (isDraggingWall)
				{
					Vector3 delta = wallDragCurrent - wallDragStart;
					delta.y = 0f;
					float dist = delta.magnitude;

					if (dist < 0.6f)
					{
						// Khoảng cách ngắn: hiển thị 1 đoạn tường
						EnsurePreviewCount(1);
						previewPool[0].SetActive(true);
						previewPool[0].transform.position = wallDragStart;
						previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
					}
					else
					{
						// Khoảng cách dài: KÉO DÀI THÀNH HÀNG TƯỜNG HOLOGRAM
						float segLen = Mathf.Max(0.5f, items[selectedIndex].segmentLength);
						int count = Mathf.Max(1, Mathf.RoundToInt(dist / segLen));
						float step = dist / count;
						Vector3 dir = delta.normalized;
						Quaternion wallRot = Quaternion.LookRotation(dir, Vector3.up);

						EnsurePreviewCount(count);
						for (int i = 0; i < count; i++)
						{
							Vector3 segPos = wallDragStart + dir * (i * step + step * 0.5f);
							previewPool[i].SetActive(true);
							previewPool[i].transform.position = segPos;
							previewPool[i].transform.rotation = wallRot;
						}
					}
				}
				else
				{
					// Chưa nhấn chuột: 1 đoạn tường hologram di chuyển theo chuột
					EnsurePreviewCount(1);
					previewPool[0].SetActive(true);
					previewPool[0].transform.position = hit.point;
					previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
				}

				// Thả chuột trái: XÂY DỰNG TOÀN BỘ CÁC ĐOẠN TƯỜNG ĐÃ KÉO
				if (Input.GetMouseButtonUp(0) && isDraggingWall)
				{
					isDraggingWall = false;

					Vector3 delta = wallDragCurrent - wallDragStart;
					delta.y = 0f;
					float dist = delta.magnitude;

					if (dist < 0.6f)
					{
						// Đặt 1 đoạn tường đơn
						SpawnRealObject(items[selectedIndex].prefab, wallDragStart, Quaternion.Euler(0f, currentYRotation, 0f));
					}
					else
					{
						// Xây cả một hàng tường nối tiếp nhau
						float segLen = Mathf.Max(0.5f, items[selectedIndex].segmentLength);
						int count = Mathf.Max(1, Mathf.RoundToInt(dist / segLen));
						float step = dist / count;
						Vector3 dir = delta.normalized;
						Quaternion wallRot = Quaternion.LookRotation(dir, Vector3.up);

						for (int i = 0; i < count; i++)
						{
							Vector3 segPos = wallDragStart + dir * (i * step + step * 0.5f);
							SpawnRealObject(items[selectedIndex].prefab, segPos, wallRot);
						}

						Debug.Log($"<color=cyan>[Tường]</color> Đã kéo dài và xây dựng thành công hàng tường gồm {count} đoạn!");
					}

					// KHÔNG THOÁT RA: Giữ nguyên chế độ để người chơi tiếp tục kéo bức tường tiếp theo!
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

	void SpawnRealObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GameObject realObj = Instantiate(prefab, position, rotation);
		realObj.name = prefab.name;
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

