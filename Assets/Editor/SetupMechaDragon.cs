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
                InspectDragon();
                SetupDragon();
            };
        }

        public static void InspectDragon()
        {
            string fbxPath = "Assets/Models/MechaDragon/source/MechaDragon.fbx";
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj == null) return;

            foreach (var smr in fbxObj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Debug.Log($"<color=magenta>[Dragon SMR]</color> {smr.name}, rootBone: {(smr.rootBone != null ? smr.rootBone.name : "null")}, bones: {smr.bones.Length}, mats: {smr.sharedMaterials.Length}");
                for (int i = 0; i < smr.sharedMaterials.Length; i++)
                {
                    Debug.Log($"   Material [{i}]: {(smr.sharedMaterials[i] != null ? smr.sharedMaterials[i].name : "null")}");
                }
            }
            foreach (var mr in fbxObj.GetComponentsInChildren<MeshRenderer>(true))
            {
                Debug.Log($"<color=yellow>[Dragon MR]</color> {mr.name}, parent: {(mr.transform.parent != null ? mr.transform.parent.name : "null")}");
            }

            // Kiểm tra các clips và curves
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && clip.name.ToLower().Contains("attack"))
                {
                    Debug.Log($"<color=cyan>[Dragon Clip]</color> {clip.name}, length: {clip.length}s, events: {clip.events.Length}");
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    foreach (var b in bindings)
                    {
                        if (b.path.ToLower().Contains("eye") || b.propertyName.ToLower().Contains("eye"))
                        {
                            Debug.Log($"   Binding: path={b.path}, prop={b.propertyName}");
                        }
                    }
                }
            }

            // Kiểm tra toàn bộ hierarchy các con của FBX
            Transform[] allT = fbxObj.GetComponentsInChildren<Transform>(true);
            foreach (var t in allT)
            {
                if (t.name.ToLower().Contains("eye") || t.name.ToLower().Contains("head") || t.name.ToLower().Contains("face"))
                {
                    Debug.Log($"<color=orange>[Dragon Bone/Part]</color> {t.name}, parent: {(t.parent != null ? t.parent.name : "null")}");
                }
            }
        }

        [MenuItem("Tools/🐉 Đặt 1 Quái Rồng Cơ Khí Vào Scene")]
        public static void SpawnDragonInScene()
        {
            SetupDragon();

            string prefabPath = "Assets/prefabs/Enemy_MechaDragon.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Xác định vị trí xuất hiện đẹp nhất trên chiến trường
            Vector3 spawnPos = new Vector3(3.5f, 15.5f, -10f);

            Enemy existingEnemy = Object.FindFirstObjectByType<Enemy>();
            if (existingEnemy != null && existingEnemy.name != "Enemy_MechaDragon")
            {
                spawnPos = existingEnemy.transform.position + new Vector3(0f, 4f, 6f);
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
                if (player != null)
                {
                    spawnPos = player.transform.position + new Vector3(0f, 4.5f, 22f);
                }
            }

            if (Terrain.activeTerrain != null)
            {
                float terrY = Terrain.activeTerrain.SampleHeight(spawnPos) + Terrain.activeTerrain.transform.position.y;
                spawnPos.y = Mathf.Max(spawnPos.y, terrY + 4f);
            }

            // Xóa instance cũ nếu đã tồn tại trong Scene
            GameObject oldDragon = GameObject.Find("Enemy_MechaDragon");
            if (oldDragon != null)
            {
                Object.DestroyImmediate(oldDragon);
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Enemy_MechaDragon";
            instance.transform.position = spawnPos;
            instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Mecha Dragon");
            Selection.activeGameObject = instance;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"<color=#00FFFF><b>[Mecha Dragon]</b> ĐÃ ĐẶT QUÁI RỒNG CƠ KHÍ VÀO SCENE TẠI: {spawnPos} VÀ ZOOM CAMERA VÀO RỒNG!</color>");
        }

        public static void SetupDragon()
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

            // 1. Cấu hình ModelImporter cho FBX
            FixDragonImporter(fbxPath);

            // 2. Tạo bộ Material PBR Kim Loại / Cơ Khí Chuẩn URP
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Thân chính: Hợp kim Titan Carbon xám thép sẫm
            Material matMain = CreatePbrMaterial(matDir, "MechaDragon_Main", urpShader,
                new Color(0.20f, 0.22f, 0.26f, 1f), metallic: 0.90f, smoothness: 0.75f);

            // Bụng: Giáp thép gia cố mạ vàng đồng Steampunk Brass
            Material matBelly = CreatePbrMaterial(matDir, "MechaDragon_Belly", urpShader,
                new Color(0.85f, 0.65f, 0.25f, 1f), metallic: 0.95f, smoothness: 0.82f);

            // Cánh: Khung hợp kim siêu nhẹ carbon bọc viền thép
            Material matWings = CreatePbrMaterial(matDir, "MechaDragon_Wings", urpShader,
                new Color(0.14f, 0.16f, 0.20f, 1f), metallic: 0.85f, smoothness: 0.68f);

            // Móng vuốt & Sừng: Crom mạ bóng sáng loáng sắc bén
            Material matClaws = CreatePbrMaterial(matDir, "MechaDragon_Claws", urpShader,
                new Color(0.92f, 0.94f, 0.96f, 1f), metallic: 0.96f, smoothness: 0.90f);

            // Mắt cảm biến: Mắt quang học Laser Neon Cyan phát sáng rực rỡ
            Material matEyes = CreatePbrMaterial(matDir, "MechaDragon_Eyes", urpShader,
                new Color(0f, 0.95f, 1f, 1f), metallic: 0.5f, smoothness: 0.9f,
                emissionColor: new Color(0f, 1f, 1f) * 4.0f);

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

            // Đổi sang Attack 2 (Dragon_Attack2 - Đòn vươn mình gầm thét quẫy cánh khạc lửa dữ dội 40 frames)
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
                fromAtk.exitTime = 0.88f; // Thời gian chuyển tiếp mượt mà cho Attack 2 (40 frames)
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

            // 5. Khởi tạo Game Object và Xây dựng Prefab Quái Rồng Cơ Khí
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = "Enemy_MechaDragon";
                instance.transform.localScale = new Vector3(2.4f, 2.4f, 2.4f);

                instance.tag = "Enemy";
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) instance.layer = enemyLayer;

                Renderer[] rends = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (enemyLayer >= 0) r.gameObject.layer = enemyLayer;
                    r.gameObject.tag = "Enemy";

                    Material[] sharedMats = r.sharedMaterials;
                    for (int i = 0; i < sharedMats.Length; i++)
                    {
                        string matName = sharedMats[i] != null ? sharedMats[i].name.ToLower() : "";
                        if (matName.Contains("belly")) sharedMats[i] = matBelly;
                        else if (matName.Contains("wing")) sharedMats[i] = matWings;
                        else if (matName.Contains("claw")) sharedMats[i] = matClaws;
                        else if (matName.Contains("eye")) sharedMats[i] = matEyes;
                        else sharedMats[i] = matMain;
                    }
                    r.sharedMaterials = sharedMats;
                }

                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                if (avatar != null) anim.avatar = avatar;

                // ================= FIX TRIỆT ĐỂ LỖI MẮT RỒNG ĐI MỘT MÌNH MỘT HƯỚNG =================
                // Trong file FBX gốc, EyeArmature và SkinnedMesh Eyes nằm ở Root chứ không được parent vào xương Head.
                // Khi Rồng cử động đầu cắn/gầm (Attack/Attack2), đầu cúi xuống lao đi nhưng mắt lại đứng im ở Root!
                // Gắn EyeArmature và Eyes làm con của Head ngay tại Rest Pose để mắt luôn dính chặt vào hốc mắt 100%!
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
                    if (eyeArmature != null)
                    {
                        eyeArmature.SetParent(headBone, true);
                    }
                    if (eyesMeshObj != null)
                    {
                        eyesMeshObj.SetParent(headBone, true);
                        var smr = eyesMeshObj.GetComponent<SkinnedMeshRenderer>();
                        if (smr != null) smr.updateWhenOffscreen = true;
                    }
                }

                // Sample Flying animation để trong Editor rồng dang rộng cánh bay lượn uy dũng
                if (clipFlying != null)
                {
                    clipFlying.SampleAnimation(instance, 0.45f);
                }

                // Collider bao trọn thân rồng
                CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
                if (col == null) col = instance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 1.2f, 0);
                col.radius = 0.9f;
                col.height = 2.4f;
                col.direction = 2; // Trục Z

                // Nguồn sáng lõi năng lượng phát sáng dịu quanh ngực rồng
                GameObject glowCore = new GameObject("Cyber_EnergyGlow");
                glowCore.transform.SetParent(instance.transform, false);
                glowCore.transform.localPosition = new Vector3(0f, 1.2f, 0.4f);
                Light coreLight = glowCore.AddComponent<Light>();
                coreLight.type = LightType.Point;
                coreLight.color = new Color(0f, 0.95f, 1f);
                coreLight.range = 4.5f;
                coreLight.intensity = 2.2f;

                // Script Enemy
                Enemy enemyComp = instance.GetComponent<Enemy>();
                if (enemyComp == null) enemyComp = instance.AddComponent<Enemy>();
                enemyComp.animator = anim;
                enemyComp.enemyName = "Mecha Cyber Dragon";
                enemyComp.maxHealth = 260f;
                enemyComp.currentHealth = 260f;
                enemyComp.moveSpeed = 3.6f;
                enemyComp.goldReward = 85;
                enemyComp.attackDamage = 35f;
                enemyComp.attackRange = 5.5f;
                enemyComp.attackCooldown = 1.5f;
                enemyComp.attackDelay = 0.5f;
                enemyComp.modelRotationOffset = 0f;

                enemyComp.isFlying = true;
                enemyComp.flightAltitude = 3.5f;
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

                // TUÂN THỦ RULE 8: KHÔNG dùng Rigidbody
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null) Object.DestroyImmediate(rb);

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
                    Transform sHead = null, sEyeArm = null, sEyes = null;
                    foreach (var t in sceneDragon.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Equals("Head", System.StringComparison.OrdinalIgnoreCase)) sHead = t;
                        else if (t.name.Equals("EyeArmature", System.StringComparison.OrdinalIgnoreCase)) sEyeArm = t;
                        else if (t.name.Equals("Eyes", System.StringComparison.OrdinalIgnoreCase)) sEyes = t;
                    }
                    if (sHead != null)
                    {
                        if (sEyeArm != null && sEyeArm.parent != sHead) sEyeArm.SetParent(sHead, true);
                        if (sEyes != null && sEyes.parent != sHead) sEyes.SetParent(sHead, true);
                    }
                    Animator sAnim = sceneDragon.GetComponent<Animator>();
                    if (sAnim != null) sAnim.runtimeAnimatorController = controller;

                    Enemy sEnemy = sceneDragon.GetComponent<Enemy>();
                    if (sEnemy != null && plasmaPrefab != null)
                    {
                        sEnemy.projectilePrefab = plasmaPrefab;
                    }

                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=#00FFFF><b>[Mecha Dragon]</b> ĐÃ CẬP NHẬT HOÀN THIỆN PREFAB QUÁI RỒNG CƠ KHÍ TẠI: " + prefabPath + "!</color>");
            }
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
