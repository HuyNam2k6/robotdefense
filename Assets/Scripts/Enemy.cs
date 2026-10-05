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
    }

    void Start()
    {
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
                transform.position += Vector3.back * (moveSpeed * Time.deltaTime);
                if (animator != null) animator.SetBool("isMoving", true);
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
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 8f);
                transform.position += dir * (moveSpeed * Time.deltaTime);
            }

            if (animator != null) animator.SetBool("isMoving", true);
        }
        else
        {
            // Đã áp sát mục tiêu -> dừng và tấn công
            if (animator != null) animator.SetBool("isMoving", false);

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                lastAttackTime = Time.time;
                Attack();
            }
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
