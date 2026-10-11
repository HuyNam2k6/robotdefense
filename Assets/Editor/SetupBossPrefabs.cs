#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupBossPrefabs
    {
        [MenuItem("Tools/👾 Tạo Và Cấu Hình 4 Loại Boss Khác Nhau (Boss Prefabs)")]
        [InitializeOnLoadMethod]
        public static void SetupAllBosses()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                ExecuteBossSetup();
            };
        }

        public static void ExecuteBossSetup()
        {
            if (!Directory.Exists("Assets/prefabsEnemy"))
            {
                Directory.CreateDirectory("Assets/prefabsEnemy");
            }

            CreateHeavyTitanBoss();
            CreateSteampunkWarlordBoss();
            CreateSpiderQueenBoss();
            CreateToxicBehemothBoss();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Boss Setup]</b> ĐÃ TẠO VÀ CẬP NHẬT HOÀN CHỈNH 5 LOẠI BOSS TRONG Assets/prefabsEnemy/ (KHÔNG DÙNG RIGIDBODY)!</color>");
        }

        private static void CreateHeavyTitanBoss()
        {
            string sourcePath = "Assets/Models/HeavyRobot/HeavyRobot_Prefab.prefab";
            string targetPath = "Assets/prefabsEnemy/Boss_HeavyTitan.prefab";

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning("[Boss Setup] Không tìm thấy: " + sourcePath);
                return;
            }

            GameObject go = Object.Instantiate(source);
            go.name = "Boss_HeavyTitan";
            go.tag = "Enemy";

            // RULE: Xóa Rigidbody
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);

            go.transform.localScale = Vector3.one * 3.5f;

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) enemy = go.AddComponent<Enemy>();
            enemy.enemyName = "Heavy Titan Colossus";
            enemy.maxHealth = 450f;
            enemy.currentHealth = 450f;
            enemy.moveSpeed = 2.2f;
            enemy.attackDamage = 60f;
            enemy.attackRange = 3.5f;
            enemy.attackCooldown = 1.8f;
            enemy.goldReward = 120;
            enemy.showHealthBar = true;
            enemy.healthBarHeight = 5.2f;

            PrefabUtility.SaveAsPrefabAsset(go, targetPath);
            Object.DestroyImmediate(go);
            Debug.Log("<color=#00FFAA>Đã tạo Boss: " + targetPath + "</color>");
        }

        private static void CreateSteampunkWarlordBoss()
        {
            string sourcePath = "Assets/Models/CombatSteampunkRobot/CombatSteampunkRobot_Prefabs.prefab";
            string targetPath = "Assets/prefabsEnemy/Boss_SteampunkWarlord.prefab";

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning("[Boss Setup] Không tìm thấy: " + sourcePath);
                return;
            }

            GameObject go = Object.Instantiate(source);
            go.name = "Boss_SteampunkWarlord";
            go.tag = "Enemy";

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);

            go.transform.localScale = Vector3.one * 3.2f;

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) enemy = go.AddComponent<Enemy>();
            enemy.enemyName = "Steampunk Warlord";
            enemy.maxHealth = 400f;
            enemy.currentHealth = 400f;
            enemy.moveSpeed = 2.8f;
            enemy.attackDamage = 50f;
            enemy.attackRange = 3.2f;
            enemy.attackCooldown = 1.4f;
            enemy.goldReward = 100;
            enemy.showHealthBar = true;
            enemy.healthBarHeight = 4.8f;

            PrefabUtility.SaveAsPrefabAsset(go, targetPath);
            Object.DestroyImmediate(go);
            Debug.Log("<color=#00FFAA>Đã tạo Boss: " + targetPath + "</color>");
        }

        private static void CreateSpiderQueenBoss()
        {
            string sourcePath = "Assets/prefabsEnemy/Enemy_Spider.prefab";
            string targetPath = "Assets/prefabsEnemy/Boss_SpiderQueen.prefab";

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning("[Boss Setup] Không tìm thấy: " + sourcePath);
                return;
            }

            GameObject go = Object.Instantiate(source);
            go.name = "Boss_SpiderQueen";
            go.tag = "Enemy";

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);

            go.transform.localScale = Vector3.one * 2.2f;

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) enemy = go.AddComponent<Enemy>();
            enemy.enemyName = "Chúa Nhện Broodmother";
            enemy.maxHealth = 380f;
            enemy.currentHealth = 380f;
            enemy.moveSpeed = 3.4f;
            enemy.attackDamage = 45f;
            enemy.attackRange = 2.8f;
            enemy.attackCooldown = 1.2f;
            enemy.goldReward = 90;
            enemy.showHealthBar = true;
            enemy.healthBarHeight = 3.5f;

            PrefabUtility.SaveAsPrefabAsset(go, targetPath);
            Object.DestroyImmediate(go);
            Debug.Log("<color=#00FFAA>Đã tạo Boss: " + targetPath + "</color>");
        }

        private static void CreateToxicBehemothBoss()
        {
            string sourcePath = "Assets/prefabsEnemy/Enemy_AlienBug.prefab";
            string targetPath = "Assets/prefabsEnemy/Boss_ToxicBehemoth.prefab";

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
            {
                Debug.LogWarning("[Boss Setup] Không tìm thấy: " + sourcePath);
                return;
            }

            GameObject go = Object.Instantiate(source);
            go.name = "Boss_ToxicBehemoth";
            go.tag = "Enemy";

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Object.DestroyImmediate(rb);

            go.transform.localScale = Vector3.one * 2.8f;

            Enemy enemy = go.GetComponent<Enemy>();
            if (enemy == null) enemy = go.AddComponent<Enemy>();
            enemy.enemyName = "Đại Bọ Gai Toxic Behemoth";
            enemy.maxHealth = 420f;
            enemy.currentHealth = 420f;
            enemy.moveSpeed = 3.0f;
            enemy.attackDamage = 55f;
            enemy.attackRange = 3.2f;
            enemy.attackCooldown = 1.3f;
            enemy.goldReward = 110;
            enemy.showHealthBar = true;
            enemy.healthBarHeight = 4.0f;

            PrefabUtility.SaveAsPrefabAsset(go, targetPath);
            Object.DestroyImmediate(go);
            Debug.Log("<color=#00FFAA>Đã tạo Boss: " + targetPath + "</color>");
        }
    }
}
#endif

