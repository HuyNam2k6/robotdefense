using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WallSegment : MonoBehaviour
{
    public static readonly List<WallSegment> AllWalls = new List<WallSegment>();

    [Header("Cấp Độ Tường (1 - 6)")]
    [Range(1, 6)]
    public int currentLevel = 1;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;

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
    }

    void Start()
    {
        if (GameEconomy.Instance != null)
        {
            ApplyLevel(GameEconomy.Instance.wallLevel);
        }
        else
        {
            ApplyLevel(currentLevel);
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

        // Định nghĩa 6 màu sắc theo yêu cầu:
        // Cấp 1: Mặc định (Vật liệu gốc ban đầu)
        // Cấp 2: Bạc hơn (Silver)
        // Cấp 3: Màu Vàng (Gold)
        // Cấp 4: Màu Bạch Kim (Platinum)
        // Cấp 5: Màu như Kim Cương (Diamond Cyan Crystal)
        // Cấp 6: Đen Titan (Titanium Black)
        Color wallColor;
        float metallic = 0.5f;
        float smoothness = 0.5f;
        Color emissionColor = Color.black;

        switch (currentLevel)
        {
            case 1:
                wallColor = Color.white; // Nguyên bản
                metallic = 0.1f;
                smoothness = 0.3f;
                break;

            case 2:
                // Cấp 2: Bạc hơn
                wallColor = new Color(0.82f, 0.86f, 0.94f, 1f);
                metallic = 0.85f;
                smoothness = 0.75f;
                break;

            case 3:
                // Cấp 3: Màu Vàng (Gold)
                wallColor = new Color(1.0f, 0.78f, 0.12f, 1f);
                metallic = 0.95f;
                smoothness = 0.8f;
                break;

            case 4:
                // Cấp 4: Màu Bạch Kim (Platinum)
                wallColor = new Color(0.94f, 0.97f, 1.0f, 1f);
                metallic = 0.92f;
                smoothness = 0.9f;
                break;

            case 5:
                // Cấp 5: Màu như Kim Cương (Diamond Gemstone)
                wallColor = new Color(0.15f, 0.92f, 1.0f, 1f);
                metallic = 0.25f;
                smoothness = 0.98f;
                emissionColor = new Color(0.05f, 0.35f, 0.45f, 1f);
                break;

            case 6:
                // Cấp 6: Đen Titan (Titanium Black)
                wallColor = new Color(0.10f, 0.11f, 0.14f, 1f);
                metallic = 0.98f;
                smoothness = 0.85f;
                break;

            default:
                wallColor = Color.white;
                break;
        }

        foreach (var rend in cachedRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);

            // Gán màu cho Shader URP hoặc Standard
            propBlock.SetColor("_BaseColor", wallColor);
            propBlock.SetColor("_Color", wallColor);
            propBlock.SetFloat("_Metallic", metallic);
            propBlock.SetFloat("_Smoothness", smoothness);

            if (currentLevel == 5)
            {
                propBlock.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                propBlock.SetColor("_EmissionColor", Color.black);
            }

            rend.SetPropertyBlock(propBlock);
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
}

