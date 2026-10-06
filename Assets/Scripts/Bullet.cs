using UnityEngine;

public class Bullet : MonoBehaviour
{
	[Header("--- Thông Số Đạn ---")]
	public float speed = 25f;
	public int damage = 10;
	public float lifeTime = 3f;
	public float radius = 0.25f;
	public LayerMask hitMask = ~0;

	private Vector3 direction;
	private bool hasHit = false;

	void Awake()
	{
		// Đảm bảo tuyệt đối Collider của viên đạn là Trigger để không bao giờ có lực vật lý đẩy lún Player xuống đất
		Collider col = GetComponent<Collider>();
		if (col != null)
		{
			col.isTrigger = true;

			// Vô hiệu hóa va chạm vật lý với tất cả Collider / CharacterController của Player
			PlayerController player = Object.FindAnyObjectByType<PlayerController>();
			if (player != null)
			{
				Collider[] pCols = player.GetComponentsInChildren<Collider>(true);
				foreach (var pc in pCols)
				{
					if (pc != null && pc != col)
					{
						Physics.IgnoreCollision(col, pc, true);
					}
				}
			}
		}
	}

	void Start()
	{
		// Tạo ánh sáng lửa ấm rực rỡ cho viên đạn khi bay
		AddBulletVisualEffect();
	}

	public void Fire(Vector3 dir)
	{
		direction = dir.normalized;
		transform.rotation = Quaternion.LookRotation(direction);
		Destroy(gameObject, lifeTime);
	}

	// Hàm nhận diện chuẩn xác 100% tất cả các bộ phận của Player
	private bool IsPlayer(Collider col)
	{
		if (col == null) return false;
		if (col.CompareTag("Player")) return true;
		if (col.transform.root.CompareTag("Player")) return true;
		if (col.GetComponentInParent<PlayerController>() != null) return true;
		if (col.transform.root.GetComponentInChildren<PlayerController>() != null) return true;
		if (col is CharacterController) return true;

		string n = col.gameObject.name.ToLower();
		string rootName = col.transform.root.gameObject.name.ToLower();
		if (n.Contains("player") || rootName.Contains("player")) return true;
		if (n.Contains("cuterobot") || rootName.Contains("cuterobot")) return true;

		return false;
	}

	void Update()
	{
		if (hasHit) return;

		float step = speed * Time.deltaTime;

		// Quét tất cả vật thể trên đường đạn bay để loại trừ Người chơi và Trụ đồng minh
		RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction, step, hitMask, QueryTriggerInteraction.Ignore);
		if (hits != null && hits.Length > 0)
		{
			System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

			for (int i = 0; i < hits.Length; i++)
			{
				Collider hitCol = hits[i].collider;
				if (hitCol == null || hitCol.gameObject == gameObject) continue;

				// BỎ QUA 100% PLAYER: Đạn đi xuyên qua người chơi hoàn toàn, không chạm, không nổ, không hiệu ứng
				if (IsPlayer(hitCol)) continue;

				// Bỏ qua đạn khác và các công trình đồng minh
				if (hitCol.GetComponent<Bullet>() != null) continue;
				if (hitCol.GetComponentInParent<UpgradableTurret>() != null) continue;
				if (hitCol.GetComponentInParent<RocketLauncherTurret>() != null) continue;
				if (hitCol.GetComponentInParent<TurretController>() != null) continue;
				if (hitCol.GetComponentInParent<WorkerBot>() != null) continue;

				// Đã trúng mục tiêu hợp lệ (Quái vật hoặc Địa hình)
				hasHit = true;
				transform.position = hits[i].point;
				OnHit(hitCol, hits[i].point);
				return;
			}
		}

		transform.position += direction * step;
	}

	void OnTriggerEnter(Collider other)
	{
		if (hasHit || other == null || other.gameObject == gameObject) return;
		if (IsPlayer(other)) return; // Xuyên qua Player hoàn toàn!
		if (other.GetComponent<Bullet>() != null) return;
		if (other.GetComponentInParent<UpgradableTurret>() != null) return;
		if (other.GetComponentInParent<TurretController>() != null) return;
		if (other.GetComponentInParent<RocketLauncherTurret>() != null) return;
		if (other.GetComponentInParent<WorkerBot>() != null) return;

		// Nếu chạm trúng Quái vật
		if (other.CompareTag("Enemy") || other.GetComponentInParent<Enemy>() != null)
		{
			hasHit = true;
			OnHit(other, transform.position);
		}
	}

	void OnHit(Collider other, Vector3 hitPoint)
	{
		// Tuyệt đối không bao giờ gây sát thương hay hiệu ứng lên Player
		if (IsPlayer(other)) return;

		if (other.CompareTag("Enemy") || other.GetComponentInParent<Enemy>() != null)
		{
			Enemy enemy = other.GetComponent<Enemy>() ?? other.GetComponentInParent<Enemy>();
			if (enemy != null)
			{
				enemy.TakeDamage(damage);
				Debug.Log($"<color=red>[Đạn Lửa]</color> Bắn trúng quái: {enemy.name}! Gây sát thương thực tế: {damage}");
			}
			else
			{
				other.SendMessage("TakeDamage", (float)damage, SendMessageOptions.DontRequireReceiver);
				Debug.Log($"<color=red>[Đạn Lửa]</color> Gửi TakeDamage tới: {other.name}! Sát thương: {damage}");
			}

			SpawnImpactEffect(hitPoint, true);
		}
		else
		{
			Debug.Log($"<color=grey>[Đạn Lửa]</color> Đập trúng vật cản: {other.name}");
			SpawnImpactEffect(hitPoint, false);
		}

		Destroy(gameObject);
	}

	// ================= HỆ THỐNG HẠT VA CHẠM DÙNG CHUNG (TỐI ƯU 0 GC ALLOC & 60 FPS) =================
	private static ParticleSystem sharedImpactPS;
	private static Material sharedImpactMat;

	private static void EnsureSharedImpactSystem()
	{
		if (sharedImpactPS != null) return;

		GameObject poolObj = new GameObject("[VFX_BulletImpacts]");
		sharedImpactPS = poolObj.AddComponent<ParticleSystem>();
		sharedImpactPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = sharedImpactPS.main;
		main.loop = false;
		main.playOnAwake = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
		main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
		main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.22f);
		main.scalingMode = ParticleSystemScalingMode.Hierarchy;

		var shape = sharedImpactPS.shape;
		shape.enabled = true;
		shape.shapeType = ParticleSystemShapeType.Sphere;
		shape.radius = 0.12f;

		var emission = sharedImpactPS.emission;
		emission.enabled = false;

		var pRenderer = sharedImpactPS.GetComponent<ParticleSystemRenderer>();
		pRenderer.renderMode = ParticleSystemRenderMode.Billboard;

		Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
		if (pShader == null) pShader = Shader.Find("Particles/Standard Unlit");
		if (pShader == null) pShader = Shader.Find("Sprites/Default");
		if (pShader != null)
		{
			sharedImpactMat = new Material(pShader);
			sharedImpactMat.color = Color.white;
			pRenderer.material = sharedImpactMat;
		}
	}

	// Hiệu ứng ánh sáng rực rỡ khi đạn bay
	private void AddBulletVisualEffect()
	{
		GameObject lightObj = new GameObject("BulletGlow");
		lightObj.transform.SetParent(transform, false);
		Light bulletLight = lightObj.AddComponent<Light>();
		bulletLight.type = LightType.Point;
		bulletLight.color = new Color(1f, 0.45f, 0.1f);
		bulletLight.range = 2.5f;
		bulletLight.intensity = 2.5f;
		bulletLight.shadows = LightShadows.None;
	}

	// Hiệu ứng nổ lửa văng tia sáng khi đạn đập trúng mục tiêu (Dùng EmitParams, không tạo/hủy GameObject, 0 lỗi duration)
	private void SpawnImpactEffect(Vector3 pos, bool hitEnemy)
	{
		EnsureSharedImpactSystem();
		if (sharedImpactPS == null) return;

		ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
		ep.position = pos;
		ep.startColor = hitEnemy ? new Color(1f, 0.55f, 0.12f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);
		ep.applyShapeToPosition = true;

		sharedImpactPS.Emit(ep, hitEnemy ? 12 : 6);
	}
}