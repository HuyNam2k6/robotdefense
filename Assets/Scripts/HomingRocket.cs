using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingRocket : MonoBehaviour
{
    public Transform target;
    public float speed = 16f;
    public float turnSpeed = 9f;
    public GameObject explosionPrefab;
    
    private Rigidbody rb;
    private bool isArmed = false;
    private float radius = 0.25f;

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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
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
        
        Vector3 randomUp = (Vector3.up * 1.5f + Random.insideUnitSphere * 0.5f).normalized;
        transform.rotation = Quaternion.LookRotation(randomUp);
        rb.linearVelocity = randomUp * (speed * 0.45f);
        
        Invoke("ArmRocket", 0.12f);
        Destroy(gameObject, 5f);
    }

    void ArmRocket()
    {
        isArmed = true;
    }

    void Update()
    {
        if (!isArmed) return;

        // 1. Kích nổ cự ly gần (Proximity Fuse) nếu đã áp sát quái dưới 1.2 mét
        if (target != null && (target.position - transform.position).sqrMagnitude <= 1.8f)
        {
            Explode();
            return;
        }

        // 2. Quét va chạm phía trước (Bỏ qua Player, bản thân và tên lửa khác)
        float distanceThisFrame = speed * Time.deltaTime * 2f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, distanceThisFrame);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.gameObject == gameObject) continue;
            if (IsPlayer(col)) continue; // Xuyên qua Player 100%, không nổ!
            if (col.GetComponent<HomingRocket>() != null) continue;
            if (col.GetComponentInParent<UpgradableTurret>() != null) continue;
            if (col.GetComponentInParent<RocketLauncherTurret>() != null) continue;

            transform.position = hits[i].point + Vector3.up * 0.4f;
            Explode();
            return;
        }
    }

    void FixedUpdate()
    {
        if (target == null || !target.gameObject.activeInHierarchy || (target.GetComponent<Enemy>() != null && target.GetComponent<Enemy>().currentHealth <= 0f))
        {
            RetargetNearestEnemy();
        }

        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            Quaternion targetRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * turnSpeed * 1.5f));
            
            rb.linearVelocity = transform.forward * speed;
        }
        else
        {
            rb.useGravity = true;
        }
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

    void OnTriggerEnter(Collider other)
    {
        if (!isArmed) return;
        if (other == null || other.gameObject == gameObject) return;
        if (IsPlayer(other)) return; // Xuyên qua Player hoàn toàn!
        if (other.GetComponent<HomingRocket>() != null) return;
        if (other.GetComponentInParent<UpgradableTurret>() != null || other.GetComponentInParent<RocketLauncherTurret>() != null) return;
        Explode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed) return;
        if (collision.gameObject == gameObject) return;
        if (IsPlayer(collision.collider)) return; // Xuyên qua Player hoàn toàn!
        if (collision.gameObject.GetComponent<HomingRocket>() != null) return;
        if (collision.collider.GetComponentInParent<UpgradableTurret>() != null || collision.collider.GetComponentInParent<RocketLauncherTurret>() != null) return;
        Explode();
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
