#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IdleFactoryDefense.Editor
{
    /// <summary>
    /// WeaponIconSnapshotGenerator:
    /// Tự động chụp ảnh mô hình 3D thực tế của các vũ khí (Pháo Tự Hành, Trụ Thunder, Súng Phun Lửa, Bức Tường...)
    /// bằng Studio Camera Isometric ảo, bổ sung khung viền công nghệ neon sang trọng và lưu đè vào Assets/UI/WeaponIcons/.
    /// Giúp người chơi nhận diện chính xác 100% hình ảnh thực tế của từng vũ khí trong Cửa Hàng!
    /// </summary>
    [InitializeOnLoad]
    public static class WeaponIconSnapshotGenerator
    {
        static WeaponIconSnapshotGenerator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("SnapshotGenerated_v3", false))
                {
                    SessionState.SetBool("SnapshotGenerated_v3", true);
                    GenerateAllSnapshots();
                }
            };
        }

        [MenuItem("Tools/📸 Chụp Hình Vũ Khí Làm Icon Cửa Hàng (3D Snapshot)")]
        public static void GenerateAllSnapshots()
        {
            string outDir = "Assets/UI/WeaponIcons";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // Danh sách các prefab cần chụp
            CapturePrefabSnapshot(
                "Assets/Gun/prefabsGun/Phaotuhanhl.prefab", 
                $"{outDir}/Icon_Phaotuhanh.png", 
                new Color(0.2f, 0.75f, 1f), 
                new Vector3(1.3f, 1.1f, -1.3f),
                1.15f
            );

            CapturePrefabSnapshot(
                "Assets/Gun/prefabsGun/Thunder.prefab", 
                $"{outDir}/Icon_Thunder.png", 
                new Color(0.1f, 0.95f, 1f), 
                new Vector3(1.1f, 0.9f, -1.4f),
                1.2f
            );

            CapturePrefabSnapshot(
                "Assets/Gun/prefabsGun/FlamethrowerTurret.prefab", 
                $"{outDir}/Icon_Flamethrower.png", 
                new Color(1f, 0.5f, 0.1f), 
                new Vector3(1.3f, 1.1f, -1.3f),
                1.15f
            );

            CapturePrefabSnapshot(
                "Assets/prefabs/Wall_Building.prefab", 
                $"{outDir}/Icon_Wall.png", 
                new Color(0.6f, 0.8f, 1f), 
                new Vector3(1.4f, 1.0f, -1.2f),
                1.3f
            );

            CapturePrefabSnapshot(
                "Assets/prefabs/RobotFactory_Building.prefab", 
                $"{outDir}/Icon_RobotFactory.png", 
                new Color(0.15f, 0.95f, 0.65f), 
                new Vector3(1.2f, 1.1f, -1.3f),
                1.2f
            );

            // Gói khởi đầu (Starter Pack): Chụp combo Pháo Tự Hành với viền vàng kim vương giả
            CapturePrefabSnapshot(
                "Assets/Gun/prefabsGun/Phaotuhanhl.prefab", 
                $"{outDir}/Icon_StarterPack.png", 
                new Color(1f, 0.85f, 0.2f), 
                new Vector3(1.2f, 1.3f, -1.2f),
                1.1f,
                true
            );

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[3D Snapshot]</b> Đã chụp và cập nhật toàn bộ icon vũ khí thực tế vào Cửa Hàng thành công!</color>");
        }

        private static void CapturePrefabSnapshot(string prefabPath, string outputPath, Color accentColor, Vector3 viewAngle, float distanceMultiplier = 1.2f, bool isPack = false)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[3D Snapshot] Không tìm thấy prefab tại {prefabPath}");
                return;
            }

            // Tạo Studio Camera & Lighting ảo
            GameObject studioRoot = new GameObject("[Snapshot_Studio_Temp]");
            studioRoot.transform.position = new Vector3(9999f, 9999f, 9999f); // Đặt ở tọa độ xa biệt lập

            GameObject modelInstance = Object.Instantiate(prefab, studioRoot.transform.position, Quaternion.identity, studioRoot.transform);
            modelInstance.name = "Model";

            // Tính toán Bounds bao bọc toàn bộ mô hình
            Renderer[] renderers = modelInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Object.DestroyImmediate(studioRoot);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                if (r is LineRenderer || r is ParticleSystemRenderer) continue;
                bounds.Encapsulate(r.bounds);
            }

            Vector3 center = bounds.center;
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim <= 0.01f) maxDim = 1.0f;

            // Camera Studio
            GameObject camObj = new GameObject("Studio_Camera");
            camObj.transform.SetParent(studioRoot.transform, false);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Color;
            cam.backgroundColor = new Color(0.06f, 0.09f, 0.16f, 0.98f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            Vector3 normalizedDir = viewAngle.normalized;
            float camDist = (maxDim / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad))) * distanceMultiplier;
            camObj.transform.position = center + normalizedDir * camDist;
            camObj.transform.LookAt(center);

            // Nguồn sáng 1: Key Light (Chiếu chính diện góc trên bên phải)
            GameObject keyLightObj = new GameObject("Key_Light");
            keyLightObj.transform.SetParent(studioRoot.transform, false);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1.0f, 0.98f, 0.92f);
            keyLight.intensity = 1.8f;
            keyLightObj.transform.rotation = Quaternion.LookRotation(new Vector3(-0.8f, -1.2f, 1.0f));

            // Nguồn sáng 2: Rim Light (Chiếu ngược tạo viền ánh sáng xanh lơ nổi khối)
            GameObject rimLightObj = new GameObject("Rim_Light");
            rimLightObj.transform.SetParent(studioRoot.transform, false);
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = accentColor;
            rimLight.intensity = 1.4f;
            rimLightObj.transform.rotation = Quaternion.LookRotation(new Vector3(0.9f, 0.6f, -1.0f));

            // Render ra RenderTexture 512x512
            int texSize = 512;
            RenderTexture rt = new RenderTexture(texSize, texSize, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 8;
            cam.targetTexture = rt;
            cam.Render();

            // Đọc pixels
            RenderTexture.active = rt;
            Texture2D snapshotTex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            snapshotTex.ReadPixels(new Rect(0, 0, texSize, texSize), 0, 0);
            snapshotTex.Apply();

            // Áp dụng khung viền tròn công nghệ (Tech Frame Overlay)
            Texture2D finalIcon = ApplyTechFrame(snapshotTex, accentColor, isPack);

            // Lưu file PNG
            byte[] bytes = finalIcon.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            // Dọn dẹp
            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(snapshotTex);
            Object.DestroyImmediate(finalIcon);
            Object.DestroyImmediate(studioRoot);

            // Import và định cấu hình Sprite
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.alphaIsTransparency = true;
                ti.mipmapEnabled = false;
                ti.SaveAndReimport();
            }
        }

        private static Texture2D ApplyTechFrame(Texture2D src, Color borderColor, bool isPack)
        {
            int size = src.width;
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxR = size * 0.485f;
            float innerR = size * 0.450f;

            Color[] srcPixels = src.GetPixels();
            Color[] outPixels = new Color[srcPixels.Length];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    float d = Vector2.Distance(new Vector2(x, y), center);

                    if (d > maxR)
                    {
                        // Ngoài khung -> Trong suốt
                        outPixels[idx] = Color.clear;
                    }
                    else if (d > innerR)
                    {
                        // Viền kim loại phát sáng
                        float t = (d - innerR) / (maxR - innerR);
                        Color c = Color.Lerp(borderColor, Color.white, Mathf.Sin(t * Mathf.PI));
                        outPixels[idx] = c;
                    }
                    else
                    {
                        // Mô hình bên trong + thêm viền tối nhẹ
                        Color original = srcPixels[idx];
                        if (d > innerR - 6f)
                        {
                            original = Color.Lerp(original, borderColor * 0.8f, 0.4f);
                        }
                        outPixels[idx] = original;
                    }
                }
            }

            result.SetPixels(outPixels);
            result.Apply();
            return result;
        }
    }
}
#endif
