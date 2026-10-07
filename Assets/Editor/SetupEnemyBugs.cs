#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    public static class SetupEnemyBugs
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                // Tự động kiểm tra và cài đặt nếu chưa có prefab
                if (!File.Exists("Assets/prefabs/Enemy_AlienBug.prefab") || !File.Exists("Assets/prefabs/Enemy_Spider.prefab"))
                {
                    SetupAllBugs();
                }
            };
        }

        // [MenuItem("Tools/🐛 Cài Đặt Quái Bọ 4 Chân (AlienBug & Spider)")]
        public static void SetupAllBugs()
        {
            SetupAlienBug();
            SetupSpider();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Enemy Setup]</b> ĐÃ CÀI ĐẶT THÀNH CÔNG 2 QUÁI BỌ 4 CHÂN: ALIEN BUG VÀ SPIDER!</color>");
        }

        // ================= 1. ALIEN BUG (BỌ 4 CHÂN NGOÀI HÀNH TINH) =================
        // [MenuItem("Tools/Enemies/1. Setup Alien Bug")]
        public static void SetupAlienBug()
        {
            string rootPath = "Assets/Models/EnemyBug";
            string fbxPath = Path.Combine(rootPath, "source/AlienBug.fbx").Replace("\\", "/");
            string texDir = Path.Combine(rootPath, "textures").Replace("\\", "/");
            string matDir = Path.Combine(rootPath, "Materials").Replace("\\", "/");
            string controllerPath = Path.Combine(rootPath, "AlienBug_Controller.controller").Replace("\\", "/");
            string prefabPath = "Assets/prefabs/Enemy_AlienBug.prefab";

            if (!File.Exists(fbxPath))
            {
                Debug.LogError("[SetupAlienBug] Không tìm thấy: " + fbxPath);
                return;
            }

            EnsureDirectory(matDir);
            EnsureDirectory("Assets/prefabs");

            // Importer
            FixModelImporter(fbxPath);

            // Material URP
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = CreateOrGetMaterial(matDir, "AlienBug_Mat", urpShader, texDir, "bug_diffuse.png", "bug_normal.png", "bug_roughness.png");

            // Animation Clips
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip clipIdle = null;
            AnimationClip clipRun = null;
            AnimationClip clipAttack = null;
            Avatar avatar = null;

            foreach (var a in allAssets)
            {
                if (a is Avatar av) avatar = av;
                else if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string cName = clip.name.ToLower();
                    if (cName.Contains("idle")) clipIdle = clip;
                    else if (cName.Contains("run")) clipRun = clip;
                    else if (cName.Contains("attack")) clipAttack = clip;
                }
            }

            // Tạo Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            while (controller.layers.Length > 0) controller.RemoveLayer(0);
            controller.AddLayer("Base Layer");
            controller.parameters = new AnimatorControllerParameter[0];

            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var stIdle = sm.AddState("Idle");
            stIdle.motion = clipIdle;

            var stRun = sm.AddState("Run");
            stRun.motion = clipRun;

            var stAttack = sm.AddState("Attack");
            stAttack.motion = clipAttack;

            sm.defaultState = stIdle;

            // Idle -> Run
            var toRun = stIdle.AddTransition(stRun);
            toRun.hasExitTime = false;
            toRun.duration = 0.1f;
            toRun.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Run -> Idle
            var toIdle = stRun.AddTransition(stIdle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

            // AnyState -> Attack
            var toAtk = sm.AddAnyStateTransition(stAttack);
            toAtk.hasExitTime = false;
            toAtk.duration = 0.05f;
            toAtk.AddCondition(AnimatorConditionMode.If, 0, "attack");

            var fromAtk = stAttack.AddTransition(stIdle);
            fromAtk.hasExitTime = true;
            fromAtk.exitTime = 0.85f;
            fromAtk.duration = 0.1f;

            EditorUtility.SetDirty(controller);

            // Tạo Prefab
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "Enemy_AlienBug";
                instance.transform.localScale = Vector3.one;

                // Gán Tag và Layer Enemy
                instance.tag = "Enemy";
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) instance.layer = enemyLayer;

                // Gán Material
                Renderer[] rends = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.sharedMaterial = mat;
                    if (enemyLayer >= 0) r.gameObject.layer = enemyLayer;
                    r.gameObject.tag = "Enemy";
                }

                // Animator
                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                if (avatar != null) anim.avatar = avatar;

                // Collider
                CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
                if (col == null) col = instance.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.4f, 0);
                col.radius = 0.45f;
                col.height = 1.0f;

                // Rigidbody
                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb == null) rb = instance.AddComponent<Rigidbody>();
                rb.mass = 5f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                // Script Enemy
                Enemy enemyComp = instance.GetComponent<Enemy>();
                if (enemyComp == null) enemyComp = instance.AddComponent<Enemy>();
                enemyComp.enemyName = "Alien Bug";
                enemyComp.maxHealth = 40f;
                enemyComp.moveSpeed = 3.2f;
                enemyComp.goldReward = 12;
                enemyComp.attackDamage = 8f;
                enemyComp.attackRange = 1.5f;

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ AlienBug Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= 2. SPIDER (NHỆN CƠ KHÍ 4 CHÂN) =================
        // [MenuItem("Tools/Enemies/2. Setup Spider")]
        public static void SetupSpider()
        {
            string rootPath = "Assets/Models/EnemySpider";
            string fbxPath = Path.Combine(rootPath, "source/Spider.fbx").Replace("\\", "/");
            string texDir = Path.Combine(rootPath, "textures").Replace("\\", "/");
            string matDir = Path.Combine(rootPath, "Materials").Replace("\\", "/");
            string controllerPath = Path.Combine(rootPath, "Spider_Controller.controller").Replace("\\", "/");
            string prefabPath = "Assets/prefabs/Enemy_Spider.prefab";

            if (!File.Exists(fbxPath))
            {
                Debug.LogError("[SetupSpider] Không tìm thấy: " + fbxPath);
                return;
            }

            EnsureDirectory(matDir);
            EnsureDirectory("Assets/prefabs");

            FixModelImporter(fbxPath);

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = CreateOrGetMaterial(matDir, "Spider_Mat", urpShader, texDir, "spider_diffuse.png", null, null);

            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            AnimationClip clipIdle = null;
            AnimationClip clipWalk = null;
            AnimationClip clipAttack = null;
            AnimationClip clipDie = null;
            Avatar avatar = null;

            foreach (var a in allAssets)
            {
                if (a is Avatar av) avatar = av;
                else if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string cName = clip.name.ToLower();
                    if (cName.Contains("neutral") || cName.Contains("idle")) clipIdle = clip;
                    else if (cName.Contains("walk")) clipWalk = clip;
                    else if (cName.Contains("attack")) clipAttack = clip;
                    else if (cName.Contains("die")) clipDie = clip;
                }
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            while (controller.layers.Length > 0) controller.RemoveLayer(0);
            controller.AddLayer("Base Layer");
            controller.parameters = new AnimatorControllerParameter[0];

            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var stIdle = sm.AddState("Idle");
            stIdle.motion = clipIdle;

            var stWalk = sm.AddState("Walk");
            stWalk.motion = clipWalk;

            var stAttack = sm.AddState("Attack");
            stAttack.motion = clipAttack;

            var stDie = sm.AddState("Die");
            stDie.motion = clipDie;

            sm.defaultState = stIdle;

            // Idle <-> Walk
            var toWalk = stIdle.AddTransition(stWalk);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.1f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            var toIdle = stWalk.AddTransition(stIdle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

            // Attack
            var toAtk = sm.AddAnyStateTransition(stAttack);
            toAtk.hasExitTime = false;
            toAtk.duration = 0.05f;
            toAtk.AddCondition(AnimatorConditionMode.If, 0, "attack");

            var fromAtk = stAttack.AddTransition(stIdle);
            fromAtk.hasExitTime = true;
            fromAtk.exitTime = 0.85f;
            fromAtk.duration = 0.1f;

            // Die
            var toDie = sm.AddAnyStateTransition(stDie);
            toDie.hasExitTime = false;
            toDie.duration = 0.05f;
            toDie.AddCondition(AnimatorConditionMode.If, 0, "die");

            EditorUtility.SetDirty(controller);

            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "Enemy_Spider";
                instance.transform.localScale = Vector3.one;

                instance.tag = "Enemy";
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                if (enemyLayer >= 0) instance.layer = enemyLayer;

                Renderer[] rends = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.sharedMaterial = mat;
                    if (enemyLayer >= 0) r.gameObject.layer = enemyLayer;
                    r.gameObject.tag = "Enemy";
                }

                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                if (avatar != null) anim.avatar = avatar;

                SphereCollider col = instance.GetComponent<SphereCollider>();
                if (col == null) col = instance.AddComponent<SphereCollider>();
                col.center = new Vector3(0, 0.4f, 0);
                col.radius = 0.5f;

                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb == null) rb = instance.AddComponent<Rigidbody>();
                rb.mass = 8f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                Enemy enemyComp = instance.GetComponent<Enemy>();
                if (enemyComp == null) enemyComp = instance.AddComponent<Enemy>();
                enemyComp.enemyName = "Scrap Spider";
                enemyComp.maxHealth = 65f;
                enemyComp.moveSpeed = 2.4f;
                enemyComp.goldReward = 20;
                enemyComp.attackDamage = 15f;
                enemyComp.attackRange = 1.6f;

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
                Debug.Log("<color=#00FF88>✅ Spider Prefab đã sẵn sàng tại: " + prefabPath + "</color>");
            }
        }

        // ================= TIỆN ÍCH CHUNG =================
        private static void FixModelImporter(string fbxPath)
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

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Material CreateOrGetMaterial(string matDir, string matName, Shader shader, string texDir, string baseColorFile, string normalFile, string roughFile)
        {
            string assetPath = Path.Combine(matDir, matName + ".mat").Replace("\\", "/");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, assetPath);
            }

            if (!string.IsNullOrEmpty(baseColorFile))
            {
                string baseColorPath = Path.Combine(texDir, baseColorFile).Replace("\\", "/");
                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
                if (baseTex != null)
                {
                    mat.SetTexture("_BaseMap", baseTex);
                    mat.SetTexture("_MainTex", baseTex);
                }
            }

            if (!string.IsNullOrEmpty(normalFile))
            {
                string normalPath = Path.Combine(texDir, normalFile).Replace("\\", "/");
                TextureImporter texImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (texImporter != null && texImporter.textureType != TextureImporterType.NormalMap)
                {
                    texImporter.textureType = TextureImporterType.NormalMap;
                    texImporter.SaveAndReimport();
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normalTex != null)
                {
                    mat.EnableKeyword("_NORMALMAP");
                    mat.SetTexture("_BumpMap", normalTex);
                }
            }

            if (!string.IsNullOrEmpty(roughFile))
            {
                string roughPath = Path.Combine(texDir, roughFile).Replace("\\", "/");
                Texture2D roughTex = AssetDatabase.LoadAssetAtPath<Texture2D>(roughPath);
                if (roughTex != null)
                {
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetTexture("_MetallicGlossMap", roughTex);
                }
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
