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

    void ShootLightning()
    {
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
            if (!buffer[i].CompareTag(enemyTag)) continue;
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

