using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
	private Animator animator;
	private CharacterController cc;

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

	// Biến đếm thời gian đứng yên
	private float idleTimer = 0f;
	private float nextIdleActionTime = 7f;

	void Awake()
	{
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

		// Lên lịch ngẫu nhiên lần đầu tiên (từ 5 đến 10 giây)
		nextIdleActionTime = Random.Range(minIdleTime, maxIdleTime);
	}

	void Update()
	{
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
		cc.Move(move * moveDt);

		// ========== 5.1. BẢO VỆ CHỐNG RƠI XUYÊN LÒNG ĐẤT VÀ CHỐNG BAY LÊN TRỜI (TRIỆT ĐỂ) ==========
		EnforceGroundBoundary();

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

	// ================= HỆ THỐNG BẢO VỆ AN TOÀN CHO PLAYER =================
	private void EnforceGroundBoundary()
	{
		Vector3 currentPos = transform.position;

		// 1. CỨU HỘ VỰC THẲM / BIỂN: Nếu rơi khỏi mép đảo xuống biển (Y < 4.0m)
		// Đưa ngay về vị trí an toàn ở trung tâm đảo, triệt tiêu vận tốc rơi
		if (currentPos.y < 4.0f)
		{
			bool wasEnabled = cc.enabled;
			if (wasEnabled) cc.enabled = false;

			transform.position = new Vector3(0f, 10.5f, -32f);
			verticalVelocity = 0f;

			if (wasEnabled) cc.enabled = true;
			Debug.LogWarning("<color=yellow>[Bảo Vệ Player]</color> Đã tự động cứu hộ Player về vị trí trung tâm đảo an toàn!");
			return;
		}

		// 2. CHỐNG BAY LÊN TRỜI: Nếu độ cao vượt quá mức cho phép bất thường (Y > 15.0m)
		// Triệt tiêu lực đẩy lên, để trọng lực kéo Player hạ cánh tự nhiên
		if (currentPos.y > 15.0f)
		{
			if (verticalVelocity > 0f)
			{
				verticalVelocity = 0f;
			}
		}

		// 3. CHỐNG LÚN SÂU XUYÊN ĐẤT: Chỉ can thiệp khi bị kẹt rơi sâu hơn 0.8m dưới bề mặt đảo
		if (currentPos.y < 7.5f && currentPos.y >= 4.0f)
		{
			if (Terrain.activeTerrain != null)
			{
				float terrainY = Terrain.activeTerrain.SampleHeight(currentPos) + Terrain.activeTerrain.transform.position.y;
				if (terrainY > 8.0f && currentPos.y < terrainY - 0.8f)
				{
					bool wasEnabled = cc.enabled;
					if (wasEnabled) cc.enabled = false;

					currentPos.y = terrainY + 0.1f;
					transform.position = currentPos;
					verticalVelocity = 0f;

					if (wasEnabled) cc.enabled = true;
				}
			}
		}
	}

	// ================= MIỄN NHIỄM SÁT THƯƠNG TỪ ĐẠN VÀ TIA SÉT =================
	/// <summary>
	/// Player hoàn toàn miễn nhiễm 100% với mọi loại đạn, tia sét và vũ khí phòng thủ
	/// </summary>
	public void TakeDamage(float amount)
	{
		// Bỏ qua 100%, không nhận sát thương hay hiệu ứng nào
	}

	public void TakeDamage(int amount)
	{
		// Bỏ qua 100%, không nhận sát thương hay hiệu ứng nào
	}
}
