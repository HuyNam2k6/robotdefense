using UnityEngine;

public class TurretWeapon : MonoBehaviour
{
	[Header("--- Cấu hình Vũ Khí ---")]
	public Bullet bulletPrefab;
	public Transform firePoint;

	[Header("--- Tốc Độ Bắn (Cooldown) ---")]
	[Tooltip("Thời gian hồi giữa mỗi phát bắn (tính bằng giây). Bạn có thể chỉnh trực tiếp giá trị này trong Unity Inspector.")]
	public float cooldown = 0.5f;

	[Tooltip("(Tạm thời tắt tính tự động) Tốc độ bắn cũ: số phát / giây")]
	public float fireRate = 2f;

	[Header("--- Sát Thương Đạn ---")]
	public int bulletDamage = 15;

	// Bộ đếm hồi chiêu nội bộ trong vòng lặp game
	private float cooldownTimer;

	void Update()
	{
		if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
	}

	public void Fire(Vector3 aimPoint)
	{
		if (cooldownTimer > 0f || bulletPrefab == null || firePoint == null) return;

		// Đạn bắn thẳng tắp theo hướng nòng súng (firePoint.forward)
		// Giúp nòng súng và tia đạn trùng khớp 100%, không bị bắn bẻ góc
		Vector3 dir = firePoint.forward;
		if (dir.sqrMagnitude < 0.0001f) return;

		Bullet b = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
		if (bulletDamage > 0) b.damage = bulletDamage;
		b.Fire(dir);

		cooldownTimer = Mathf.Max(0.001f, cooldown);
	}
}