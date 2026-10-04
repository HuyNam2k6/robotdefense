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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        
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

        // 2. Quét va chạm phía trước (Bỏ qua bản thân và tên lửa khác)
        float distanceThisFrame = speed * Time.deltaTime * 2f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, distanceThisFrame);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || col.gameObject == gameObject) continue;
            if (col.CompareTag("Player")) continue;
            if (col.GetComponent<HomingRocket>() != null) continue;

            transform.position = hits[i].point + Vector3.up * 0.4f;
            Explode();
            return;
        }
    }

    void FixedUpdate()
    {
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

    void OnTriggerEnter(Collider other)
    {
        if (!isArmed) return;
        if (other.gameObject == gameObject) return;
        if (other.CompareTag("Player") || other.GetComponent<HomingRocket>() != null) return;
        Explode();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isArmed) return;
        if (collision.gameObject == gameObject) return;
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<HomingRocket>() != null) return;
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
