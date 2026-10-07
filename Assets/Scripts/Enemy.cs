using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("--- Thông Số Quái Vật ---")]
    public string enemyName = "Scrap Bug";
    public float maxHealth = 50f;
    public float currentHealth;
    public float moveSpeed = 3f;
    public int goldReward = 15;
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.2f;

    [Header("--- Hiệu Chỉnh Góc Xoay Model ---")]
    [Tooltip("Góc bù xoay nếu model gốc bị quay ngược (ví dụ 180 độ đối với Spider)")]
    public float modelRotationOffset = 0f;

    [Header("--- Mục Tiêu Tấn Công ---")]
    public Transform target;

    [Header("--- Chế Độ Bay (Flying / Aerial Enemy) ---")]
    public bool isFlying = false;
    public float flightAltitude = 3.2f;
    public float hoverBobSpeed = 2.2f;
    public float hoverBobAmount = 0.35f;

    [Header("--- Hiệu Ứng & Animation ---")]
    public Animator animator;
    public GameObject deathVFX;

    [Header("--- Hiệu Ứng Đạn / Đòn Đánh (Projectile VFX) ---")]
    [Tooltip("Prefab quả cầu đạn năng lượng (Hiển thị trong Thư viện Project: Dragon_PlasmaBall.prefab)")]
    public GameObject projectilePrefab;
    [Tooltip("Màu sắc của quả cầu đạn nếu tự sinh bằng code")]
    public Color projectileColor = new Color(0f, 1f, 1f, 1f);
    [Tooltip("Vị trí bắn đạn ra từ miệng rồng (tự động tìm xương Head nếu để trống)")]
    public Transform shootPoint;
    [Tooltip("Độ trễ (giây) từ lúc bắt đầu animation đến lúc đầu chúi xuống phun đạn")]
    public float attackDelay = 0.5f;

    private float lastAttackTime = 0f;
    private bool isDead = false;
    private Collider myCollider;

    void Awake()
    {
        currentHealth = maxHealth;
        myCollider = GetComponent<Collider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Tự động xóa Rigidbody nếu có (game 3D dùng trọng lực code theo quy tắc dự án)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }

        // Tự động bù góc 180 độ cho Nhện nếu chưa đặt (vì model gốc hướng đầu về -Z)
        if (modelRotationOffset == 0f && (name.ToLower().Contains("spider") || enemyName.ToLower().Contains("spider")))
        {
            modelRotationOffset = 180f;
        }

        // Tự động gắn chặt mắt rồng vào xương đầu (Head) để mắt không bao giờ bay lạc hướng khi tấn công
        if (isFlying || name.ToLower().Contains("dragon"))
        {
            EnsureDragonEyesAttachedToHead();
        }
    }

    private void EnsureDragonEyesAttachedToHead()
    {
        Transform head = null;
        Transform eyeArm = null;
        Transform eyesMesh = null;

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase)) head = t;
            else if (t.name.Equals("EyeArmature", System.StringComparison.OrdinalIgnoreCase)) eyeArm = t;
            else if (t.name.Equals("Eyes", System.StringComparison.OrdinalIgnoreCase)) eyesMesh = t;
        }

        if (head != null)
        {
            if (eyeArm != null && eyeArm.parent != head)
            {
                eyeArm.SetParent(head, true);
            }
            if (eyesMesh != null)
            {
                if (eyesMesh.parent != head)
                {
                    eyesMesh.SetParent(head, true);
                }
                var smr = eyesMesh.GetComponent<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    smr.updateWhenOffscreen = true;
                }
            }
        }
    }

    void Start()
    {
        SnapToGroundImmediately();
        FindTarget();
    }

    public void FindTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;

        // 1. Ưu tiên tìm Căn cứ (Base / BaseHQ)
        GameObject baseObj = GameObject.FindGameObjectWithTag("Base") ?? GameObject.Find("Base") ?? GameObject.Find("BaseHQ");
        if (baseObj != null)
        {
            target = baseObj.transform;
            return;
        }

        // 2. Tìm Người chơi (Player)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        if (playerObj != null)
        {
            target = playerObj.transform;
            return;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            FindTarget();
            if (target == null)
            {
                // Nếu chưa có mục tiêu, tự động bò xuôi theo trục Z hướng về phía dưới căn cứ
                Vector3 fallbackDir = Vector3.back;
                Quaternion targetRot = Quaternion.LookRotation(fallbackDir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                transform.position += fallbackDir * (moveSpeed * Time.deltaTime);
                if (animator != null) animator.SetBool("isMoving", true);
                ApplyGroundGravity();
                return;
            }
        }

        // Tính khoảng cách tới mục tiêu (Đối với quái bay isFlying: đo theo mặt phẳng ngang XZ để không bị lỗi khoảng cách trục Y)
        Vector3 targetFlatPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        float distance = isFlying ? Vector3.Distance(transform.position, targetFlatPos) : Vector3.Distance(transform.position, target.position);

        if (distance > attackRange)
        {
            // Di chuyển về phía mục tiêu
            Vector3 dir = (targetFlatPos - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }

            if (animator != null) animator.SetBool("isMoving", true);
        }
        else
        {
            // Đã áp sát mục tiêu -> dừng bước và xoay đầu hướng về phía mục tiêu để tấn công
            Vector3 aimDir = (target.position - transform.position);
            if (!isFlying) aimDir.y = 0f;
            if (aimDir.sqrMagnitude > 0.01f)
            {
                aimDir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(aimDir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            if (animator != null) animator.SetBool("isMoving", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                Attack();
            }
        }

        ApplyGroundGravity();
    }

    // ================= TRỌNG LỰC BẰNG CODE CHO GAME 3D (RULE 8) =================
    private void SnapToGroundImmediately()
    {
        Vector3 currentPos = transform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 15f, currentPos.z);
        int layerMask = ~LayerMask.GetMask("Enemy");
        float groundY = currentPos.y;
        bool hasGround = false;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 35f, layerMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            hasGround = true;
        }
        else if (Terrain.activeTerrain != null)
        {
            groundY = Terrain.activeTerrain.SampleHeight(currentPos) + Terrain.activeTerrain.transform.position.y;
            hasGround = true;
        }

        if (hasGround)
        {
            currentPos.y = isFlying ? (groundY + flightAltitude) : groundY;
            transform.position = currentPos;
        }
    }

    private void ApplyGroundGravity()
    {
        Vector3 currentPos = transform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + (isFlying ? 15f : 4f), currentPos.z);
        int layerMask = ~LayerMask.GetMask("Enemy");
        float groundY = currentPos.y;
        bool hasGround = false;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, layerMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            hasGround = true;
        }
        else if (Terrain.activeTerrain != null)
        {
            groundY = Terrain.activeTerrain.SampleHeight(currentPos) + Terrain.activeTerrain.transform.position.y;
            hasGround = true;
        }

        if (hasGround)
        {
            if (isFlying)
            {
                // Quái bay: duy trì độ cao lượn êm ái trên không theo hình sin
                float targetY = groundY + flightAltitude + Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;
                currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, 12f * Time.deltaTime);
            }
            else
            {
                // Quái bò đất: bám sát mặt đất
                currentPos.y = Mathf.MoveTowards(currentPos.y, groundY, 20f * Time.deltaTime);
            }
            transform.position = currentPos;
        }
    }

    void Attack()
    {
        if (animator != null)
        {
            animator.SetTrigger("attack");
        }

        if (target != null)
        {
            StartCoroutine(ExecuteAttackWithDelay(target));
        }
    }

    private IEnumerator ExecuteAttackWithDelay(Transform currentTarget)
    {
        if (attackDelay > 0f)
        {
            yield return new WaitForSeconds(attackDelay);
        }

        if (isDead) yield break;

        if (currentTarget != null)
        {
            currentTarget.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);

            if (isFlying)
            {
                StartCoroutine(SpawnFlyingAttackVFX(currentTarget.position));
            }
        }
    }

    private IEnumerator SpawnFlyingAttackVFX(Vector3 targetPos)
    {
        Vector3 spawnPos;
        if (shootPoint != null)
        {
            spawnPos = shootPoint.position;
        }
        else
        {
            Transform head = null;
            Transform[] allT = GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    head = t;
                    break;
                }
            }
            spawnPos = head != null ? head.position + head.forward * 0.8f : (transform.position + transform.forward * 1.5f + Vector3.down * 0.3f);
        }

        GameObject projectile;
        if (projectilePrefab != null)
        {
            projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "[Dragon_PlasmaBall]";
            projectile.transform.position = spawnPos;
            projectile.transform.localScale = Vector3.one * 0.65f;

            Collider col = projectile.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = projectile.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(sh);
                mat.color = projectileColor;
                r.material = mat;
            }
        }

        Vector3 startPos = projectile.transform.position;
        Vector3 endPos = targetPos + Vector3.up * 0.5f;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (projectile != null)
            {
                projectile.transform.position = Vector3.Lerp(startPos, endPos, t);
            }
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private bool HasAnimatorParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"<color=orange>[Enemy]</color> {name} bị bắn trúng! HP còn: {currentHealth:F0}/{maxHealth}");

        if (animator != null && currentHealth > 0f && HasAnimatorParameter("hit"))
        {
            animator.SetTrigger("hit");
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"<color=red>[Enemy]</color> {name} đã bị tiêu diệt! +{goldReward}🪙 Vàng");

        // Rơi vàng vào GameEconomy
        if (GameEconomy.Instance != null && goldReward > 0)
        {
            GameEconomy.Instance.AddCoins(goldReward);
        }

        // Kích nổ hiệu ứng chết
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }

        if (animator != null && HasAnimatorParameter("die"))
        {
            animator.SetTrigger("die");
        }

        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        Destroy(gameObject, 0.9f);
    }
}

