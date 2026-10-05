#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RestorePhaotuhanh
{
    static RestorePhaotuhanh()
    {
        EditorApplication.delayCall += () =>
        {
            ExecuteRestore();
        };
    }

    [MenuItem("Tools/🚀 Khôi Phục Pháo Tự Hành MLRS & SampleScene")]
    public static void ExecuteRestore()
    {
        // 1. Mở SampleScene nếu chưa mở
        string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != scenePath)
        {
            currentScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        bool sceneDirty = false;

        // 2. Khôi phục Phaotuhanhl (Pháo tự hành tên lửa MLRS)
        GameObject phaoObj = GameObject.Find("Phaotuhanhl");
        if (phaoObj == null)
        {
            GameObject missileModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gun/Missile Turret/MissileTurret.fbx");
            if (missileModel != null)
            {
                phaoObj = (GameObject)PrefabUtility.InstantiatePrefab(missileModel);
                phaoObj.name = "Phaotuhanhl";
                phaoObj.transform.position = new Vector3(-6.5f, 9.1f, -7.8f);
                phaoObj.transform.rotation = Quaternion.identity;

                RocketLauncherTurret launcher = phaoObj.AddComponent<RocketLauncherTurret>();
                launcher.rocketPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/PerfectRocket.prefab");
                launcher.rotationSpeed = 10f;
                launcher.fireCooldown = 2f;
                launcher.rocketsPerVolley = 16;
                launcher.attackRange = 30f;
                launcher.enemyTag = "Enemy";
                launcher.enemyLayer = ~0;

                // Tự tìm turretHead và bệ phóng
                Transform[] allChildren = phaoObj.GetComponentsInChildren<Transform>();
                foreach (var t in allChildren)
                {
                    string n = t.name.ToLower();
                    if (launcher.turretHead == null && n.Contains("head"))
                    {
                        launcher.turretHead = t;
                    }
                    if (launcher.leftPod == null && (n.Contains("left") || n.Contains("pod1") || n.Contains("pod_l")))
                    {
                        launcher.leftPod = t;
                    }
                    if (launcher.rightPod == null && (n.Contains("right") || n.Contains("pod2") || n.Contains("pod_r")))
                    {
                        launcher.rightPod = t;
                    }
                }

                if (launcher.turretHead == null) launcher.turretHead = phaoObj.transform;
                if (launcher.leftPod == null) launcher.leftPod = phaoObj.transform;
                if (launcher.rightPod == null) launcher.rightPod = phaoObj.transform;

                sceneDirty = true;
                Debug.Log("<color=green>[Khôi Phục]</color> Đã khôi phục thành công Pháo Tự Hành MLRS (Phaotuhanhl) vào Scene!");
            }
        }

        // 3. Khôi phục PlasmaTurret nếu thiếu
        GameObject plasmaObj = GameObject.Find("PlasmaTurret");
        if (plasmaObj == null)
        {
            GameObject plasmaModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gun/Plasma Turret/PlasmaTurret.fbx");
            if (plasmaModel != null)
            {
                plasmaObj = (GameObject)PrefabUtility.InstantiatePrefab(plasmaModel);
                plasmaObj.name = "PlasmaTurret";
                plasmaObj.transform.position = new Vector3(-0.045f, 11.05f, -35.7f);
                plasmaObj.transform.rotation = Quaternion.identity;
                sceneDirty = true;
                Debug.Log("<color=green>[Khôi Phục]</color> Đã khôi phục thành công PlasmaTurret vào Scene!");
            }
        }

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);
            Debug.Log("<color=cyan>[Đã Lưu]</color> SampleScene đã được lưu an toàn với đầy đủ các ụ pháo!");
        }
    }
}
#endif
