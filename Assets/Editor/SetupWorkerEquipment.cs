#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupWorkerEquipment
    {
        private const string PICKAXE_OBJ_PATH = "Assets/Models/Pickaxe/Pickaxe.obj";
        private const string SKIP_ROCKS_FBX = "Assets/Map/Models/FBX format/skip-rocks.fbx";
        private const string SKIP_FBX = "Assets/Map/Models/FBX format/skip.fbx";
        private const string SPACE_MAT_PATH = "Assets/Map/Models/Materials/SpaceStation_Mat.mat";
        private const string BIPED_PREFAB_PATH = "Assets/Models/BipedRobot/BipedRobot_Prefab.prefab";

        static SetupWorkerEquipment()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SetupEquipment();
            };
        }

        [MenuItem("Tools/⛏️ Gắn Cúp & Giỏ Cho BipedRobot (Mining Bot)")]
        public static void SetupEquipment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            // 1. Nạp Material SpaceStation
            Material spaceMat = AssetDatabase.LoadAssetAtPath<Material>(SPACE_MAT_PATH);
            if (spaceMat == null)
            {
                SetupSpaceStationKit.SetupMaterials();
                spaceMat = AssetDatabase.LoadAssetAtPath<Material>(SPACE_MAT_PATH);
            }

            // 2. Nạp Mô Hình Cúp (Pickaxe)
            GameObject pickaxeModel = AssetDatabase.LoadAssetAtPath<GameObject>(PICKAXE_OBJ_PATH);
            if (pickaxeModel == null)
            {
                Debug.LogError("[WorkerEquipment] Không tìm thấy: " + PICKAXE_OBJ_PATH);
                return;
            }

            // 3. Nạp Mô Hình Giỏ (Skip-Rocks)
            GameObject basketModel = AssetDatabase.LoadAssetAtPath<GameObject>(SKIP_ROCKS_FBX);
            if (basketModel == null)
            {
                basketModel = AssetDatabase.LoadAssetAtPath<GameObject>(SKIP_FBX);
            }
            if (basketModel == null)
            {
                Debug.LogError("[WorkerEquipment] Không tìm thấy giỏ skip-rocks hoặc skip trong Assets/Map!");
                return;
            }

            // 4. Gắn vào BipedRobot trong Scene hiện tại
            SetupSceneRobot(pickaxeModel, basketModel, spaceMat);

            // 5. Gắn vào BipedRobot Prefab trong Project
            SetupPrefabRobot(pickaxeModel, basketModel, spaceMat);

            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00FF88><b>[WorkerEquipment]</b> ĐÃ DỌN DẸP TRÙNG LẶP & GẮN CÚP + GIỎ CHO ROBOT THÀNH CÔNG!</color>");
        }

        private static void SetupSceneRobot(GameObject pickaxeModel, GameObject basketModel, Material spaceMat)
        {
            // Tìm tất cả robot tiềm năng trong Scene (BipedRobot_Prefab hoặc đối tượng có WorkerBot / Bip001 Pelvis)
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            System.Collections.Generic.List<Transform> robots = new System.Collections.Generic.List<Transform>();

            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (t.name == "BipedRobot_Prefab" || t.GetComponent<WorkerBot>() != null)
                {
                    if (!robots.Contains(t)) robots.Add(t);
                }
                else if (t.name == "Bip001 Pelvis" && t.parent != null)
                {
                    if (!robots.Contains(t.parent)) robots.Add(t.parent);
                }
            }

            // Tìm và gắn mục tiêu tảng đá thực tế (rocks (1) hoặc rocks)
            GameObject targetRockObj = GameObject.Find("rocks (1)") ?? GameObject.Find("rocks");
            if (targetRockObj != null)
            {
                Collider col = targetRockObj.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider bc = targetRockObj.AddComponent<BoxCollider>();
                    bc.size = new Vector3(1.8f, 1.2f, 1.8f);
                    bc.center = new Vector3(0f, 0.6f, 0f);
                }
            }

            foreach (var robot in robots)
            {
                // Đảm bảo có WorkerBot
                WorkerBot worker = robot.GetComponent<WorkerBot>();
                if (worker == null) worker = robot.gameObject.AddComponent<WorkerBot>();

                if (targetRockObj != null)
                {
                    worker.targetRock = targetRockObj.transform;
                    worker.stopDistance = 1.3f;
                    worker.mineInterval = 1.2f;
                    worker.moveSpeed = 2.5f;
                    EditorUtility.SetDirty(worker);
                }

                AttachEquipments(robot, pickaxeModel, basketModel, spaceMat);
                EditorUtility.SetDirty(robot.gameObject);
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }
        }

        private static void SetupPrefabRobot(GameObject pickaxeModel, GameObject basketModel, Material spaceMat)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(BIPED_PREFAB_PATH);
            if (prefabRoot == null) return;

            try
            {
                AttachEquipments(prefabRoot.transform, pickaxeModel, basketModel, spaceMat);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, BIPED_PREFAB_PATH);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void AttachEquipments(Transform robotRoot, GameObject pickaxeModel, GameObject basketModel, Material spaceMat)
        {
            // XÓA TRIỆT ĐỂ TOÀN BỘ CÚP VÀ GIỎ CŨ (tránh bị trùng lặp nhiều giỏ như hình ảnh)
            System.Collections.Generic.List<GameObject> oldItems = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in robotRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child != robotRoot)
                {
                    if (child.name.StartsWith("Mining_Basket") || child.name.StartsWith("Mining_Pickaxe"))
                    {
                        oldItems.Add(child.gameObject);
                    }
                }
            }
            foreach (var g in oldItems)
            {
                Object.DestroyImmediate(g);
            }

            // 1. Gắn CÚP (Pickaxe) vào tay phải Bip001 R Hand
            Transform rHand = FindChildRecursive(robotRoot, "Bip001 R Hand");
            if (rHand != null)
            {
                GameObject pickaxe = Object.Instantiate(pickaxeModel);
                pickaxe.name = "Mining_Pickaxe";
                pickaxe.transform.SetParent(rHand, false);

                // Gán Material SpaceStation
                foreach (Renderer r in pickaxe.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = spaceMat;
                }

                // Cân chỉnh vị trí, góc xoay và tỉ lệ lòng bàn tay Robot
                pickaxe.transform.localPosition = new Vector3(-0.06f, 0.05f, 0.02f);
                pickaxe.transform.localRotation = Quaternion.Euler(75f, -95f, 15f);
                pickaxe.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            }

            // 2. Gắn GIỎ ĐỰNG ĐÁ (Mining Basket) vào lưng Bip001 Spine1
            Transform spine1 = FindChildRecursive(robotRoot, "Bip001 Spine1");
            if (spine1 == null) spine1 = FindChildRecursive(robotRoot, "Bip001 Spine");

            if (spine1 != null)
            {
                GameObject basket = Object.Instantiate(basketModel);
                basket.name = "Mining_Basket";
                basket.transform.SetParent(spine1, false);

                foreach (Renderer r in basket.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = spaceMat;
                }

                // Cân chỉnh vị trí, góc xoay và tỉ lệ đeo sau lưng Robot
                basket.transform.localPosition = new Vector3(-0.18f, -0.22f, 0f);
                basket.transform.localRotation = Quaternion.Euler(0f, 90f, -75f);
                basket.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            }
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
