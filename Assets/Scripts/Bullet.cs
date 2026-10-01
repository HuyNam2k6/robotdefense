using UnityEngine;

public class Bullet : MonoBehaviour
{
	public float speed = 25f;
	public int damage = 10;
	public float lifeTime = 3f;
	public float radius = 0.2f;          // độ dày đạn, tăng lên cho dễ trúng
	public LayerMask hitMask = ~0;       // bỏ layer của trụ súng/đạn ra khỏi mask

	private Vector3 direction;

	// Gọi ngay sau Instantiate
	public void Fire(Vector3 dir)
	{
		direction = dir.normalized;
		transform.rotation = Quaternion.LookRotation(direction);
		Destroy(gameObject, lifeTime);
	}

	void Update()
	{
		float step = speed * Time.deltaTime;

		if (Physics.SphereCast(transform.position, radius, direction,
				out RaycastHit hit, step, hitMask, QueryTriggerInteraction.Ignore))
		{
			transform.position = hit.point;
			OnHit(hit.collider);
			return;
		}

		transform.position += direction * step;
	}

	void OnHit(Collider other)
	{
		if (other.CompareTag("Enemy"))
		{
			Debug.Log($"<color=red>[Đạn]</color> Bắn trúng quái: {other.name}! Sát thương: {damage}");
			// TODO: other.GetComponentInParent<EnemyHealth>()?.TakeDamage(damage);
		}
		else
		{
			Debug.Log($"<color=grey>[Đạn]</color> Đập trúng vật cản: {other.name}");
		}
		Destroy(gameObject);
	}
}