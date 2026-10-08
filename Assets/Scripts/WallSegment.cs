using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WallSegment : MonoBehaviour
{
    public static readonly List<WallSegment> AllWalls = new List<WallSegment>();

    [Header("Cấp Độ Tường (1 - 6)")]
    [Range(1, 6)]
    public int currentLevel = 1;

    [Header("Máu Tường (Wall Health)")]
    public float maxHealth = 150f;
    public float currentHealth = 150f;
    public bool isDestroyed = false;

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;
        currentHealth -= amount;
        FloatingDamageText.Spawn(transform.position + Vector3.up * 1.5f, amount, new Color(0.9f, 0.6f, 0.2f));
        if (currentHealth <= 0f)
        {
            isDestroyed = true;
            AllWalls.Remove(this);
            Destroy(gameObject);
        }
    }

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private Vector3 baseScale = Vector3.zero;
    private Vector3 baseLocalPosition = Vector3.zero;

    // --- Hiệu ứng Phản Ứng Khi Chọn Tường (Clash of Clans Style) ---
    private bool isSelected = false;
    private Coroutine hopCoroutine;

    // --- Hiệu ứng Lấp Lánh Điểm Xuyết (Subtle Sparkle VFX từ Cấp 4 trở lên) ---
    private ParticleSystem sparkleParticles;

    // Texture và Material dùng chung cho hiệu ứng hạt lấp lánh
    private static Texture2D sharedSparkleTexture;
    private static Material sharedSparkleMaterial;

    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
    }

    void OnEnable()
    {
        if (!AllWalls.Contains(this)) AllWalls.Add(this);
    }

    void OnDisable()
    {
        AllWalls.Remove(this);
    }

    void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        propBlock = new MaterialPropertyBlock();
        baseScale = transform.localScale;
        baseLocalPosition = transform.localPosition;
    }

    void Start()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        if (baseLocalPosition == Vector3.zero) baseLocalPosition = transform.localPosition;

        EnsureSparkleParticles();

        if (GameEconomy.Instance != null && currentLevel <= 1)
        {
            ApplyLevel(GameEconomy.Instance.wallLevel);
        }
        else
        {
            ApplyLevel(currentLevel);
        }
    }

    void Update()
    {
        // Hiệu ứng nhấp nháy màu xanh dương và trở về màu nguyên bản liên tục khi được chọn
        if (isSelected && cachedRenderers != null)
        {
            // Dao động hình sin êm ái: 0.0 (Màu nguyên bản) <---> 1.0 (Màu xanh dương)
            float t = (Mathf.Sin(Time.time * 6.5f) + 1f) * 0.5f;
            Color neonBlue = new Color(0.12f, 0.65f, 1.0f, 1f);
            Color blueEmission = new Color(0.15f, 0.75f, 2.2f, 1f) * t;

            Color origWallColor = GetWallColorForLevel(currentLevel);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                var rend = cachedRenderers[i];
                if (rend == null) continue;
                rend.GetPropertyBlock(propBlock);

                Color curCol = Color.Lerp(origWallColor, neonBlue, t);
                propBlock.SetColor("_BaseColor", curCol);
                propBlock.SetColor("_Color", curCol);
                propBlock.SetColor("_EmissionColor", blueEmission);

                rend.SetPropertyBlock(propBlock);
            }
        }
    }

    // ================= PHẢN ỨNG KHI CHỌN TƯỜNG (CLASH OF CLANS STYLE) =================

    private Coroutine selectScaleCoroutine;

    public Vector3 GetBaseScale()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        if (baseScale.x <= 1.05f && transform.localScale.x > 1.5f) baseScale = transform.localScale;
        if (baseScale == Vector3.one || baseScale.x <= 1.05f) baseScale = Vector3.one * 3f;
        return baseScale;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (gameObject.activeInHierarchy)
        {
            if (selectScaleCoroutine != null) StopCoroutine(selectScaleCoroutine);
            selectScaleCoroutine = StartCoroutine(AnimateSelectScale(selected));
        }
        else
        {
            transform.localScale = selected ? GetBaseScale() * 1.5f : GetBaseScale();
        }

        if (!selected)
        {
            // Trả lại màu và emission bình thường của cấp tường
            ApplyLevel(currentLevel);
        }
    }

    private IEnumerator AnimateSelectScale(bool selected)
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = selected ? GetBaseScale() * 1.5f : GetBaseScale();

        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            yield return null;
        }

        transform.localScale = targetScale;
        selectScaleCoroutine = null;
    }

    public void TriggerSelectHop(float delay = 0f, float hopHeight = 0.45f)
    {
        if (gameObject.activeInHierarchy)
        {
            if (hopCoroutine != null) StopCoroutine(hopCoroutine);
            hopCoroutine = StartCoroutine(SelectHopCoroutine(delay, hopHeight));
        }
    }

    private IEnumerator SelectHopCoroutine(float delay, float hopHeight)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (baseLocalPosition == Vector3.zero) baseLocalPosition = transform.localPosition;
        if (baseScale == Vector3.zero) baseScale = transform.localScale;

        Vector3 origPos = baseLocalPosition;
        Vector3 origScale = baseScale;
        if (origScale == Vector3.one) origScale = Vector3.one * 3f;

        float duration = 0.26f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Parabol nảy lên rồi hạ xuống
            // Parabol nảy lên rồi hạ xuống vị trí gốc
            float hopCurve = 4f * t * (1f - t);
            transform.localPosition = origPos + Vector3.up * (hopHeight * hopCurve);

            // Co giãn đàn hồi Squash & Stretch
            float stretchY = 1f + hopCurve * 0.16f;
            float squashXZ = 1f - hopCurve * 0.08f;
            transform.localScale = new Vector3(origScale.x * squashXZ, origScale.y * stretchY, origScale.z * squashXZ);

            yield return null;
        }

        transform.localPosition = origPos;
        transform.localScale = origScale;
        hopCoroutine = null;
    }

    // ================= TẠO VẬT LIỆU & HỆ THỐNG HẠT LẤP LÁNH =================

    public static Material GetSharedSparkleMaterial()
    {
        if (sharedSparkleMaterial != null) return sharedSparkleMaterial;

        // Tạo Texture hình ngôi sao 4 cánh lấp lánh (sắc nét hơn 20%)
        if (sharedSparkleTexture == null)
        {
            int size = 64;
            sharedSparkleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            sharedSparkleTexture.name = "Sparkle_Star_Texture";
            sharedSparkleTexture.filterMode = FilterMode.Bilinear;
            sharedSparkleTexture.wrapMode = TextureWrapMode.Clamp;

            float center = (size - 1) * 0.5f;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs((x - center) / center);
                    float dy = Mathf.Abs((y - center) / center);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Quầng sáng tâm tròn rõ nét hơn 20%
                    float glow = Mathf.Max(0f, 1f - dist * 2.5f);
                    glow = glow * glow;

                    // 4 cánh sao nhọn mảnh rõ ràng
                    float armX = Mathf.Max(0f, 1f - dx * 1.15f) * Mathf.Max(0f, 1f - dy * 7f);
                    float armY = Mathf.Max(0f, 1f - dy * 1.15f) * Mathf.Max(0f, 1f - dx * 7f);
                    float star = Mathf.Max(armX, armY);

                    float alpha = Mathf.Clamp01(glow * 0.85f + star * 1.0f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            sharedSparkleTexture.SetPixels(pixels);
            sharedSparkleTexture.Apply();
        }

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        if (s == null) s = Shader.Find("Mobile/Particles/Additive");
        if (s == null) s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Unlit/Transparent");

        sharedSparkleMaterial = new Material(s);
        sharedSparkleMaterial.name = "Sparkle_VFX_Material";
        if (sharedSparkleMaterial.HasProperty("_BaseMap")) sharedSparkleMaterial.SetTexture("_BaseMap", sharedSparkleTexture);
        if (sharedSparkleMaterial.HasProperty("_MainTex")) sharedSparkleMaterial.SetTexture("_MainTex", sharedSparkleTexture);
        if (sharedSparkleMaterial.HasProperty("_Color")) sharedSparkleMaterial.SetColor("_Color", Color.white);
        if (sharedSparkleMaterial.HasProperty("_BaseColor")) sharedSparkleMaterial.SetColor("_BaseColor", Color.white);

        if (sharedSparkleMaterial.HasProperty("_Surface")) sharedSparkleMaterial.SetFloat("_Surface", 1);
        if (sharedSparkleMaterial.HasProperty("_Blend")) sharedSparkleMaterial.SetFloat("_Blend", 1);

        return sharedSparkleMaterial;
    }

    private void EnsureSparkleParticles()
    {
        if (sparkleParticles != null) return;

        Transform child = transform.Find("Sparkle_VFX");
        if (child != null)
        {
            sparkleParticles = child.GetComponent<ParticleSystem>();
        }
        else
        {
            GameObject pObj = new GameObject("Sparkle_VFX");
            pObj.transform.SetParent(transform, false);
            sparkleParticles = pObj.AddComponent<ParticleSystem>();
        }

        if (sparkleParticles == null) return;

        if (sparkleParticles.isPlaying)
        {
            sparkleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var renderer = sparkleParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetSharedSparkleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        var main = sparkleParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 2.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
        // Kích thước hạt rõ hơn 20% (0.12 - 0.20 thay vì 0.09 - 0.16)
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.20f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var shape = sparkleParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.9f, 0.7f, 0.25f);

        var velocityOverLifetime = sparkleParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        // Đảm bảo cả 3 trục x, y, z đều cùng chế độ TwoConstants để tránh lỗi Unity
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        // Nở ra rồi thu nhỏ nhẹ
        var sizeOverLifetime = sparkleParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.4f, 1f);
        sizeCurve.AddKey(0.8f, 0.65f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Alpha rõ nét hơn 20%
        var colorOverLifetime = sparkleParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1.0f, 0.25f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var emission = sparkleParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
    }

    public void EmitSparkleBurst(int count = 10)
    {
        EnsureSparkleParticles();
        if (sparkleParticles != null && gameObject.activeInHierarchy)
        {
            sparkleParticles.Emit(count);
        }
    }

    // ================= CẬP NHẬT CẤP ĐỘ & MÀU SẮC =================

    public static Color GetWallColorForLevel(int level)
    {
        switch (level)
        {
            case 1: return Color.white;
            case 2: return new Color(0.84f, 0.88f, 0.94f, 1f); // Bạc
            case 3: return new Color(1.0f, 0.80f, 0.16f, 1f);  // Vàng
            case 4: return new Color(0.93f, 0.96f, 1.0f, 1f);  // Bạch Kim
            case 5: return new Color(0.18f, 0.92f, 1.0f, 1f);  // Kim Cương
            case 6: return new Color(0.28f, 0.30f, 0.36f, 1f); // Đen Titan
            default: return Color.white;
        }
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, 6);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        EnsureSparkleParticles();

        Color wallColor = GetWallColorForLevel(currentLevel);
        float metallic = 0.5f;
        float smoothness = 0.5f;
        Color sparkleColor = Color.white;
        float sparkleRate = 0f; // CHỈ TỪ CẤP 4 TRỞ ĐI MỚI CÓ LẤP LÁNH

        switch (currentLevel)
        {
            case 1:
                metallic = 0.1f;
                smoothness = 0.3f;
                sparkleRate = 0f;
                break;

            case 2:
                metallic = 0.88f;
                smoothness = 0.8f;
                sparkleRate = 0f;
                break;

            case 3:
                metallic = 0.95f;
                smoothness = 0.88f;
                sparkleRate = 0f;
                break;

            case 4:
                metallic = 0.92f;
                smoothness = 0.92f;
                sparkleColor = new Color(0.96f, 0.98f, 1.0f, 0.95f);
                sparkleRate = 2.6f;
                break;

            case 5:
                metallic = 0.3f;
                smoothness = 0.98f;
                sparkleColor = new Color(0.35f, 0.96f, 1.0f, 1.0f);
                sparkleRate = 3.6f;
                break;

            case 6:
                metallic = 0.95f;
                smoothness = 0.90f;
                sparkleColor = new Color(0.85f, 0.60f, 1.0f, 0.95f);
                sparkleRate = 3.0f;
                break;
        }

        // Bề mặt tường: KHÔNG phát sáng rực cả khối (tránh lóa chói mắt), chỉ bóng bẩy kim loại cao cấp
        foreach (var rend in cachedRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);

            propBlock.SetColor("_BaseColor", wallColor);
            propBlock.SetColor("_Color", wallColor);
            propBlock.SetFloat("_Metallic", metallic);
            propBlock.SetFloat("_Smoothness", smoothness);

            // Emission tắt hoặc chỉ 1 chút xíu ở cấp Kim Cương để giữ màu ngọc tự nhiên
            if (currentLevel == 5)
            {
                propBlock.SetColor("_EmissionColor", new Color(0.02f, 0.08f, 0.1f, 1f));
            }
            else
            {
                propBlock.SetColor("_EmissionColor", Color.black);
            }

            rend.SetPropertyBlock(propBlock);
        }

        // Cập nhật hệ thống hạt lấp lánh (CHỈ C4 TRỞ ĐI MỚI CÓ, RÕ HƠN 20%)
        if (sparkleParticles != null)
        {
            var emission = sparkleParticles.emission;
            emission.rateOverTime = sparkleRate;

            var main = sparkleParticles.main;
            main.startColor = sparkleColor;

            if (sparkleRate > 0 && !sparkleParticles.isPlaying)
            {
                sparkleParticles.Play();
            }
            else if (sparkleRate <= 0 && sparkleParticles.isPlaying)
            {
                sparkleParticles.Stop();
                sparkleParticles.Clear();
            }
        }
    }

    public static void UpgradeAllWallsToLevel(int newLevel)
    {
        for (int i = AllWalls.Count - 1; i >= 0; i--)
        {
            if (AllWalls[i] != null)
            {
                AllWalls[i].ApplyLevel(newLevel);
            }
        }
    }

    // ================= NÂNG CẤP TỪNG ĐOẠN HOẶC CẢ HÀNG (CLASH OF CLANS STYLE) =================

    public int GetUpgradeCost()
    {
        // Tạm thời chỉnh lại coin nâng cấp về 0 hết theo yêu cầu
        return 0;
    }

    public bool UpgradeSingle()
    {
        if (currentLevel >= 6) return false;

        int cost = GetUpgradeCost();
        if (cost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(cost)))
        {
            ApplyLevel(currentLevel + 1);
            TriggerUpgradePunch();
            // Chỉ từ cấp 4 trở lên mới bắn hạt sao lấp lánh
            if (currentLevel >= 4)
            {
                EmitSparkleBurst(10);
            }
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để nâng cấp đoạn tường này!");
        }
        return false;
    }

    public void TriggerUpgradePunch(float delay = 0f)
    {
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PunchScaleCoroutine(delay));
        }
    }

    private IEnumerator PunchScaleCoroutine(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        Vector3 orig = baseScale;
        if (orig.x <= 1.05f && transform.localScale.x > 1.5f) orig = transform.localScale;
        if (orig == Vector3.one) orig = Vector3.one * 3f;

        float elapsed = 0f;
        float duration = 0.24f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
            transform.localScale = orig * bounce;
            yield return null;
        }

        transform.localScale = orig;
    }

    public List<WallSegment> GetConnectedRow(float maxDistance = 3.8f)
    {
        List<WallSegment> connected = new List<WallSegment>();
        Queue<WallSegment> queue = new Queue<WallSegment>();
        HashSet<WallSegment> visited = new HashSet<WallSegment>();

        queue.Enqueue(this);
        visited.Add(this);

        while (queue.Count > 0)
        {
            WallSegment cur = queue.Dequeue();
            connected.Add(cur);

            for (int i = 0; i < AllWalls.Count; i++)
            {
                WallSegment other = AllWalls[i];
                if (other == null || visited.Contains(other)) continue;

                float dist = Vector3.Distance(cur.transform.position, other.transform.position);
                if (dist <= maxDistance)
                {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        return connected;
    }

    public int CalculateRowUpgradeCost(List<WallSegment> row)
    {
        int total = 0;
        if (row == null) return 0;
        for (int i = 0; i < row.Count; i++)
        {
            if (row[i] != null && row[i].currentLevel < 6)
            {
                total += row[i].GetUpgradeCost();
            }
        }
        return total;
    }

    public bool UpgradeConnectedRow()
    {
        List<WallSegment> row = GetConnectedRow();
        List<WallSegment> upgradeable = new List<WallSegment>();
        for (int i = 0; i < row.Count; i++)
        {
            if (row[i] != null && row[i].currentLevel < 6)
            {
                upgradeable.Add(row[i]);
            }
        }

        if (upgradeable.Count == 0) return false;

        int totalCost = CalculateRowUpgradeCost(upgradeable);
        if (totalCost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(totalCost)))
        {
            for (int i = 0; i < upgradeable.Count; i++)
            {
                WallSegment wall = upgradeable[i];
                wall.ApplyLevel(wall.currentLevel + 1);
                wall.TriggerUpgradePunch(i * 0.04f);
                if (wall.currentLevel >= 4)
                {
                    wall.EmitSparkleBurst(8);
                }
            }
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins("Không đủ vàng để nâng cấp cả hàng tường!");
        }
        return false;
    }

    /// <summary>
    /// Xoay hướng đoạn tường 90 độ tại chỗ và nhấp nhô nhẹ phản hồi
    /// </summary>
    public void Rotate90Degrees()
    {
        transform.Rotate(0f, 90f, 0f, Space.World);
        baseLocalPosition = transform.localPosition;
        TriggerSelectHop(0f, 0.35f);
    }
}
