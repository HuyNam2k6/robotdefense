using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MechaDragonBoss - Hệ Thống Kỹ Năng Đỉnh Cao Cho Đại Boss Mecha Cyber Dragon:
/// 1. Tự động nâng cấp HP lên 3500, Sát thương 60, Thưởng 500 Vàng.
/// 2. Kỹ năng 1: [🔥 HƠI THỞ PLASMA BÃO TÁP] - Bắn liên hoàn chùm 7 quả cầu plasma hình nón thiêu đốt mục tiêu.
/// 3. Kỹ năng 2: [☄️ MƯA THIÊN THẠCH CYBER] - Trút 8 quả cầu thiên thạch rơi từ trên không xuống nổ tung diện rộng AOE.
/// 4. Kỹ năng 3: [⚡ ĐỊA CHẤN SÓNG XUNG KÍCH EMP] - Phóng vòng sóng điện từ EMP bán kính 12m hất văng & gây 70 sát thương.
/// 5. Kỹ năng 4: [👾 TRIỆU HỒI ĐỆ TỬ CƠ KHÍ] - Thả 2 bầy bọ Alien Bug / Spider chi viện (100% từ Assets/prefabsEnemy/).
/// 6. Kỹ năng 5: [🛡️ LÁ CHẮN NĂNG LƯỢNG THÉP] - Kích hoạt khi HP < 50%, giảm 50% toàn bộ sát thương nhận vào trong 8 giây.
/// Tuân thủ nghiêm ngặt Quy tắc dự án: 100% Raycast & Lerp đường đạn (Zero Rigidbody trên đạn và quái).
/// </summary>
public class MechaDragonBoss : MonoBehaviour
{
    [Header("=== THAM CHIẾU ENEMY GỐC ===")]
    public Enemy enemy;
    public Animator animator;

    [Header("=== THỜI GIAN HỒI CHIÊU CÁC KỸ NĂNG (COOLDOWNS) ===")]
    public float flameBreathCooldown = 8.0f;
    public float meteorBarrageCooldown = 15.0f;
    public float empShockwaveCooldown = 18.0f;
    public float summonMinionsCooldown = 22.0f;

    [Header("=== TRẠNG THÁI SHIELD BẢO VỆ ===")]
    public bool isShieldActive = false;
    private bool hasTriggeredHalfHpShield = false;
    private GameObject shieldVfxObj;

    // Bộ đếm thời gian kỹ năng
    private float flameTimer = 3.0f;
    private float meteorTimer = 8.0f;
    private float shockwaveTimer = 14.0f;
    private float summonTimer = 18.0f;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        animator = GetComponentInChildren<Animator>();

        if (enemy != null)
        {
            // Thiết lập chỉ số Đại Boss cực khỏe
            enemy.enemyName = "👑 MECHA CYBER DRAGON [ĐẠI BOSS]";
            enemy.maxHealth = 3500f;
            enemy.currentHealth = 3500f;
            enemy.attackDamage = 60f;
            enemy.goldReward = 500;
            enemy.moveSpeed = 3.2f;
            enemy.attackRange = 9.0f;
            enemy.healthBarHeight = 2.4f;
            enemy.flightAltitude = 4.2f;
        }
    }

    private void Start()
    {
        if (enemy != null)
        {
            enemy.UpdateHealthBar();
        }

        // Báo hiệu Boss xuất hiện
        FloatingDamageText.Spawn(transform.position + Vector3.up * 3f, 0, new Color(1f, 0.2f, 0.2f));
    }

    private void Update()
    {
        if (enemy == null || enemy.currentHealth <= 0f) return;

        // 1. Kiểm tra kích hoạt Kỹ năng 5: [Lá Chắn Năng Lượng Thép] khi HP < 50%
        if (!hasTriggeredHalfHpShield && enemy.currentHealth <= enemy.maxHealth * 0.5f)
        {
            hasTriggeredHalfHpShield = true;
            StartCoroutine(ActivateEnergyShield(8.0f));
        }

        // Cập nhật vị trí vòng bảo vệ nếu đang bật
        if (shieldVfxObj != null)
        {
            shieldVfxObj.transform.position = transform.position + Vector3.up * 1.5f;
            shieldVfxObj.transform.Rotate(Vector3.up, 90f * Time.deltaTime);
        }

        Transform target = enemy.target;
        if (target == null || !target.gameObject.activeInHierarchy) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);
        if (distToTarget > 25f) return; // Chỉ thi triển skill khi trong tầm hoạt động

        // 2. Kỹ năng 1: [🔥 HƠI THỞ PLASMA BÃO TÁP]
        flameTimer -= Time.deltaTime;
        if (flameTimer <= 0f)
        {
            flameTimer = flameBreathCooldown;
            StartCoroutine(SkillFlameBreath(target));
        }

        // 3. Kỹ năng 2: [☄️ MƯA THIÊN THẠCH CYBER]
        meteorTimer -= Time.deltaTime;
        if (meteorTimer <= 0f)
        {
            meteorTimer = meteorBarrageCooldown;
            StartCoroutine(SkillMeteorBarrage(target));
        }

        // 4. Kỹ năng 3: [⚡ ĐỊA CHẤN SÓNG XUNG KÍCH EMP]
        shockwaveTimer -= Time.deltaTime;
        if (shockwaveTimer <= 0f)
        {
            shockwaveTimer = empShockwaveCooldown;
            StartCoroutine(SkillEmpShockwave());
        }

        // 5. Kỹ năng 4: [👾 TRIỆU HỒI ĐỆ TỬ CƠ KHÍ]
        summonTimer -= Time.deltaTime;
        if (summonTimer <= 0f)
        {
            summonTimer = summonMinionsCooldown;
            StartCoroutine(SkillSummonMinions());
        }
    }

    // ================= KỸ NĂNG 1: HƠI THỞ PLASMA BÃO TÁP =================
    private IEnumerator SkillFlameBreath(Transform target)
    {
        if (target == null) yield break;

        ShowSkillAnnouncement("🔥 [HƠI THỞ PLASMA BÃO TÁP]!", new Color(1f, 0.45f, 0.1f));
        if (animator != null) animator.SetTrigger("attack");

        yield return new WaitForSeconds(0.45f);

        Vector3 origin = transform.position + transform.forward * 2.0f + Vector3.up * 0.5f;
        int projectileCount = 7;

        for (int i = 0; i < projectileCount; i++)
        {
            if (target == null) break;

            Vector3 aimTarget = target.position + Vector3.up * 0.5f + (Random.insideUnitSphere * 1.5f);
            aimTarget.y = Mathf.Max(aimTarget.y, target.position.y);

            StartCoroutine(LaunchRaycastPlasmaBall(origin, aimTarget, 28f));
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator LaunchRaycastPlasmaBall(Vector3 start, Vector3 end, float damage)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "[Boss_Plasma_Ball]";
        ball.transform.position = start;
        ball.transform.localScale = Vector3.one * 0.7f;

        Collider col = ball.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = ball.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = new Color(1f, 0.4f, 0.05f);
            rend.material = mat;
        }

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (ball != null)
            {
                ball.transform.position = Vector3.Lerp(start, end, t);
            }
            yield return null;
        }

        if (ball != null)
        {
            ExplodeAtPoint(end, 2.5f, damage);
            Destroy(ball);
        }
    }

    // ================= KỸ NĂNG 2: MƯA THIÊN THẠCH CYBER =================
    private IEnumerator SkillMeteorBarrage(Transform target)
    {
        if (target == null) yield break;

        ShowSkillAnnouncement("☄️ [MƯA THIÊN THẠCH CYBER]!", new Color(1f, 0.75f, 0.15f));
        if (animator != null) animator.SetTrigger("attack");

        Vector3 center = target.position;
        int meteorCount = 8;

        for (int i = 0; i < meteorCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * 7.5f;
            Vector3 targetImpact = new Vector3(center.x + randomCircle.x, center.y, center.z + randomCircle.y);
            Vector3 skySpawn = targetImpact + Vector3.up * 18f;

            StartCoroutine(LaunchMeteor(skySpawn, targetImpact, 45f));
            yield return new WaitForSeconds(0.16f);
        }
    }

    private IEnumerator LaunchMeteor(Vector3 start, Vector3 end, float damage)
    {
        GameObject meteor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meteor.name = "[Boss_Cyber_Meteor]";
        meteor.transform.position = start;
        meteor.transform.localScale = new Vector3(0.9f, 1.4f, 0.9f);

        Collider col = meteor.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = meteor.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = new Color(1f, 0.2f, 0.1f);
            rend.material = mat;
        }

        float duration = 0.55f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (meteor != null)
            {
                meteor.transform.position = Vector3.Lerp(start, end, t * t);
            }
            yield return null;
        }

        if (meteor != null)
        {
            ExplodeAtPoint(end, 3.8f, damage);
            Destroy(meteor);
        }
    }

    // ================= KỸ NĂNG 3: ĐỊA CHẤN SÓNG XUNG KÍCH EMP =================
    private IEnumerator SkillEmpShockwave()
    {
        ShowSkillAnnouncement("⚡ [ĐỊA CHẤN SÓNG XUNG KÍCH EMP]!", new Color(0.1f, 0.9f, 1f));
        if (animator != null) animator.SetTrigger("attack");

        yield return new WaitForSeconds(0.4f);

        Vector3 waveOrigin = transform.position;
        float maxRadius = 12.0f;
        float waveDuration = 0.6f;

        GameObject waveRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waveRing.name = "[Boss_EMP_WaveRing]";
        waveRing.transform.position = waveOrigin + Vector3.up * 0.2f;
        waveRing.transform.localScale = new Vector3(1f, 0.05f, 1f);

        Collider col = waveRing.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = waveRing.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = new Color(0f, 0.85f, 1f, 0.85f);
            rend.material = mat;
        }

        float elapsed = 0f;
        HashSet<Transform> hitTargets = new HashSet<Transform>();

        while (elapsed < waveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / waveDuration;
            float currentRadius = Mathf.Lerp(1f, maxRadius, t);

            if (waveRing != null)
            {
                waveRing.transform.localScale = new Vector3(currentRadius * 2f, 0.05f, currentRadius * 2f);
            }

            // Quét tia Raycast kiểm tra các mục tiêu xung quanh
            Collider[] cols = Physics.OverlapSphere(waveOrigin, currentRadius);
            foreach (var c in cols)
            {
                if (c == null) continue;
                Transform root = c.transform.root;
                if (root == transform || hitTargets.Contains(root)) continue;

                var wall = root.GetComponent<WallSegment>();
                var turret = root.GetComponent<UpgradableTurret>();
                var player = root.GetComponent<PlayerController>();
                var friendlyBot = root.GetComponent<FriendlyCombatRobot>();

                if (wall != null || turret != null || player != null || friendlyBot != null)
                {
                    hitTargets.Add(root);
                    root.SendMessage("TakeDamage", 65f, SendMessageOptions.DontRequireReceiver);
                    FloatingDamageText.Spawn(root.position + Vector3.up * 1.5f, 65f, new Color(0f, 0.9f, 1f));
                }
            }

            yield return null;
        }

        if (waveRing != null) Destroy(waveRing);
    }

    // ================= KỸ NĂNG 4: TRIỆU HỒI ĐỆ TỬ CƠ KHÍ =================
    private IEnumerator SkillSummonMinions()
    {
        ShowSkillAnnouncement("👾 [TRIỆU HỒI ĐỆ TỬ CƠ KHÍ]!", new Color(0.85f, 0.2f, 1f));

        if (EnemySpawner.Instance == null || EnemySpawner.Instance.normalEnemyPrefabs == null || EnemySpawner.Instance.normalEnemyPrefabs.Length == 0)
            yield break;

        for (int i = 0; i < 2; i++)
        {
            Vector3 offset = (i == 0 ? transform.right : -transform.right) * 3.5f + Vector3.up * 5f;
            Vector3 spawnPos = transform.position + offset;

            GameObject prefab = EnemySpawner.Instance.normalEnemyPrefabs[Random.Range(0, EnemySpawner.Instance.normalEnemyPrefabs.Length)];
            if (prefab != null)
            {
                GameObject minion = Instantiate(prefab, spawnPos, transform.rotation);
                minion.name = $"Minion_{prefab.name}_{Time.frameCount}";
                minion.transform.localScale = Vector3.one;

                Rigidbody rb = minion.GetComponent<Rigidbody>();
                if (rb != null) Destroy(rb);

                if (!minion.CompareTag("Enemy"))
                {
                    try { minion.tag = "Enemy"; } catch { }
                }

                FloatingDamageText.Spawn(spawnPos, 0, Color.magenta);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ================= KỸ NĂNG 5: LÁ CHẮN NĂNG LƯỢNG THÉP =================
    private IEnumerator ActivateEnergyShield(float duration)
    {
        isShieldActive = true;
        ShowSkillAnnouncement("🛡️ [LÁ CHẮN NĂNG LƯỢNG - GIẢM 50% DMG]!", new Color(0f, 1f, 0.8f));

        shieldVfxObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVfxObj.name = "[Boss_Energy_Shield_VFX]";
        shieldVfxObj.transform.position = transform.position + Vector3.up * 1.5f;
        shieldVfxObj.transform.localScale = Vector3.one * 5.2f;

        Collider col = shieldVfxObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer rend = shieldVfxObj.GetComponent<Renderer>();
        if (rend != null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material mat = new Material(sh);
            mat.color = new Color(0f, 0.9f, 0.85f, 0.45f);
            rend.material = mat;
        }

        yield return new WaitForSeconds(duration);

        isShieldActive = false;
        if (shieldVfxObj != null)
        {
            Destroy(shieldVfxObj);
        }
    }

    // ================= TIỆN ÍCH GÂY NỔ VÀ THÔNG BÁO CHIÊU THỨC =================
    private void ExplodeAtPoint(Vector3 center, float radius, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius);
        HashSet<Transform> processedRoots = new HashSet<Transform>();

        foreach (var c in hits)
        {
            if (c == null) continue;
            Transform root = c.transform.root;
            if (root == transform || processedRoots.Contains(root)) continue;

            var wall = root.GetComponent<WallSegment>();
            var turret = root.GetComponent<UpgradableTurret>();
            var player = root.GetComponent<PlayerController>();
            var friendlyBot = root.GetComponent<FriendlyCombatRobot>();

            if (wall != null || turret != null || player != null || friendlyBot != null)
            {
                processedRoots.Add(root);
                root.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                FloatingDamageText.Spawn(root.position + Vector3.up * 1.5f, damage, new Color(1f, 0.5f, 0.1f));
            }
        }
    }

    private void ShowSkillAnnouncement(string text, Color color)
    {
        FloatingDamageText.Spawn(transform.position + Vector3.up * 3.5f, 0, color);
        Debug.Log($"<color=red><b>[ĐẠI BOSS MECHA]</b></color> Tung chiêu: <color=yellow><b>{text}</b></color>");
    }

    private void OnDestroy()
    {
        if (shieldVfxObj != null)
        {
            Destroy(shieldVfxObj);
        }
    }
}
