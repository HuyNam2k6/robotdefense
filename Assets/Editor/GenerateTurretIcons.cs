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
            string outDir = "Assets/UI/WeaponIcons";
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            CreateWeaponIcon(outDir, "Icon_Phaotuhanh.png", 0);     // Pháo tự hành tên lửa (Xanh dương / Cam)
            CreateWeaponIcon(outDir, "Icon_Thunder.png", 1);        // Trụ sét Thunder (Cyan Neon / Điện)
            CreateWeaponIcon(outDir, "Icon_Flamethrower.png", 2);   // Trụ phun lửa (Đỏ / Cam lửa)
            CreateWeaponIcon(outDir, "Icon_Wall.png", 3);           // Bức tường (Xám thép / Vàng)

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateWeaponIcon(string dir, string fileName, int type)
        {
            string savePath = Path.Combine(dir, fileName).Replace("\\", "/");

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseBg = new Color(0.06f, 0.09f, 0.15f, 0.98f);
            Color borderColor = type == 0 ? new Color(0.2f, 0.7f, 1.0f) :
                                type == 1 ? new Color(0.0f, 0.95f, 1.0f) :
                                type == 2 ? new Color(1.0f, 0.45f, 0.1f) :
                                            new Color(0.7f, 0.75f, 0.85f);

            Color coreColor = type == 0 ? new Color(1.0f, 0.75f, 0.2f) :   // Tên lửa vàng cam
                              type == 1 ? new Color(0.2f, 0.95f, 1.0f) :   // Sét cyan sáng
                              type == 2 ? new Color(1.0f, 0.3f, 0.1f) :    // Lửa đỏ rực
                                          new Color(0.4f, 0.6f, 0.8f);

            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    // Bo tròn góc Cyber
                    if (d > size * 0.48f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (d > size * 0.45f)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                    else if (d > size * 0.43f)
                    {
                        tex.SetPixel(x, y, new Color(0.02f, 0.04f, 0.08f, 1f));
                    }
                    else
                    {
                        // Background gradient công nghệ
                        float grad = (float)y / size;
                        Color c = Color.Lerp(baseBg, baseBg * 1.4f, grad);

                        // Vẽ hình đặc trưng cho từng loại vũ khí
                        bool isShape = false;
                        if (type == 0) // Pháo Tự Hành: Bệ phóng đôi + đạn tên lửa
                        {
                            // Khung bệ phóng kép
                            if ((x >= 70 && x <= 110 && y >= 75 && y <= 180) ||
                                (x >= 146 && x <= 186 && y >= 75 && y <= 180) ||
                                (x >= 60 && x <= 196 && y >= 65 && y <= 95))
                            {
                                isShape = true;
                            }
                            // Đầu tên lửa nhọn
                            if ((y > 180 && y <= 210) && (Mathf.Abs(x - 90) <= (210 - y) * 0.6f || Mathf.Abs(x - 166) <= (210 - y) * 0.6f))
                            {
                                isShape = true;
                            }
                        }
                        else if (type == 1) // Trụ Sét: Cuộn Tesla coil + Tia sét chéo
                        {
                            // Trụ tháp Tesla tròn nhiều tầng
                            if (d < 45f || (d < 75f && d > 60f) || (x >= 115 && x <= 141 && y >= 50 && y <= 190))
                            {
                                isShape = true;
                            }
                            // Biểu tượng tia chớp zig-zag
                            if ((Mathf.Abs(x + y * 0.5f - 180) < 14 && y > 100 && y < 190) ||
                                (Mathf.Abs(x - y * 0.6f - 30) < 14 && y > 60 && y < 140))
                            {
                                isShape = true;
                            }
                        }
                        else if (type == 2) // Phun Lửa: Nòng súng kép hình trụ + ngọn lửa
                        {
                            // Bệ súng tròn vát
                            if ((x >= 95 && x <= 161 && y >= 60 && y <= 145) ||
                                (x >= 110 && x <= 146 && y >= 140 && y <= 185))
                            {
                                isShape = true;
                            }
                            // Ngọn lửa bùng phát phía trên
                            float fireDist = Vector2.Distance(new Vector2(x, y), new Vector2(128, 195));
                            if (fireDist < 35f && y >= 180)
                            {
                                isShape = true;
                            }
                        }
                        else // Tường thành chắn
                        {
                            if ((x >= 50 && x <= 206 && y >= 70 && y <= 170) && (x % 38 > 6))
                            {
                                isShape = true;
                            }
                        }

                        if (isShape)
                        {
                            tex.SetPixel(x, y, coreColor);
                        }
                        else
                        {
                            tex.SetPixel(x, y, c);
                        }
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
                ti.SaveAndReimport();
            }
        }
    }
}
#endif
