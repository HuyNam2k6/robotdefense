using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private readonly List<Transform> targetList = new List<Transform>();
    private readonly Collider[] buffer = new Collider[64];

    void Awake()
    {
        if (rocketPrefab == null)
        {
#if UNITY_EDITOR
            rocketPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/PerfectRocket.prefab")
                        ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/PerfectRocket.prefab");
#endif
        }
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        FindTargets();

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

            if (cooldownTimer <= 0f && targetList.Count > 0)
            {
                StartCoroutine(FireVolley());
                cooldownTimer = fireCooldown;
            }
        }
    }

    IEnumerator FireVolley()
    {
        // Snapshot danh sách các mục tiêu tại thời điểm bắt đầu loạt bắn
        List<Transform> activeTargets = new List<Transform>(targetList);

        for (int i = 0; i < rocketsPerVolley; i++)
        {
            // Lọc nhanh bỏ các mục tiêu đã bị tiêu diệt giữa loạt bắn
            for (int tIdx = activeTargets.Count - 1; tIdx >= 0; tIdx--)
            {
                Transform t = activeTargets[tIdx];
                if (t == null || !t.gameObject.activeInHierarchy)
                {
                    activeTargets.RemoveAt(tIdx);
                    continue;
                }
                Enemy e = t.GetComponent<Enemy>();
                if (e != null && e.currentHealth <= 0f)
                {
                    activeTargets.RemoveAt(tIdx);
                }
            }

            // Nếu danh sách tạm thời hết quái vật, thử nạp lại từ targetList mới nhất
            if (activeTargets.Count == 0)
            {
                FindTargets();
                activeTargets.AddRange(targetList);
            }

            // Nếu hoàn toàn không còn bất kỳ quái vật nào trong tầm bắn -> dừng loạt phóng
            if (activeTargets.Count == 0) yield break;

            // ================= LOGIC PHÂN BỔ HỎA LỰC =================
            // - Khi CHỈ CÓ 1 CON ENEMY: Dồn 100% hỏa lực vào con enemy duy nhất đó!
            // - Khi CÓ NHIỀU ENEMY: Tấn công nhiều mục tiêu (phân chia luân phiên các quả tên lửa vào từng con)
            Transform assignedTarget = (activeTargets.Count == 1)
                ? activeTargets[0]
                : activeTargets[i % activeTargets.Count];

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
                    rocket.target = assignedTarget;
                }
            }

            yield return new WaitForSeconds(0.06f);
        }
    }

    void FindTargets()
    {
        targetList.Clear();

        // 1. Quét danh sách quái vật trong Enemy.AllEnemies (tối ưu hiệu năng CPU, zero allocation)
        float rangeSqr = attackRange * attackRange;
        for (int i = 0; i < Enemy.AllEnemies.Count; i++)
        {
            Enemy e = Enemy.AllEnemies[i];
            if (e == null || !e.gameObject.activeInHierarchy || e.currentHealth <= 0f) continue;
            float distSqr = (e.transform.position - transform.position).sqrMagnitude;
            if (distSqr <= rangeSqr)
            {
                targetList.Add(e.transform);
            }
        }

        // 2. Fallback quét Collider nếu Enemy.AllEnemies chưa có dữ liệu
        if (targetList.Count == 0)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = buffer[i];
                if (col == null) continue;
                if (!col.CompareTag(enemyTag) && col.GetComponentInParent<Enemy>() == null) continue;

                Enemy enemyComp = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
                Transform t = enemyComp != null ? enemyComp.transform : col.transform;
                if (enemyComp != null && enemyComp.currentHealth <= 0f) continue;

                if (!targetList.Contains(t))
                {
                    targetList.Add(t);
                }
            }
        }

        // Sắp xếp mục tiêu theo khoảng cách tăng dần (gần nhất đứng đầu)
        if (targetList.Count > 1)
        {
            targetList.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;
                float da = (a.position - transform.position).sqrMagnitude;
                float db = (b.position - transform.position).sqrMagnitude;
                return da.CompareTo(db);
            });
        }

        currentTarget = (targetList.Count > 0) ? targetList[0] : null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
