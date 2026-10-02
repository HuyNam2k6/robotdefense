#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class PlantNatureOnIsland
    {
        static PlantNatureOnIsland()
        {
            EditorApplication.update += CheckAndPlant;
        }

        private static void CheckAndPlant()
        {
            GameObject existing = GameObject.Find("_IslandVegetation");
            if (existing != null && existing.transform.Find("🌲 Cây Thông (Pines)") != null)
            {
                EditorApplication.update -= CheckAndPlant;
                return;
            }

            if (EditorApplication.isPaused) return;

            EditorApplication.update -= CheckAndPlant;
            PlantFoliage();
        }

        [MenuItem("Tools/🌿 Trồng Cây Cối & Bụi Rậm Khắp Đảo (Plant Nature)")]
        public static void PlantFoliage()
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[PlantNature] Không tìm thấy Terrain trong Scene!");
                return;
            }

            // 1. Danh sách Prefab Cây Thông & Cây Lá Kim (Pines & Conifers) từ Terrain Demo Scene
            string[] pinePaths = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Conifer/Conifer.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_A/Pine_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_B/Pine_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_C/Pine_C.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Pines/Pine_D/Pine_D.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Trees/Cypress/Cypress_Forest_Desktop.prefab"
            };

            // 2. Danh sách Cây Tán Tròn / Cổ Thụ (Broadleaf & Oak)
            string[] broadleafPaths = new string[]
            {
                "Assets/ALP_Assets/Big Oak Tree FREE/Prefabs/OakBigTree01_pr.prefab",
                "Assets/ALP_Assets/Poplar Tree FREE/Prefabs/PoplarTree001_pr.prefab"
            };

            // 3. Danh sách Tảng Đá Tự Nhiên & Đá Rêu (Rocks & Boulders)
            string[] rockPaths = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_C.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_Overgrown_D.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_A_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_B_02.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_C_01.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Rocks/Rock_D.prefab"
            };

            // 4. Danh sách Bụi Cây, Cây Bụi Đỏ & Dương Xỉ (Bushes & Details)
            string[] bushPaths = new string[]
            {
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_Red.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Bush_Twig.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Shrub.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Heather_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Fern_A.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Details/Fern_B.prefab",
                "Assets/TerrainDemoScene_URP/Prefabs/Foliage/Red_Bush/Red_Bush.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Bush_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Bush_B.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Plant_A.prefab",
                "Assets/TerrainSampleAssets/Prefabs/Plant_B.prefab"
            };

            List<GameObject> pineList = LoadPrefabs(pinePaths);
            List<GameObject> broadleafList = LoadPrefabs(broadleafPaths);
            List<GameObject> rockList = LoadPrefabs(rockPaths);
            List<GameObject> bushList = LoadPrefabs(bushPaths);

            if (pineList.Count == 0 && broadleafList.Count == 0)
            {
                Debug.LogWarning("[PlantNature] Không tìm thấy prefab cây nào!");
                return;
            }

            // Dọn dẹp cụm cũ nếu có
            GameObject rootFoliage = GameObject.Find("_IslandVegetation");
            if (rootFoliage != null)
            {
                Object.DestroyImmediate(rootFoliage);
            }
            rootFoliage = new GameObject("_IslandVegetation");
            Undo.RegisterCreatedObjectUndo(rootFoliage, "Plant Island Nature");

            // Tạo các nhóm con trong Hierarchy cho gọn gàng
            Transform groupPines = new GameObject("🌲 Cây Thông (Pines)").transform;
            groupPines.SetParent(rootFoliage.transform);

            Transform groupOaks = new GameObject("🌳 Cây Tán Tròn (Oaks)").transform;
            groupOaks.SetParent(rootFoliage.transform);

            Transform groupRocks = new GameObject("🪨 Tảng Đá Rêu (Rocks)").transform;
            groupRocks.SetParent(rootFoliage.transform);

            Transform groupBushes = new GameObject("🌿 Bụi Rậm & Hoa (Bushes)").transform;
            groupBushes.SetParent(rootFoliage.transform);

            Random.InitState(424242);
            int totalPlanted = 0;

            // Vùng trung tâm căn cứ cần tránh để người chơi tự do xây dựng và robot đào mỏ
            // X: [-14, 14], Z: [-48, -16]
            System.Func<float, float, bool> isInBaseArea = (x, z) =>
            {
                return Mathf.Abs(x) < 14f && z > -48f && z < -16f;
            };

            // Hàm tính độ cao Terrain chuẩn xác
            System.Func<float, float, float> getTerrainHeight = (x, z) =>
            {
                Vector3 samplePos = new Vector3(x, 0f, z);
                return terrain.SampleHeight(samplePos) + terrain.transform.position.y;
            };

            // ================= 1. TRỒNG CÁC CỤM CÂY THÔNG (PINE CLUSTERS) =================
            // Trồng 16 cụm cây thông (mỗi cụm 2-4 cây + 1 tảng đá to bên cạnh)
            int clusterCount = 16;
            for (int c = 0; c < clusterCount; c++)
            {
                // Chọn tâm cụm
                float cx = Random.Range(-32f, 32f);
                float cz = Random.Range(-58f, 58f);

                if (isInBaseArea(cx, cz)) continue;

                float ch = getTerrainHeight(cx, cz);
                if (ch < 7.8f) continue; // Không trồng ở vực sâu hay dưới nước

                // Số lượng cây thông trong cụm (2 - 4 cây)
                int treesInCluster = Random.Range(2, 5);
                for (int t = 0; t < treesInCluster; t++)
                {
                    float ox = cx + Random.Range(-3.5f, 3.5f);
                    float oz = cz + Random.Range(-3.5f, 3.5f);
                    if (isInBaseArea(ox, oz)) continue;

                    float oh = getTerrainHeight(ox, oz);
                    if (oh < 7.8f) continue;

                    GameObject pPrefab = pineList[Random.Range(0, pineList.Count)];
                    float scale = Random.Range(0.85f, 1.25f);
                    SpawnNatureItem(pPrefab, new Vector3(ox, oh, oz), Vector3.one * scale, groupPines);
                    totalPlanted++;
                }

                // Đi kèm 1 tảng đá lớn rêu xanh bên chân cụm thông
                if (rockList.Count > 0 && Random.value < 0.85f)
                {
                    float rx = cx + Random.Range(-2f, 2f);
                    float rz = cz + Random.Range(-2f, 2f);
                    float rh = getTerrainHeight(rx, rz);
                    if (rh >= 7.8f)
                    {
                        GameObject rPrefab = rockList[Random.Range(0, rockList.Count)];
                        float rScale = Random.Range(1.4f, 2.2f);
                        SpawnNatureItem(rPrefab, new Vector3(rx, rh - 0.2f, rz), Vector3.one * rScale, groupRocks);
                        totalPlanted++;
                    }
                }

                // Đi kèm 1-2 bụi cây quanh gốc
                if (bushList.Count > 0)
                {
                    int bushes = Random.Range(1, 3);
                    for (int b = 0; b < bushes; b++)
                    {
                        float bx = cx + Random.Range(-3f, 3f);
                        float bz = cz + Random.Range(-3f, 3f);
                        float bh = getTerrainHeight(bx, bz);
                        if (bh >= 7.8f)
                        {
                            GameObject bPrefab = bushList[Random.Range(0, bushList.Count)];
                            float bScale = Random.Range(1.5f, 2.5f);
                            SpawnNatureItem(bPrefab, new Vector3(bx, bh, bz), Vector3.one * bScale, groupBushes);
                            totalPlanted++;
                        }
                    }
                }
            }

            // ================= 2. TRỒNG CÂY TÁN TRÒN / CỔ THỤ (OAKS & BROADLEAF) =================
            // Trồng 8-10 cây sồi / cây dương đơn lẻ hoặc theo cặp ở sườn cỏ thoai thoải
            int broadleafAttempts = 0;
            int broadleafPlanted = 0;
            while (broadleafPlanted < 8 && broadleafAttempts < 50 && broadleafList.Count > 0)
            {
                broadleafAttempts++;
                float ox = Random.Range(-28f, 28f);
                float oz = Random.Range(-55f, 55f);

                if (isInBaseArea(ox, oz)) continue;

                float oh = getTerrainHeight(ox, oz);
                if (oh < 8.2f) continue;

                GameObject oPrefab = broadleafList[Random.Range(0, broadleafList.Count)];
                // Scale cây sồi vừa vặn (0.45x - 0.65x vì model gốc rất to)
                float scale = Random.Range(0.45f, 0.65f);
                SpawnNatureItem(oPrefab, new Vector3(ox, oh, oz), Vector3.one * scale, groupOaks);
                broadleafPlanted++;
                totalPlanted++;

                // Thêm bụi cây nhỏ dưới bóng râm
                if (bushList.Count > 0)
                {
                    GameObject bPrefab = bushList[Random.Range(0, bushList.Count)];
                    SpawnNatureItem(bPrefab, new Vector3(ox + 1.2f, oh, oz + 0.8f), Vector3.one * 1.8f, groupBushes);
                    totalPlanted++;
                }
            }

            // ================= 3. TRỒNG CÁC TẢNG ĐÁ VEN VÁCH NÚI (CLIFFSIDE ROCKS) =================
            // Trồng 15 tảng đá lớn nhô ra quanh rìa đảo
            int rockAttempts = 0;
            int rocksPlanted = 0;
            while (rocksPlanted < 15 && rockAttempts < 80 && rockList.Count > 0)
            {
                rockAttempts++;
                // Chọn vị trí ven rìa đảo: |x| ~ [20, 33] hoặc |z| ~ [40, 60]
                float rx = Random.Range(-33f, 33f);
                float rz = Random.Range(-62f, 62f);

                bool isEdge = (Mathf.Abs(rx) > 20f || Mathf.Abs(rz) > 38f);
                if (!isEdge || isInBaseArea(rx, rz)) continue;

                float rh = getTerrainHeight(rx, rz);
                if (rh < 7.5f) continue;

                GameObject rPrefab = rockList[Random.Range(0, rockList.Count)];
                float rScale = Random.Range(1.8f, 3.0f);
                SpawnNatureItem(rPrefab, new Vector3(rx, rh - 0.3f, rz), Vector3.one * rScale, groupRocks);
                rocksPlanted++;
                totalPlanted++;
            }

            // ================= 4. BỤI CÂY & HOA RẢI RÁC (MEADOW DETAILS) =================
            int bushAttempts = 0;
            int bushesPlanted = 0;
            while (bushesPlanted < 40 && bushAttempts < 120 && bushList.Count > 0)
            {
                bushAttempts++;
                float bx = Random.Range(-30f, 30f);
                float bz = Random.Range(-58f, 58f);

                if (isInBaseArea(bx, bz)) continue;

                float bh = getTerrainHeight(bx, bz);
                if (bh < 8.0f) continue;

                GameObject bPrefab = bushList[Random.Range(0, bushList.Count)];
                float bScale = Random.Range(1.5f, 2.6f);
                SpawnNatureItem(bPrefab, new Vector3(bx, bh, bz), Vector3.one * bScale, groupBushes);
                bushesPlanted++;
                totalPlanted++;
            }

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
                EditorSceneManager.SaveScene(terrain.gameObject.scene);
            }

            Debug.Log($"<color=#00FFAA><b>[PlantNature]</b> ĐÃ TRỒNG HOÀN THÀNH {totalPlanted} CÂY CỐI & TẢNG ĐÁ (GỒM CÂY THÔNG, CÂY TÁN TRÒN, ĐÁ RÊU) KHẮP ĐẢO!</color>");
        }

        private static List<GameObject> LoadPrefabs(string[] paths)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (var path in paths)
            {
                GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null) list.Add(p);
            }
            return list;
        }

        private static void SpawnNatureItem(GameObject prefab, Vector3 position, Vector3 scale, Transform parent)
        {
            if (prefab == null) return;

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject instance = null;

            if (!Application.isPlaying)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab, position, rotation, parent);
            }
            else
            {
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }

            instance.transform.localScale = scale;

            // Tắt Collider để người chơi và robot di chuyển hoàn toàn thông suốt, không cản trở đặt tường
            foreach (var col in instance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }
    }
}
#endif
