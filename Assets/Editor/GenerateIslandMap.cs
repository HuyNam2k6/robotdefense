#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class GenerateIslandMap
    {
        private const string REGEN_FLAG_KEY = "IslandMap_Expanded_V1";

        static GenerateIslandMap()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(REGEN_FLAG_KEY, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(REGEN_FLAG_KEY, true);
                    Generate();
                }
            };
        }

        // [MenuItem("Tools/🏝️ Tự Động Tạo Đảo Bán Nguyệt Giữa Biển (Island Map)")]
        public static void Generate()
        {
            // 1. Tìm hoặc tạo Terrain trong Scene
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                TerrainData newTData = new TerrainData();
                GameObject tObj = Terrain.CreateTerrainGameObject(newTData);
                terrain = tObj.GetComponent<Terrain>();
            }

            TerrainData tData = terrain.terrainData;
            Undo.RegisterCompleteObjectUndo(tData, "Generate Island Map");

            // Kích thước mở rộng: Rộng 95m (tăng ~35%), Dài 165m (tăng ~27%), Cao 20m
            float mapWidth = 95f;
            float mapLength = 165f;
            float maxHeight = 20f;
            tData.size = new Vector3(mapWidth, maxHeight, mapLength);

            // Căn chỉnh vị trí Terrain để tâm đảo nằm quanh gốc tọa độ (0, 0, 0)
            terrain.transform.position = new Vector3(-mapWidth / 2f, 0f, -mapLength / 2f);

            int hRes = tData.heightmapResolution;
            float[,] heights = new float[hRes, hRes];

            float plateauHeightNorm = 0.45f; // Đảo nhô cao 9m so với mặt biển
            float seaBedHeightNorm = 0.05f;  // Đáy biển nông 1m

            for (int y = 0; y < hRes; y++)
            {
                float v = (float)y / (hRes - 1); // Trục dọc (Z)
                for (int x = 0; x < hRes; x++)
                {
                    float u = (float)x / (hRes - 1); // Trục ngang (X)

                    // Bán kính chiều ngang của đảo mở rộng rộng rãi hơn (0.25 thay vì 0.22)
                    float uDist = Mathf.Abs(u - 0.5f);
                    float islandHalfWidth = 0.25f + 0.04f * Mathf.Sin(v * Mathf.PI);

                    // Nhiễu Perlin Noise để viền đá nhấp nhô tự nhiên
                    float edgeNoise = (Mathf.PerlinNoise(u * 14f, v * 14f) - 0.5f) * 0.07f;
                    float effectiveBoundary = islandHalfWidth + edgeNoise;

                    // Giới hạn theo chiều dọc trải dài hơn (0.07 đến 0.93)
                    float distFromSide = effectiveBoundary - uDist;
                    float distFromTop = 0.93f - v;
                    float distFromBottom = v - 0.07f;
                    float edgeDist = Mathf.Min(distFromSide, Mathf.Min(distFromTop, distFromBottom));

                    // Tạo vách dốc đứng (Cliff)
                    float cliffWidth = 0.055f;
                    float factor = Mathf.Clamp01(edgeDist / cliffWidth);
                    // Đường cong dốc tự nhiên (Hermite curve)
                    factor = factor * factor * (3f - 2f * factor);

                    float h = Mathf.Lerp(seaBedHeightNorm, plateauHeightNorm, factor);

                    // Gợn địa hình nhẹ trên mặt bằng phẳng
                    if (factor > 0.9f)
                    {
                        h += (Mathf.PerlinNoise(u * 25f, v * 25f) - 0.5f) * 0.008f;
                    }

                    heights[y, x] = h;
                }
            }

            tData.SetHeights(0, 0, heights);

            // 2. Thiết lập Texture: Sơn Cỏ mặt trên, Đá ở vách dốc
            SetupTerrainLayers(tData);

            // 3. Tạo Mặt Nước Biển (Ocean Plane) mở rộng tương ứng
            SetupOcean(terrain.transform.position, mapWidth, mapLength);

            // 4. Tạo Tường vô hình chặn Player không rơi xuống biển theo kích thước mới
            SetupInvisibleBoundaries(terrain.transform, mapWidth, mapLength, maxHeight * plateauHeightNorm);

            // 5. Căn chỉnh góc nhìn Main Camera
            SetupIsometricCamera(terrain.transform.position, mapWidth, mapLength, maxHeight * plateauHeightNorm);

            // 6. Tối ưu làm đẹp texture PBR
            BeautifyTerrainTextures.ApplyBeautification();

            EditorUtility.SetDirty(tData);
            EditorUtility.SetDirty(terrain);
            AssetDatabase.SaveAssets();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
                EditorSceneManager.SaveScene(terrain.gameObject.scene);
            }

            Debug.Log($"<color=#00FFAA><b>[IslandGenerator]</b> ĐÃ MỞ RỘNG BẢN ĐỒ THÀNH CÔNG! Kích thước mới: Rộng={mapWidth}m, Dài={mapLength}m.</color>");
        }

        private static void SetupTerrainLayers(TerrainData tData)
        {
            string grassTexPath = "Assets/TerrainSampleAssets/Textures/Terrain/Grass_Soil_BaseColor.tif";
            if (!File.Exists(grassTexPath)) grassTexPath = "Assets/TerrainSampleAssets/Textures/Terrain/Grass_A_BaseColor.tif";

            string rockTexPath = "Assets/TerrainSampleAssets/Textures/Terrain/Rock_BaseColor.tif";

            Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>(grassTexPath);
            Texture2D rockTex = AssetDatabase.LoadAssetAtPath<Texture2D>(rockTexPath);

            if (grassTex == null || rockTex == null) return;

            string layerFolder = "Assets/TerrainSampleAssets/Layers";
            if (!Directory.Exists(layerFolder)) Directory.CreateDirectory(layerFolder);

            string grassLayerPath = Path.Combine(layerFolder, "Island_Grass.terrainlayer");
            string rockLayerPath = Path.Combine(layerFolder, "Island_Rock.terrainlayer");

            TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(grassLayerPath);
            if (grassLayer == null)
            {
                grassLayer = new TerrainLayer();
                grassLayer.diffuseTexture = grassTex;
                grassLayer.tileSize = new Vector2(4f, 4f);
                AssetDatabase.CreateAsset(grassLayer, grassLayerPath);
            }

            TerrainLayer rockLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(rockLayerPath);
            if (rockLayer == null)
            {
                rockLayer = new TerrainLayer();
                rockLayer.diffuseTexture = rockTex;
                rockLayer.tileSize = new Vector2(4.5f, 4.5f);
                AssetDatabase.CreateAsset(rockLayer, rockLayerPath);
            }

            tData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer };

            int aRes = tData.alphamapResolution;
            float[,,] alphaMaps = new float[aRes, aRes, 2];

            for (int y = 0; y < aRes; y++)
            {
                float v = (float)y / (aRes - 1);
                for (int x = 0; x < aRes; x++)
                {
                    float u = (float)x / (aRes - 1);
                    float steepness = tData.GetSteepness(u, v);
                    float height = tData.GetInterpolatedHeight(u, v);

                    if (steepness > 22f || height < 4f)
                    {
                        alphaMaps[y, x, 0] = 0f;
                        alphaMaps[y, x, 1] = 1f;
                    }
                    else if (steepness > 12f)
                    {
                        float blend = Mathf.InverseLerp(12f, 22f, steepness);
                        alphaMaps[y, x, 0] = 1f - blend;
                        alphaMaps[y, x, 1] = blend;
                    }
                    else
                    {
                        alphaMaps[y, x, 0] = 1f;
                        alphaMaps[y, x, 1] = 0f;
                    }
                }
            }

            tData.SetAlphamaps(0, 0, alphaMaps);
        }

        private static void SetupOcean(Vector3 terrainPos, float mapWidth, float mapLength)
        {
            GameObject ocean = GameObject.Find("Ocean");
            if (ocean == null)
            {
                ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ocean.name = "Ocean";
            }

            ocean.transform.position = new Vector3(0f, 2.0f, 0f);
            ocean.transform.localScale = new Vector3(mapWidth * 0.45f, 1f, mapLength * 0.45f);

            string matFolder = "Assets/Map/Models/Materials";
            if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);
            string oceanMatPath = Path.Combine(matFolder, "Ocean_Mat.mat");

            Material oceanMat = AssetDatabase.LoadAssetAtPath<Material>(oceanMatPath);
            if (oceanMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null) shader = Shader.Find("Standard");

                oceanMat = new Material(shader);
                oceanMat.color = new Color(0.05f, 0.22f, 0.38f, 1f);
                if (oceanMat.HasProperty("_BaseColor"))
                {
                    oceanMat.SetColor("_BaseColor", new Color(0.05f, 0.22f, 0.38f, 1f));
                }
                oceanMat.SetFloat("_Smoothness", 0.92f);
                oceanMat.SetFloat("_Metallic", 0.15f);
                AssetDatabase.CreateAsset(oceanMat, oceanMatPath);
            }

            ocean.GetComponent<MeshRenderer>().sharedMaterial = oceanMat;
        }

        private static void SetupInvisibleBoundaries(Transform parent, float width, float length, float islandY)
        {
            Transform oldBound = parent.Find("_Boundaries");
            if (oldBound != null) Object.DestroyImmediate(oldBound.gameObject);

            GameObject boundObj = new GameObject("_Boundaries");
            boundObj.transform.SetParent(parent, false);

            float playWidth = width * 0.48f;
            float playLength = length * 0.84f;

            CreateWall(boundObj.transform, new Vector3(width/2f - playWidth/2f, islandY + 2f, length/2f), new Vector3(0.5f, 5f, playLength));
            CreateWall(boundObj.transform, new Vector3(width/2f + playWidth/2f, islandY + 2f, length/2f), new Vector3(0.5f, 5f, playLength));
            CreateWall(boundObj.transform, new Vector3(width/2f, islandY + 2f, length/2f - playLength/2f), new Vector3(playWidth, 5f, 0.5f));
            CreateWall(boundObj.transform, new Vector3(width/2f, islandY + 2f, length/2f + playLength/2f), new Vector3(playWidth, 5f, 0.5f));
        }

        private static void CreateWall(Transform parent, Vector3 localPos, Vector3 size)
        {
            GameObject wall = new GameObject("BoundaryWall");
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = size;
        }

        private static void SetupIsometricCamera(Vector3 terrainPos, float width, float length, float islandY)
        {
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;

            // Nếu không có Cinemachine điều khiển riêng thì căn chỉnh camera Isometric
            if (cam.GetComponent("CinemachineBrain") == null)
            {
                cam.transform.position = new Vector3(0f, islandY + 45f, -length * 0.46f);
                cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
            }
        }
    }
}
#endif
