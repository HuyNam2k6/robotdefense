#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    [InitializeOnLoad]
    public static class GenerateTurretIcons
    {
        static GenerateTurretIcons()
        {
            EditorApplication.delayCall += EnsureTurretIcons;
        }

        [MenuItem("Tools/🎨 Tạo Bộ Icon Vũ Khí Phòng Thủ (Phaotuhanh, Thunder, Flamethrower)")]
        public static void EnsureTurretIcons()
        {
            WeaponIconSnapshotGenerator.GenerateAllSnapshots();
        }

        private static void CreateWeaponIcon(string dir, string fileName, int type)
        {
            string savePath = Path.Combine(dir, fileName).Replace("\\", "/");

            int size = 512;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color baseBg = new Color(0.06f, 0.09f, 0.16f, 1f);
            Color borderColor = type == 0 ? new Color(0.18f, 0.72f, 1.0f) :
                                type == 1 ? new Color(0.0f, 0.95f, 1.0f) :
                                type == 2 ? new Color(1.0f, 0.42f, 0.1f) :
                                            new Color(0.72f, 0.78f, 0.90f);

            Color coreColor = type == 0 ? new Color(1.0f, 0.78f, 0.18f) :   // Vàng cam tên lửa
                              type == 1 ? new Color(0.15f, 0.95f, 1.0f) :   // Sét cyan neon
                              type == 2 ? new Color(1.0f, 0.35f, 0.12f) :   // Lửa đỏ cam
                                          new Color(0.45f, 0.65f, 0.85f);   // Khiên thép tường

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float rOuter = size * 0.47f;
            float rInner = size * 0.44f;
            float rBg = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                float ny = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    if (d > rOuter + 1.5f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float outerAlpha = Mathf.Clamp01(rOuter + 1.5f - d);
                        Color pixelColor;

                        if (d > rInner)
                        {
                            // Vành viền kim loại sáng
                            float t = (d - rInner) / (rOuter - rInner);
                            pixelColor = Color.Lerp(borderColor * 0.8f, borderColor * 1.2f, t);
                        }
                        else if (d > rBg)
                        {
                            // Viền tối ngăn cách
                            pixelColor = new Color(0.02f, 0.04f, 0.08f, 1f);
                        }
                        else
                        {
                            // Background gradient công nghệ
                            pixelColor = Color.Lerp(baseBg * 0.7f, baseBg * 1.3f, ny);

                            // Lưới vi mô công nghệ
                            if (d < rBg * 0.85f && ((x % 32 == 0) || (y % 32 == 0)))
                            {
                                pixelColor = Color.Lerp(pixelColor, borderColor, 0.08f);
                            }

                            // Vẽ biểu tượng vũ khí sắc nét
                            bool isShape = false;
                            Color shapeColor = coreColor;

                            if (type == 0) // Pháo Tự Hành (MLRS Rocket Launcher)
                            {
                                // Bệ xoay kép
                                if ((nx >= 0.28f && nx <= 0.43f && ny >= 0.30f && ny <= 0.70f) ||
                                    (nx >= 0.57f && nx <= 0.72f && ny >= 0.30f && ny <= 0.70f) ||
                                    (nx >= 0.22f && nx <= 0.78f && ny >= 0.24f && ny <= 0.36f))
                                {
                                    isShape = true;
                                    shapeColor = coreColor;
                                }
                                // Mũi tên lửa nhọn
                                float leftDist = Mathf.Abs(nx - 0.355f);
                                float rightDist = Mathf.Abs(nx - 0.645f);
                                if (ny > 0.70f && ny <= 0.82f)
                                {
                                    float maxWidth = (0.82f - ny) * 0.6f;
                                    if (leftDist <= maxWidth || rightDist <= maxWidth)
                                    {
                                        isShape = true;
                                        shapeColor = new Color(1f, 0.45f, 0.15f); // Đầu nổ đỏ cam
                                    }
                                }
                            }
                            else if (type == 1) // Trụ Sét Thunder (Tesla Coil)
                            {
                                // Các vòng năng lượng điện từ trường
                                float coilD = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.5f));
                                if ((coilD >= 0.10f && coilD <= 0.13f) || (coilD >= 0.22f && coilD <= 0.26f))
                                {
                                    isShape = true;
                                    shapeColor = coreColor;
                                }
                                // Tháp trụ giữa
                                if (nx >= 0.45f && nx <= 0.55f && ny >= 0.20f && ny <= 0.75f)
                                {
                                    isShape = true;
                                    shapeColor = Color.white;
                                }
                                // Tia sét zig-zag sắc nét
                                float boltX1 = 0.5f + (0.55f - ny) * 0.5f;
                                float boltX2 = 0.5f - (ny - 0.45f) * 0.5f;
                                if ((ny >= 0.42f && ny <= 0.75f && Mathf.Abs(nx - boltX1) < 0.03f) ||
                                    (ny >= 0.22f && ny <= 0.52f && Mathf.Abs(nx - boltX2) < 0.03f))
                                {
                                    isShape = true;
                                    shapeColor = Color.white;
                                }
                            }
                            else if (type == 2) // Súng Phun Lửa Flamethrower
                            {
                                // Thân súng nòng thép
                                if ((nx >= 0.42f && nx <= 0.58f && ny >= 0.22f && ny <= 0.52f) ||
                                    (nx >= 0.36f && nx <= 0.64f && ny >= 0.50f && ny <= 0.62f))
                                {
                                    isShape = true;
                                    shapeColor = new Color(0.85f, 0.35f, 0.15f);
                                }
                                // Ngọn lửa bốc cháy
                                float fD = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.5f, 0.72f));
                                if (fD <= 0.13f && ny >= 0.60f)
                                {
                                    isShape = true;
                                    shapeColor = (fD < 0.07f) ? Color.yellow : new Color(1f, 0.25f, 0.05f);
                                }
                            }
                            else // Tường Phòng Thủ (Modular Wall)
                            {
                                // Hàng cọc tường rào chắn vững chắc
                                if (nx >= 0.22f && nx <= 0.78f && ny >= 0.32f && ny <= 0.68f)
                                {
                                    float seg = (nx - 0.22f) / (0.78f - 0.22f) * 5f;
                                    float frac = seg - Mathf.Floor(seg);
                                    if (frac > 0.18f && frac < 0.82f)
                                    {
                                        isShape = true;
                                        shapeColor = coreColor;
                                    }
                                }
                                // Thanh dầm ngang gia cố
                                if (nx >= 0.20f && nx <= 0.80f && (Mathf.Abs(ny - 0.40f) < 0.03f || Mathf.Abs(ny - 0.60f) < 0.03f))
                                {
                                    isShape = true;
                                    shapeColor = new Color(0.9f, 0.95f, 1f);
                                }
                            }

                            if (isShape) pixelColor = shapeColor;
                        }

                        pixelColor.a *= outerAlpha;
                        tex.SetPixel(x, y, pixelColor);
                    }
                }
            }

            tex.Apply();
            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(savePath, pngBytes);

            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(savePath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.maxTextureSize = 512;
                ti.SaveAndReimport();
            }
        }
    }
}
#endif
