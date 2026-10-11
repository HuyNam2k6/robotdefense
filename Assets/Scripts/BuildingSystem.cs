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

	[Header("--- Giới Hạn Mua & Miễn Phí Ban Đầu ---")]
	public const int MaxWeaponLimit = 3; // Giới hạn mua tối đa là 3 cho tất cả các loại vũ khí
	public static int freePhaotuhanhRemaining = 1; // Miễn phí 1 Pháo Tự Hành khi mới bắt đầu
	public static int freeThunderRemaining = 1;     // Miễn phí 1 Thunder khi mới bắt đầu

	public static int GetTurretCountByType(TurretType type)
	{
		int count = 0;
		var list = UpgradableTurret.AllTurrets;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i] != null && list[i].gameObject.activeInHierarchy && list[i].turretType == type)
			{
				count++;
			}
		}
		return count;
	}

	public static int GetWeaponCount(int itemIndex)
	{
		if (itemIndex == 0) return GetTurretCountByType(TurretType.Phaotuhanh);
		if (itemIndex == 1) return GetTurretCountByType(TurretType.Thunder);
		if (itemIndex == 2) return GetTurretCountByType(TurretType.FlamethrowerTurret);
		return 0;
	}

	public static bool CanPurchaseWeapon(int itemIndex, out string reason)
	{
		if (itemIndex == 3) // Tường: Không giới hạn
		{
			reason = "";
			return true;
		}

		int count = GetWeaponCount(itemIndex);
		if (count >= MaxWeaponLimit)
		{
			string wName = (itemIndex == 0) ? "Pháo Tự Hành" : (itemIndex == 1) ? "Trụ Sét Thunder" : "Súng Phun Lửa";
			reason = $"⚠️ Đã đạt giới hạn tối đa ({count}/{MaxWeaponLimit}) cho {wName}!";
			return false;
		}

		reason = "";
		return true;
	}

	public static int GetWeaponEffectiveCost(int itemIndex, int defaultCost)
	{
		if (itemIndex == 0 && freePhaotuhanhRemaining > 0) return 0; // Miễn phí lần đầu
		if (itemIndex == 1 && freeThunderRemaining > 0) return 0;     // Miễn phí lần đầu
		return defaultCost;
	}

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
		// Phím tắt nhanh 1 (Pháo Tự Hành), 2 (Trụ Sét), 3 (Súng Phun Lửa), 4 (Tường)
		if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
		{
			SelectItem(0);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
		{
			SelectItem(1);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
		{
			SelectItem(2);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
		{
			SelectItem(3);
		}

		if (selectedIndex < 0) return;

		// Phím R: Xoay công trình 90 độ chuẩn lưới
		if (Input.GetKeyDown(KeyCode.R))
		{
			RotatePreview(90f);
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

		// 1. XỬ LÝ CHO BỨC TƯỜNG (KÉO DÀI THÀNH HÀNG TƯỜNG HOẶC ĐẶT TỪNG ĐOẠN)
		if (isCurrentWall)
		{
			if (hasGroundHit)
			{
				float segLen = 1.425f; // Bước tường nhỏ gọn chuẩn tỉ lệ (nhỏ đi gấp 2 theo yêu cầu)

				// Bắt đầu kéo khi nhấn chuột trái xuống
				if (Input.GetMouseButtonDown(0))
				{
					if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
					{
						isDraggingWall = true;
						wallDragStart = GetWallPlacementSnap(hit.point, segLen);
					}
				}

				Vector3 startSnap = isDraggingWall ? wallDragStart : GetWallPlacementSnap(hit.point, segLen);
				Vector3 curSnap = SnapToGrid(hit.point, segLen);

				float dx = curSnap.x - startSnap.x;
				float dz = curSnap.z - startSnap.z;

				// Phân biệt: kéo thành hàng dài hay chỉ là click chuột đặt 1 đoạn đơn
				bool isDragMove = (Mathf.Abs(dx) >= segLen * 0.5f) || (Mathf.Abs(dz) >= segLen * 0.5f);

				int count = 1;
				// Khi không kéo dài: góc xoay tuân thủ 100% hướng xoay người chơi vừa bấm nút XOAY hoặc phím R
				bool isHorizontal = (Mathf.Abs(currentYRotation - 0f) < 25f || Mathf.Abs(currentYRotation - 180f) < 25f);
				float dirSign = 1f;
				Quaternion wallRot = Quaternion.Euler(0f, currentYRotation, 0f);

				if (isDraggingWall && isDragMove)
				{
					// Khi kéo chuột: tự động xác định hàng ngang hay dọc theo hướng kéo
					isHorizontal = Mathf.Abs(dx) >= Mathf.Abs(dz);
					count = isHorizontal 
						? Mathf.RoundToInt(Mathf.Abs(dx) / segLen) + 1 
						: Mathf.RoundToInt(Mathf.Abs(dz) / segLen) + 1;

					dirSign = isHorizontal ? (dx >= 0 ? 1f : -1f) : (dz >= 0 ? 1f : -1f);
					wallRot = isHorizontal ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
				}

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
						// Giới hạn số đoạn kéo theo số vàng
						if (wallCost > 0 && count > maxCanAfford)
						{
							count = maxCanAfford;
							if (GameEconomy.Instance != null)
							{
								GameEconomy.Instance.TriggerNotEnoughCoins("Đã hết vàng! Không thể kéo dài thêm tường!");
							}
						}

						EnsurePreviewCount(count);
						float fixedY = startSnap.y; // KHÓA ĐỘ CAO CHUẨN: Cả hàng nằm phẳng lì trên 1 mặt bằng!

						for (int i = 0; i < count; i++)
						{
							float x = isHorizontal ? startSnap.x + i * segLen * dirSign : startSnap.x;
							float z = isHorizontal ? startSnap.z : startSnap.z + i * segLen * dirSign;

							previewPool[i].SetActive(true);
							previewPool[i].transform.position = new Vector3(x, fixedY, z);
							previewPool[i].transform.rotation = wallRot;
						}
					}
				}
				else
				{
					// Chưa nhấn chuột: 1 đoạn tường hologram di chuyển theo chuột, giữ đúng hướng xoay người chơi đã chọn
					Vector3 hoverSnap = GetWallPlacementSnap(hit.point, segLen);

					EnsurePreviewCount(1);
					previewPool[0].SetActive(true);
					previewPool[0].transform.position = hoverSnap;
					previewPool[0].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
				}

				// Thả chuột trái: XÂY DỰNG TOÀN BỘ CÁC ĐOẠN TƯỜNG
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
							float fixedY = startSnap.y; // KHÓA ĐỘ CAO CHUẨN

							for (int i = 0; i < count; i++)
							{
								float x = isHorizontal ? startSnap.x + i * segLen * dirSign : startSnap.x;
								float z = isHorizontal ? startSnap.z : startSnap.z + i * segLen * dirSign;

								SpawnRealObject(items[selectedIndex].prefab, new Vector3(x, fixedY, z), wallRot);
							}

							// Nếu vừa kéo hàng dài: cập nhật currentYRotation theo hướng vừa kéo để preview tiếp theo cùng hướng
							if (isDragMove)
							{
								currentYRotation = isHorizontal ? 0f : 90f;
							}

							Debug.Log($"<color=cyan>[Bức Tường]</color> Đã kéo và xây thành công {count} đoạn tường chuẩn lưới thẳng hàng khít rịt!");
						}
					}

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
						// 1. Kiểm tra giới hạn mua tối đa là 3 cho vũ khí (Tường không giới hạn)
						if (!CanPurchaseWeapon(selectedIndex, out string limitReason))
						{
							if (GameEconomy.Instance != null) GameEconomy.Instance.TriggerNotEnoughCoins(limitReason);
							CancelPlacement();
							return;
						}

						// 2. Kiểm tra giá tiền thực tế (có miễn phí 1 Thunder và 1 Pháo Tự Hành ban đầu không)
						int itemCost = GetWeaponEffectiveCost(selectedIndex, items[selectedIndex].cost);
						if (itemCost > 0 && GameEconomy.Instance != null)
						{
							if (!GameEconomy.Instance.SpendCoins(itemCost))
							{
								GameEconomy.Instance.TriggerNotEnoughCoins($"Không đủ vàng để mua {items[selectedIndex].itemName}!");
								return;
							}
						}

						// 3. Tiêu hao lượt miễn phí ban đầu nếu vừa sử dụng
						if (selectedIndex == 0 && freePhaotuhanhRemaining > 0)
						{
							freePhaotuhanhRemaining--;
							Debug.Log("<color=#00FF88>🎁 [Khởi Đầu Miễn Phí]</color> Đã mua và đặt Pháo Tự Hành MIỄN PHÍ thành công!");
						}
						else if (selectedIndex == 1 && freeThunderRemaining > 0)
						{
							freeThunderRemaining--;
							Debug.Log("<color=#00FF88>🎁 [Khởi Đầu Miễn Phí]</color> Đã mua và đặt Trụ Sét Thunder MIỄN PHÍ thành công!");
						}

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

	/// <summary>
	/// Tự động hút dính khít (Smart Magnetic Snap) vào mép tường có sẵn hoặc snap vào ô lưới
	/// </summary>
	private Vector3 GetWallPlacementSnap(Vector3 rawPoint, float segLen)
	{
		// 1. Tìm đoạn tường đã có trong bán kính 1.45m để hút dính khít theo các socket vuông góc
		WallSegment nearest = null;
		float minDistSqr = 1.45f * 1.45f;

		for (int i = 0; i < WallSegment.AllWalls.Count; i++)
		{
			WallSegment w = WallSegment.AllWalls[i];
			if (w == null || !w.gameObject.activeInHierarchy) continue;

			float dSqr = (w.transform.position.x - rawPoint.x) * (w.transform.position.x - rawPoint.x) + 
			             (w.transform.position.z - rawPoint.z) * (w.transform.position.z - rawPoint.z);
			if (dSqr < minDistSqr)
			{
				minDistSqr = dSqr;
				nearest = w;
			}
		}

		if (nearest != null)
		{
			Vector3 nearPos = nearest.transform.position;
			// 4 vị trí socket xung quanh tường gần nhất
			Vector3[] sockets = new Vector3[]
			{
				new Vector3(nearPos.x + segLen, nearPos.y, nearPos.z),
				new Vector3(nearPos.x - segLen, nearPos.y, nearPos.z),
				new Vector3(nearPos.x, nearPos.y, nearPos.z + segLen),
				new Vector3(nearPos.x, nearPos.y, nearPos.z - segLen)
			};

			Vector3 bestSocket = sockets[0];
			float bestDistSqr = float.MaxValue;
			for (int s = 0; s < sockets.Length; s++)
			{
				float d = (sockets[s].x - rawPoint.x) * (sockets[s].x - rawPoint.x) + 
				          (sockets[s].z - rawPoint.z) * (sockets[s].z - rawPoint.z);
				if (d < bestDistSqr)
				{
					bestDistSqr = d;
					bestSocket = sockets[s];
				}
			}
			bestSocket.y = SampleGroundY(bestSocket.x, bestSocket.z, nearPos.y);
			return bestSocket;
		}

		// 2. Nếu ở khoảng đất trống chưa có tường -> Snap theo lưới chuẩn
		Vector3 gridSnap = SnapToGrid(rawPoint, segLen);
		gridSnap.y = SampleGroundY(gridSnap.x, gridSnap.z, rawPoint.y);
		return gridSnap;
	}

	private float SampleGroundY(float x, float z, float defaultY)
	{
		RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 100f, z), Vector3.down, 200f, groundLayer);
		if (hits != null && hits.Length > 0)
		{
			System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
			foreach (var h in hits)
			{
				if (h.collider == null || h.collider.isTrigger) continue;
				if (h.collider.GetComponentInParent<WallSegment>() != null) continue;
				if (h.collider.GetComponentInParent<WorkerBot>() != null) continue;
				if (h.collider.GetComponentInParent<TurretController>() != null) continue;

				return h.point.y;
			}
		}
		return defaultY;
	}

	GameObject SpawnRealObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		GameObject realObj = Instantiate(prefab, position, rotation);
		realObj.name = prefab.name;

		if (selectedIndex >= 0 && selectedIndex < items.Length && items[selectedIndex].isWall)
		{
			// Hạ thấp chiều cao tường (Y = 0.95f) để pháo có thể bắn qua được
			Vector3 wallScale = new Vector3(1.5f, 0.95f, 1.5f);
			realObj.transform.localScale = wallScale;

			WallSegment ws = realObj.GetComponent<WallSegment>();
			if (ws == null) ws = realObj.AddComponent<WallSegment>();
			ws.SetBaseScale(wallScale);
			if (GameEconomy.Instance != null)
			{
				ws.ApplyLevel(GameEconomy.Instance.wallLevel);
			}
		}
		else
		{
			UpgradableTurret ut = realObj.GetComponent<UpgradableTurret>();
			if (ut == null) ut = realObj.AddComponent<UpgradableTurret>();
			ut.EnsureColliderExists();
		}

		return realObj;
	}

	// Gọi từ nút bấm trên UI Shop hoặc phím tắt
	public void SelectItem(int index)
	{
		if (items == null || index < 0 || index >= items.Length || items[index].prefab == null) return;

		// Kiểm tra giới hạn mua tối đa là 3 cho vũ khí trước khi bắt đầu chọn đặt
		if (!CanPurchaseWeapon(index, out string limitReason))
		{
			if (GameEconomy.Instance != null) GameEconomy.Instance.TriggerNotEnoughCoins(limitReason);
			return;
		}

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
			if (items[selectedIndex].isWall)
			{
				p.transform.localScale = new Vector3(1.5f, 0.95f, 1.5f);
			}

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

	public void RotatePreview(float angle = 90f)
	{
		currentYRotation = (currentYRotation + angle) % 360f;
		if (currentYRotation < 0f) currentYRotation += 360f;
		// Snap tròn vào các góc vuông 0°, 90°, 180°, 270° chuẩn lưới
		currentYRotation = Mathf.Round(currentYRotation / 90f) * 90f % 360f;

		bool isH = (Mathf.Abs(currentYRotation - 0f) < 25f || Mathf.Abs(currentYRotation - 180f) < 25f);
		string dirName = isH ? "HÀNG NGANG (Trục X)" : "HÀNG DỌC (Trục Z)";
		if (Camera.main != null)
		{
			FloatingDamageText.SpawnResource(Camera.main.transform.position + Camera.main.transform.forward * 9.0f, $"🔄 Hướng Đặt: {dirName}", new Color(0.15f, 0.85f, 1f), 0.12f);
		}
		Debug.Log($"<color=cyan>[BuildingSystem]</color> Đã xoay công trình sang hướng: {dirName} (Góc: {currentYRotation}°)");

		for (int i = 0; i < previewPool.Count; i++)
		{
			if (previewPool[i] != null)
			{
				previewPool[i].transform.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
			}
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

