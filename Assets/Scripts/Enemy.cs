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

    [Header("--- Hiệu Ứng & Animation ---")]
    public Animator animator;
    public GameObject deathVFX;

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

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > attackRange)
        {
            // Di chuyển về phía mục tiêu
            Vector3 dir = (target.position - transform.position);
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
            // Đã áp sát mục tiêu -> dừng và xoay đầu hướng về phía mục tiêu để tấn công
            Vector3 dir = (target.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();
                Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelRotationOffset, 0f);
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

    // ================= TRỌNG LỰC BẰNG CODE CHO GAME 3D =================
    private void SnapToGroundImmediately()
    {
        Vector3 currentPos = transform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 10f, currentPos.z);
        int layerMask = ~LayerMask.GetMask("Enemy");
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 30f, layerMask, QueryTriggerInteraction.Ignore))
        {
            currentPos.y = hit.point.y;
            transform.position = currentPos;
        }
        else if (Terrain.activeTerrain != null)
        {
            currentPos.y = Terrain.activeTerrain.SampleHeight(currentPos) + Terrain.activeTerrain.transform.position.y;
            transform.position = currentPos;
        }
    }

    private void ApplyGroundGravity()
    {
        Vector3 currentPos = transform.position;
        Vector3 rayOrigin = new Vector3(currentPos.x, currentPos.y + 4f, currentPos.z);
        int layerMask = ~LayerMask.GetMask("Enemy");
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 15f, layerMask, QueryTriggerInteraction.Ignore))
        {
            float targetY = hit.point.y;
            currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, 20f * Time.deltaTime);
            transform.position = currentPos;
        }
        else if (Terrain.activeTerrain != null)
        {
            float terrainY = Terrain.activeTerrain.SampleHeight(currentPos) + Terrain.activeTerrain.transform.position.y;
            currentPos.y = Mathf.MoveTowards(currentPos.y, terrainY, 20f * Time.deltaTime);
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
            target.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        Debug.Log($"<color=orange>[Enemy]</color> {name} bị bắn trúng! HP còn: {currentHealth:F0}/{maxHealth}");

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

        if (animator != null)
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

