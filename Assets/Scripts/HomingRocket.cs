using UnityEngine;

public class HomingRocket : MonoBehaviour
{
    public Transform target;
    public float speed = 16f;
    public float turnSpeed = 9f;
    public GameObject explosionPrefab;
    
    private bool isArmed = false;

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

    void Awake()
    {
        // QUY TẮC MỚI: Tên lửa tuyệt đối KHÔNG CÓ RIGIDBODY hay COLLIDER (100% Raycast)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols)
        {
            if (c != null) Destroy(c);
        }
    }

    void Start()
    {
        Vector3 randomUp = (Vector3.up * 1.5f + Random.insideUnitSphere * 0.5f).normalized;
        transform.rotation = Quaternion.LookRotation(randomUp);

        Invoke("ArmRocket", 0.12f);
        Destroy(gameObject, 5f);
    }

    void ArmRocket()
    {
        isArmed = true;
    }

    void Update()
    {
        // 1. Tự động tìm lại mục tiêu quái vật còn sống nếu mục tiêu cũ đã bị tiêu diệt
        if (target == null || !target.gameObject.activeInHierarchy || (target.GetComponent<Enemy>() != null && target.GetComponent<Enemy>().currentHealth <= 0f))
        {
            RetargetNearestEnemy();
        }

        // 2. Tính toán góc quay tự dẫn đường (Homing logic bằng Code thuần)
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * 75f * Time.deltaTime);
        }

        float step = speed * Time.deltaTime;

        if (isArmed)
        {
            // 3. Kích nổ cự ly gần (Proximity Fuse) nếu đã áp sát quái dưới 1.8 mét
            if (target != null && (target.position - transform.position).sqrMagnitude <= 2.2f)
            {
                Explode();
                return;
            }

            // 4. BẮN TIA RAYCAST VỀ PHÍA TRƯỚC THEO QUỸ ĐẠO BAY (100% Raycast, KHÔNG DÙNG COLLIDER TRÊN ĐẠN)
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, step * 1.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                Collider col = hit.collider;
                if (col != null && !IsPlayer(col) && 
                    col.GetComponentInParent<UpgradableTurret>() == null && 
                    col.GetComponentInParent<RocketLauncherTurret>() == null &&
                    col.GetComponentInParent<TurretController>() == null &&
                    col.GetComponentInParent<WorkerBot>() == null)
                {
                    transform.position = hit.point + Vector3.up * 0.3f;
                    Explode();
                    return;
                }
            }
        }

        // 5. Di chuyển tên lửa về phía trước theo Transform
        transform.position += transform.forward * step;
    }

    private void RetargetNearestEnemy()
    {
        float bestDistSqr = 35f * 35f;
        Transform bestT = null;

        for (int i = 0; i < Enemy.AllEnemies.Count; i++)
        {
            Enemy e = Enemy.AllEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && e.currentHealth > 0f)
            {
                float dSqr = (e.transform.position - transform.position).sqrMagnitude;
                if (dSqr < bestDistSqr)
                {
                    bestDistSqr = dSqr;
                    bestT = e.transform;
                }
            }
        }

        if (bestT != null)
        {
            target = bestT;
        }
    }

    void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
