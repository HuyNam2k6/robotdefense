#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    public static class SetupAllRobotsMaterials
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                // Kiểm tra nếu chưa có đủ prefab thì tự chạy setup
                bool needSetup = !File.Exists("Assets/Models/BipedRobot/BipedRobot_Prefab.prefab")
                              || !File.Exists("Assets/Models/HeavyRobot/HeavyRobot_Prefab.prefab")
                              || !File.Exists("Assets/Models/Drone/Drone_Prefab.prefab");
                if (needSetup)
                {
                    SetupAll();
                }
            };
        }

        // [MenuItem("Tools/🎨 Cài Đặt Màu & Tạo Prefab Toàn Bộ Robot")]
        public static void SetupAll()
        {
            SetupSteampunkRobot();
            SetupBipedRobot();
            SetupHeavyRobot();
            SetupDrone();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Robot Setup]</b> ĐÃ HOÀN TẤT CÀI ĐẶT MÀU VÀ TẠO PREFAB CHO TẤT CẢ 4 ROBOT!</color>");
        }

        // ================= 1. COMBAT STEAMPUNK ROBOT =================
        // [MenuItem("Tools/Robots/1. Steampunk Robot")]
        public static void SetupSteampunkRobot()
        {
            string rootPath = "Assets/Models/CombatSteampunkRobot";
            string texPath = Path.Combine(rootPath, "textures");
            string matPath = Path.Combine(rootPath, "Materials");
            string fbxPath = Path.Combine(rootPath, "source/SteampunkRobot.fbx");

            EnsureDirectory(matPath);
            FixFbxImporter(fbxPath);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material matDefault = CreateOrGetMaterial(matPath, "DefaultMaterial", urpShader, texPath, "DefaultMaterial_Base_color.png", "DefaultMaterial_Normal_OpenGL.png", "DefaultMaterial_Metallic.png");
            Material mat1001 = CreateOrGetMaterial(matPath, "1001", urpShader, texPath, "1001_Base_color.png", "1001_Normal_OpenGL.png", "1001_Metallic.png");
            Material matTT00 = CreateOrGetMaterial(matPath, "TT_color_00", urpShader, texPath, "TT_color_00_Base_color.png", "TT_color_00_Normal_OpenGL.png", "TT_color_00_Metallic.png");

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "CombatSteampunkRobot";

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string mName = mats[i] != null ? mats[i].name : "";
                        if (mName.Contains("1001")) mats[i] = mat1001;
                        else if (mName.Contains("TT_color_00")) mats[i] = matTT00;
                        else mats[i] = matDefault;
                    }
                    r.sharedMaterials = mats;
                }

                string prefabPath = Path.Combine(rootPath, "CombatSteampunkRobot_Prefabs.prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ Steampunk Robot Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= 2. BIPED ROBOT =================
        // [MenuItem("Tools/Robots/2. Biped Robot")]
        public static void SetupBipedRobot()
        {
            string rootPath = "Assets/Models/BipedRobot";
            string texPath = Path.Combine(rootPath, "textures");
            string matPath = Path.Combine(rootPath, "Materials");
            string fbxPath = Path.Combine(rootPath, "source/Robot.FBX");

            EnsureDirectory(matPath);
            FixFbxImporter(fbxPath);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material matID1 = CreateOrGetMaterial(matPath, "ID1_PaintMetal", urpShader, texPath, "ID1_PaintMetal_albedo.jpg", "ID1_PaintMetal_normal.jpg", null);
            Material matID2 = CreateOrGetMaterial(matPath, "ID2_Metal", urpShader, texPath, "ID2_Metal_albedo.jpg", "ID2_Metal_normal.jpg", null);
            Material matID3 = CreateOrGetMaterial(matPath, "ID3_Rubber", urpShader, texPath, "ID3_Rubber_albedo.jpg", "ID3_Rubber_normal.jpg", null);

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "BipedRobot";

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string mName = mats[i] != null ? mats[i].name : "";
                        if (mName.Contains("ID1") || mName.Contains("Paint")) mats[i] = matID1;
                        else if (mName.Contains("ID2") || mName.Contains("Metal")) mats[i] = matID2;
                        else if (mName.Contains("ID3") || mName.Contains("Rubber")) mats[i] = matID3;
                        else mats[i] = matID1;
                    }
                    r.sharedMaterials = mats;
                }

                string prefabPath = Path.Combine(rootPath, "BipedRobot_Prefab.prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ Biped Robot Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= 3. HEAVY ROBOT =================
        // [MenuItem("Tools/Robots/3. Heavy Robot")]
        public static void SetupHeavyRobot()
        {
            string rootPath = "Assets/Models/HeavyRobot";
            string texPath = Path.Combine(rootPath, "textures");
            string matPath = Path.Combine(rootPath, "Materials");
            string fbxPath = Path.Combine(rootPath, "source/Robot.fbx");

            EnsureDirectory(matPath);
            FixFbxImporter(fbxPath);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material matHeavy = CreateOrGetMaterial(matPath, "HeavyRobot_Mat", urpShader, texPath, "Robot_Base_Color.png", "Robot_Normal_Map.png", "Robot_Metallic.png");

            // Gán AO texture nếu có
            string aoPath = Path.Combine(texPath, "Robot_AO.png").Replace("\\", "/");
            Texture2D aoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);
            if (aoTex != null)
            {
                matHeavy.SetTexture("_OcclusionMap", aoTex);
            }

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "HeavyRobot";

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = matHeavy;
                    }
                    r.sharedMaterials = mats;
                }

                string prefabPath = Path.Combine(rootPath, "HeavyRobot_Prefab.prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ Heavy Robot Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= 4. DRONE =================
        // [MenuItem("Tools/Robots/4. Drone")]
        public static void SetupDrone()
        {
            string rootPath = "Assets/Models/Drone";
            string texPath = Path.Combine(rootPath, "textures");
            string matPath = Path.Combine(rootPath, "Materials");
            string fbxPath = Path.Combine(rootPath, "source/Drone.FBX");

            EnsureDirectory(matPath);
            FixFbxImporter(fbxPath);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material matDrone = CreateOrGetMaterial(matPath, "Drone_Mat", urpShader, texPath, "drone_d.png", "drone_n.png", "drone_m.png");

            // Emission & Occlusion
            string emissivePath = Path.Combine(texPath, "drone_e.png").Replace("\\", "/");
            Texture2D emissiveTex = AssetDatabase.LoadAssetAtPath<Texture2D>(emissivePath);
            if (emissiveTex != null)
            {
                matDrone.EnableKeyword("_EMISSION");
                matDrone.SetTexture("_EmissionMap", emissiveTex);
                matDrone.SetColor("_EmissionColor", Color.white);
            }

            string aoPath = Path.Combine(texPath, "drone_ao.png").Replace("\\", "/");
            Texture2D aoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(aoPath);
            if (aoTex != null)
            {
                matDrone.SetTexture("_OcclusionMap", aoTex);
            }

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "Drone";

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = matDrone;
                    }
                    r.sharedMaterials = mats;
                }

                string prefabPath = Path.Combine(rootPath, "Drone_Prefab.prefab");
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ Drone Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= TIỆN ÍCH CHUNG =================
        private static void FixFbxImporter(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.importCameras)
                {
                    importer.importCameras = false;
                    changed = true;
                }
                if (importer.importLights)
                {
                    importer.importLights = false;
                    changed = true;
                }
                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Material CreateOrGetMaterial(string matDir, string matName, Shader shader, string texDir, string baseColorFile, string normalFile, string metallicFile)
        {
            string assetPath = Path.Combine(matDir, matName + ".mat").Replace("\\", "/");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            if (!string.IsNullOrEmpty(baseColorFile))
            {
                string baseColorPath = Path.Combine(texDir, baseColorFile).Replace("\\", "/");
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
                if (baseTex != null)
                {
                    mat.SetTexture("_BaseMap", baseTex);
                    mat.SetTexture("_MainTex", baseTex);
                }
            }

            if (!string.IsNullOrEmpty(normalFile))
            {
                string normalPath = Path.Combine(texDir, normalFile).Replace("\\", "/");
                // Đảm bảo texture type là NormalMap
                TextureImporter texImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (texImporter != null && texImporter.textureType != TextureImporterType.NormalMap)
                {
                    texImporter.textureType = TextureImporterType.NormalMap;
                    texImporter.SaveAndReimport();
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normalTex);
                }
            }

            if (!string.IsNullOrEmpty(metallicFile))
            {
                string metallicPath = Path.Combine(texDir, metallicFile).Replace("\\", "/");
                Texture2D metalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
                if (metalTex != null)
                {
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetTexture("_MetallicGlossMap", metalTex);
                }
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
