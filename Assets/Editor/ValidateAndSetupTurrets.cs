#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ValidateAndSetupTurrets
{
    static ValidateAndSetupTurrets()
    {
        EditorApplication.delayCall += () =>
        {
            SetupTurretsInSceneAndPrefabs();
        };
    }

    [MenuItem("Tools/⚡ Thiết Lập & Nâng Cấp Turrets (Thunder, Pháo, Flamethrower, Tường)")]
    public static void SetupTurretsInSceneAndPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; bool sceneDirty = false;

        // 1. Quét thiết lập trong Scene đang mở
        Scene scene = EditorSceneManager.GetActiveScene();

        // Đảm bảo Canvas có WallSelectionManager
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas != null && canvas.GetComponent<WallSelectionManager>() == null)
        {
            canvas.gameObject.AddComponent<WallSelectionManager>();
            sceneDirty = true;
        }

        // Tìm Thunder, Phaotuhanhl, FlamethrowerTurret
        UpgradableTurret.AutoSetupAllTurretsInScene();

        UpgradableTurret[] allTurrets = Object.FindObjectsByType<UpgradableTurret>(FindObjectsInactive.Include);
        foreach (var t in allTurrets)
        {
            t.EnsureColliderExists();
            sceneDirty = true;
            Debug.Log($"<color=green>[Turret Setup]</color> Đã kích hoạt tính năng nâng cấp cho: {t.name} ({t.turretDisplayName})!");
        }

        // Đảm bảo tất cả WallSegment có collider
        WallSegment[] allWalls = Object.FindObjectsByType<WallSegment>(FindObjectsInactive.Include);
        foreach (var w in allWalls)
        {
            if (w.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider bc = w.gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(2.85f, 3.5f, 1f);
                bc.center = new Vector3(0f, 1.75f, 0f);
                sceneDirty = true;
            }
        }

        // Đảm bảo tất cả TurretController và TurretWeapon được liên kết hoàn chỉnh
        TurretController[] allControllers = Object.FindObjectsByType<TurretController>(FindObjectsInactive.Include);
        foreach (var tc in allControllers)
        {
            if (tc.weapon == null) tc.weapon = tc.GetComponent<TurretWeapon>() ?? tc.GetComponentInChildren<TurretWeapon>();
            if (tc.headTransform == null) tc.headTransform = tc.transform.Find("rotator");
            sceneDirty = true;
        }

        TurretWeapon[] allWeapons = Object.FindObjectsByType<TurretWeapon>(FindObjectsInactive.Include);
        foreach (var tw in allWeapons)
        {
            if (tw.firePoint == null) tw.firePoint = tw.transform.Find("rotator/firepoint");
            if (tw.bulletPrefab == null) tw.bulletPrefab = AssetDatabase.LoadAssetAtPath<Bullet>("Assets/prefabsbot/Cube (1).prefab");
            sceneDirty = true;
        }

        // Đảm bảo Player trong Scene có Tag Player
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null && !player.CompareTag("Player"))
        {
            player.tag = "Player";
            sceneDirty = true;
        }

        // 2. Thiết lập trên Prefabs
        SetupPrefabTurret("Assets/prefabsbot/FlamethrowerTurret_Building.prefab", TurretType.FlamethrowerTurret);
        SetupPrefabTurret("Assets/Gun/Missile Turret/Phaotuhanhl.prefab", TurretType.Phaotuhanh);

        if (sceneDirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=cyan>[Đã Lưu Scene]</color> Hoàn tất cài đặt tính năng nâng cấp và nhấp nháy xanh dương cho Thunder, Phaotuhanh, FlamethrowerTurret và Tường!");
        }
    }

    private static void SetupPrefabTurret(string prefabPath, TurretType type)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        bool modified = false;

        UpgradableTurret ut = instance.GetComponent<UpgradableTurret>();
        if (ut == null)
        {
            ut = instance.AddComponent<UpgradableTurret>();
            ut.turretType = type;
            modified = true;
        }

        if (instance.GetComponentInChildren<Collider>() == null)
        {
            ut.EnsureColliderExists();
            modified = true;
        }

        if (modified)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log($"<color=green>[Prefab Setup]</color> Đã cập nhật UpgradableTurret và Collider cho prefab: {prefabPath}");
        }

        PrefabUtility.UnloadPrefabContents(instance);
    }
}
#endif
