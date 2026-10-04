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

	void Start()
	{
		animator = GetComponent<Animator>();
		cc = GetComponent<CharacterController>();
		mainCam = Camera.main;

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
			verticalVelocity += gravity * Time.deltaTime;
			if (verticalVelocity < terminalVelocity)
			{
				verticalVelocity = terminalVelocity;
			}
		}

		// ========== 5. DI CHUYỂN BẰNG CHARACTER CONTROLLER ==========
		Vector3 move = transform.forward * currentSpeed;
		move.y = verticalVelocity;
		cc.Move(move * Time.deltaTime);

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
}
