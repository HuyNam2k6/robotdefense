using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	private Animator animator;
	private CharacterController cc;

	[Header("Cài đặt Máu & Sinh Mệnh (Health & Death)")]
	public float maxHealth = 100f;
	public float currentHealth = 100f;
	public bool isDead = false;

	[Header("Điểm Hồi Sinh (Respawn Point)")]
	[Tooltip("Kéo Transform điểm hồi sinh vào đây. Nếu để trống, game sẽ tự động tạo/lấy điểm RespawnPoint")]
	public Transform respawnPoint;
	[Tooltip("Thời gian chờ hồi sinh (giây) sau khi chết")]
	public float respawnDelay = 3.0f;
	[Tooltip("Bật/tắt thanh máu nổi trên đầu Player")]
	public bool showFloatingHealthBar = true;

	[Header("Cài đặt Di Chuyển")]
	public float moveSpeed = 4.5f;
	public float rotateSpeed = 14f;
	public float turnSmoothTime = 0.06f; // Thời gian xoay mượt mà (0.05-0.08s)
	public float acceleration = 12f;     // Tốc độ vào đà
	public float deceleration = 18f;     // Tốc độ phanh lại
	public bool alignWithCamera = true;

	[Header("Cài đặt Trọng Lực & Rơi Tự Do")]
	[Tooltip("Độ mạnh của trọng lực game (Mặc định -25f rơi rất đầm chắc, chân thực; thay vì -9.81 lơ lửng như trên mặt trăng)")]
	public float gravity = -25f;
	[Tooltip("Lực ép bám sát dốc khi chạy nhanh qua sườn đồi gồ ghề")]
	public float groundStickForce = -6f;
	[Tooltip("Tốc độ rơi tối đa khi rơi từ độ cao lớn")]
	public float terminalVelocity = -40f;

	[Header("Cài đặt Bật Nhảy Vật Lý")]
	public float jumpHeight = 1.6f;      // Chiều cao nhảy lên không trung (mét)

	[Header("Hành Động Khi Đứng Yên (Idle Fidget)")]
	public bool enableIdleFidget = true; // Bật tính năng đứng im làm trò
	public float minIdleTime = 5f;       // Tối thiểu 5 giây đứng im
	public float maxIdleTime = 10f;      // Tối đa 10 giây đứng im

	[Header("Điều Khiển Trên Mobile")]
	public VirtualJoystick joystick;

	// Biến nội bộ
	private float currentSpeed = 0f;
	private float turnSmoothVelocity;
	private float verticalVelocity = 0f; // Trọng lực và lực nhảy
	private Camera mainCam;

	private Vector3 initialSpawnPos;
	private Quaternion initialSpawnRot;
	private GameObject healthBarObj;
	private RectTransform healthBarFill;
	private Image healthBarFillImage;
	private Text healthBarText;
	private RectTransform hudHealthBarFill;
	private Image hudHealthBarFillImage;
	private Text hudHealthBarText;

	// Biến đếm thời gian đứng yên
	private float idleTimer = 0f;
	private float nextIdleActionTime = 7f;

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
	}

	void Start()
	{
		animator = GetComponent<Animator>();
		cc = GetComponent<CharacterController>();
		mainCam = Camera.main;

		// Cấu hình CharacterController chuẩn xác để tránh trèo leo hoặc nảy vật lý
		if (cc != null)
		{
			cc.stepOffset = 0.35f;
			cc.slopeLimit = 55f;
			cc.skinWidth = 0.08f;
			cc.minMoveDistance = 0f;
		}

		if (joystick == null)
			joystick = FindAnyObjectByType<VirtualJoystick>();

		initialSpawnPos = transform.position;
		initialSpawnRot = transform.rotation;
		currentHealth = maxHealth;

		EnsureRespawnPointExists();
		CreateFloatingHealthBar();
		CreateHUDHealthBar();

		// Lên lịch ngẫu nhiên lần đầu tiên (từ 5 đến 10 giây)
		nextIdleActionTime = Random.Range(minIdleTime, maxIdleTime);
	}

	void OnDestroy()
	{
		if (healthBarObj != null) Destroy(healthBarObj);
	}

	void Update()
	{
		if (!enabled || isDead)
		{
			return;
		}
		// ========== 1. ĐỌC INPUT ==========
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

		// ========== 2. XOAY NGƯỜI MƯỢT MÀ ==========
		if (hasInput)
		{
			// Hủy ngay lập tức các động tác đứng yên nếu người chơi nhấn di chuyển
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

		// ========== 3. TĂNG / GIẢM TỐC ==========
		float targetSpeed = hasInput ? moveSpeed * inputMagnitude : 0f;
		float rate = (targetSpeed > currentSpeed) ? acceleration : deceleration;
		currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

		// ========== 4. TRỌNG LỰC & RƠI TỰ DO ==========
		if (cc.isGrounded)
		{
			if (verticalVelocity < 0f)
			{
				verticalVelocity = groundStickForce; // Ép chặt vào mặt đất, không bị bay bổng khi xuống dốc
			}
		}
		else
		{
			// Đang trên không trung: Rơi tự do kéo xuống đất
			float dt = Mathf.Min(Time.deltaTime, 0.04f); // Chặn spike deltaTime để tránh rơi xuyên sàn
			verticalVelocity += gravity * dt;
			if (verticalVelocity < terminalVelocity)
			{
				verticalVelocity = terminalVelocity;
			}
		}

		// KHÓA AN TOÀN: Tuyệt đối không cho phép vận tốc hướng lên vượt quá 10m/s (tránh PhysX bắn vọt lên trời)
		if (verticalVelocity > 10f)
		{
			verticalVelocity = 10f;
		}

		// ========== 5. DI CHUYỂN BẰNG CHARACTER CONTROLLER ==========
		float moveDt = Mathf.Min(Time.deltaTime, 0.04f);
		Vector3 move = transform.forward * currentSpeed;
		move.y = verticalVelocity;
		if (cc != null && cc.enabled)
		{
			cc.Move(move * moveDt);
		}

		// ========== 5.1. BẢO VỆ CHỐNG RƠI XUYÊN LÒNG ĐẤT VÀ CHỐNG BAY LÊN TRỜI (TRIỆT ĐỂ) ==========
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

		// ========== 8. TỰ ĐỘNG RANDOM VẪY TAY HOẶC NHẢY KHI ĐỨNG IM 5 - 10 GIÂY ==========
		HandleIdleFidget(hasInput);
	}

	void HandleIdleFidget(bool hasInput)
	{
		// Chỉ đếm giờ khi robot hoàn toàn đứng yên một chỗ và đang đứng trên mặt đất
		if (enableIdleFidget && !hasInput && currentSpeed <= 0.05f && cc.isGrounded)
		{
			idleTimer += Time.deltaTime;

			if (idleTimer >= nextIdleActionTime)
			{
				idleTimer = 0f;
				nextIdleActionTime = Random.Range(minIdleTime, maxIdleTime);

				// Random 50% Vẫy tay chào (Wave), 50% Nhảy múa (Dance)
				if (Random.value < 0.5f)
				{
					OnWaveButtonPressed();
				}
				else
				{
					OnDanceButtonPressed(); // Đổi sang Nhảy múa (Dance)
				}
			}
		}
		else
		{
			// Reset đồng hồ ngay lập tức nếu người chơi di chuyển
			idleTimer = 0f;
		}
	}

	// ================= CÁC HÀM XỬ LÝ HÀNH ĐỘNG =================

	// Bật nhảy vật lý thực sự
	public void OnJumpButtonPressed()
	{
		if (cc.isGrounded)
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
		// Kiểm tra cây trong bán kính 2.5m phía trước người chơi
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

	// Vẫy tay chào
	public void OnWaveButtonPressed()
	{
		if (animator != null)
			animator.SetTrigger("wave");
	}

	// Nhảy múa
	public void OnDanceButtonPressed()
	{
		if (animator != null)
			animator.SetTrigger("dance");
	}

	void LateUpdate()
	{
		if (healthBarObj != null)
		{
			healthBarObj.transform.position = transform.position + Vector3.up * 2.25f;
			if (mainCam == null) mainCam = Camera.main;
			if (mainCam != null)
			{
				healthBarObj.transform.rotation = mainCam.transform.rotation;
			}
		}
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

	// ================= HỆ THỐNG MÁU, NHẬN SÁT THƯƠNG, TỬ NẠN & HỒI SINH =================
	public void TakeDamage(float amount)
	{
		if (!enabled || isDead) return;

		currentHealth -= amount;
		currentHealth = Mathf.Max(0f, currentHealth);

		// Hiện số nảy sát thương màu đỏ cảnh báo
		FloatingDamageText.Spawn(transform.position + Vector3.up * 1.8f, amount, new Color(1f, 0.25f, 0.25f));

		Debug.Log($"<color=red>[Player]</color> Bị đánh trúng, nhận {amount} sát thương! HP: {currentHealth:F0}/{maxHealth}");

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

		// Vô hiệu hóa CharacterController để Player nằm yên trên sàn, không bị trọng lực hoặc va chạm làm trôi
		if (cc != null) cc.enabled = false;

		// Kích hoạt animation chết (nằm gục xuống đất)
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

		// Đảm bảo không bị kẹt dưới địa hình
		if (Terrain.activeTerrain != null)
		{
			float terrainH = Terrain.activeTerrain.SampleHeight(targetSpawnPos) + Terrain.activeTerrain.transform.position.y;
			targetSpawnPos.y = Mathf.Max(targetSpawnPos.y, terrainH + 0.1f);
		}

		transform.position = targetSpawnPos;
		transform.rotation = targetSpawnRot;

		if (cc != null) cc.enabled = true;

		currentHealth = maxHealth;
		isDead = false;
		verticalVelocity = 0f;
		currentSpeed = 0f;

		if (animator != null)
		{
			animator.ResetTrigger("die");
			animator.SetTrigger("respawn");
			animator.Play("Idle", 0, 0f);
		}

		UpdateHealthBar();

		SpawnRespawnVFX(targetSpawnPos);

		Debug.Log($"<color=cyan><b>[Player]</b> ĐÃ HỒI SINH THÀNH CÔNG TẠI: {targetSpawnPos}! HP đầy lại {maxHealth}/{maxHealth}.</color>");
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
			mat.color = new Color(0f, 0.9f, 1f, 0.8f); // Cyan phát sáng
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

	private void CreateFloatingHealthBar()
	{
		if (!showFloatingHealthBar) return;
		if (healthBarObj != null)
		{
			Destroy(healthBarObj);
			healthBarObj = null;
		}

		healthBarObj = new GameObject("[Player_HealthBar]");
		healthBarObj.transform.position = transform.position + Vector3.up * 2.25f;

		Canvas canvas = healthBarObj.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		CanvasScaler cs = healthBarObj.AddComponent<CanvasScaler>();
		cs.dynamicPixelsPerUnit = 20;

		RectTransform rt = healthBarObj.GetComponent<RectTransform>();
		rt.sizeDelta = new Vector2(1.3f, 0.18f);
		rt.localScale = Vector3.one;

		// Khung nền đen xám viền
		GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
		bg.transform.SetParent(healthBarObj.transform, false);
		RectTransform bgRt = bg.GetComponent<RectTransform>();
		bgRt.anchorMin = Vector2.zero;
		bgRt.anchorMax = Vector2.one;
		bgRt.sizeDelta = Vector2.zero;
		Image bgImg = bg.GetComponent<Image>();
		bgImg.color = new Color(0.08f, 0.12f, 0.16f, 0.88f);

		// Thanh fill máu
		GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
		fill.transform.SetParent(healthBarObj.transform, false);
		healthBarFill = fill.GetComponent<RectTransform>();
		healthBarFill.anchorMin = new Vector2(0.02f, 0.12f);
		healthBarFill.anchorMax = new Vector2(0.98f, 0.88f);
		healthBarFill.sizeDelta = Vector2.zero;
		healthBarFill.pivot = new Vector2(0f, 0.5f);
		healthBarFill.anchoredPosition = Vector2.zero;
		healthBarFillImage = fill.GetComponent<Image>();
		healthBarFillImage.color = new Color(0.2f, 0.95f, 0.35f, 1f);

		// Text hiển thị số HP
		GameObject textObj = new GameObject("HP_Text", typeof(RectTransform), typeof(Text));
		textObj.transform.SetParent(healthBarObj.transform, false);
		RectTransform textRt = textObj.GetComponent<RectTransform>();
		textRt.anchorMin = Vector2.zero;
		textRt.anchorMax = Vector2.one;
		textRt.sizeDelta = Vector2.zero;
		healthBarText = textObj.GetComponent<Text>();
		Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
		healthBarText.font = font;
		healthBarText.fontSize = 11;
		healthBarText.fontStyle = FontStyle.Bold;
		healthBarText.alignment = TextAnchor.MiddleCenter;
		healthBarText.color = Color.white;

		UpdateHealthBar();
	}

	private void CreateHUDHealthBar()
	{
		// Tìm đúng Screen-Space Overlay Canvas (tránh nhầm lẫn WorldSpace Canvas của quái)
		Canvas canvas = null;
		Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
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
		rt.anchoredPosition = new Vector2(24f, -125f);
		rt.sizeDelta = new Vector2(250f, 32f);

		// Nền đen viền Cyan Neon công nghệ cao
		Image bg = hudBar.AddComponent<Image>();
		bg.color = new Color(0.06f, 0.10f, 0.16f, 0.95f);
		Outline outline = hudBar.AddComponent<Outline>();
		outline.effectColor = new Color(0f, 0.85f, 1f, 0.9f);
		outline.effectDistance = new Vector2(1.5f, -1.5f);

		// Fill máu xanh lá ngọc
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

		// 1. Cập nhật thanh máu nổi trên đầu
		if (healthBarFill != null)
		{
			healthBarFill.localScale = new Vector3(pct, 1f, 1f);
		}
		if (healthBarFillImage != null)
		{
			if (pct > 0.5f)
				healthBarFillImage.color = Color.Lerp(new Color(1f, 0.85f, 0.1f), new Color(0.2f, 0.9f, 0.3f), (pct - 0.5f) * 2f);
			else
				healthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(1f, 0.85f, 0.1f), pct * 2f);
		}
		if (healthBarText != null)
		{
			healthBarText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}";
		}

		// 2. Cập nhật thanh máu HUD trên màn hình
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
