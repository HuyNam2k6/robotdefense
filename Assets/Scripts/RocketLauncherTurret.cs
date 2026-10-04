using UnityEngine;
using System.Collections;

public class RocketLauncherTurret : MonoBehaviour
{
    [Header("--- Vũ khí ---")]
    public GameObject rocketPrefab;
    
    [Header("--- Vị trí phóng (Kéo bệ trái và phải vào đây) ---")]
    public Transform leftPod;
    public Transform rightPod;
    
    [Header("--- Xoay đầu pháo ---")]
    [Tooltip("Kéo cái HeadMissileTurret1 vào đây để nó xoay theo quái")]
    public Transform turretHead;
    public float rotationSpeed = 10f;

    [Header("--- Cài đặt bắn ---")]
    public float fireCooldown = 2f;
    public int rocketsPerVolley = 16;
    public float attackRange = 30f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "Enemy";

    private float cooldownTimer = 0f;
    private Transform currentTarget;
    private readonly Collider[] buffer = new Collider[32];

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        FindTarget();

        if (currentTarget != null)
        {
            // Chỉ xoay cái mâm (trục Y cục bộ), tuyệt đối giữ nguyên độ nghiêng X và Z
            if (turretHead != null && turretHead.parent != null)
            {
                Vector3 targetLocalPos = turretHead.parent.InverseTransformPoint(currentTarget.position);
                float targetYaw = Mathf.Atan2(targetLocalPos.x, targetLocalPos.z) * Mathf.Rad2Deg;
                float currentYaw = turretHead.localEulerAngles.y;
                float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationSpeed);
                
                turretHead.localEulerAngles = new Vector3(turretHead.localEulerAngles.x, newYaw, turretHead.localEulerAngles.z);
            }

            if (cooldownTimer <= 0f)
            {
                StartCoroutine(FireVolley());
                cooldownTimer = fireCooldown;
            }
        }
    }

    IEnumerator FireVolley()
    {
        for (int i = 0; i < rocketsPerVolley; i++)
        {
            if (currentTarget == null) break;

            Transform selectedPod = (i % 2 == 0) ? leftPod : rightPod;
            if (selectedPod == null) selectedPod = transform;

            Vector3 randomOffset = new Vector3(
                Random.Range(-0.8f, 0.8f), 
                Random.Range(-0.8f, 0.8f), 
                Random.Range(0f, 0.5f)
            );
            
            Vector3 spawnPos = selectedPod.position + selectedPod.TransformDirection(randomOffset);

            if (rocketPrefab != null)
            {
                GameObject rocketObj = Instantiate(rocketPrefab, spawnPos, selectedPod.rotation);
                HomingRocket rocket = rocketObj.GetComponent<HomingRocket>();
                if (rocket != null)
                {
                    rocket.target = currentTarget;
                }
            }

            yield return new WaitForSeconds(0.06f);
        }
    }

    void FindTarget()
    {
        if (currentTarget != null)
        {
            float sqr = (currentTarget.position - transform.position).sqrMagnitude;
            if (sqr <= attackRange * attackRange && currentTarget.gameObject.activeInHierarchy) return;
            currentTarget = null;
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer, QueryTriggerInteraction.Collide);
        float best = Mathf.Infinity;
        
        for (int i = 0; i < count; i++)
        {
            if (!buffer[i].CompareTag(enemyTag)) continue;
            float sqr = (buffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                currentTarget = buffer[i].transform;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
