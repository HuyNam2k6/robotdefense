using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("--- Kích thước & Sát thương vụ nổ ---")]
    public float explosionRadius = 1.2f; // Thu nhỏ lại gọn gàng vừa vặn
    public float damage = 20f;
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "Enemy";

    private GameObject fireball;
    private Light flashLight;
    private float duration = 0.28f; // Nổ nhanh, dứt khoát
    private float timer = 0f;
    private Material fireballMat;

    void Start()
    {
        // 1. Ánh sáng chớp sáng nhỏ gọn
        GameObject lightObj = new GameObject("ExplosionFlash");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        flashLight = lightObj.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.55f, 0.1f);
        flashLight.range = explosionRadius * 3f;
        flashLight.intensity = 4f;

        // 2. Quả cầu lửa nở bùng thu nhỏ gấp 4 lần
        fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "FireballCore";
        fireball.transform.SetParent(transform);
        fireball.transform.localPosition = Vector3.zero;
        fireball.transform.localScale = Vector3.one * 0.2f;
        
        Collider c = fireball.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
        fireballMat = new Material(unlitShader);
        fireballMat.color = new Color(1f, 0.45f, 0f, 1f);
        
        fireballMat.SetFloat("_Surface", 1);
        fireballMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fireballMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fireballMat.SetInt("_ZWrite", 0);
        fireballMat.renderQueue = 3000;

        Renderer r = fireball.GetComponent<Renderer>();
        if (r != null) r.material = fireballMat;

        // 3. Sát thương diện rộng
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius * 1.5f, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag(enemyTag))
            {
                hitCollider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        Destroy(gameObject, 0.8f);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / duration);

        if (fireball != null)
        {
            // Bán kính nở tối đa khoảng 1.5m (thay vì 7m khổng lồ như cũ)
            float scale = Mathf.Lerp(0.2f, explosionRadius * 1.6f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            fireball.transform.localScale = Vector3.one * scale;

            if (fireballMat != null)
            {
                Color col = fireballMat.color;
                col.a = Mathf.Lerp(1f, 0f, progress * progress);
                col.r = Mathf.Lerp(1f, 0.9f, progress);
                col.g = Mathf.Lerp(0.6f, 0.1f, progress);
                col.b = 0f;
                fireballMat.color = col;
            }
        }

        if (flashLight != null)
        {
            flashLight.intensity = Mathf.Lerp(4f, 0f, progress);
        }

        if (progress >= 1f && fireball != null)
        {
            Destroy(fireball);
        }
    }
}
