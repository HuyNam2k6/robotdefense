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

    private LineRenderer lineRenderer;
    private Transform currentTarget;
    private Collider currentCollider;
    private readonly Collider[] buffer = new Collider[32];
    private Vector3[] segmentPositions;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = lightningSegments;
        lineRenderer.startWidth = lightningWidth;
        lineRenderer.endWidth = lightningWidth;
        lineRenderer.enabled = false;
        
        segmentPositions = new Vector3[lightningSegments];

        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint_Auto");
            fp.transform.SetParent(transform);
            
            Collider col = GetComponentInChildren<Collider>();
            if (col != null)
                fp.transform.position = new Vector3(transform.position.x, col.bounds.max.y, transform.position.z);
            else
                fp.transform.localPosition = new Vector3(0, 2f, 0);
            
            firePoint = fp.transform;
        }
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
        Vector3 startPos = firePoint.position;
        Vector3 targetPos = currentCollider != null ? currentCollider.bounds.center : currentTarget.position + Vector3.up;
        
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
    }

    void FindTarget()
    {
        if (currentTarget != null)
        {
            float sqr = (currentTarget.position - transform.position).sqrMagnitude;
            if (sqr <= attackRange * attackRange && currentTarget.gameObject.activeInHierarchy) return;
            currentTarget = null;
            currentCollider = null;
        }
        int count = Physics.OverlapSphereNonAlloc(transform.position, attackRange, buffer, enemyLayer, QueryTriggerInteraction.Collide);
        float best = Mathf.Infinity;
        for (int i = 0; i < count; i++)
        {
            // BỎ QUA 100% PLAYER: Tuyệt đối không nhắm vào người chơi
            if (IsPlayer(buffer[i])) continue;

            if (!buffer[i].CompareTag(enemyTag) && buffer[i].GetComponentInParent<Enemy>() == null) continue;

            float sqr = (buffer[i].transform.position - transform.position).sqrMagnitude;
            if (sqr < best)
            {
                best = sqr;
                currentTarget = buffer[i].transform;
                currentCollider = buffer[i];
            }
        }
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

