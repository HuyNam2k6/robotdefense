using UnityEngine;

public class TurretController : MonoBehaviour
{
	[Header("--- References ---")]
	[Tooltip("Phần đế xoay của trụ (kéo GameObject 'rotator' vào đây)")]
	public Transform headTransform;

	[Tooltip("(Tùy chọn) Phần nòng súng ngẩng lên/cúi xuống. Nếu để trống, cả cụm rotator sẽ vừa xoay ngang vừa ngẩng lên.")]
	public Transform barrelTransform;

	public TurretWeapon weapon;

	[Header("--- Cài đặt Tầm Bắn & Tốc Độ Xoay ---")]
	[Tooltip("Tầm quét quái vật (mét).")]
	public float attackRange = 25f;
	[Tooltip("Tốc độ xoay đầu trụ (độ/giây).")]
	public float rotationSpeed = 360f;

	[Header("--- Cài đặt Góc Ngẩng / Cúi (Elevation / Pitch) ---")]
	[Tooltip("BẬT tùy chọn này để nòng súng có thể ngẩng lên theo quái vật ở trên cao / trên đồi dốc.")]
	public bool allowPitch = true;

	[Tooltip("Góc ngẩng lên tối đa (độ).")]
	[Range(0f, 80f)]
	public float maxElevationAngle = 45f;

	[Tooltip("Góc cúi xuống tối đa (độ).")]
	[Range(0f, 45f)]
	public float maxDepressionAngle = 15f;

	[Header("--- Cài đặt Bắn & Dung Sai ---")]
	[Tooltip("Góc sai số cho phép để nhả đạn (độ). Đặt 12 - 20 độ để bắn liên tục.")]
	public float aimTolerance = 15f;

	[Tooltip("Bắn đón đầu mục tiêu đang chạy.")]
	public bool leadTarget = false;

	public string enemyTag = "Enemy";
	public LayerMask enemyLayer = ~0;

	[Header("--- Hiển Thị Tia Debug ---")]
	[Tooltip("Tích vào đây nếu muốn hiện lại 2 vạch tia ngắm (Đỏ và Xanh lá) trong Scene View để kiểm tra.")]
	public bool showDebugLines = false;

	private Transform currentTarget;
	private Collider currentCollider;
	private Vector3 lastTargetPos;
	private Vector3 targetVelocity;
	private readonly Collider[] buffer = new Collider[32];

	void Start()
	{
		if (weapon == null)
			weapon = GetComponentInChildren<TurretWeapon>();

		// Tự động nhận diện nếu người dùng kéo nhầm mesh con FBX thay vì GameObject 'rotator'
		if (headTransform != null && headTransform.parent != null && headTransform.parent.name.ToLower().Contains("rotator"))
		{
			if (barrelTransform == null)
				barrelTransform = headTransform;

			headTransform = headTransform.parent;
		}

		if (rotationSpeed <= 0f)
			rotationSpeed = 360f;
	}

	void Update()
	{
		FindTarget();
		if (currentTarget == null || headTransform == null || weapon == null || weapon.firePoint == null) return;

		// 1. Vận tốc đón đầu
		if (leadTarget)
		{
			targetVelocity = (currentTarget.position - lastTargetPos) / Mathf.Max(Time.deltaTime, 0.0001f);
			lastTargetPos = currentTarget.position;
		}
		else
		{
			targetVelocity = Vector3.zero;
		}

		// 2. Điểm ngắm
		Vector3 aimPoint = GetAimPoint();

		// 3. Tính hướng ngắm trực tiếp từ đầu nòng (firePoint) tới mục tiêu để nòng chĩa thẳng tắp vào tâm quái
		Vector3 lookDir = aimPoint - weapon.firePoint.position;
		if (lookDir.sqrMagnitude < 0.001f) return;

		// 4. Tính góc xoay ngang (Yaw) quanh trục Y
		float targetYaw = Mathf.Atan2(lookDir.x, lookDir.z) * Mathf.Rad2Deg;

		// 5. Tính góc ngẩng/cúi (Pitch) quanh trục X
		float targetPitch = 0f;
		if (allowPitch)
		{
			float flatDistance = Mathf.Sqrt(lookDir.x * lookDir.x + lookDir.z * lookDir.z);
			// Trong Unity: góc X âm là ngẩng đầu lên trời, góc X dương là cúi xuống
			float rawPitch = -Mathf.Atan2(lookDir.y, flatDistance) * Mathf.Rad2Deg;
			targetPitch = Mathf.Clamp(rawPitch, -maxElevationAngle, maxDepressionAngle);
		}

		float speed = rotationSpeed > 0f ? rotationSpeed : 360f;

		// 6. Xoay đầu súng / nòng súng
		if (barrelTransform != null && barrelTransform != headTransform)
		{
			// Tách nòng: rotator xoay ngang, barrelTransform ngẩng lên/cúi xuống
			Quaternion wantYaw = Quaternion.Euler(0f, targetYaw, 0f);
			headTransform.rotation = Quaternion.RotateTowards(headTransform.rotation, wantYaw, speed * Time.deltaTime);

			Quaternion wantPitch = Quaternion.Euler(targetPitch, 0f, 0f);
			barrelTransform.localRotation = Quaternion.RotateTowards(barrelTransform.localRotation, wantPitch, speed * Time.deltaTime);
		}
		else
		{
			// Cả cụm rotator vừa xoay ngang vừa ngẩng lên
			Quaternion wantRot = Quaternion.Euler(targetPitch, targetYaw, 0f);
			headTransform.rotation = Quaternion.RotateTowards(headTransform.rotation, wantRot, speed * Time.deltaTime);
		}

		// 7. Kiểm tra góc ngắm thực tế của nòng súng so với mục tiêu
		Vector3 fireDir = aimPoint - weapon.firePoint.position;
		float currentAngle = Vector3.Angle(weapon.firePoint.forward, fireDir);

		// Hiển thị tia debug (mặc định đã ẩn)
		if (showDebugLines)
		{
			Debug.DrawRay(weapon.firePoint.position, weapon.firePoint.forward * 8f, Color.red);
			Debug.DrawLine(weapon.firePoint.position, aimPoint, Color.green);
		}

		// 8. Bắn đạn khi nòng đã chĩa vào mục tiêu
		if (currentAngle <= aimTolerance)
		{
			weapon.Fire(aimPoint);
		}
	}

	Vector3 GetAimPoint()
	{
		Vector3 p = currentCollider != null ? currentCollider.bounds.center : currentTarget.position;

		if (leadTarget && weapon != null && weapon.bulletPrefab != null && weapon.firePoint != null)
		{
			float t = Vector3.Distance(weapon.firePoint.position, p) / weapon.bulletPrefab.speed;
			p += targetVelocity * t;
		}
		return p;
	}

	void FindTarget()
	{
		if (currentTarget != null)
		{
			float sqr = (currentTarget.position - transform.position).sqrMagnitude;
			if (sqr <= attackRange * attackRange && currentTarget.gameObject.activeInHierarchy) return;
			currentTarget = null;
			currentCollider = null;
		}

		int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer,
			enemyLayer, QueryTriggerInteraction.Collide);

		float best = Mathf.Infinity;
		for (int i = 0; i < count; i++)
		{
			if (!buffer[i].CompareTag(enemyTag)) continue;
			float sqr = (buffer[i].transform.position - transform.position).sqrMagnitude;
			if (sqr < best)
			{
				best = sqr;
				currentTarget = buffer[i].transform;
				currentCollider = buffer[i];
				lastTargetPos = currentTarget.position;
				targetVelocity = Vector3.zero;
			}
		}
	}

	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(transform.position, attackRange);

		if (weapon != null && weapon.firePoint != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawRay(weapon.firePoint.position, weapon.firePoint.forward * 5f);
		}
	}
}