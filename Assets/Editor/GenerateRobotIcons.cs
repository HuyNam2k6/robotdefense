#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    public static class GenerateRobotIcons
    {
        [MenuItem("Tools/🎨 Tạo 4 Icon Robot Từ Model 3D")]
        public static void GenerateIcons()
        {
            string outDir = "Assets/UI/RobotIcons";
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
                AssetDatabase.Refresh();
            }

            string[] models = new string[]
            {
                "Assets/Models/NewMech/Stan.fbx",
                "Assets/Models/NewMech/Mike.fbx",
                "Assets/Models/NewMech/George.fbx",
                "Assets/Models/NewMech/Leela.fbx"
            };

            string[] iconNames = new string[]
            {
                "Icon_Stan.png",
                "Icon_Mike.png",
                "Icon_George.png",
                "Icon_Leela.png"
            };

            for (int i = 0; i < models.Length; i++)
            {
                GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(models[i]);
                string savePath = Path.Combine(outDir, iconNames[i]).Replace("\\", "/");

                if (fbx != null)
                {
                    Texture2D preview = AssetPreview.GetAssetPreview(fbx);
                    int attempts = 0;
                    while (preview == null && attempts < 20)
                    {
                        System.Threading.Thread.Sleep(30);
                        preview = AssetPreview.GetAssetPreview(fbx);
                        attempts++;
                    }

                    if (preview == null)
                    {
                        // Nếu AssetPreview chưa load kịp, tạo icon dạng render phong cách Sci-Fi
                        preview = CreateFallbackRobotIcon(i);
                    }

                    if (preview != null)
                    {
                        byte[] bytes = preview.EncodeToPNG();
                        File.WriteAllBytes(savePath, bytes);
                    }
                }
                else
                {
                    Texture2D fallback = CreateFallbackRobotIcon(i);
                    byte[] bytes = fallback.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);
                }

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Robot Icons]</b> ĐÃ TẠO THÀNH CÔNG 4 ICON SPRITE CHO 4 ROBOT TẠI: Assets/UI/RobotIcons/</color>");
        }

        private static Texture2D CreateFallbackRobotIcon(int index)
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color bg = new Color(0.08f, 0.12f, 0.18f, 0.95f);
            Color border = new Color(0f, 0.85f, 1f, 0.9f);
            Color robotColor = index == 0 ? new Color(1f, 0.7f, 0.1f) : // Vàng Stan
                               index == 1 ? new Color(0.9f, 0.3f, 0.1f) : // Cam Mike
                               index == 2 ? new Color(0.2f, 0.8f, 1f) :   // Cyan George
                                            new Color(0.8f, 0.3f, 0.9f);  // Tím Leela

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f));
                    if (distFromCenter > (size / 2f - 4))
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (distFromCenter > (size / 2f - 10))
                    {
                        tex.SetPixel(x, y, border);
                    }
                    else if (distFromCenter < 70)
                    {
                        tex.SetPixel(x, y, robotColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, bg);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
#endif
