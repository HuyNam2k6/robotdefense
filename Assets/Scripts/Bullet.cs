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
		// QUY TẮC MỚI: Đạn tuyệt đối KHÔNG DÙNG COLLIDER hay RIGIDBODY
		Collider[] cols = GetComponentsInChildren<Collider>();
		foreach (var c in cols) if (c != null) Destroy(c);

		Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
		foreach (var r in rbs) if (r != null) Destroy(r);
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

		// 100% Raycast: Quét tia Raycast về phía trước dọc theo đường đạn bay mỗi frame
		if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step * 1.25f, hitMask, QueryTriggerInteraction.Ignore))
		{
			Collider hitCol = hit.collider;
			if (hitCol != null && hitCol.gameObject != gameObject)
			{
				// BỎ QUA 100% PLAYER và các công trình đồng minh
				if (!IsPlayer(hitCol) && 
				    hitCol.GetComponentInParent<UpgradableTurret>() == null && 
				    hitCol.GetComponentInParent<RocketLauncherTurret>() == null && 
				    hitCol.GetComponentInParent<TurretController>() == null && 
				    hitCol.GetComponentInParent<WorkerBot>() == null)
				{
					hasHit = true;
					transform.position = hit.point;
					OnHit(hitCol, hit.point);
					return;
				}
			}
		}

		transform.position += direction * step;
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