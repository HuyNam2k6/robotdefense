#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningTurret : MonoBehaviour
{
    [Header("--- Cài đặt cơ bản ---")]
    public float attackRange = 15f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "Enemy";

    [Header("--- Hiển thị tia sét ---")]
    public Transform firePoint;
    public float lightningWidth = 0.2f;
    
    [Tooltip("Số đoạn gấp khúc của tia sét")]
    public int lightningSegments = 12;
    [Tooltip("Độ rung giật của tia sét")]
    public float jitterAmount = 0.6f;

    public float damagePerSecond = 10f;

    [Header("--- Sét Lan Chuỗi (Chain Lightning) ---")]
    [Tooltip("Bán kính lây lan sang các quái lân cận (3 mét theo yêu cầu)")]
    public float chainRadius = 3.0f;
    [Tooltip("Số mục tiêu quái vật lan tối đa")]
    public int maxChainTargets = 4;
    [Tooltip("Tỷ lệ sát thương giật lan")]
    public float chainDamageRatio = 0.8f;
    [Tooltip("Số đoạn zíc zắc của tia sét lan")]
    public int chainSegments = 8;

    private LineRenderer lineRenderer;
    private Transform currentTarget;
    private Collider currentCollider;
    private readonly Collider[] buffer = new Collider[32];
    private Vector3[] segmentPositions;
    private readonly System.Collections.Generic.List<LineRenderer> chainRenderers = new System.Collections.Generic.List<LineRenderer>();
    private readonly System.Collections.Generic.List<Vector3[]> chainSegmentBuffers = new System.Collections.Generic.List<Vector3[]>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = lightningSegments;
        lineRenderer.startWidth = lightningWidth;
        lineRenderer.endWidth = lightningWidth;
        lineRenderer.enabled = false;
        
        // Đảm bảo Material hiển thị tia sét màu Cyan/Xanh Lam phát sáng rực rỡ
        if (lineRenderer.material == null || lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name == "Default-Material")
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                Material mat = new Material(shader);
                Color glowColor = new Color(0.25f, 0.9f, 1.0f, 1.0f) * 2.5f;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glowColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", glowColor);
                lineRenderer.material = mat;
            }
        }

        segmentPositions = new Vector3[lightningSegments];
        EnsureChainRenderers();

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint_Auto");
            fp.transform.SetParent(transform);
            
            Collider col = GetComponentInChildren<Collider>();
            if (col != null && col.bounds.max.y > transform.position.y + 0.5f)
                fp.transform.position = new Vector3(transform.position.x, col.bounds.max.y, transform.position.z);
            else
                fp.transform.position = transform.position + Vector3.up * 2.8f;
            
            firePoint = fp.transform;
        }
    }

    private void EnsureChainRenderers()
    {
        while (chainRenderers.Count < maxChainTargets)
        {
            int idx = chainRenderers.Count;
            GameObject go = new GameObject($"ChainLightning_{idx}");
            go.transform.SetParent(transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = chainSegments;
            lr.startWidth = lightningWidth * 0.7f;
            lr.endWidth = lightningWidth * 0.7f;
            lr.material = lineRenderer.material;
            lr.enabled = false;
            chainRenderers.Add(lr);
            chainSegmentBuffers.Add(new Vector3[chainSegments]);
        }
    }

    private void DisableAllChainLines()
    {
        for (int k = 0; k < chainRenderers.Count; k++)
        {
            if (chainRenderers[k] != null) chainRenderers[k].enabled = false;
        }
    }

    void OnDisable()
    {
        if (lineRenderer != null) lineRenderer.enabled = false;
        DisableAllChainLines();
    }

    void Update()
    {
        FindTarget();
        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            ShootLightning();
        }
        else
        {
            lineRenderer.enabled = false;
            DisableAllChainLines();
        }
    }

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

    void ShootLightning()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            lineRenderer.enabled = false;
            DisableAllChainLines();
            return;
        }

        // Tuyệt đối không gây sát thương hay hiệu ứng lên Player
        if (!IsPlayer(currentCollider) && currentTarget.GetComponentInParent<PlayerController>() == null)
        {
            Enemy enemy = currentTarget.GetComponent<Enemy>() ?? currentTarget.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damagePerSecond * Time.deltaTime);
            }
            else
            {
                currentTarget.SendMessage("TakeDamage", damagePerSecond * Time.deltaTime, SendMessageOptions.DontRequireReceiver);
            }
        }

        lineRenderer.enabled = true;
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 2.8f;
        if (startPos.y < transform.position.y + 1.2f) startPos.y = transform.position.y + 2.8f;

        Vector3 targetPos = currentCollider != null ? currentCollider.bounds.center : (currentTarget.position + Vector3.up * 1.0f);
        
        if (segmentPositions == null || segmentPositions.Length != lightningSegments)
            segmentPositions = new Vector3[lightningSegments];
            
        lineRenderer.positionCount = lightningSegments;
        segmentPositions[0] = startPos;
        segmentPositions[lightningSegments - 1] = targetPos;

        for (int i = 1; i < lightningSegments - 1; i++)
        {
            float t = (float)i / (lightningSegments - 1);
            Vector3 pointOnLine = Vector3.Lerp(startPos, targetPos, t);
            Vector3 randomJitter = new Vector3(
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount),
                Random.Range(-jitterAmount, jitterAmount)
            );
            segmentPositions[i] = pointOnLine + randomJitter;
        }
        lineRenderer.SetPositions(segmentPositions);

        // ================= SÉT LAN 3 MÉT CHO CÁC CON QUÁI GẦN NHAU =================
        EnsureChainRenderers();
        int activeChainCount = 0;
        float radiusSqr = chainRadius * chainRadius;

        for (int i = 0; i < Enemy.AllEnemies.Count; i++)
        {
            Enemy e = Enemy.AllEnemies[i];
            if (e == null || !e.gameObject.activeInHierarchy || e.isDead || e.currentHealth <= 0f) continue;
            if (e.transform == currentTarget) continue; // Bỏ qua mục tiêu chính đang bị bắn

            Vector3 ePos = e.transform.position + Vector3.up * 0.8f;
            float distSqr = (ePos - targetPos).sqrMagnitude;
            if (distSqr <= radiusSqr)
            {
                // Sát thương lây lan
                e.TakeDamage(damagePerSecond * chainDamageRatio * Time.deltaTime);

                // Hiển thị tia sét nhánh zíc zắc từ mục tiêu chính sang quái phụ
                if (activeChainCount < chainRenderers.Count)
                {
                    LineRenderer cLr = chainRenderers[activeChainCount];
                    cLr.enabled = true;
                    Vector3[] cSegs = chainSegmentBuffers[activeChainCount];
                    cLr.positionCount = chainSegments;
                    cSegs[0] = targetPos;
                    cSegs[chainSegments - 1] = ePos;

                    for (int s = 1; s < chainSegments - 1; s++)
                    {
                        float t = (float)s / (chainSegments - 1);
                        Vector3 p = Vector3.Lerp(targetPos, ePos, t);
                        Vector3 j = new Vector3(
                            Random.Range(-jitterAmount * 0.65f, jitterAmount * 0.65f),
                            Random.Range(-jitterAmount * 0.65f, jitterAmount * 0.65f),
                            Random.Range(-jitterAmount * 0.65f, jitterAmount * 0.65f)
                        );
                        cSegs[s] = p + j;
                    }
                    cLr.SetPositions(cSegs);
                    activeChainCount++;
                }

                if (activeChainCount >= maxChainTargets) break;
            }
        }

        // Tắt các nhánh sét thừa không có mục tiêu
        for (int k = activeChainCount; k < chainRenderers.Count; k++)
        {
            if (chainRenderers[k] != null) chainRenderers[k].enabled = false;
        }
    }

    void FindTarget()
    {
        if (currentTarget != null)
        {
            if (currentTarget.gameObject.activeInHierarchy)
            {
                Enemy curEnemy = currentTarget.GetComponent<Enemy>() ?? currentTarget.GetComponentInParent<Enemy>();
                if (curEnemy != null && curEnemy.currentHealth > 0f)
                {
                    float sqr = (currentTarget.position - transform.position).sqrMagnitude;
                    if (sqr <= attackRange * attackRange) return;
                }
            }
            currentTarget = null;
            currentCollider = null;
        }

        float bestDistSqr = attackRange * attackRange;
        Transform bestTarget = null;
        Collider bestCollider = null;

        // 1. Quét danh sách quái vật trong Enemy.AllEnemies (100% không phụ thuộc Collider, tối ưu CPU)
        for (int i = 0; i < Enemy.AllEnemies.Count; i++)
        {
            Enemy e = Enemy.AllEnemies[i];
            if (e == null || !e.gameObject.activeInHierarchy || e.isDead || e.currentHealth <= 0f) continue;

            float distSqr = (e.transform.position - transform.position).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestTarget = e.transform;
                bestCollider = e.GetComponent<Collider>();
            }
        }

        // 2. Dự phòng quét Physics.OverlapSphereNonAlloc nếu có đối tượng khác mang Tag Enemy
        if (bestTarget == null)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = buffer[i];
                if (col == null || IsPlayer(col)) continue;
                if (!col.CompareTag(enemyTag) && col.GetComponentInParent<Enemy>() == null) continue;

                Enemy enemyComp = col.GetComponent<Enemy>() ?? col.GetComponentInParent<Enemy>();
                if (enemyComp != null && enemyComp.currentHealth <= 0f) continue;

                float sqr = (col.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestDistSqr)
                {
                    bestDistSqr = sqr;
                    bestTarget = enemyComp != null ? enemyComp.transform : col.transform;
                    bestCollider = col;
                }
            }
        }

        currentTarget = bestTarget;
        currentCollider = bestCollider;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null && (lineRenderer.sharedMaterial == null || lineRenderer.sharedMaterial.name == "Default-Material" || lineRenderer.sharedMaterial.name.Contains("lightnight")))
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/LightningGlow.mat");
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                
                if (shader != null)
                {
                    mat = new Material(shader);
                    // Màu xanh lam sáng rực (cyan/blue glow)
                    Color glowColor = new Color(0.2f, 0.8f, 1.0f, 1.0f) * 2.5f; 
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", glowColor);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", glowColor);
                    
                    AssetDatabase.CreateAsset(mat, "Assets/LightningGlow.mat");
                    AssetDatabase.SaveAssets();
                }
            }
            if (mat != null) lineRenderer.sharedMaterial = mat;
        }
    }
#endif
}

