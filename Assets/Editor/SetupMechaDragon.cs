#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    [InitializeOnLoad]
    public static class SetupMechaDragon
    {
        static SetupMechaDragon()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                CleanFriendlyDragonRemnants();
                SetupEnemyBossMechaDragon();
            };
        }

        [MenuItem("Tools/🐉 Cài Đặt Boss Rồng Cơ Khí (Mecha Cyber Dragon [BOSS])")]
        public static void MenuSetupBossDragon()
        {
            CleanFriendlyDragonRemnants();
            SetupEnemyBossMechaDragon();
        }

        [MenuItem("Tools/⚡ Triệu Hồi Boss Rồng Cơ Khí Vào Trận Đấu")]
        public static void MenuSpawnBossInScene()
        {
            CleanFriendlyDragonRemnants();
            SetupEnemyBossMechaDragon();
            SpawnBossInScene();
        }

        public static void CleanFriendlyDragonRemnants()
        {
            // 1. Quét và dọn sạch các bản rồng thân thiện và rồng thừa trong Scene (chỉ giữ đúng 1 Boss)
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int cleaned = 0;
            GameObject keptBoss = null;
            foreach (var go in allObjects)
            {
                if (go == null) continue;
                string n = go.name.ToLower();
                if (n.Contains("friendly_mechadragon") || n.Contains("friendlymecha"))
                {
                    Undo.DestroyObjectImmediate(go);
                    cleaned++;
                }
                else if (n.Contains("enemy_mechadragon") || n.Equals("enemy_mechadragon"))
                {
                    if (keptBoss == null)
                    {
                        keptBoss = go;
                    }
                    else
                    {
                        Undo.DestroyObjectImmediate(go);
                        cleaned++;
                    }
                }
            }
            if (cleaned > 0)
            {
                Debug.Log($"<color=#00FF88>🧹 Đã dọn dẹp sạch {cleaned} bản Rồng trùng lặp, chỉ giữ duy nhất 1 Boss Rồng trong Scene!</color>");
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            // 2. Xóa prefab Friendly_MechaDragon nếu còn tồn tại
            if (File.Exists("Assets/prefabs/Friendly_MechaDragon.prefab"))
            {
                AssetDatabase.DeleteAsset("Assets/prefabs/Friendly_MechaDragon.prefab");
            }
        }

        public static void SetupEnemyBossMechaDragon()
        {
            string rootPath = "Assets/Models/MechaDragon";
            string fbxPath = Path.Combine(rootPath, "source/MechaDragon.fbx").Replace("\\", "/");
            string matDir = Path.Combine(rootPath, "Materials").Replace("\\", "/");
            string controllerPath = Path.Combine(rootPath, "MechaDragon_Controller.controller").Replace("\\", "/");
            string prefabPath = "Assets/prefabs/Enemy_MechaDragon.prefab";

            if (!File.Exists(fbxPath))
            {
                Debug.LogError("[SetupMechaDragon] Không tìm thấy file FBX: " + fbxPath);
                return;
            }

            EnsureDirectory(matDir);
            EnsureDirectory("Assets/prefabs");
            if (Directory.Exists("Assets/prefabsEnemy"))
            {
                EnsureDirectory("Assets/prefabsEnemy");
            }

            // 1. Cấu hình ModelImporter cho FBX
            FixDragonImporter(fbxPath);

            // 2. Tạo bộ Material PBR Kim Loại / Cơ Khí Chuẩn URP (Tone màu Hắc Kim Cyber Boss cực ngầu)
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Thân chính: Hợp kim Titan xám sẫm công nghệ cao
            Material matMain = CreatePbrMaterial(matDir, "MechaDragon_Main", urpShader,
                new Color(0.18f, 0.20f, 0.24f, 1f), metallic: 0.92f, smoothness: 0.78f);

            // Bụng: Giáp cường lực mạ đồng Steampunk Brass
            Material matBelly = CreatePbrMaterial(matDir, "MechaDragon_Belly", urpShader,
                new Color(0.85f, 0.62f, 0.22f, 1f), metallic: 0.95f, smoothness: 0.82f);

            // Cánh: Khung composite siêu nhẹ bọc viền thép
            Material matWings = CreatePbrMaterial(matDir, "MechaDragon_Wings", urpShader,
                new Color(0.14f, 0.16f, 0.20f, 1f), metallic: 0.88f, smoothness: 0.70f);

            // Móng vuốt & Sừng: Crom sắc bén phản quang
            Material matClaws = CreatePbrMaterial(matDir, "MechaDragon_Claws", urpShader,
                new Color(0.92f, 0.94f, 0.96f, 1f), metallic: 0.96f, smoothness: 0.90f);

            // Mắt Boss: Laser Neon Đỏ / Xanh Plasma rực lửa chết chóc
            Material matEyes = CreatePbrMaterial(matDir, "MechaDragon_Eyes", urpShader,
                new Color(0f, 0.95f, 1f, 1f), metallic: 0.5f, smoothness: 0.9f,
                emissionColor: new Color(0f, 1f, 1f) * 4.5f);

            // 3. Tải Animation Clips & Avatar từ FBX
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip clipFlying = null;
            AnimationClip clipAttack1 = null;
            AnimationClip clipAttack2 = null;
            AnimationClip clipHit = null;
            AnimationClip clipDeath = null;
            Avatar avatar = null;

            foreach (var a in allAssets)
            {
                if (a is Avatar av) avatar = av;
                else if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string cName = clip.name.ToLower();
                    if (cName.Contains("flying") || cName.Contains("fly") || cName.Contains("walk")) clipFlying = clip;
                    else if (cName.Contains("attack2")) clipAttack2 = clip;
                    else if (cName.Contains("attack")) clipAttack1 = clip;
                    else if (cName.Contains("hit")) clipHit = clip;
                    else if (cName.Contains("death") || cName.Contains("die")) clipDeath = clip;
                }
            }

            // Đòn Attack 2 (Dragon_Attack2 - Đòn vươn mình gầm thét quẫy cánh khạc đạn plasma)
            AnimationClip chosenAttackClip = clipAttack2 != null ? clipAttack2 : clipAttack1;

            // 4. Cấu hình Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            if (clipFlying != null)
            {
                while (controller.layers.Length > 0) controller.RemoveLayer(0);
                controller.AddLayer("Base Layer");
                controller.parameters = new AnimatorControllerParameter[0];

                controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
                controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);
                controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

                var sm = controller.layers[0].stateMachine;

                var stFly = sm.AddState("Flying");
                stFly.motion = clipFlying;
                sm.defaultState = stFly;

                var stAttack = sm.AddState("Attack");
                stAttack.motion = chosenAttackClip;

                var toAtk = sm.AddAnyStateTransition(stAttack);
                toAtk.hasExitTime = false;
                toAtk.duration = 0.05f;
                toAtk.AddCondition(AnimatorConditionMode.If, 0, "attack");

                var fromAtk = stAttack.AddTransition(stFly);
                fromAtk.hasExitTime = true;
                fromAtk.exitTime = 0.88f;
                fromAtk.duration = 0.15f;

                var stDie = sm.AddState("Die");
                stDie.motion = clipDeath;

                var toDie = sm.AddAnyStateTransition(stDie);
                toDie.hasExitTime = false;
                toDie.duration = 0.05f;
                toDie.AddCondition(AnimatorConditionMode.If, 0, "die");

                if (clipHit != null)
                {
                    var stHit = sm.AddState("Hit");
                    stHit.motion = clipHit;

                    var toHit = sm.AddAnyStateTransition(stHit);
                    toHit.hasExitTime = false;
                    toHit.duration = 0.05f;
                    toHit.AddCondition(AnimatorConditionMode.If, 0, "hit");

                    var fromHit = stHit.AddTransition(stFly);
                    fromHit.hasExitTime = true;
                    fromHit.exitTime = 0.7f;
                    fromHit.duration = 0.1f;
                }
                EditorUtility.SetDirty(controller);
            }

            // 5. Khởi tạo Game Object và Xây dựng Prefab Boss Rồng Cơ Khí (ENEMY BOSS)
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                if (PrefabUtility.IsPartOfPrefabInstance(instance))
                {
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }
                instance.name = "Enemy_MechaDragon";
                instance.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

                // THUỘC PHE ENEMY (BOSS)
                instance.tag = "Enemy";
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) instance.layer = enemyLayer;

                Renderer[] rends = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (enemyLayer >= 0) r.gameObject.layer = enemyLayer;
                    Material[] mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string mName = r.sharedMaterials[i] != null ? r.sharedMaterials[i].name.ToLower() : "";
                        if (mName.Contains("belly")) mats[i] = matBelly;
                        else if (mName.Contains("wing")) mats[i] = matWings;
                        else if (mName.Contains("claw") || mName.Contains("horn")) mats[i] = matClaws;
                        else if (mName.Contains("eye")) mats[i] = matEyes;
                        else mats[i] = matMain;
                    }
                    r.sharedMaterials = mats;
                }

                // Animator
                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                if (avatar != null) anim.avatar = avatar;
                anim.applyRootMotion = false;

                // Cố định mắt rồng vào xương Head (khắc phục triệt để lỗi mắt bay lệch của model FBX gốc)
                Transform headBone = null;
                Transform eyeArmature = null;
                Transform eyesMeshObj = null;

                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase)) headBone = t;
                    else if (t.name.Equals("EyeArmature", System.StringComparison.OrdinalIgnoreCase)) eyeArmature = t;
                    else if (t.name.Equals("Eyes", System.StringComparison.OrdinalIgnoreCase)) eyesMeshObj = t;
                }

                if (headBone != null)
                {
                    if (eyeArmature != null) eyeArmature.SetParent(headBone, true);
                    if (eyesMeshObj != null)
                    {
                        eyesMeshObj.SetParent(headBone, true);
                        var smr = eyesMeshObj.GetComponent<SkinnedMeshRenderer>();
                        if (smr != null) smr.updateWhenOffscreen = true;
                    }
                }

                if (clipFlying != null)
                {
                    clipFlying.SampleAnimation(instance, 0.45f);
                }

                // Collider
                CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
                if (col == null) col = instance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 1.2f, 0);
                col.radius = 1.0f;
                col.height = 2.6f;
                col.direction = 2; // Trục Z

                // Nguồn sáng lõi năng lượng phát sáng quanh thân rồng
                GameObject glowCore = new GameObject("Cyber_EnergyGlow");
                glowCore.transform.SetParent(instance.transform, false);
                glowCore.transform.localPosition = new Vector3(0, 1.2f, 0.3f);
                Light coreLight = glowCore.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.color = new Color(0f, 0.95f, 1f);
                coreLight.range = 5.0f;
                coreLight.intensity = 2.5f;

                // GẮN SCRIPT ENEMY (BOSS CHÍNH THỨC CỦA PHE QUÁI VẬT)
                Enemy enemyComp = instance.GetComponent<Enemy>();
                if (enemyComp == null) enemyComp = instance.AddComponent<Enemy>();
                enemyComp.animator = anim;
                enemyComp.enemyName = "Mecha Cyber Dragon [BOSS]";
                enemyComp.maxHealth = 1200f;
                enemyComp.currentHealth = 1200f;
                enemyComp.moveSpeed = 3.8f;
                enemyComp.goldReward = 250;
                enemyComp.attackDamage = 45f;
                enemyComp.attackRange = 7.0f;
                enemyComp.attackCooldown = 1.6f;
                enemyComp.attackDelay = 0.5f;
                enemyComp.modelRotationOffset = 0f;

                enemyComp.isFlying = true;
                enemyComp.flightAltitude = 3.8f;
                enemyComp.hoverBobSpeed = 2.4f;
                enemyComp.hoverBobAmount = 0.35f;

                GameObject explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/PerfectExplosion.prefab");
                if (explosionPrefab == null)
                    explosionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabsEnemy/PerfectExplosion.prefab");
                if (explosionPrefab != null)
                {
                    enemyComp.deathVFX = explosionPrefab;
                }

                GameObject plasmaPrefab = CreateDragonPlasmaBallPrefab();
                if (plasmaPrefab != null)
                {
                    enemyComp.projectilePrefab = plasmaPrefab;
                }

                // TUÂN THỦ RULE 8: KHÔNG dùng Rigidbody (trọng lực code)
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null) Object.DestroyImmediate(rb);

                // Lưu Prefab vào cả 2 thư mục để tương thích toàn dự án
                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (Directory.Exists("Assets/prefabsEnemy"))
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, "Assets/prefabsEnemy/Enemy_MechaDragon.prefab");
                }
                Object.DestroyImmediate(instance);

                // Đồng bộ cập nhật cho con Rồng trong Scene nếu đang tồn tại
                GameObject sceneDragon = GameObject.Find("Enemy_MechaDragon");
                if (sceneDragon != null)
                {
                    sceneDragon.tag = "Enemy";
                    if (enemyLayer >= 0) sceneDragon.layer = enemyLayer;
                    Enemy sEnemy = sceneDragon.GetComponent<Enemy>();
                    if (sEnemy != null && plasmaPrefab != null)
                    {
                        sEnemy.projectilePrefab = plasmaPrefab;
                    }
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=#FF0055><b>[BOSS MECHA DRAGON]</b> ĐÃ THIẾT LẬP HOÀN TẤT PREFAB BOSS RỒNG CƠ KHÍ TẠI: " + prefabPath + " (TAG: ENEMY)!</color>");
            }
        }

        public static void SpawnBossInScene()
        {
            string prefabPath = "Assets/prefabs/Enemy_MechaDragon.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                SetupEnemyBossMechaDragon();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            if (prefab == null) return;

            // Xóa boss cũ trong scene nếu có
            GameObject oldBoss = GameObject.Find("Enemy_MechaDragon");
            if (oldBoss != null)
            {
                Undo.DestroyObjectImmediate(oldBoss);
            }

            // Vị trí xuất hiện: Trên không trung, phía sau làn quái vật (xa căn cứ)
            Vector3 spawnPos = new Vector3(0f, 18f, 25f);
            GameObject baseHQ = GameObject.FindGameObjectWithTag("Base") ?? GameObject.Find("Base") ?? GameObject.Find("BaseHQ");
            if (baseHQ != null)
            {
                spawnPos = baseHQ.transform.position + new Vector3(0f, 5.0f, 32f);
            }

            if (Terrain.activeTerrain != null)
            {
                float terrY = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;
                spawnPos.y = Mathf.Max(spawnPos.y, terrY + 4.5f);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Enemy_MechaDragon";
            instance.transform.position = spawnPos;
            instance.transform.rotation = Quaternion.Euler(0, 180, 0); // Quay mặt về phía căn cứ

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Boss Mecha Dragon");
            Selection.activeGameObject = instance;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#FF3366><b>[BOSS MECHA DRAGON]</b> ĐÃ TRIỆU HỒI BOSS RỒNG CƠ KHÍ TẠI: {spawnPos} (MÁU 1200, ENEMY BOSS)!</color>");
        }

        public static GameObject CreateDragonPlasmaBallPrefab()
        {
            string prefabDir = Directory.Exists("Assets/prefabsEnemy") ? "Assets/prefabsEnemy" : "Assets/prefabs";
            string ballPath = $"{prefabDir}/Dragon_PlasmaBall.prefab";

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ballPath);
            if (existing != null) return existing;

            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Dragon_PlasmaBall";
            ball.transform.localScale = Vector3.one * 0.7f;

            Collider col = ball.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(urpShader);
            mat.name = "Dragon_PlasmaBall_Mat";
            Color cyanColor = new Color(0f, 0.95f, 1f, 1f);
            mat.color = cyanColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cyanColor);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", cyanColor * 4.5f);

            string matDir = "Assets/Models/MechaDragon/Materials";
            if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);
            string matPath = $"{matDir}/Dragon_PlasmaBall_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);

            Renderer ren = ball.GetComponent<Renderer>();
            if (ren != null) ren.sharedMaterial = mat;

            Light pl = ball.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = cyanColor;
            pl.intensity = 2.5f;
            pl.range = 5f;

            TrailRenderer tr = ball.AddComponent<TrailRenderer>();
            tr.time = 0.25f;
            tr.startWidth = 0.5f;
            tr.endWidth = 0.05f;
            tr.material = mat;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(cyanColor, 0f), new GradientColorKey(new Color(0f, 0.4f, 1f), 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            tr.colorGradient = grad;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(ball, ballPath);
            Object.DestroyImmediate(ball);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00FFFF><b>[Mecha Dragon]</b> Đã tạo Prefab Quả Cầu Năng Lượng tại: {ballPath}</color>");
            return savedPrefab;
        }

        // ================= HÀM TIỆN ÍCH =================
        private static void FixDragonImporter(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.animationType != ModelImporterAnimationType.Generic)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    changed = true;
                }
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    changed = true;
                }
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

                var clips = importer.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    foreach (var c in clips)
                    {
                        if (c.name.ToLower().Contains("fly"))
                        {
                            c.loopTime = true;
                            c.loopPose = true;
                            changed = true;
                        }
                    }
                    importer.clipAnimations = clips;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Material CreatePbrMaterial(string matDir, string matName, Shader shader, Color baseColor, float metallic, float smoothness, Color? emissionColor = null)
        {
            string assetPath = Path.Combine(matDir, matName + ".mat").Replace("\\", "/");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emissionColor.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor.Value);
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
