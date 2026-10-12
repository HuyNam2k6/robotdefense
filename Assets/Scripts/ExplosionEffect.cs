using UnityEngine;
using System.Collections.Generic;

public class ExplosionEffect : MonoBehaviour
{
    [Header("--- Kích thước & Sát thương vụ nổ ---")]
    public float explosionRadius = 1.2f; // Thu nhỏ lại gọn gàng vừa vặn
    public float damage = 5f; // Giảm sát thương pháo tự hành mỗi quả theo yêu cầu (nhiều đạn nhưng sát thương vừa phải)
    public LayerMask enemyLayer = ~0;
    public string enemyTag = "Enemy";

    private GameObject fireball;
    private Light flashLight;
    private float duration = 0.28f; // Nổ nhanh, dứt khoát
    private float timer = 0f;
    private Material fireballMat;

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

        // 2. Quả cầu lửa nở bùng thu nhỏ gọn gàng
        fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "FireballCore";
        fireball.transform.SetParent(transform);
        fireball.transform.localPosition = Vector3.zero;
        fireball.transform.localScale = Vector3.one * 0.2f;
        
        Collider c = fireball.GetComponent<Collider>();
        if (c != null)
        {
            c.enabled = false;
            DestroyImmediate(c);
        }

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        fireballMat = new Material(unlitShader);
        fireballMat.color = new Color(1f, 0.45f, 0f, 1f);
        
        fireballMat.SetFloat("_Surface", 1);
        fireballMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fireballMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fireballMat.SetInt("_ZWrite", 0);
        fireballMat.renderQueue = 3000;

        Renderer r = fireball.GetComponent<Renderer>();
        if (r != null) r.material = fireballMat;

        // 3. Sát thương diện rộng không dùng collider (Duyệt Enemy.AllEnemies)
        float blastDist = explosionRadius * 1.6f;
        var damagedEnemies = new HashSet<Enemy>();

        for (int i = 0; i < Enemy.AllEnemies.Count; i++)
        {
            Enemy e = Enemy.AllEnemies[i];
            if (e != null && e.gameObject.activeInHierarchy && !e.isDead && e.currentHealth > 0f)
            {
                if (Vector3.Distance(transform.position, e.transform.position) <= blastDist)
                {
                    e.TakeDamage(damage);
                    damagedEnemies.Add(e);
                }
            }
        }

        // Dự phòng quét Collider nếu có đối tượng khác mang Tag Enemy
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, blastDist, enemyLayer);
        foreach (var hitCollider in hitColliders)
        {
            if (IsPlayer(hitCollider)) continue; // Bỏ qua 100% Player!

            if (hitCollider.CompareTag(enemyTag) || hitCollider.GetComponentInParent<Enemy>() != null)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>() ?? hitCollider.GetComponentInParent<Enemy>();
                if (enemy != null && !damagedEnemies.Contains(enemy))
                {
                    enemy.TakeDamage(damage);
                    damagedEnemies.Add(enemy);
                }
                else if (enemy == null)
                {
                    hitCollider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                }
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
