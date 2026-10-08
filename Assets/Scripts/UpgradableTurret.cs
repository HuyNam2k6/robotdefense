using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TurretType
{
    Thunder,            // Sấm Sét / Lightning Turret
    Phaotuhanh,         // Pháo Tự Hành MLRS / Rocket Launcher Turret
    FlamethrowerTurret  // Pháo Lửa / Flamethrower Turret
}

[DisallowMultipleComponent]
public class UpgradableTurret : MonoBehaviour
{
    public static readonly List<UpgradableTurret> AllTurrets = new List<UpgradableTurret>();

    [Header("--- Cấu Hình Trụ ---")]
    public TurretType turretType;
    public string turretDisplayName = "Trụ Phòng Thủ";

    [Header("--- Cấp Độ Hiện Tại (1 - 6) ---")]
    [Range(1, 6)]
    public int currentLevel = 1;
    public const int MaxLevel = 6;

    public static readonly string[] TurretLevelNames = new string[]
    {
        "Cơ Bản (Gốc)",
        "Bạc (Silver)",
        "Vàng (Gold)",
        "Bạch Kim (Platinum)",
        "Kim Cương (Diamond)",
        "Titan - MAX (Tối Đa)"
    };

    [Header("--- Chi Phí Nâng Cấp Từng Cấp (1->2, 2->3, 3->4, 4->5, 5->6) ---")]
    public int[] upgradeCosts = new int[] { 60, 120, 220, 380, 580 };

    [Header("--- Tham Chiếu Component ---")]
    public LightningTurret lightningTurret;
    public RocketLauncherTurret rocketLauncher;
    public TurretController turretController;
    public TurretWeapon turretWeapon;

    [Header("Máu Trụ Phòng Thủ (Turret Health)")]
    public float maxHealth = 250f;
    public float currentHealth = 250f;
    public bool isDestroyed = false;

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;
        currentHealth -= amount;
        FloatingDamageText.Spawn(transform.position + Vector3.up * 2.2f, amount, new Color(1f, 0.5f, 0.2f));
        if (currentHealth <= 0f)
        {
            isDestroyed = true;
            AllTurrets.Remove(this);
            Destroy(gameObject);
        }
    }

    // Quản lý Renderers, Vật Liệu Gốc và Vật Liệu Cấp Độ
    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private Vector3 baseScale = Vector3.zero;
    private bool isSelected = false;
    private Coroutine punchCoroutine;

    // Hệ thống hạt lấp lánh (Sparkle Particles từ Cấp 4 trở lên)
    private ParticleSystem sparkleParticles;

    private struct RendererMaterialBackup
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Color originalBaseColor;
    }
    private readonly List<RendererMaterialBackup> materialBackups = new List<RendererMaterialBackup>();

    // Cache vật liệu chất lượng cao cho từng cấp độ (2 -> 6)
    private static readonly Material[] cachedLevelMaterials = new Material[7];

    void OnEnable()
    {
        if (!AllTurrets.Contains(this)) AllTurrets.Add(this);
    }

    void OnDisable()
    {
        AllTurrets.Remove(this);
    }

    void Awake()
    {
        baseScale = transform.localScale;
        propBlock = new MaterialPropertyBlock();

        DetectAndInitTurretComponents();
        EnsureColliderExists();
        CacheRenderersAndMaterials();
    }

    void Start()
    {
        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        EnsureSparkleParticles();
        ApplyLevel(currentLevel);
    }

    void Update()
    {
        // Giữ Update sạch sẽ: TUYỆT ĐỐI KHÔNG ghi đè màu neon xanh lên thân súng mỗi frame.
        // Vỏ vũ khí phải luôn hiển thị 100% màu sắc và chất liệu kim loại rực rỡ của cấp độ hiện tại!
    }

    /// <summary>
    /// Bật hoặc tắt trạng thái chọn trụ
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selected)
        {
            TriggerPunchHop();
        }
        else
        {
            ApplyTurretVisuals(currentLevel);
        }
    }

    public void ResetVisualsToOriginal()
    {
        ApplyTurretVisuals(currentLevel);
    }

    private void DetectAndInitTurretComponents()
    {
        string n = gameObject.name.ToLower();

        lightningTurret = GetComponent<LightningTurret>() ?? GetComponentInChildren<LightningTurret>();
        rocketLauncher = GetComponent<RocketLauncherTurret>() ?? GetComponentInChildren<RocketLauncherTurret>();
        turretController = GetComponent<TurretController>() ?? GetComponentInChildren<TurretController>();
        turretWeapon = GetComponent<TurretWeapon>() ?? GetComponentInChildren<TurretWeapon>();

        if (lightningTurret != null || n.Contains("thunder") || n.Contains("lightn") || n.Contains("emp"))
        {
            turretType = TurretType.Thunder;
            turretDisplayName = "⚡ THUNDER (SẤM SÉT)";
            upgradeCosts = new int[] { 60, 120, 220, 380, 580 };
            if (lightningTurret == null) lightningTurret = gameObject.AddComponent<LightningTurret>();
        }
        else if (rocketLauncher != null || n.Contains("phao") || n.Contains("missile") || n.Contains("rocket"))
        {
            turretType = TurretType.Phaotuhanh;
            turretDisplayName = "🚀 PHÁO TỰ HÀNH (MLRS)";
            upgradeCosts = new int[] { 80, 160, 280, 450, 700 };
            if (rocketLauncher == null) rocketLauncher = gameObject.AddComponent<RocketLauncherTurret>();
        }
        else if (turretController != null || n.Contains("flame") || n.Contains("turret"))
        {
            turretType = TurretType.FlamethrowerTurret;
            turretDisplayName = "🔥 FLAMETHROWER TURRET";
            upgradeCosts = new int[] { 50, 110, 200, 350, 550 };
            if (turretController == null) turretController = gameObject.AddComponent<TurretController>();
            if (turretWeapon == null) turretWeapon = GetComponentInChildren<TurretWeapon>();
        }
    }

    private void CacheRenderersAndMaterials()
    {
        materialBackups.Clear();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in cachedRenderers)
        {
            if (r == null || r is LineRenderer || r is ParticleSystemRenderer) continue;

            RendererMaterialBackup backup = new RendererMaterialBackup();
            backup.renderer = r;
            backup.originalMaterials = r.sharedMaterials;

            if (r.sharedMaterial != null)
            {
                backup.originalBaseColor = r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") :
                                           (r.sharedMaterial.HasProperty("_Color") ? r.sharedMaterial.GetColor("_Color") : Color.white);
            }
            else
            {
                backup.originalBaseColor = Color.white;
            }

            materialBackups.Add(backup);
        }
    }

    // ================= HIỆU ỨNG HẠT LẤP LÁNH (SPARKLE PARTICLES TỪ CẤP 4 TRỞ LÊN) =================

    private void EnsureSparkleParticles()
    {
        if (sparkleParticles != null) return;

        Transform child = transform.Find("Turret_Sparkle_VFX");
        if (child != null)
        {
            sparkleParticles = child.GetComponent<ParticleSystem>();
        }
        else
        {
            GameObject pObj = new GameObject("Turret_Sparkle_VFX");
            pObj.transform.SetParent(transform, false);
            sparkleParticles = pObj.AddComponent<ParticleSystem>();
        }

        if (sparkleParticles == null) return;

        if (sparkleParticles.isPlaying)
        {
            sparkleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var renderer = sparkleParticles.GetComponent<ParticleSystemRenderer>();
        renderer.material = WallSegment.GetSharedSparkleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;

        var main = sparkleParticles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 2.0f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.26f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        var shape = sparkleParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.2f, 2.4f, 2.2f);
        shape.position = new Vector3(0f, 1.3f, 0f);

        var velocityOverLifetime = sparkleParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var sizeOverLifetime = sparkleParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0f);
        sizeCurve.AddKey(0.4f, 1f);
        sizeCurve.AddKey(0.8f, 0.65f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

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

    public void EmitSparkleBurst(int count = 16)
    {
        EnsureSparkleParticles();
        if (sparkleParticles != null && gameObject.activeInHierarchy)
        {
            sparkleParticles.Emit(count);
        }
    }

    // ================= MÀU SẮC & CHẤT LIỆU THEO CẤP ĐỘ (1 - 6) =================

    public static Color GetTurretColorForLevel(int level)
    {
        switch (level)
        {
            case 1: return Color.white;
            case 2: return new Color(0.88f, 0.90f, 0.96f, 1f); // Bạc sáng ánh kim
            case 3: return new Color(1.0f, 0.82f, 0.12f, 1f);  // Vàng hoàng gia rực rỡ
            case 4: return new Color(0.94f, 0.97f, 1.0f, 1f);  // Bạch Kim trắng ngọc
            case 5: return new Color(0.15f, 0.94f, 1.0f, 1f);  // Kim Cương pha lê phát quang
            case 6: return new Color(0.25f, 0.26f, 0.35f, 1f); // Đen Titan vi mạch tím
            default: return Color.white;
        }
    }

    public static Material GetTurretMaterialForLevel(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        if (level == 1) return null; // Cấp 1 dùng material gốc của prefab

        if (cachedLevelMaterials[level] != null) return cachedLevelMaterials[level];

        Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Mobile/Diffuse");
        Material mat = new Material(s);
        mat.name = $"Mat_Turret_Level_{level}";

        Color baseCol = Color.white;
        float metallic = 0.5f;
        float smoothness = 0.5f;
        Color emissionCol = Color.black;

        switch (level)
        {
            case 2: // Bạc (Silver Metallic)
                baseCol = new Color(0.88f, 0.90f, 0.96f, 1f);
                metallic = 0.94f;
                smoothness = 0.88f;
                emissionCol = new Color(0.04f, 0.05f, 0.08f, 1f);
                break;

            case 3: // Vàng (Gold Metallic)
                baseCol = new Color(1.0f, 0.82f, 0.12f, 1f);
                metallic = 0.96f;
                smoothness = 0.90f;
                emissionCol = new Color(0.20f, 0.14f, 0.02f, 1f);
                break;

            case 4: // Bạch Kim (Platinum)
                baseCol = new Color(0.94f, 0.97f, 1.0f, 1f);
                metallic = 0.92f;
                smoothness = 0.95f;
                emissionCol = new Color(0.12f, 0.16f, 0.22f, 1f);
                break;

            case 5: // Kim Cương (Diamond Crystal)
                baseCol = new Color(0.15f, 0.94f, 1.0f, 1f);
                metallic = 0.25f;
                smoothness = 0.98f;
                emissionCol = new Color(0.12f, 0.42f, 0.58f, 1f);
                break;

            case 6: // Titan (Titanium Dark Violet-Blue)
                baseCol = new Color(0.25f, 0.26f, 0.35f, 1f);
                metallic = 0.96f;
                smoothness = 0.92f;
                emissionCol = new Color(0.22f, 0.10f, 0.38f, 1f);
                break;
        }

        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

        if (emissionCol != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionCol);
        }

        cachedLevelMaterials[level] = mat;
        return mat;
    }

    public void ApplyTurretVisuals(int level)
    {
        if (materialBackups == null || materialBackups.Count == 0)
        {
            CacheRenderersAndMaterials();
        }
        if (materialBackups == null || materialBackups.Count == 0) return;
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        EnsureSparkleParticles();

        Color turretColor = GetTurretColorForLevel(level);
        Material levelMat = GetTurretMaterialForLevel(level);

        Color sparkleColor = Color.white;
        float sparkleRate = 0f;

        switch (level)
        {
            case 1:
            case 2:
            case 3:
                sparkleRate = 0f;
                break;
            case 4:
                sparkleColor = new Color(0.96f, 0.98f, 1.0f, 0.95f);
                sparkleRate = 3.2f;
                break;
            case 5:
                sparkleColor = new Color(0.35f, 0.96f, 1.0f, 1.0f);
                sparkleRate = 4.2f;
                break;
            case 6:
                sparkleColor = new Color(0.85f, 0.60f, 1.0f, 0.95f);
                sparkleRate = 3.6f;
                break;
        }

        for (int i = 0; i < materialBackups.Count; i++)
        {
            var b = materialBackups[i];
            if (b.renderer == null) continue;

            if (level == 1)
            {
                // Khôi phục 100% vật liệu nguyên bản của prefab
                if (b.originalMaterials != null && b.originalMaterials.Length > 0)
                {
                    b.renderer.sharedMaterials = b.originalMaterials;
                }
                b.renderer.SetPropertyBlock(null);
            }
            else
            {
                // Gán vật liệu ánh kim cao cấp theo cấp độ
                if (levelMat != null)
                {
                    int matCount = (b.originalMaterials != null && b.originalMaterials.Length > 0) ? b.originalMaterials.Length : 1;
                    Material[] newMats = new Material[matCount];
                    for (int m = 0; m < matCount; m++) newMats[m] = levelMat;
                    b.renderer.sharedMaterials = newMats;
                }

                // Đồng thời áp dụng PropertyBlock để đảm bảo hiển thị đồng bộ hoàn hảo
                b.renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_BaseColor", turretColor);
                propBlock.SetColor("_Color", turretColor);
                if (level == 5)
                {
                    propBlock.SetColor("_EmissionColor", new Color(0.08f, 0.35f, 0.45f, 1f));
                }
                else if (level == 6)
                {
                    propBlock.SetColor("_EmissionColor", new Color(0.18f, 0.10f, 0.30f, 1f));
                }
                else if (level == 3)
                {
                    propBlock.SetColor("_EmissionColor", new Color(0.15f, 0.12f, 0.02f, 1f));
                }
                else
                {
                    propBlock.SetColor("_EmissionColor", Color.black);
                }
                b.renderer.SetPropertyBlock(propBlock);
            }
        }

        // Cập nhật hệ thống hạt lấp lánh (CHỈ CẤP 4 TRỞ LÊN MỚI CÓ)
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

    /// <summary>
    /// Đảm bảo trụ luôn có Collider để người chơi click raycast chọn được
    /// </summary>
    public void EnsureColliderExists()
    {
        // 1. Chuẩn hóa đặc biệt cho FlamethrowerTurret: Tránh BoxCollider bị quá khổ (11m) chắn đường và lơ lửng trên mặt đất
        if (turretType == TurretType.FlamethrowerTurret || name.ToLower().Contains("flame"))
        {
            BoxCollider flameCol = GetComponent<BoxCollider>();
            if (flameCol == null) flameCol = gameObject.AddComponent<BoxCollider>();

            // Tỉ lệ scale thực tế: localScale {1.27, 1.19, 1.95}
            // Size mới: X=2.0 (World 2.54m), Y=2.4 (World 2.86m), Z=1.4 (World 2.73m)
            flameCol.size = new Vector3(2.0f, 2.4f, 1.4f);
            flameCol.center = new Vector3(0f, 1.15f, 0.1f);
            return;
        }

        Collider existingCol = GetComponentInChildren<Collider>();
        if (existingCol == null)
        {
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            Renderer[] rends = GetComponentsInChildren<Renderer>();
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    if (rends[i] is LineRenderer || rends[i] is ParticleSystemRenderer) continue;
                    b.Encapsulate(rends[i].bounds);
                }

                col.center = transform.InverseTransformPoint(b.center);
                Vector3 worldSize = b.size;
                col.size = new Vector3(
                    Mathf.Max(1.6f, worldSize.x / Mathf.Max(0.01f, transform.lossyScale.x)),
                    Mathf.Max(2.2f, worldSize.y / Mathf.Max(0.01f, transform.lossyScale.y)),
                    Mathf.Max(1.6f, worldSize.z / Mathf.Max(0.01f, transform.lossyScale.z))
                );
            }
            else
            {
                col.center = new Vector3(0f, 1.5f, 0f);
                col.size = new Vector3(2.5f, 3.2f, 2.5f);
            }
        }
    }

    public int GetUpgradeCost()
    {
        // Tạm thời chỉnh lại coin nâng cấp về 0 hết theo yêu cầu
        return 0;
    }

    /// <summary>
    /// Nâng cấp trụ lên cấp kế tiếp
    /// </summary>
    public bool Upgrade()
    {
        if (currentLevel >= MaxLevel) return false;

        int cost = GetUpgradeCost();
        if (cost <= 0 || (GameEconomy.Instance != null && GameEconomy.Instance.SpendCoins(cost)))
        {
            currentLevel++;
            ApplyLevel(currentLevel);
            TriggerPunchHop();
            if (currentLevel >= 4)
            {
                EmitSparkleBurst(14);
            }
            Debug.Log($"<color=#00FFFF>[Nâng Cấp]</color> Đã nâng cấp thành công {turretDisplayName} lên Cấp {currentLevel}: {TurretLevelNames[currentLevel - 1]}!");
            return true;
        }
        else if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.TriggerNotEnoughCoins($"Không đủ vàng để nâng cấp {turretDisplayName}!");
        }

        return false;
    }

    /// <summary>
    /// Xoay hướng Ụ Pháo 90 độ tại chỗ
    /// </summary>
    public void Rotate90Degrees()
    {
        transform.Rotate(0f, 90f, 0f, Space.World);
        TriggerPunchHop();
    }

    public void ApplyLevel(int level)
    {
        currentLevel = Mathf.Clamp(level, 1, MaxLevel);

        switch (turretType)
        {
            case TurretType.Thunder:
                if (lightningTurret != null)
                {
                    // Cấp 1 -> 6: 15, 38, 61, 84, 107, 130 DPS
                    lightningTurret.damagePerSecond = 15f + (currentLevel - 1) * 23f;
                    // Tầm bắn: 15m, 17.5m, 20m, 22.5m, 25m, 27.5m
                    lightningTurret.attackRange = 15f + (currentLevel - 1) * 2.5f;
                    lightningTurret.lightningWidth = 0.2f + (currentLevel - 1) * 0.05f;
                }
                break;

            case TurretType.Phaotuhanh:
                if (rocketLauncher != null)
                {
                    // Cấp 1 -> 6: 16, 22, 28, 34, 40, 48 quả
                    rocketLauncher.rocketsPerVolley = 16 + (currentLevel - 1) * (currentLevel == 6 ? 8 : 6);
                    // Nạp: 2.0s -> 0.75s
                    rocketLauncher.fireCooldown = Mathf.Max(0.75f, 2.0f - (currentLevel - 1) * 0.25f);
                    // Tầm: 30m, 34m, 38m, 42m, 46m, 50m
                    rocketLauncher.attackRange = 30f + (currentLevel - 1) * 4f;
                    
                    var field = rocketLauncher.GetType().GetField("rocketDamage");
                    if (field != null)
                    {
                        field.SetValue(rocketLauncher, 25f + (currentLevel - 1) * 15f);
                    }
                }
                break;

            case TurretType.FlamethrowerTurret:
                if (turretController != null)
                {
                    // Tầm: 25m, 29m, 33m, 37m, 41m, 45m
                    turretController.attackRange = 25f + (currentLevel - 1) * 4f;
                }
                if (turretWeapon != null)
                {
                    // Tốc độ bắn: 0.65s -> 0.18s
                    turretWeapon.cooldown = Mathf.Max(0.18f, 0.65f - (currentLevel - 1) * 0.094f);
                    // Sát thương: 15, 37, 59, 81, 103, 125
                    turretWeapon.bulletDamage = 15 + (currentLevel - 1) * 22;
                }
                break;
        }

        ApplyTurretVisuals(currentLevel);
    }

    public string GetStatsDescription()
    {
        switch (turretType)
        {
            case TurretType.Thunder:
                float curDps = 15f + (currentLevel - 1) * 23f;
                float curRange = 15f + (currentLevel - 1) * 2.5f;
                if (currentLevel < MaxLevel)
                {
                    float nextDps = 15f + currentLevel * 23f;
                    float nextRange = 15f + currentLevel * 2.5f;
                    return $"⚡ DPS: <b>{curDps}</b> ➔ <color=#00FFFF><b>{nextDps}</b></color>  |  Tầm: <b>{curRange}m</b> ➔ <color=#00FFFF><b>{nextRange}m</b></color>";
                }
                return $"⚡ DPS: <b>{curDps}</b> (TỐI ĐA)  |  Tầm Bắn: <b>{curRange}m</b>";

            case TurretType.Phaotuhanh:
                int curRockets = 16 + (currentLevel - 1) * (currentLevel == 6 ? 8 : 6);
                float curCd = Mathf.Max(0.75f, 2.0f - (currentLevel - 1) * 0.25f);
                if (currentLevel < MaxLevel)
                {
                    int nextRockets = 16 + currentLevel * (currentLevel + 1 == 6 ? 8 : 6);
                    float nextCd = Mathf.Max(0.75f, 2.0f - currentLevel * 0.25f);
                    return $"🚀 Loạt: <b>{curRockets} quả</b> ➔ <color=#00FFFF><b>{nextRockets} quả</b></color>  |  Nạp: <b>{curCd:F1}s</b> ➔ <color=#00FFFF><b>{nextCd:F1}s</b></color>";
                }
                return $"🚀 Loạt Bắn: <b>{curRockets} quả</b> (TỐI ĐA)  |  Nạp: <b>{curCd:F1}s</b>";

            case TurretType.FlamethrowerTurret:
                int curDmg = 15 + (currentLevel - 1) * 22;
                float curFireCd = Mathf.Max(0.18f, 0.65f - (currentLevel - 1) * 0.094f);
                if (currentLevel < MaxLevel)
                {
                    int nextDmg = 15 + currentLevel * 22;
                    float nextFireCd = Mathf.Max(0.18f, 0.65f - currentLevel * 0.094f);
                    return $"🔥 Sát Thương: <b>{curDmg}</b> ➔ <color=#00FFFF><b>{nextDmg}</b></color>  |  Tốc Độ: <b>{curFireCd:F2}s</b> ➔ <color=#00FFFF><b>{nextFireCd:F2}s</b></color>";
                }
                return $"🔥 Sát Thương: <b>{curDmg}</b> (TỐI ĐA)  |  Tốc Độ: <b>{curFireCd:F2}s</b>";

            default:
                return $"Cấp độ: {currentLevel}";
        }
    }

    public Vector3 GetMarkerPosition()
    {
        Collider c = GetComponentInChildren<Collider>();
        if (c != null)
        {
            return new Vector3(c.bounds.center.x, c.bounds.max.y + 0.6f, c.bounds.center.z);
        }
        return transform.position + Vector3.up * 2.8f;
    }

    // Cache các bộ phận con hiển thị để chỉ tạo hiệu ứng nhún nhảy đàn hồi trên đồ họa,
    // TUYỆT ĐỐI KHÔNG scale Root Transform để tránh làm BoxCollider vật lý phình to đè hất tung Player và Robot!
    private struct TransformScaleBackup
    {
        public Transform transform;
        public Vector3 originalLocalScale;
    }
    private readonly List<TransformScaleBackup> visualChildBackups = new List<TransformScaleBackup>();

    private void CacheVisualChildren()
    {
        visualChildBackups.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<ParticleSystem>() != null) continue;
            if (child.name.Contains("Sparkle") || child.name.Contains("Selection") || child.name.Contains("Marker") || child.name.Contains("VFX")) continue;

            if (child.GetComponentInChildren<Renderer>() != null)
            {
                visualChildBackups.Add(new TransformScaleBackup
                {
                    transform = child,
                    originalLocalScale = child.localScale
                });
            }
        }
    }

    public void TriggerPunchHop()
    {
        if (gameObject.activeInHierarchy)
        {
            if (punchCoroutine != null) StopCoroutine(punchCoroutine);
            punchCoroutine = StartCoroutine(DoPunchHop());
        }
    }

    private IEnumerator DoPunchHop()
    {
        if (visualChildBackups.Count == 0)
        {
            CacheVisualChildren();
        }

        float elapsed = 0f;
        float duration = 0.22f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;

            if (visualChildBackups.Count > 0)
            {
                for (int i = 0; i < visualChildBackups.Count; i++)
                {
                    var b = visualChildBackups[i];
                    if (b.transform != null)
                    {
                        b.transform.localScale = b.originalLocalScale * bounce;
                    }
                }
            }
            yield return null;
        }

        // Khôi phục kích thước hiển thị ban đầu
        for (int i = 0; i < visualChildBackups.Count; i++)
        {
            var b = visualChildBackups[i];
            if (b.transform != null)
            {
                b.transform.localScale = b.originalLocalScale;
            }
        }

        punchCoroutine = null;
    }

    // ================= TỰ ĐỘNG GẮN COMPONENT KHI CHẠY SCENE =================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void AutoSetupAllTurretsInScene()
    {
        // 1. Quét tìm theo script
        LightningTurret[] lTurrets = Object.FindObjectsByType<LightningTurret>(FindObjectsInactive.Include);
        foreach (var lt in lTurrets)
        {
            if (lt.GetComponent<UpgradableTurret>() == null)
            {
                lt.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        RocketLauncherTurret[] rTurrets = Object.FindObjectsByType<RocketLauncherTurret>(FindObjectsInactive.Include);
        foreach (var rt in rTurrets)
        {
            if (rt.GetComponent<UpgradableTurret>() == null)
            {
                rt.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        TurretController[] tControllers = Object.FindObjectsByType<TurretController>(FindObjectsInactive.Include);
        foreach (var tc in tControllers)
        {
            if (tc.GetComponent<UpgradableTurret>() == null)
            {
                tc.gameObject.AddComponent<UpgradableTurret>();
            }
        }

        // Dọn dẹp: Gỡ bỏ UpgradableTurret nếu bị gắn nhầm trên các bộ phận con (child parts)
        UpgradableTurret[] allTurrets = Object.FindObjectsByType<UpgradableTurret>(FindObjectsInactive.Include);
        foreach (var ut in allTurrets)
        {
            if (ut.transform.parent != null && ut.transform.parent.GetComponentInParent<UpgradableTurret>() != null)
            {
                Object.DestroyImmediate(ut);
            }
        }

        // 2. Quét tìm theo tên đối tượng cụ thể theo yêu cầu: thunder, phaotuhanh, FlamethrowerTurret (CHỈ GẮN TRÊN GỐC ROOT)
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var obj in allObjects)
        {
            // Bỏ qua object con nếu đối tượng cha đã có hoặc thuộc về một Turret
            if (obj.transform.parent != null && (obj.transform.parent.GetComponentInParent<UpgradableTurret>() != null || obj.transform.parent.GetComponentInParent<TurretController>() != null))
                continue;

            string n = obj.name.ToLower();
            if (n == "thunder" || n.StartsWith("thunder") || 
                n == "phaotuhanhl" || n.StartsWith("phaotu") || 
                n == "flamethrowerturret" || n.StartsWith("flamethrowerturret") || n == "flamethrowerturret_building")
            {
                if (obj.GetComponent<UpgradableTurret>() == null)
                {
                    obj.AddComponent<UpgradableTurret>();
                }
            }
        }
    }
}

// Trigger refresh
