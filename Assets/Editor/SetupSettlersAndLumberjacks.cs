#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupSettlersAndLumberjacks
    {
        [MenuItem("Tools/🤖 Cài Đặt Dân Cư & Nhà Trú Ẩn (Settlers & Village)")]
        [InitializeOnLoadMethod]
        public static void SetupAll()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                ExecuteSetup();
            };
        }

        public static void ExecuteSetup()
        {
            EnsureDirectories();

            // 1. Gắn ChoppableTree vào toàn bộ cây cối trên đảo
            SetupAllIslandTrees();

            // 2. Tạo Prefab Nhà Dân Cư Trú Ẩn (Civilian House)
            GameObject housePrefab = CreateCivilianHousePrefab();

            // 3. Tạo Prefab Dân Cư (CuteRobot đổi màu sắc, TUYỆT ĐỐI KHÔNG DÙNG COLLIDER)
            GameObject settlerPrefab = CreateSettlerPrefab();

            // 4. Đặt Nhà cửa, Dân cư vào SampleScene, phóng to Player, xóa thợ chặt cây & giảm đá 20%
            PlaceEntitiesInScene(housePrefab, settlerPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Village Setup]</b> ĐÃ HOÀN TẤT THIẾT LẬP DÂN CƯ (KHÔNG COLLIDER), PHÓNG TO PLAYER VÀ GIẢM ĐÁ 20%!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/prefabs")) Directory.CreateDirectory("Assets/prefabs");
        }

        // =========================================================================
        // 1. TỰ ĐỘNG GẮN CHOPPABLETREE CHO CÂY TRÊN ĐẢO
        // =========================================================================
        public static void SetupAllIslandTrees()
        {
            int treeCount = 0;
            GameObject vegetationRoot = GameObject.Find("_IslandVegetation");
            if (vegetationRoot != null)
            {
                var allRenderers = vegetationRoot.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in allRenderers)
                {
                    Transform t = r.transform;
                    string n = t.name.ToLower();
                    string parentN = t.parent != null ? t.parent.name.ToLower() : "";
                    if (n.Contains("pine") || n.Contains("conifer") || n.Contains("cypress") || n.Contains("oak") || n.Contains("tree") ||
                        parentN.Contains("pine") || parentN.Contains("conifer") || parentN.Contains("cypress") || parentN.Contains("oak") || parentN.Contains("tree"))
                    {
                        Transform treeRoot = (t.parent != null && t.parent != vegetationRoot.transform) ? t.parent : t;
                        if (treeRoot.GetComponent<ChoppableTree>() == null)
                        {
                            ChoppableTree ct = treeRoot.gameObject.AddComponent<ChoppableTree>();
                            ct.maxHits = 3;
                            ct.woodPerHit = 1;
                            ct.bonusWoodOnFelled = 2;
                            ct.respawnTime = 25f;
                            treeCount++;
                        }
                    }
                }
            }

            Debug.Log($"<color=#7CFC00><b>[ChoppableTree Setup]</b> Đã gắn kịch bản khai thác cho {treeCount} cây trên đảo!</color>");
        }

        // =========================================================================
        // 2. TẠO PREFAB NGÔI NHÀ DÂN CƯ (CIVILIAN HOUSE)
        // =========================================================================
        private static GameObject CreateCivilianHousePrefab()
        {
            string houseFbxPath = "Assets/Map/Models/FBX format/container-wide.fbx";
            string doorFbxPath = "Assets/Map/Models/FBX format/door-single.fbx";
            string matPath = "Assets/Map/Models/Materials/SpaceStation_Mat.mat";
            string targetPrefabPath = "Assets/prefabs/CivilianHouse_Building.prefab";

            GameObject baseFbx = AssetDatabase.LoadAssetAtPath<GameObject>(houseFbxPath);
            GameObject doorFbx = AssetDatabase.LoadAssetAtPath<GameObject>(doorFbxPath);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

            if (baseFbx == null)
            {
                Debug.LogError("Không tìm thấy model nhà: " + houseFbxPath);
                return null;
            }

            GameObject instance = new GameObject("CivilianHouse_Building");

            // Phần thân nhà
            GameObject bodyObj = Object.Instantiate(baseFbx, instance.transform);
            bodyObj.name = "House_Body";
            bodyObj.transform.localPosition = Vector3.zero;
            bodyObj.transform.localRotation = Quaternion.identity;
            bodyObj.transform.localScale = Vector3.one * 1.6f;

            if (mat != null)
            {
                foreach (var r in bodyObj.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            // Cửa trước
            GameObject doorAnchor = new GameObject("EntranceDoor");
            doorAnchor.transform.SetParent(instance.transform, false);
            doorAnchor.transform.localPosition = new Vector3(0, 0, -1.8f);

            if (doorFbx != null)
            {
                GameObject doorVisual = Object.Instantiate(doorFbx, doorAnchor.transform);
                doorVisual.name = "DoorVisual";
                doorVisual.transform.localPosition = Vector3.zero;
                doorVisual.transform.localRotation = Quaternion.identity;
                doorVisual.transform.localScale = Vector3.one * 1.5f;

                if (mat != null)
                {
                    foreach (var r in doorVisual.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterial = mat;
                    }
                }
            }

            // Collider cho ngôi nhà
            BoxCollider box = instance.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 1.3f, 0);
            box.size = new Vector3(4.2f, 2.6f, 3.8f);

            // Gắn Script CivilianHouse
            CivilianHouse houseScript = instance.AddComponent<CivilianHouse>();
            houseScript.houseName = "Nhà Dân Cư";
            houseScript.doorPoint = doorAnchor.transform;
            houseScript.enterDistance = 1.8f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log("<color=#00FFAA>Đã tạo Prefab Ngôi Nhà Dân Cư: " + targetPrefabPath + "</color>");
            return prefab;
        }

        // =========================================================================
        // 3. TẠO PREFAB DÂN CƯ (CUTE ROBOT - TUYỆT ĐỐI KHÔNG DÙNG COLLIDER ĐỂ TỐI ƯU CPU)
        // =========================================================================
        private static GameObject CreateSettlerPrefab()
        {
            string basePrefabPath = "Assets/Models/AnimatedRobot/CuteRobot_Prefab.prefab";
            string targetPrefabPath = "Assets/prefabs/Settler_CuteCitizen.prefab";

            GameObject cuteBase = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (cuteBase == null)
            {
                Debug.LogError("Không tìm thấy: " + basePrefabPath);
                return null;
            }

            GameObject instance = Object.Instantiate(cuteBase);
            instance.name = "Settler_CuteCitizen";
            instance.tag = "Untagged"; // Không được đặt tag Player

            // Gỡ bỏ PlayerController hoặc CuteBot nếu có
            var playerCtrl = instance.GetComponent<PlayerController>();
            if (playerCtrl != null) Object.DestroyImmediate(playerCtrl);
            var cuteBot = instance.GetComponent<CuteBot>();
            if (cuteBot != null) Object.DestroyImmediate(cuteBot);

            // Gắn IslandSettler
            var settler = instance.GetComponent<IslandSettler>();
            if (settler == null)
            {
                settler = instance.AddComponent<IslandSettler>();
            }
            settler.walkSpeed = 2.4f;
            settler.runSpeed = 4.2f;
            settler.wanderRadius = 20f;
            settler.themeColor = new Color(0.08f, 0.85f, 1.0f); // Xanh Cyan mặc định

            // Đảm bảo không có Rigidbody
            var rb = instance.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);

            // TỐI ƯU CPU (SNAPDRAGON 810): GỠ BỎ HOÀN TOÀN TẤT CẢ COLLIDER TRÊN DÂN CƯ
            var allColliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < allColliders.Length; i++)
            {
                Object.DestroyImmediate(allColliders[i]);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, targetPrefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log("<color=#00FFAA>Đã tạo Prefab Dân Cư (Zero Collider): " + targetPrefabPath + "</color>");
            return prefab;
        }

        // =========================================================================
        // 4. THIẾT LẬP SCENE: DÂN CƯ, NHÀ, PHÓNG TO PLAYER, XÓA THỢ CHẶT CÂY & GIẢM ĐÁ 20%
        // =========================================================================
        private static void PlaceEntitiesInScene(GameObject housePrefab, GameObject settlerPrefab)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var terrain = Object.FindFirstObjectByType<Terrain>();
            var baseHQ = Object.FindFirstObjectByType<BaseHQ>();

            Vector3 hqPos = (baseHQ != null) ? baseHQ.transform.position : new Vector3(0, 8f, -32f);

            // 1. Phóng to Player ngang hàng dân cư (Scale 1.0)
            EnforcePlayerFullScale();

            // 2. Xóa bỏ hoàn toàn phần người chặt cây (Lumberjacks) không cần nữa
            RemoveLumberjacksFromScene();

            // 3. Giảm độ to các tảng đá bớt đi 20%
            ApplyRockScaleReduction20Percent();

            // Container nhóm cư dân và nhà cửa
            GameObject villageGroup = GameObject.Find("_IslandSettlement");
            if (villageGroup == null)
            {
                villageGroup = new GameObject("_IslandSettlement");
            }

            // Spawn 2 Ngôi Nhà Trú Ẩn (Civilian Houses) gần BaseHQ
            if (housePrefab != null)
            {
                Transform houseGroup = villageGroup.transform.Find("🏠 Nhà Trú Ẩn (Houses)");
                if (houseGroup == null)
                {
                    houseGroup = new GameObject("🏠 Nhà Trú Ẩn (Houses)").transform;
                    houseGroup.SetParent(villageGroup.transform);
                }

                for (int i = houseGroup.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(houseGroup.GetChild(i).gameObject);
                }

                Vector3[] houseOffsets = new Vector3[]
                {
                    new Vector3(-8.5f, 0, 4.0f),
                    new Vector3(8.5f, 0, 4.0f)
                };

                string[] houseNames = new string[] { "Nhà Trú Ẩn Phía Tây", "Nhà Trú Ẩn Phía Đông" };

                for (int i = 0; i < houseOffsets.Length; i++)
                {
                    Vector3 hPos = hqPos + houseOffsets[i];
                    if (terrain != null)
                    {
                        hPos.y = terrain.SampleHeight(hPos) + terrain.transform.position.y;
                    }

                    GameObject hObj = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab, houseGroup);
                    hObj.transform.position = hPos;
                    hObj.transform.rotation = Quaternion.Euler(0, i == 0 ? 30f : -30f, 0);
                    hObj.name = houseNames[i];

                    var hScript = hObj.GetComponent<CivilianHouse>();
                    if (hScript != null) hScript.houseName = houseNames[i];
                }
            }

            // Spawn 4 Dân Cư (Cute Settlers) với màu sắc sặc sỡ khác hẳn Player (Vàng), KHÔNG DÙNG COLLIDER
            if (settlerPrefab != null)
            {
                Transform settlerGroup = villageGroup.transform.Find("🤖 Dân Cư (Settlers)");
                if (settlerGroup == null)
                {
                    settlerGroup = new GameObject("🤖 Dân Cư (Settlers)").transform;
                    settlerGroup.SetParent(villageGroup.transform);
                }

                for (int i = settlerGroup.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(settlerGroup.GetChild(i).gameObject);
                }

                Vector3[] settlerOffsets = new Vector3[]
                {
                    new Vector3(-4.0f, 0, 2.5f),
                    new Vector3(4.0f, 0, 2.5f),
                    new Vector3(-2.0f, 0, -4.5f),
                    new Vector3(2.5f, 0, -4.5f)
                };

                string[] names = new string[] { "Bobi", "Sparky", "Nomi", "Gizmo" };
                Color[] colors = new Color[]
                {
                    new Color(0.05f, 0.85f, 1.0f),  // Xanh Cyan Neon
                    new Color(0.12f, 0.95f, 0.42f), // Xanh Ngọc Lục Bảo
                    new Color(1.0f, 0.22f, 0.62f),  // Hồng Tím Magenta
                    new Color(1.0f, 0.52f, 0.12f)   // Cam San Hô
                };

                for (int i = 0; i < settlerOffsets.Length; i++)
                {
                    Vector3 spawnPos = hqPos + settlerOffsets[i];
                    if (terrain != null)
                    {
                        spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
                    }

                    GameObject sObj = (GameObject)PrefabUtility.InstantiatePrefab(settlerPrefab, settlerGroup);
                    sObj.transform.position = spawnPos;
                    sObj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    sObj.name = $"Dân Cư - {names[i]}";

                    // Gỡ bỏ toàn bộ Collider trên dân cư để tối ưu CPU 100%
                    var sCols = sObj.GetComponentsInChildren<Collider>(true);
                    for (int c = 0; c < sCols.Length; c++)
                    {
                        Object.DestroyImmediate(sCols[c]);
                    }

                    // Gán Material riêng biệt bằng sharedMaterial trong Edit Mode
                    Material sMat = CreateOrGetSettlerMaterial(colors[i], names[i]);
                    Material blackEyesMat = CreateOrGetBlackEyesMaterial();
                    foreach (var r in sObj.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r == null || r.gameObject.name.Contains("Overhead")) continue;

                        if (r.gameObject.name.ToLower().Contains("head") && r.sharedMaterials.Length >= 3)
                        {
                            Material[] hMats = new Material[r.sharedMaterials.Length];
                            for (int m = 0; m < hMats.Length; m++)
                            {
                                hMats[m] = (m == 2) ? blackEyesMat : sMat;
                            }
                            r.sharedMaterials = hMats;
                        }
                        else
                        {
                            r.sharedMaterial = sMat;
                        }
                    }

                    var sScript = sObj.GetComponent<IslandSettler>();
                    if (sScript != null)
                    {
                        sScript.citizenName = names[i];
                        sScript.themeColor = colors[i];
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        /// <summary>
        /// Phóng to kích cỡ Player ngang hàng dân cư (Scale 1.0)
        /// </summary>
        public static void EnforcePlayerFullScale()
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.localScale = Vector3.one;
                if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                }
                Debug.Log("<color=#00FFAA><b>[Player Size]</b> Đã phóng to kích thước Player lên 1.0 (ngang hàng dân cư)!</color>");
            }
        }

        /// <summary>
        /// Xóa bỏ hoàn toàn phần người chặt cây (Lumberjacks) theo yêu cầu
        /// </summary>
        public static void RemoveLumberjacksFromScene()
        {
            GameObject villageGroup = GameObject.Find("_IslandSettlement");
            if (villageGroup != null)
            {
                Transform lumberGroup = villageGroup.transform.Find("🪓 Thợ Chặt Cây (Lumberjacks)");
                if (lumberGroup != null)
                {
                    Object.DestroyImmediate(lumberGroup.gameObject);
                    Debug.Log("<color=#FFAA00><b>[Lumberjacks Removed]</b> Đã xóa bỏ cụm Thợ Chặt Cây khỏi Scene!</color>");
                }
            }

            // Xóa file prefab thợ chặt cây nếu có
            string lumberPrefabPath = "Assets/prefabs/Worker_MechaLumberjack.prefab";
            if (File.Exists(lumberPrefabPath))
            {
                AssetDatabase.DeleteAsset(lumberPrefabPath);
            }
        }

        /// <summary>
        /// Giảm độ to cục đá bớt đi 20% (scale *= 0.8f)
        /// </summary>
        public static void ApplyRockScaleReduction20Percent()
        {
            GameObject rocksRoot = GameObject.Find("🪨 Tảng Đá Rêu (Rocks)");
            if (rocksRoot != null && !EditorPrefs.GetBool("Rocks_Scaled_Minus_20_V2", false))
            {
                int count = 0;
                for (int i = 0; i < rocksRoot.transform.childCount; i++)
                {
                    Transform r = rocksRoot.transform.GetChild(i);
                    r.localScale *= 0.8f;
                    if (PrefabUtility.IsPartOfPrefabInstance(r.gameObject))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                    count++;
                }
                EditorPrefs.SetBool("Rocks_Scaled_Minus_20_V2", true);
                Debug.Log($"<color=#00FFAA><b>[Rock Scale]</b> Đã giảm 20% kích cỡ của {count} tảng đá trên đảo!</color>");
            }
        }

        private static Material CreateOrGetSettlerMaterial(Color col, string name)
        {
            string folder = "Assets/Materials";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string path = $"{folder}/Settler_Mat_{name}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                             ?? Shader.Find("Universal Render Pipeline/Simple Lit") 
                             ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = $"Settler_Mat_{name}";
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = col;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * 0.45f);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetBlackEyesMaterial()
        {
            string folder = "Assets/Materials";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string path = $"{folder}/Settler_Mat_BlackEyes.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                             ?? Shader.Find("Universal Render Pipeline/Simple Lit") 
                             ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "Settler_Mat_BlackEyes";
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = Color.black;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.black);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.DisableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
