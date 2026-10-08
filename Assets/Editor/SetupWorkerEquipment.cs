#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupWorkerEquipment
    {
        private const string BIPED_PREFAB_PATH = "Assets/Models/BipedRobot/BipedRobot_Prefab.prefab";
        private const string PREFABSBOT_PATH = "Assets/prefabsbot/BipedRobot_Prefab.prefab";
        private const string SETUP_KEY = "Setup_MineBotFarm_V1";

        static SetupWorkerEquipment()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!EditorPrefs.GetBool(SETUP_KEY, false))
                {
                    EditorPrefs.SetBool(SETUP_KEY, true);
                    SetupMineBotFarm();
                }
            };
        }

        [MenuItem("Tools/⛏️ Gắn Script mineBotFarm Vào BipedRobot")]
        public static void SetupMineBotFarm()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("<color=yellow>[mineBotFarm] Bắt đầu gắn script mineBotFarm vào BipedRobot...</color>");

            // Tìm mục tiêu tảng đá trong Scene
            GameObject targetRockObj = GameObject.Find("rocks (1)") ?? GameObject.Find("Rock_D") ?? GameObject.Find("rocks") ?? GameObject.Find("Rock_06_A_LOD0");

            // 1. Gắn vào BipedRobot trong Scene hiện tại
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t == null) continue;
                if (t.name == "BipedRobot_Prefab" || (t.name.StartsWith("BipedRobot") && t.parent == null))
                {
                    // Đảm bảo không còn CharacterController hay Rigidbody nào
                    var cc = t.GetComponent<CharacterController>();
                    if (cc != null) Object.DestroyImmediate(cc, true);

                    var rb = t.GetComponent<Rigidbody>();
                    if (rb != null) Object.DestroyImmediate(rb, true);

                    // Gắn mineBotFarm
                    var farm = t.GetComponent<mineBotFarm>();
                    if (farm == null) farm = t.gameObject.AddComponent<mineBotFarm>();

                    farm.moveSpeed = 3.0f;
                    farm.stopDistance = 1.4f;
                    farm.gravity = -9.18f;
                    farm.targetTag = "Rock";
                    farm.mineInterval = 1.2f;
                    if (targetRockObj != null) farm.targetRock = targetRockObj.transform;

                    Vector3 pos = t.position;
                    pos.y = 10.5f;
                    t.position = pos;

                    EditorUtility.SetDirty(t.gameObject);
                }
            }

            // 2. Gắn vào 2 Prefab
            string[] prefabPaths = new string[] { BIPED_PREFAB_PATH, PREFABSBOT_PATH };
            foreach (var path in prefabPaths)
            {
                if (!File.Exists(path)) continue;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null) continue;

                try
                {
                    var cc = prefabRoot.GetComponent<CharacterController>();
                    if (cc != null) Object.DestroyImmediate(cc, true);

                    var rb = prefabRoot.GetComponent<Rigidbody>();
                    if (rb != null) Object.DestroyImmediate(rb, true);

                    var farm = prefabRoot.GetComponent<mineBotFarm>();
                    if (farm == null) farm = prefabRoot.AddComponent<mineBotFarm>();

                    farm.moveSpeed = 3.0f;
                    farm.stopDistance = 1.4f;
                    farm.gravity = -9.18f;
                    farm.targetTag = "Rock";
                    farm.mineInterval = 1.2f;

                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            // 3. Lưu Scene và AssetDatabase
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00FF88><b>[mineBotFarm]</b> ĐÃ GẮN SCRIPT mineBotFarm VÀO BIPEDROBOT THÀNH CÔNG (DÙNG RAYCAST + TRỌNG LỰC -9.18, ZERO RIGIDBODY)!</color>");
        }
    }
}
#endif
