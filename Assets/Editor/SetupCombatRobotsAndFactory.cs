#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace RobotDefense.Editor
{
    public static class SetupCombatRobotsAndFactory
    {
        [MenuItem("Tools/🤖 Cài Đặt 4 Robot Chiến Đấu & Nhà Máy Sản Xuất")]
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

            // 1. Cấu hình Animation Importer cho 4 FBX
            FixFbxClips("Assets/Models/NewMech/Stan.fbx");
            FixFbxClips("Assets/Models/NewMech/Mike.fbx");
            FixFbxClips("Assets/Models/NewMech/George.fbx");
            FixFbxClips("Assets/Models/NewMech/Leela.fbx");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Tạo Materials URP
            Material matStan = CreateOrGetUrpMaterial("Assets/Models/NewMech/Materials", "Stan_Mat", "Assets/Models/NewMech/Textures/Stan_Texture.png");
            Material matMike = CreateOrGetUrpMaterial("Assets/Models/NewMech/Materials", "Mike_Mat", "Assets/Models/NewMech/Textures/Mike_Texture.png");
            Material matGeorge = CreateOrGetUrpMaterial("Assets/Models/NewMech/Materials", "George_Mat", "Assets/Models/NewMech/Textures/George_Texture.png");
            Material matLeela = CreateOrGetUrpMaterial("Assets/Models/NewMech/Materials", "Leela_Mat", "Assets/Models/NewMech/Textures/Leela_Texture.png");
            Material matFactory = CreateOrGetUrpMaterial("Assets/Models/FactoryKit/Models/Materials", "Factory_Mat", "Assets/Models/FactoryKit/Models/FBX format/Textures/colormap.png");

            // 3. Tạo Animator Controller chung cho Robot Chiến Đấu
            AnimatorController robotController = CreateCombatRobotAnimatorController();

            // 4. Tạo Prefab cho 4 Robot Chiến Đấu
            GameObject prefabStan = CreateRobotPrefab("Stan", "Assets/Models/NewMech/Stan.fbx", matStan, robotController, FriendlyCombatRobot.RobotClass.Stan_Artillery);
            GameObject prefabMike = CreateRobotPrefab("Mike", "Assets/Models/NewMech/Mike.fbx", matMike, robotController, FriendlyCombatRobot.RobotClass.Mike_Brawler);
            GameObject prefabGeorge = CreateRobotPrefab("George", "Assets/Models/NewMech/George.fbx", matGeorge, robotController, FriendlyCombatRobot.RobotClass.George_Assassin);
            GameObject prefabLeela = CreateRobotPrefab("Leela", "Assets/Models/NewMech/Leela.fbx", matLeela, robotController, FriendlyCombatRobot.RobotClass.Leela_Sniper);

            // 5. Ghép và Tạo Prefab Nhà Máy Sản Xuất Robot
            GameObject[] combatPrefabs = new GameObject[] { prefabStan, prefabMike, prefabGeorge, prefabLeela };
            GameObject factoryPrefab = CreateFactoryPrefab(combatPrefabs, matFactory);

            // 6. Đặt 1 Nhà máy vào SampleScene
            PlaceFactoryInScene(factoryPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Robot Setup]</b> ĐÃ HOÀN TẤT THIẾT LẬP 4 ROBOT CHIẾN ĐẤU VÀ NHÀ MÁY SẢN XUẤT!</color>");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists("Assets/Models/NewMech/Materials")) Directory.CreateDirectory("Assets/Models/NewMech/Materials");
            if (!Directory.Exists("Assets/Models/FactoryKit/Models/Materials")) Directory.CreateDirectory("Assets/Models/FactoryKit/Models/Materials");
            if (!Directory.Exists("Assets/prefabs")) Directory.CreateDirectory("Assets/prefabs");
        }

        private static void FixFbxClips(string fbxPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips == null || clips.Length == 0) return;

            List<ModelImporterClipAnimation> configuredClips = new List<ModelImporterClipAnimation>();

            foreach (var clip in clips)
            {
                string take = clip.name;
                string lower = take.ToLower();

                ModelImporterClipAnimation c = new ModelImporterClipAnimation
                {
                    name = take,
                    takeName = clip.takeName,
                    firstFrame = clip.firstFrame,
                    lastFrame = clip.lastFrame,
                    wrapMode = clip.wrapMode
                };

                if (lower.Contains("idle") || lower.Contains("walk") || lower.Contains("run"))
                {
                    c.loopTime = true;
                    c.loopPose = true;
                }
                else
                {
                    c.loopTime = false;
                }

                configuredClips.Add(c);
            }

            importer.clipAnimations = configuredClips.ToArray();
            importer.SaveAndReimport();
        }

        private static Material CreateOrGetUrpMaterial(string folder, string matName, string texPath)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string matPath = $"{folder}/{matName}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit") 
                         ?? Shader.Find("Universal Render Pipeline/Lit") 
                         ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else if (mat.shader != shader && shader != null)
            {
                mat.shader = shader;
            }

            if (File.Exists(texPath))
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null)
                {
                    mat.mainTexture = tex;
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
                }
            }

            return mat;
        }

        private static AnimatorController CreateCombatRobotAnimatorController()
        {
            string controllerPath = "Assets/Models/NewMech/FriendlyRobot_Controller.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            // Xóa sạch layers cũ để tạo chuẩn
            while (controller.layers.Length > 0) controller.RemoveLayer(0);
            controller.AddLayer("Base Layer");

            // Thêm Parameters
            controller.parameters = new AnimatorControllerParameter[0];
            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("attackType", AnimatorControllerParameterType.Int);
            controller.AddParameter("attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("victory", AnimatorControllerParameterType.Trigger);

            var rootSm = controller.layers[0].stateMachine;

            // Tìm clips từ mẫu Stan.fbx
            string sampleFbx = "Assets/Models/NewMech/Stan.fbx";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sampleFbx);

            AnimationClip clipIdle = null, clipWalk = null;
            AnimationClip clipShoot = null, clipPunch = null, clipSlash = null, clipKick = null;
            AnimationClip clipHit = null, clipDie = null, clipDance = null;

            foreach (var a in assets)
            {
                if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                {
                    string n = c.name.ToLower();
                    if (n.Contains("idle")) clipIdle = c;
                    else if (n.Contains("walk")) clipWalk = c;
                    else if (n.Contains("shoot")) clipShoot = c;
                    else if (n.Contains("punch")) clipPunch = c;
                    else if (n.Contains("swordslash")) clipSlash = c;
                    else if (n.Contains("kick")) clipKick = c;
                    else if (n.Contains("hitrecieve_1") || n.Contains("hitrecieve")) clipHit = c;
                    else if (n.Contains("death")) clipDie = c;
                    else if (n.Contains("dance")) clipDance = c;
                }
            }

            // Tạo các States
            var stateIdle = rootSm.AddState("Idle", new Vector3(300, 50, 0));
            stateIdle.motion = clipIdle;
            rootSm.defaultState = stateIdle;

            var stateMove = rootSm.AddState("Move", new Vector3(300, 150, 0));
            stateMove.motion = clipWalk;

            // Chuyển động Idle <-> Move
            var toMove = stateIdle.AddTransition(stateMove);
            toMove.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
            toMove.duration = 0.15f;
            toMove.hasExitTime = false;

            var toIdle = stateMove.AddTransition(stateIdle);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
            toIdle.duration = 0.15f;
            toIdle.hasExitTime = false;

            // Các chiêu tấn công
            var stateShoot = rootSm.AddState("Attack_Shoot", new Vector3(600, -50, 0));
            stateShoot.motion = clipShoot;

            var statePunch = rootSm.AddState("Attack_Punch", new Vector3(600, 50, 0));
            statePunch.motion = clipPunch;

            var stateSlash = rootSm.AddState("Attack_Slash", new Vector3(600, 150, 0));
            stateSlash.motion = clipSlash;

            var stateKick = rootSm.AddState("Attack_Kick", new Vector3(600, 250, 0));
            stateKick.motion = clipKick;

            // AnyState -> Tấn công
            AddAttackTransition(rootSm, stateShoot, 0);
            AddAttackTransition(rootSm, statePunch, 1);
            AddAttackTransition(rootSm, stateSlash, 2);
            AddAttackTransition(rootSm, stateKick, 3);

            // Tấn công xong quay về Idle
            stateShoot.AddTransition(stateIdle).hasExitTime = true;
            statePunch.AddTransition(stateIdle).hasExitTime = true;
            stateSlash.AddTransition(stateIdle).hasExitTime = true;
            stateKick.AddTransition(stateIdle).hasExitTime = true;

            // Hit, Die, Victory
            var stateHit = rootSm.AddState("Hit", new Vector3(300, -100, 0));
            stateHit.motion = clipHit;
            var anyToHit = rootSm.AddAnyStateTransition(stateHit);
            anyToHit.AddCondition(AnimatorConditionMode.If, 0, "hit");
            anyToHit.duration = 0.05f;
            stateHit.AddTransition(stateIdle).hasExitTime = true;

            var stateDie = rootSm.AddState("Die", new Vector3(50, -100, 0));
            stateDie.motion = clipDie;
            var anyToDie = rootSm.AddAnyStateTransition(stateDie);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "die");
            anyToDie.duration = 0.1f;

            var stateVic = rootSm.AddState("Victory", new Vector3(50, 50, 0));
            stateVic.motion = clipDance;
            var anyToVic = rootSm.AddAnyStateTransition(stateVic);
            anyToVic.AddCondition(AnimatorConditionMode.If, 0, "victory");
            anyToVic.duration = 0.2f;

            return controller;
        }

        private static void AddAttackTransition(AnimatorStateMachine sm, AnimatorState targetState, int typeIndex)
        {
            var trans = sm.AddAnyStateTransition(targetState);
            trans.AddCondition(AnimatorConditionMode.If, 0, "attack");
            trans.AddCondition(AnimatorConditionMode.Equals, typeIndex, "attackType");
            trans.duration = 0.05f;
            trans.hasExitTime = false;
        }

        private static GameObject CreateRobotPrefab(string name, string fbxPath, Material mat, RuntimeAnimatorController controller, FriendlyCombatRobot.RobotClass rClass)
        {
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj == null) return null;

            GameObject instance = Object.Instantiate(fbxObj);
            instance.name = $"FriendlyRobot_{name}";

            // Gán Material cho mọi Renderer
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = mat;
            }

            // Gán Tag
            instance.tag = "Untagged"; // Tránh nhầm với Enemy

            // Animator
            Animator anim = instance.GetComponent<Animator>();
            if (anim == null) anim = instance.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            // Collider nhẹ
            CapsuleCollider col = instance.GetComponent<CapsuleCollider>();
            if (col == null) col = instance.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.75f, 0);
            col.radius = 0.45f;
            col.height = 1.6f;

            // Script FriendlyCombatRobot
            FriendlyCombatRobot combatScript = instance.GetComponent<FriendlyCombatRobot>();
            if (combatScript == null) combatScript = instance.AddComponent<FriendlyCombatRobot>();
            combatScript.animator = anim;
            combatScript.SetupRobotClass(rClass);

            string prefabPath = $"Assets/prefabs/FriendlyRobot_{name}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"<color=#00FF88>✅ Đã tạo Prefab Robot Đồng Minh: {prefabPath}</color>");
            return savedPrefab;
        }

        private static GameObject CreateFactoryPrefab(GameObject[] combatPrefabs, Material factoryMat)
        {
            GameObject root = new GameObject("RobotFactory_Building");

            // 1. Khối buồng máy chính
            GameObject machineModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/machine-fortified.fbx")
                                   ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/machine.fbx");
            GameObject bodyObj = null;
            if (machineModel != null)
            {
                bodyObj = Object.Instantiate(machineModel, root.transform);
                bodyObj.name = "FactoryBody";
                bodyObj.transform.localPosition = Vector3.zero;
                ApplyMaterialToAll(bodyObj, factoryMat);
            }

            // 2. Băng chuyền xuất xưởng
            GameObject conveyorModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/conveyor.fbx");
            Transform spawnPoint = null;
            if (conveyorModel != null)
            {
                GameObject conveyor = Object.Instantiate(conveyorModel, root.transform);
                conveyor.name = "Conveyor_Exit";
                conveyor.transform.localPosition = new Vector3(0f, 0f, 2.0f);
                ApplyMaterialToAll(conveyor, factoryMat);

                GameObject spawnNode = new GameObject("RobotSpawnPoint");
                spawnNode.transform.SetParent(conveyor.transform);
                spawnNode.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
                spawnPoint = spawnNode.transform;
            }

            // 3. Cánh tay robot hàn xì
            GameObject armModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/robot-arm-a.fbx");
            List<Transform> armsList = new List<Transform>();
            if (armModel != null)
            {
                GameObject arm = Object.Instantiate(armModel, root.transform);
                arm.name = "WeldingRobotArm";
                arm.transform.localPosition = new Vector3(1.2f, 1.1f, 0.2f);
                arm.transform.localRotation = Quaternion.Euler(-15f, -45f, 0f);
                ApplyMaterialToAll(arm, factoryMat);
                armsList.Add(arm.transform);
            }

            // 4. Bánh răng cơ khí
            GameObject cogModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/cog-a.fbx");
            List<Transform> cogsList = new List<Transform>();
            if (cogModel != null)
            {
                GameObject cog1 = Object.Instantiate(cogModel, root.transform);
                cog1.name = "Gear_Left";
                cog1.transform.localPosition = new Vector3(-1.1f, 1.4f, -0.4f);
                cog1.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                ApplyMaterialToAll(cog1, factoryMat);
                cogsList.Add(cog1.transform);

                GameObject cog2 = Object.Instantiate(cogModel, root.transform);
                cog2.name = "Gear_Right";
                cog2.transform.localPosition = new Vector3(-1.1f, 0.8f, 0.3f);
                cog2.transform.localRotation = Quaternion.Euler(0f, 90f, 30f);
                cog2.transform.localScale = Vector3.one * 0.75f;
                ApplyMaterialToAll(cog2, factoryMat);
                cogsList.Add(cog2.transform);
            }

            // 5. Màn hình điều khiển
            GameObject screenModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/FactoryKit/Models/FBX format/screen-wide.fbx");
            if (screenModel != null)
            {
                GameObject screen = Object.Instantiate(screenModel, root.transform);
                screen.name = "ControlScreen";
                screen.transform.localPosition = new Vector3(-1.2f, 0.6f, 1.1f);
                screen.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                ApplyMaterialToAll(screen, factoryMat);
            }

            // 6. Đèn tia lửa hàn
            GameObject lightObj = new GameObject("WeldingSparksLight");
            lightObj.transform.SetParent(root.transform);
            lightObj.transform.localPosition = new Vector3(0.5f, 1.2f, 0.5f);
            Light wLight = lightObj.AddComponent<Light>();
            wLight.type = LightType.Point;
            wLight.color = new Color(0.2f, 0.8f, 1f); // Xanh điện hàn
            wLight.range = 5f;
            wLight.intensity = 2f;
            wLight.enabled = false;

            // 7. Điểm tập kết quân (Rally Point)
            GameObject rallyObj = new GameObject("ArmyRallyPoint");
            rallyObj.transform.SetParent(root.transform);
            rallyObj.transform.localPosition = new Vector3(0f, 0f, 5.0f);

            // 8. BoxCollider cho Nhà máy
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, 0.5f);
            col.size = new Vector3(3.2f, 2.4f, 3.8f);

            // 9. Gắn Script RobotFactory
            RobotFactory factoryComp = root.AddComponent<RobotFactory>();
            factoryComp.productionTime = 4.0f;
            factoryComp.autoProduce = true;
            factoryComp.maxArmySize = 8;
            factoryComp.combatRobotPrefabs = combatPrefabs;
            factoryComp.spawnPoint = (spawnPoint != null) ? spawnPoint : root.transform;
            factoryComp.rallyPoint = rallyObj.transform;
            factoryComp.cogs = cogsList.ToArray();
            factoryComp.robotArms = armsList.ToArray();
            factoryComp.factoryBody = (bodyObj != null) ? bodyObj.transform : null;
            factoryComp.weldingLight = wLight;

            string prefabPath = "Assets/prefabs/RobotFactory_Building.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"<color=#00FF88>✅ Đã tạo Prefab Nhà Máy Sản Xuất Robot: {prefabPath}</color>");
            return savedPrefab;
        }

        private static void ApplyMaterialToAll(GameObject obj, Material mat)
        {
            if (obj == null || mat == null) return;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void PlaceFactoryInScene(GameObject factoryPrefab)
        {
            if (factoryPrefab == null) return;

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != scenePath)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            GameObject existing = GameObject.Find("RobotFactory_Building");
            if (existing == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(factoryPrefab);
                instance.name = "RobotFactory_Building";

                // Đặt ở vị trí căn cứ của người chơi
                Vector3 placePos = new Vector3(-8.5f, 9.1f, -14.0f);
                if (Terrain.activeTerrain != null)
                {
                    placePos.y = Terrain.activeTerrain.SampleHeight(placePos) + Terrain.activeTerrain.transform.position.y;
                }
                instance.transform.position = placePos;
                instance.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"<color=#00FF88>✅ Đã đặt Nhà Máy Robot vào Scene tại tọa độ: {placePos}</color>");
            }
        }
    }
}
#endif
