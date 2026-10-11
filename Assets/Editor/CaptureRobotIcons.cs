#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    [InitializeOnLoad]
    public static class CaptureRobotIcons
    {
        private static bool hasExecuted = false;

        static CaptureRobotIcons()
        {
            if (hasExecuted) return;
            hasExecuted = true;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                CaptureAllRobotIcons();
            };
        }

        [MenuItem("Tools/📸 Chụp Ảnh Thật 4 Robot Mecha Ra Icon UI")]
        public static void CaptureAllRobotIcons()
        {
            string outDir = "Assets/UI/RobotIcons";
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
                AssetDatabase.Refresh();
            }

            string[] prefabPaths = new string[]
            {
                "Assets/prefabs/FriendlyRobot_Stan.prefab",
                "Assets/prefabs/FriendlyRobot_Mike.prefab",
                "Assets/prefabs/FriendlyRobot_George.prefab",
                "Assets/prefabs/FriendlyRobot_Leela.prefab",
                "Assets/prefabsbot/BipedRobot_Prefab.prefab"
            };

            string[] fileNames = new string[]
            {
                "Icon_Stan.png",
                "Icon_Mike.png",
                "Icon_George.png",
                "Icon_Leela.png",
                "Icon_Miner.png"
            };

            // Vị trí Studio cô lập ở rất xa tọa độ Map chính
            Vector3 studioPos = new Vector3(0, 5000f, 0);
            GameObject studioRoot = new GameObject("[Temp_PhotoStudio]");
            studioRoot.transform.position = studioPos;

            // Camera Studio
            GameObject camObj = new GameObject("Studio_Camera");
            camObj.transform.SetParent(studioRoot.transform);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.08f, 0.12f, 1f); // Nền Sci-Fi tối sang trọng
            cam.fieldOfView = 26f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;

            // Key Light (Ánh sáng chính chiếu góc 32 độ)
            GameObject keyLightObj = new GameObject("KeyLight");
            keyLightObj.transform.SetParent(studioRoot.transform);
            Light keyLight = keyLightObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.98f, 0.92f);
            keyLight.intensity = 1.35f;
            keyLightObj.transform.rotation = Quaternion.Euler(32f, 38f, 0f);

            // Rim Light (Ánh sáng ven Cyan công nghệ cao)
            GameObject rimLightObj = new GameObject("RimLight");
            rimLightObj.transform.SetParent(studioRoot.transform);
            Light rimLight = rimLightObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = new Color(0.15f, 0.85f, 1f);
            rimLight.intensity = 1.6f;
            rimLightObj.transform.rotation = Quaternion.Euler(-20f, -145f, 0f);

            // Fill Light (Ánh sáng hắt dịu mắt)
            GameObject fillLightObj = new GameObject("FillLight");
            fillLightObj.transform.SetParent(studioRoot.transform);
            Light fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.85f, 0.75f, 0.6f);
            fillLight.intensity = 0.75f;
            fillLightObj.transform.rotation = Quaternion.Euler(15f, -60f, 0f);

            int renderSize = 512;
            RenderTexture rt = new RenderTexture(renderSize, renderSize, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 8;
            cam.targetTexture = rt;

            for (int i = 0; i < prefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                {
                    Debug.LogWarning($"[CaptureRobotIcons] Không tìm thấy prefab: {prefabPaths[i]}");
                    continue;
                }

                GameObject instance = Object.Instantiate(prefab, studioPos, Quaternion.identity);
                instance.name = "Target_Robot";

                // Vô hiệu hóa hoặc xóa các script logic game để không can thiệp lúc chụp ảnh
                foreach (var b in instance.GetComponentsInChildren<MonoBehaviour>())
                {
                    if (b != null) Object.DestroyImmediate(b);
                }
                foreach (var c in instance.GetComponentsInChildren<Collider>())
                {
                    if (c != null) Object.DestroyImmediate(c);
                }

                // Căn góc xoay mô hình đẹp nhất cho từng loại robot
                float yRot = (i == 0) ? -20f : (i == 1) ? -18f : (i == 2) ? -25f : (i == 3) ? -15f : -20f;
                instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

                // Tính toán bao quanh Bounds của tất cả Renderer
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Bounds bounds = new Bounds(studioPos, Vector3.zero);
                bool hasBounds = false;
                foreach (var r in renderers)
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }

                Vector3 center = hasBounds ? bounds.center : studioPos + Vector3.up * 1f;
                float maxDim = hasBounds ? Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) : 2f;
                if (maxDim < 0.6f) maxDim = 1.6f;

                // Đặt Camera nhìn bao quát toàn bộ robot
                float camDist = (maxDim / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad))) * 1.05f;
                Vector3 camOffset = new Vector3(0.18f * maxDim, maxDim * 0.15f, -camDist);
                cam.transform.position = center + camOffset;
                cam.transform.LookAt(center);

                cam.Render();

                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(renderSize, renderSize, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, renderSize, renderSize), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                // Thêm viền công nghệ Sci-Fi cho icon
                AddSciFiIconFrame(tex, i);

                byte[] pngBytes = tex.EncodeToPNG();
                string savePath = Path.Combine(outDir, fileNames[i]).Replace("\\", "/");
                File.WriteAllBytes(savePath, pngBytes);

                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(instance);

                Debug.Log($"<color=#00FFAA>📸 Đã chụp ảnh thành công: {fileNames[i]}</color>");
            }

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(studioRoot);

            AssetDatabase.Refresh();

            for (int i = 0; i < fileNames.Length; i++)
            {
                string savePath = Path.Combine(outDir, fileNames[i]).Replace("\\", "/");
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

            // Tự động gán lại ảnh vào bảng UI
            SetupRobotCraftingUI.BuildCraftingUI();
            Debug.Log("<color=#00FF88><b>[Robot Icons]</b> ĐÃ CHỤP XONG VÀ GÁN 100% ẢNH THỰC TẾ 4 ROBOT + ROBOT FARM ĐÁ VÀO BẢNG CHẾ TẠO!</color>");
        }

        private static void AddSciFiIconFrame(Texture2D tex, int index)
        {
            int size = tex.width;
            float radius = size * 0.485f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Color borderColor = (index == 0) ? new Color(1f, 0.75f, 0.1f, 0.95f) :   // Vàng Stan
                                (index == 1) ? new Color(0.95f, 0.35f, 0.1f, 0.95f) : // Đỏ cam Mike
                                (index == 2) ? new Color(0f, 0.85f, 1f, 0.95f) :     // Cyan George
                                (index == 3) ? new Color(0.85f, 0.3f, 0.95f, 0.95f) : // Tím Leela
                                               new Color(0.2f, 0.95f, 0.5f, 0.95f);  // Xanh lục Miner

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else if (dist > radius - 7f)
                    {
                        tex.SetPixel(x, y, borderColor);
                    }
                }
            }
            tex.Apply();
        }
    }
}
#endif
