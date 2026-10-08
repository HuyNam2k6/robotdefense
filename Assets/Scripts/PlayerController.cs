using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerController - Hệ Thống Điều Khiển Nhân Vật Người Chơi Tối Ưu CPU (Snapdragon 810):
/// - Trọng lực thuần code: gravity = -9.18f (Không dùng Rigidbody hay CharacterController PhysX solver).
/// - Di chuyển & tính toán va chạm vật cản 100% bằng Raycast (Tự động trượt mượt mà dọc theo tường/đá).
/// - Nhận sát thương bằng Raycast và kiểm tra đường ngắm (Line of Sight) không phụ thuộc Collider.
/// - Thanh máu: Xóa bỏ hoàn toàn thanh máu nổi trên đầu, hiển thị thanh máu cố định trên HUD (gần Task).
/// </summary>
public class PlayerController : MonoBehaviour
{
	private Animator animator;
	private CharacterController cc; // Giữ để tránh lỗi tham chiếu nếu có code ngoài truy vấn

	[Header("Cài đặt Máu & Sinh Mệnh (Health & Death)")]
	public float maxHealth = 100f;
	public float currentHealth = 100f;
	public bool isDead = false;

	[Header("Điểm Hồi Sinh (Respawn Point)")]
	[Tooltip("Kéo Transform điểm hồi sinh vào đây. Nếu để trống, game sẽ tự động tạo/lấy điểm RespawnPoint")]
	public Transform respawnPoint;
	[Tooltip("Thời gian chờ hồi sinh (giây) sau khi chết")]
	public float respawnDelay = 3.0f;
	[Tooltip("Bật/tắt thanh máu nổi trên đầu Player (Mặc định FALSE theo yêu cầu)")]
	public bool showFloatingHealthBar = false;

	[Header("Cài đặt Di Chuyển")]
	public float moveSpeed = 4.5f;
	public float rotateSpeed = 14f;
	public float turnSmoothTime = 0.06f;
	public float acceleration = 12f;
	public float deceleration = 18f;
	public bool alignWithCamera = true;

	[Header("Cài đặt Trọng Lực & Rơi Tự Do Bằng Code (CPU Optimized)")]
	[Tooltip("Độ mạnh của trọng lực game bằng code = -9.18 theo yêu cầu chính xác")]
	public float gravity = -9.18f;
	[Tooltip("Tốc độ rơi tối đa khi rơi từ độ cao lớn")]
	public float terminalVelocity = -35f;

	[Header("Cài đặt Bật Nhảy & Va Chạm Raycast")]
	public float jumpHeight = 1.6f;
	[Tooltip("Khoảng cách tia Raycast phát hiện vật cản trước mặt")]
	public float obstacleCheckDistance = 0.4f;
	[Tooltip("Độ cao tia Raycast tính từ chân Player (ngang thắt lưng)")]
	public float raycastHeightOffset = 0.6f;

	[Header("Hành Động Khi Đứng Yên (Idle Fidget)")]
	public bool enableIdleFidget = true;
	public float minIdleTime = 5f;
	public float maxIdleTime = 10f;

	[Header("Điều Khiển Trên Mobile")]
	public VirtualJoystick joystick;

	// Biến nội bộ
	private float currentSpeed = 0f;
	private float turnSmoothVelocity;
	private float verticalVelocity = 0f;
	private bool isGrounded = true;
	private Camera mainCam;

	private Vector3 initialSpawnPos;
	private Quaternion initialSpawnRot;

	// Thanh máu HUD gần Task
	private RectTransform hudHealthBarFill;
	private Image hudHealthBarFillImage;
	private Text hudHealthBarText;

	// Đếm thời gian đứng yên
	private float idleTimer = 0f;
	private float nextIdleActionTime = 7f;

	// Quét sát thương Raycast định kỳ
	private float lastRaycastScanTime = 0f;

	void Awake()
	{
		// KHÓA AN TOÀN 1: Nếu trên cùng GameObject có 2 component PlayerController -> Tự hủy bản sao thừa
		PlayerController[] localPCs = GetComponents<PlayerController>();
		if (localPCs.Length > 1 && localPCs[0] != this)
		{
			enabled = false;
			Destroy(this);
			return;
		}

		// Đảm bảo đối tượng luôn có Tag Player chuẩn xác để hệ thống vũ khí tự động bỏ qua
		if (!gameObject.CompareTag("Player"))
		{
			try { gameObject.tag = "Player"; } catch { }
		}

		// RULE 8 & TỐI ƯU CPU: Vô hiệu hóa CharacterController và Rigidbody để hoàn toàn dùng Raycast & Code
		cc = GetComponent<CharacterController>();
		if (cc != null)
		{
			cc.enabled = false; // Tắt hoàn toàn PhysX simulation của CharacterController
		}

		Rigidbody rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			Destroy(rb); // Không dùng Rigidbody
		}

		// Biến Collider thành Trigger hoặc tắt để PhysX không tốn CPU tính toán va chạm cho Player
		Collider col = GetComponent<Collider>();
		if (col != null && !(col is CharacterController))
		{
			col.isTrigger = true;
		}
	}

	void Start()
	{
		animator = GetComponent<Animator>();
		mainCam = Camera.main;

		if (joystick == null)
			joystick = FindAnyObjectByType<VirtualJoystick>();

		initialSpawnPos = transform.position;
		initialSpawnRot = transform.rotation;
		currentHealth = maxHealth;

		EnsureRespawnPointExists();

		// Bám đất tức thì bằng Raycast khi khởi động
		SnapToGroundImmediately();

		// Tạo thanh máu HUD cố định trên màn hình gần Task
		CreateHUDHealthBar();

		// Lên lịch ngẫu nhiên lần đầu tiên (từ 5 đến 10 giây)
		nextIdleActionTime = Random.Range(minIdleTime, maxIdleTime);
	}

	void Update()
	{
		if (!enabled || isDead)
		{
			return;
		}

		// ========== 1. ĐỌC INPUT (BÀN PHÍM HOẶC JOYSTICK) ==========
		float h = Input.GetAxisRaw("Horizontal");
		float v = Input.GetAxisRaw("Vertical");

		if (joystick != null && (Mathf.Abs(joystick.Horizontal) > 0.05f || Mathf.Abs(joystick.Vertical) > 0.05f))
		{
			h = joystick.Horizontal;
			v = joystick.Vertical;
		}

		Vector3 inputDir = new Vector3(h, 0f, v);
		float inputMagnitude = Mathf.Clamp01(inputDir.magnitude);
		bool hasInput = inputMagnitude > 0.05f;

		// ========== 2. XOAY NGƯỜI MƯỢT MÀ THEO CAMERA ==========
		if (hasInput)
		{
			if (animator != null)
			{
				animator.ResetTrigger("wave");
				animator.ResetTrigger("dance");
				animator.ResetTrigger("mine");
			}

			float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;

			if (alignWithCamera && mainCam != null)
				targetAngle += mainCam.transform.eulerAngles.y;

			float smoothAngle = Mathf.SmoothDampAngle(
				transform.eulerAngles.y,
				targetAngle,
				ref turnSmoothVelocity,
				turnSmoothTime
			);
			transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
		}

		// ========== 3. TĂNG / GIẢM TỐC DI CHUYỂN ==========
		float targetSpeed = hasInput ? moveSpeed * inputMagnitude : 0f;
		float rate = (targetSpeed > currentSpeed) ? acceleration : deceleration;
		currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

		// ========== 4. DI CHUYỂN & TÍNH TOÁN VA CHẠM BẰNG RAYCAST (KHÔNG DÙNG COLLIDER) ==========
		if (currentSpeed > 0.01f)
		{
			float moveDist = currentSpeed * Time.deltaTime;
			Vector3 moveDir = transform.forward;
			Vector3 rayOrigin = transform.position + Vector3.up * raycastHeightOffset;

			// Bắn Raycast quét phía trước để phát hiện vật cản (Đá, Cây, Tường, Chướng ngại vật)
			float checkDist = moveDist + obstacleCheckDistance;
			if (Physics.Raycast(rayOrigin, moveDir, out RaycastHit hit, checkDist))
			{
				if (hit.transform != transform && !hit.transform.IsChildOf(transform))
				{
					// Trượt men theo mặt phẳng của vật cản (Slide smoothly along obstacle normal)
					Vector3 slideDir = Vector3.ProjectOnPlane(moveDir, hit.normal);
					slideDir.y = 0f;

					if (slideDir.sqrMagnitude > 0.001f)
					{
						slideDir.Normalize();
						// Kiểm tra xem đường trượt có bị vật cản khác chắn không
						if (!Physics.Raycast(rayOrigin, slideDir, checkDist))
						{
							float slideDot = Mathf.Clamp01(Vector3.Dot(moveDir, slideDir));
							transform.position += slideDir * (currentSpeed * Time.deltaTime * slideDot);
						}
					}
				}
				else
				{
					transform.position += moveDir * moveDist;
				}
			}
			else
			{
				transform.position += moveDir * moveDist;
			}
		}

		// ========== 5. TRỌNG LỰC CODE = -9.18 & BÁM MẶT ĐẤT BẰNG RAYCAST ==========
		float dt = Mathf.Min(Time.deltaTime, 0.04f); // Chặn deltaTime spike
		verticalVelocity += gravity * dt; // v = v0 + g * dt (g = -9.18)

		if (verticalVelocity < terminalVelocity) verticalVelocity = terminalVelocity;
		if (verticalVelocity > 10f) verticalVelocity = 10f; // Chặn xung lực đẩy lên trời

		float groundY = GetTrueGroundHeight(transform.position);
		Vector3 curPos = transform.position;
		float newY = curPos.y + verticalVelocity * dt;

		// Khóa sàn chống rơi xuyên đất
		if (newY <= groundY)
		{
			newY = groundY;
			verticalVelocity = 0f;
			isGrounded = true;
		}
		else
		{
			isGrounded = (curPos.y <= groundY + 0.08f);
		}

		curPos.y = newY;
		transform.position = curPos;

		// ========== 5.1. BẢO VỆ CHỐNG RƠI XUỐNG BIỂN ==========
		if (!isDead)
		{
			EnforceGroundBoundary();
		}

		// ========== 6. ANIMATOR ==========
		if (animator != null)
			animator.SetBool("isMoving", currentSpeed > 0.1f);

		// ========== 7. PHÍM BẤM ĐIỀU KHIỂN TRÊN PC ==========
		if (Input.GetKeyDown(KeyCode.Space))
			OnJumpButtonPressed();

		if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
			OnPunchButtonPressed();

		if (Input.GetKeyDown(KeyCode.H))
			OnWaveButtonPressed();

		if (Input.GetKeyDown(KeyCode.B))
			OnDanceButtonPressed();

		// ========== 8. TỰ ĐỘNG ĐỨNG YÊN LÀM TRÒ ==========
		HandleIdleFidget(hasInput);

		// ========== 9. QUÉT NHẬN SÁT THƯƠNG BẰNG RAYCAST (KHÔNG CẦN COLLIDER) ==========
		CheckIncomingRaycastAttacks();
	}

	/// <summary>
	/// Bám sát mặt đất ngay lập tức bằng Raycast & Terrain
	/// </summary>
	private void SnapToGroundImmediately()
	{
		float gy = GetTrueGroundHeight(transform.position);
		Vector3 p = transform.position;
		p.y = gy;
		transform.position = p;
		verticalVelocity = 0f;
		isGrounded = true;
	}

	/// <summary>
	/// Dò tìm độ cao mặt đất chính xác bằng Raycast từ trên xuống kết hợp Terrain
	/// </summary>
	private float GetTrueGroundHeight(Vector3 pos)
	{
		float terrainGround = -999f;
		if (Terrain.activeTerrain != null)
		{
			terrainGround = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
		}

		// Bắn Raycast từ ngang thắt lưng (pos.y + 0.6f) xuống dưới để tìm sàn/mặt đất
		Vector3 rayStart = new Vector3(pos.x, pos.y + 0.6f, pos.z);
		RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 15f);

		float bestHitY = -999f;
		foreach (var h in hits)
		{
			if (h.transform == transform || h.transform.IsChildOf(transform)) continue;

			string hitName = h.collider.name.ToLower();
			if (hitName.Contains("bullet") || hitName.Contains("enemy") || hitName.Contains("wall") || hitName.Contains("turret")) continue;

			// Chỉ chấp nhận bề mặt sàn/đất dưới chân hoặc gờ thềm nhỏ (<= pos.y + 0.35f)
			if (h.point.y > bestHitY && h.point.y <= pos.y + 0.35f)
			{
				bestHitY = h.point.y;
			}
		}

		return Mathf.Max(terrainGround, bestHitY > -900f ? bestHitY : terrainGround);
	}

	void HandleIdleFidget(bool hasInput)
	{
		if (enableIdleFidget && !hasInput && currentSpeed <= 0.05f && isGrounded)
		{
			idleTimer += Time.deltaTime;

			if (idleTimer >= nextIdleActionTime)
			{
				idleTimer = 0f;
				nextIdleActionTime = Random.Range(minIdleTime, maxIdleTime);

				if (Random.value < 0.5f)
				{
					OnWaveButtonPressed();
				}
				else
				{
					OnDanceButtonPressed();
				}
			}
		}
		else
		{
			idleTimer = 0f;
		}
	}

	// ================= CÁC HÀM XỬ LÝ HÀNH ĐỘNG =================

	// Bật nhảy bằng công thức vận tốc code: v = sqrt(2 * h * -g)
	public void OnJumpButtonPressed()
	{
		if (isGrounded)
		{
			verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

			if (animator != null)
				animator.SetTrigger("jump");
		}
	}

	// Đấm / Đào đá / Chặt cây
	public void OnPunchButtonPressed()
	{
		if (animator != null)
			animator.SetTrigger("mine");

		TryChopNearbyTree();
	}

	private void TryChopNearbyTree()
	{
		Vector3 center = transform.position + transform.forward * 1.0f + Vector3.up * 0.5f;
		Collider[] colliders = Physics.OverlapSphere(center, 2.0f);
		ChoppableTree nearestTree = null;
		float minDist = float.MaxValue;

		foreach (var col in colliders)
		{
			ChoppableTree tree = col.GetComponentInParent<ChoppableTree>();
			if (tree != null && tree.CanBeChopped)
			{
				float dist = Vector3.Distance(transform.position, tree.transform.position);
				if (dist < minDist)
				{
					minDist = dist;
					nearestTree = tree;
				}
			}
		}

		if (nearestTree != null)
		{
			nearestTree.Chop(1, transform);
		}
	}

	public void OnWaveButtonPressed()
	{
		if (animator != null)
			animator.SetTrigger("wave");
	}

	public void OnDanceButtonPressed()
	{
		if (animator != null)
			animator.SetTrigger("dance");
	}

	// ================= HỆ THỐNG CỨU HỘ VỰC BIỂN =================
	private void EnforceGroundBoundary()
	{
		Vector3 currentPos = transform.position;

		// Rơi khỏi đảo xuống biển (Y < 2.0m)
		if (currentPos.y < 2.0f && !isDead)
		{
			Debug.LogWarning("<color=yellow>[Cứu Hộ]</color> Player rơi xuống biển! Kích hoạt tử nạn và hồi sinh...");
			TakeDamage(maxHealth);
		}
	}

	// ================= NHẬN SÁT THƯƠNG BẰNG RAYCAST (THAY VÌ COLLIDER) =================

	/// <summary>
	/// Quét kiểm tra các đòn đánh / quái vật lân cận có đường ngắm thẳng bằng Raycast (Line of Sight)
	/// Không cần bất kỳ Trigger / Collision Collider nào trên Player.
	/// </summary>
	private void CheckIncomingRaycastAttacks()
	{
		if (Time.time < lastRaycastScanTime + 0.25f) return;
		lastRaycastScanTime = Time.time;

		Enemy[] enemies = FindObjectsByType<Enemy>();
		for (int i = 0; i < enemies.Length; i++)
		{
			Enemy e = enemies[i];
			if (e == null || !e.gameObject.activeInHierarchy || e.currentHealth <= 0f) continue;

			if (e.target == transform)
			{
				float dist = Vector3.Distance(transform.position, e.transform.position);
				if (dist <= e.attackRange + 0.2f)
				{
					// Bắn tia Raycast kiểm tra không bị tường hoặc đá cản giữa quái và Player
					Vector3 enemyPos = e.transform.position + Vector3.up * 1.0f;
					Vector3 playerPos = transform.position + Vector3.up * 1.0f;
					Vector3 dir = (playerPos - enemyPos).normalized;

					if (Physics.Raycast(enemyPos, dir, out RaycastHit hit, dist + 0.5f))
					{
						if (hit.transform == transform || hit.transform.IsChildOf(transform))
						{
							// Đường ngắm Raycast thông suốt! Đòn đánh hợp lệ
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Nhận sát thương trực tiếp với xác thực đường bắn / tia Raycast không bị cản bởi tường
	/// </summary>
	public void TakeDamageFromRaycast(Vector3 sourcePos, float amount)
	{
		if (!enabled || isDead) return;

		Vector3 toPlayer = (transform.position + Vector3.up * 1f) - sourcePos;
		float dist = toPlayer.magnitude;

		if (dist > 0.05f)
		{
			if (Physics.Raycast(sourcePos, toPlayer.normalized, out RaycastHit hit, dist))
			{
				if (hit.transform != transform && !hit.transform.IsChildOf(transform))
				{
					string hName = hit.collider.name.ToLower();
					if (hName.Contains("wall") || hName.Contains("rock"))
					{
						Debug.Log($"<color=cyan>[Raycast Shield]</color> Đòn tấn công từ {sourcePos} bị tường che chắn!");
						return;
					}
				}
			}
		}

		TakeDamage(amount);
	}

	public void TakeDamage(float amount)
	{
		if (!enabled || isDead) return;

		currentHealth -= amount;
		currentHealth = Mathf.Max(0f, currentHealth);

		// Hiện số nảy sát thương màu đỏ cảnh báo
		FloatingDamageText.Spawn(transform.position + Vector3.up * 1.8f, amount, new Color(1f, 0.25f, 0.25f));

		Debug.Log($"<color=red>[Player Raycast]</color> Nhận {amount} sát thương! HP: {currentHealth:F0}/{maxHealth}");

		UpdateHealthBar();

		if (currentHealth <= 0f)
		{
			Die();
		}
	}

	public void TakeDamage(int amount)
	{
		TakeDamage((float)amount);
	}

	public void Die()
	{
		if (isDead) return;
		isDead = true;

		Debug.Log("<color=red><b>[Player]</b> ĐÃ BỊ TIÊU DIỆT! Sẽ hồi sinh sau " + respawnDelay + " giây...</color>");

		currentSpeed = 0f;
		verticalVelocity = 0f;

		// Kích hoạt animation chết
		if (animator != null)
		{
			animator.ResetTrigger("die");
			animator.SetTrigger("die");
			animator.Play("Death", 0, 0f);
		}

		UpdateHealthBar();

		StartCoroutine(RespawnRoutine());
	}

	private IEnumerator RespawnRoutine()
	{
		yield return new WaitForSeconds(respawnDelay);
		Respawn();
	}

	public void Respawn()
	{
		Vector3 targetSpawnPos;
		Quaternion targetSpawnRot = initialSpawnRot;

		if (respawnPoint != null)
		{
			targetSpawnPos = respawnPoint.position;
			targetSpawnRot = respawnPoint.rotation;
		}
		else
		{
			targetSpawnPos = initialSpawnPos;
		}

		if (Terrain.activeTerrain != null)
		{
			float terrainH = Terrain.activeTerrain.SampleHeight(targetSpawnPos) + Terrain.activeTerrain.transform.position.y;
			targetSpawnPos.y = Mathf.Max(targetSpawnPos.y, terrainH + 0.1f);
		}

		transform.position = targetSpawnPos;
		transform.rotation = targetSpawnRot;

		currentHealth = maxHealth;
		isDead = false;
		verticalVelocity = 0f;
		currentSpeed = 0f;
		isGrounded = true;

		if (animator != null)
		{
			animator.ResetTrigger("die");
			animator.SetTrigger("respawn");
			animator.Play("Idle", 0, 0f);
		}

		UpdateHealthBar();

		SpawnRespawnVFX(targetSpawnPos);

		Debug.Log($"<color=cyan><b>[Player]</b> ĐÃ HỒI SINH TẠI: {targetSpawnPos}! HP đầy lại {maxHealth}/{maxHealth}.</color>");
	}

	private void EnsureRespawnPointExists()
	{
		if (respawnPoint == null)
		{
			GameObject existingPoint = GameObject.Find("RespawnPoint") ?? GameObject.FindWithTag("Respawn");
			if (existingPoint != null)
			{
				respawnPoint = existingPoint.transform;
			}
			else
			{
				GameObject newPoint = new GameObject("RespawnPoint");
				newPoint.transform.position = transform.position;
				newPoint.transform.rotation = transform.rotation;
				try { newPoint.tag = "Respawn"; } catch { }

				CreateRespawnPadVisual(newPoint.transform);
				respawnPoint = newPoint.transform;
			}
		}
	}

	private void CreateRespawnPadVisual(Transform parentPoint)
	{
		GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		pad.name = "[RespawnPad_Visual]";
		pad.transform.SetParent(parentPoint, false);
		pad.transform.localPosition = new Vector3(0f, 0.04f, 0f);
		pad.transform.localScale = new Vector3(2.2f, 0.04f, 2.2f);

		Collider col = pad.GetComponent<Collider>();
		if (col != null) Destroy(col);

		Renderer r = pad.GetComponent<Renderer>();
		if (r != null)
		{
			Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
			Material mat = new Material(sh);
			mat.color = new Color(0f, 0.9f, 1f, 0.8f);
			r.material = mat;
		}
	}

	private void SpawnRespawnVFX(Vector3 pos)
	{
		GameObject aura = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		aura.name = "[Respawn_Aura]";
		aura.transform.position = pos + Vector3.up * 1.0f;
		aura.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);

		Collider col = aura.GetComponent<Collider>();
		if (col != null) Destroy(col);

		Renderer r = aura.GetComponent<Renderer>();
		if (r != null)
		{
			Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
			Material mat = new Material(sh);
			mat.color = new Color(0.1f, 1f, 0.8f, 0.75f);
			r.material = mat;
		}

		Destroy(aura, 1.5f);
	}

	// ================= THANH MÁU HUD TRÊN MÀN HÌNH (GẦN TASK) =================

	private void CreateHUDHealthBar()
	{
		Canvas canvas = null;
		Canvas[] allCanvases = FindObjectsByType<Canvas>();
		foreach (var c in allCanvases)
		{
			if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.name.Contains("Shop_Canvas") || c.name.Contains("Canvas"))
			{
				canvas = c;
				break;
			}
		}
		if (canvas == null && allCanvases.Length > 0) canvas = allCanvases[0];
		if (canvas == null) return;

		Transform existing = canvas.transform.Find("[HUD_Player_HealthBar]");
		if (existing != null) Destroy(existing.gameObject);

		GameObject hudBar = new GameObject("[HUD_Player_HealthBar]", typeof(RectTransform));
		hudBar.transform.SetParent(canvas.transform, false);
		RectTransform rt = hudBar.GetComponent<RectTransform>();
		rt.anchorMin = new Vector2(0f, 1f);
		rt.anchorMax = new Vector2(0f, 1f);
		rt.pivot = new Vector2(0f, 1f);
		rt.anchoredPosition = new Vector2(24f, -125f); // Vị trí hiển thị gần thanh task / top bar
		rt.sizeDelta = new Vector2(260f, 34f);

		// Nền đen xám viền Cyan Neon
		Image bg = hudBar.AddComponent<Image>();
		bg.color = new Color(0.06f, 0.10f, 0.16f, 0.95f);
		Outline outline = hudBar.AddComponent<Outline>();
		outline.effectColor = new Color(0f, 0.85f, 1f, 0.9f);
		outline.effectDistance = new Vector2(1.5f, -1.5f);

		// Fill máu xanh ngọc gradient
		GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
		fill.transform.SetParent(hudBar.transform, false);
		hudHealthBarFill = fill.GetComponent<RectTransform>();
		hudHealthBarFill.anchorMin = new Vector2(0.02f, 0.15f);
		hudHealthBarFill.anchorMax = new Vector2(0.98f, 0.85f);
		hudHealthBarFill.pivot = new Vector2(0f, 0.5f);
		hudHealthBarFill.anchoredPosition = Vector2.zero;
		hudHealthBarFill.sizeDelta = Vector2.zero;
		hudHealthBarFillImage = fill.GetComponent<Image>();
		hudHealthBarFillImage.color = new Color(0.15f, 0.95f, 0.4f, 1f);

		// Text thông số máu
		GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
		textObj.transform.SetParent(hudBar.transform, false);
		RectTransform tRt = textObj.GetComponent<RectTransform>();
		tRt.anchorMin = Vector2.zero;
		tRt.anchorMax = Vector2.one;
		tRt.sizeDelta = Vector2.zero;
		hudHealthBarText = textObj.GetComponent<Text>();
		Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
		hudHealthBarText.font = font;
		hudHealthBarText.fontSize = 18;
		hudHealthBarText.fontStyle = FontStyle.Bold;
		hudHealthBarText.alignment = TextAnchor.MiddleCenter;
		hudHealthBarText.color = Color.white;
		Shadow sHud = textObj.AddComponent<Shadow>();
		sHud.effectColor = new Color(0f, 0f, 0f, 0.9f);
		sHud.effectDistance = new Vector2(1.5f, -1.5f);

		UpdateHealthBar();
	}

	private void UpdateHealthBar()
	{
		float pct = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

		// Cập nhật duy nhất thanh máu HUD trên màn hình (đã bỏ thanh máu trên đầu)
		if (hudHealthBarFill != null)
		{
			hudHealthBarFill.localScale = new Vector3(pct, 1f, 1f);
		}
		if (hudHealthBarFillImage != null)
		{
			if (pct > 0.5f)
				hudHealthBarFillImage.color = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.9f, 0.3f), (pct - 0.5f) * 2f);
			else
				hudHealthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.85f, 0.1f), pct * 2f);
		}
		if (hudHealthBarText != null)
		{
			hudHealthBarText.text = $"❤️ MÁU: {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
		}
	}
}
