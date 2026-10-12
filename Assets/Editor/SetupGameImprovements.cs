using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RobotDefense.Editor
{
    public static class SetupGameImprovements
    {
        [InitializeOnLoadMethod]
        [MenuItem("Tools/🚀 Cập Nhật Âm Thanh Pháo, Scale Nhà Máy 2X, Nút Chế Tạo & Thanh Máu")]
        public static void ApplyAllImprovements()
        {
            Debug.Log("<color=cyan><b>[SetupGameImprovements]</b> Bắt đầu cập nhật toàn bộ theo yêu cầu...</color>");

            // 1. Refresh AssetDatabase để nạp âm thanh mới
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            AudioClip fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/phongphaotuhanh.wav")
                              ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/phongphaotuhanh.mp3");

            if (fireClip != null)
            {
                Debug.Log($"<color=#00FF88>[SetupGameImprovements]</color> Đã tìm thấy file âm thanh pháo tự hành: {AssetDatabase.GetAssetPath(fireClip)}");
            }
            else
            {
                Debug.LogWarning("[SetupGameImprovements] Chưa tìm thấy AudioClip tại Assets/Audio/phongphaotuhanh.wav!");
            }

            // 2. Gắn Audio vào Prefab Phaotuhanhl.prefab
            string phaoPrefabPath = "Assets/Gun/prefabsGun/Phaotuhanhl.prefab";
            GameObject phaoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(phaoPrefabPath);
            if (phaoPrefab != null)
            {
                RocketLauncherTurret launcher = phaoPrefab.GetComponent<RocketLauncherTurret>();
                if (launcher != null)
                {
                    AudioSource aSource = phaoPrefab.GetComponent<AudioSource>();
                    if (aSource == null) aSource = phaoPrefab.AddComponent<AudioSource>();
                    aSource.playOnAwake = false;
                    aSource.loop = false;
                    aSource.spatialBlend = 0.5f;
                    aSource.minDistance = 5f;
                    aSource.maxDistance = 50f;

                    launcher.audioSource = aSource;
                    if (fireClip != null) launcher.fireSound = fireClip;

                    EditorUtility.SetDirty(phaoPrefab);
                    PrefabUtility.SavePrefabAsset(phaoPrefab);
                    Debug.Log("<color=#00FF88>✅ [SetupGameImprovements]</color> Đã gắn AudioSource & fireSound vào Phaotuhanhl.prefab!");
                }
            }

            // 3. Tăng kích thước Prefab Nhà Máy Robot lên gấp đôi (2x)
            string factoryPrefabPath = "Assets/prefabs/RobotFactory_Building.prefab";
            GameObject factoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(factoryPrefabPath);
            if (factoryPrefab != null)
            {
                factoryPrefab.transform.localScale = Vector3.one * 2f;
                EditorUtility.SetDirty(factoryPrefab);
                PrefabUtility.SavePrefabAsset(factoryPrefab);
                Debug.Log("<color=#00FF88>✅ [SetupGameImprovements]</color> Đã tăng kích thước RobotFactory_Building.prefab lên gấp đôi (scale 2.0)!");
            }

            // 4. Mở SampleScene và cập nhật các đối tượng trong Scene
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path != scenePath)
            {
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // Cập nhật Nhà máy trong Scene
            GameObject factorySceneObj = GameObject.Find("RobotFactory_Building");
            if (factorySceneObj != null)
            {
                factorySceneObj.transform.localScale = Vector3.one * 2f;
                // Căn chỉnh độ cao mặt đất nếu có Terrain
                if (Terrain.activeTerrain != null)
                {
                    Vector3 p = factorySceneObj.transform.position;
                    p.y = Terrain.activeTerrain.SampleHeight(p) + Terrain.activeTerrain.transform.position.y;
                    factorySceneObj.transform.position = p;
                }
                EditorUtility.SetDirty(factorySceneObj);
                Debug.Log("<color=#00FF88>✅ [SetupGameImprovements]</color> Đã phóng to Nhà Máy Robot trong Scene lên gấp đôi (scale 2.0)!");
            }

            // Cập nhật Pháo Tự Hành trong Scene nếu có
            RocketLauncherTurret[] launchersInScene = Object.FindObjectsByType<RocketLauncherTurret>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in launchersInScene)
            {
                if (l != null)
                {
                    AudioSource aSource = l.GetComponent<AudioSource>();
                    if (aSource == null) aSource = l.gameObject.AddComponent<AudioSource>();
                    aSource.playOnAwake = false;
                    aSource.loop = false;
                    aSource.spatialBlend = 0.5f;

                    l.audioSource = aSource;
                    if (fireClip != null) l.fireSound = fireClip;
                    EditorUtility.SetDirty(l.gameObject);
                }
            }

            // 5. Cập nhật Virtual Joystick ở giữa màn game
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject joyZone = VirtualJoystick.CreateJoystickHierarchy(canvas.gameObject);
                if (joyZone != null)
                {
                    VirtualJoystick vj = joyZone.GetComponent<VirtualJoystick>();
                    PlayerController player = Object.FindAnyObjectByType<PlayerController>();
                    if (player != null && vj != null)
                    {
                        player.joystick = vj;
                        EditorUtility.SetDirty(player);
                    }
                    EditorUtility.SetDirty(canvas.gameObject);
                    Debug.Log("<color=#00FF88>✅ [SetupGameImprovements]</color> Đã tạo Joystick di chuyển ở giữa màn game và liên kết thành công với Player!");
                }
            }

            // 6. Cập nhật giảm giá tiền mua vũ khí trong BuildingSystem và BuildingShopUI
            BuildingSystem bs = Object.FindAnyObjectByType<BuildingSystem>();
            if (bs != null && bs.items != null)
            {
                if (bs.items.Length > 0 && bs.items[0] != null) bs.items[0].cost = 150;
                if (bs.items.Length > 1 && bs.items[1] != null) bs.items[1].cost = 250;
                if (bs.items.Length > 2 && bs.items[2] != null) bs.items[2].cost = 300;
                if (bs.items.Length > 3 && bs.items[3] != null) bs.items[3].cost = 25;
                EditorUtility.SetDirty(bs);
            }

            BuildingShopUI shopUI = Object.FindAnyObjectByType<BuildingShopUI>();
            if (shopUI != null)
            {
                shopUI.ApplyReducedWeaponCosts();
                EditorUtility.SetDirty(shopUI.gameObject);
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00FF88><b>🎉 [SetupGameImprovements]</b> HOÀN TẤT 100% CẬP NHẬT: Sinh quái random ban ngày, Giảm giá vũ khí, Đứng game khi vỡ Lõi Năng Lượng & Nút Chơi Lại!</color>");
        }
    }
}
