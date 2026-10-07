#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace IdleFactoryDefense.Editor
{
    public static class SetupCuteRobot
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                string controllerPath = "Assets/Models/AnimatedRobot/CuteRobot_Controller.controller";
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null || !System.Array.Exists(controller.parameters, p => p.name == "die"))
                {
                    SetupRobot();
                }
            };
        }

        [MenuItem("Tools/🤖 Cài Đặt Cute Robot (Full States & Animations)")]
        public static void SetupRobot()
        {
            string rootPath = "Assets/Models/AnimatedRobot";
            string fbxPath = Path.Combine(rootPath, "source/Robot.fbx").Replace("\\", "/");
            string animDir = Path.Combine(rootPath, "Animations").Replace("\\", "/");
            string controllerPath = Path.Combine(rootPath, "CuteRobot_Controller.controller").Replace("\\", "/");
            string prefabPath = Path.Combine(rootPath, "CuteRobot_Prefab.prefab").Replace("\\", "/");

            if (!File.Exists(fbxPath))
            {
                Debug.LogError("Không tìm thấy file: " + fbxPath);
                return;
            }

            if (!Directory.Exists(animDir))
            {
                Directory.CreateDirectory(animDir);
                AssetDatabase.Refresh();
            }

            // 0. BẬT TỰ ĐỘNG TẠO AVATAR TRONG MODEL IMPORTER
            ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                bool needReimport = false;
                if (importer.animationType != ModelImporterAnimationType.Generic)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    needReimport = true;
                }
                if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                {
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    needReimport = true;
                }
                if (needReimport)
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }

            // 1. NHÂN BẢN (CTRL+D) TOÀN BỘ ANIMATION CLIPS RA NGOÀI & TÍCH LOOP TIME
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            Avatar robotAvatar = null;

            foreach (var asset in allAssets)
            {
                if (asset is Avatar av)
                {
                    robotAvatar = av;
                }
                else if (asset is AnimationClip sourceClip && !sourceClip.name.StartsWith("__preview__"))
                {
                    string cleanName = sourceClip.name
                        .Replace("RobotArmature|Robot_", "")
                        .Replace("RobotArmature|", "")
                        .Replace("Robot_", "");

                    string animAssetPath = Path.Combine(animDir, cleanName + ".anim").Replace("\\", "/");

                    // QUAN TRỌNG: Chỉ Loop cho Idle, Walk, Run, Dance
                    // Standing trong bộ này là hành động "đứng dậy từ tư thế ngồi", KHÔNG ĐƯỢC LOOP!
                    bool isLoop = cleanName.Equals("Idle") 
                               || cleanName.Equals("Walking") 
                               || cleanName.Equals("Running") 
                               || cleanName.Equals("Dance");

                    AnimationClip dupClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animAssetPath);
                    if (dupClip == null)
                    {
                        dupClip = Object.Instantiate(sourceClip);
                        dupClip.name = cleanName;

                        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(dupClip);
                        settings.loopTime = isLoop;
                        AnimationUtility.SetAnimationClipSettings(dupClip, settings);

                        AssetDatabase.CreateAsset(dupClip, animAssetPath);
                    }
                    else
                    {
                        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(dupClip);
                        settings.loopTime = isLoop;
                        AnimationUtility.SetAnimationClipSettings(dupClip, settings);
                        EditorUtility.SetDirty(dupClip);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Load chính xác từng clip bằng đường dẫn chuẩn
            AnimationClip clipIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Idle.anim"));
            AnimationClip clipWalking = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Walking.anim"));
            AnimationClip clipWave = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Wave.anim"));
            AnimationClip clipPunch = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Punch.anim"));
            AnimationClip clipJump = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Jump.anim"));
            AnimationClip clipDance = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Dance.anim"));
            AnimationClip clipDeath = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.Combine(animDir, "Death.anim"));

            Debug.Log($"<color=#00FF88>[CuteRobot] Đã chuẩn hóa: Idle={clipIdle != null}, Walking={clipWalking != null}, Dance={clipDance != null}, Death={clipDeath != null}</color>");

            // 2. TẠO HOẶC LẤY ANIMATOR CONTROLLER
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            }

            while (controller.layers.Length > 0)
            {
                controller.RemoveLayer(0);
            }

            controller.AddLayer("Base Layer");
            controller.parameters = new AnimatorControllerParameter[0];

            // Thêm Parameters
            controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("wave", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("mine", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("dance", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("die", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("respawn", AnimatorControllerParameterType.Trigger);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Tạo các State
            AnimatorState stateIdle = rootStateMachine.AddState("Idle");
            stateIdle.motion = clipIdle;

            AnimatorState stateWalking = rootStateMachine.AddState("Walking");
            stateWalking.motion = clipWalking;

            AnimatorState stateWave = rootStateMachine.AddState("Wave");
            stateWave.motion = clipWave;

            AnimatorState statePunch = rootStateMachine.AddState("Punch");
            statePunch.motion = clipPunch;

            AnimatorState stateJump = rootStateMachine.AddState("Jump");
            stateJump.motion = clipJump;

            AnimatorState stateDance = rootStateMachine.AddState("Dance");
            stateDance.motion = clipDance;

            AnimatorState stateDeath = rootStateMachine.AddState("Death");
            stateDeath.motion = clipDeath;

            // Đặt Idle chuẩn làm Default State!
            rootStateMachine.defaultState = stateIdle;

            // Nối dây: Idle <-> Walking
            var toWalk = stateIdle.AddTransition(stateWalking);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.05f;
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            var toIdle = stateWalking.AddTransition(stateIdle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.05f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

            // Nối dây: AnyState -> Wave
            var toWave = rootStateMachine.AddAnyStateTransition(stateWave);
            toWave.hasExitTime = false;
            toWave.duration = 0.1f;
            toWave.AddCondition(AnimatorConditionMode.If, 0, "wave");

            var fromWave = stateWave.AddTransition(stateIdle);
            fromWave.hasExitTime = true;
            fromWave.exitTime = 0.9f;
            fromWave.duration = 0.1f;

            // BẤM DI CHUYỂN LÀ CẮT VẪY TAY NGAY LẬP TỨC
            var fromWaveToWalk = stateWave.AddTransition(stateWalking);
            fromWaveToWalk.hasExitTime = false;
            fromWaveToWalk.duration = 0.05f;
            fromWaveToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Nối dây: AnyState -> Punch
            var toPunch = rootStateMachine.AddAnyStateTransition(statePunch);
            toPunch.hasExitTime = false;
            toPunch.duration = 0.05f;
            toPunch.AddCondition(AnimatorConditionMode.If, 0, "mine");

            var fromPunch = statePunch.AddTransition(stateIdle);
            fromPunch.hasExitTime = true;
            fromPunch.exitTime = 0.9f;
            fromPunch.duration = 0.1f;

            // BẤM DI CHUYỂN LÀ CẮT ĐẤM NGAY LẬP TỨC
            var fromPunchToWalk = statePunch.AddTransition(stateWalking);
            fromPunchToWalk.hasExitTime = false;
            fromPunchToWalk.duration = 0.05f;
            fromPunchToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Nối dây: AnyState -> Jump
            var toJump = rootStateMachine.AddAnyStateTransition(stateJump);
            toJump.hasExitTime = false;
            toJump.duration = 0.05f;
            toJump.AddCondition(AnimatorConditionMode.If, 0, "jump");

            var fromJumpToIdle = stateJump.AddTransition(stateIdle);
            fromJumpToIdle.hasExitTime = true;
            fromJumpToIdle.exitTime = 0.85f;
            fromJumpToIdle.duration = 0.1f;
            fromJumpToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");

            var fromJumpToWalk = stateJump.AddTransition(stateWalking);
            fromJumpToWalk.hasExitTime = true;
            fromJumpToWalk.exitTime = 0.85f;
            fromJumpToWalk.duration = 0.1f;
            fromJumpToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Nối dây: AnyState -> Dance
            var toDance = rootStateMachine.AddAnyStateTransition(stateDance);
            toDance.hasExitTime = false;
            toDance.duration = 0.1f;
            toDance.AddCondition(AnimatorConditionMode.If, 0, "dance");

            var fromDance = stateDance.AddTransition(stateIdle);
            fromDance.hasExitTime = true;
            fromDance.exitTime = 0.95f;
            fromDance.duration = 0.15f;

            // BẤM DI CHUYỂN LÀ CẮT NHẢY MÚA NGAY LẬP TỨC
            var fromDanceToWalk = stateDance.AddTransition(stateWalking);
            fromDanceToWalk.hasExitTime = false;
            fromDanceToWalk.duration = 0.05f;
            fromDanceToWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");

            // Nối dây: AnyState -> Death (Khi máu về 0)
            var toDeath = rootStateMachine.AddAnyStateTransition(stateDeath);
            toDeath.hasExitTime = false;
            toDeath.duration = 0.05f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "die");

            // Nối dây: Death -> Idle (Khi hồi sinh)
            var fromDeathToIdle = stateDeath.AddTransition(stateIdle);
            fromDeathToIdle.hasExitTime = false;
            fromDeathToIdle.duration = 0.15f;
            fromDeathToIdle.AddCondition(AnimatorConditionMode.If, 0, "respawn");

            // 3. CẬP NHẬT PREFAB
            GameObject fbxObj = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxObj != null)
            {
                GameObject instance = Object.Instantiate(fbxObj);
                instance.name = "CuteRobot_Prefab";

                Animator anim = instance.GetComponent<Animator>();
                if (anim == null) anim = instance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                if (robotAvatar != null) anim.avatar = robotAvatar;

                CharacterController cc = instance.GetComponent<CharacterController>();
                if (cc == null) cc = instance.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0.8f, 0);
                cc.height = 1.6f;
                cc.radius = 0.5f;

                PlayerController playerScript = instance.GetComponent<PlayerController>();
                if (playerScript == null) playerScript = instance.AddComponent<PlayerController>();
                playerScript.moveSpeed = 4.5f;
                playerScript.rotateSpeed = 14f;
                playerScript.alignWithCamera = true;

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=#00FF88><b>[CuteRobot Setup]</b> ĐÃ SỬA CHUẨN IDLE THỞ ĐUNG ĐƯA, THÊM JUMP VÀ DANCE THÀNH CÔNG!</color>");
            }
        }
    }
}
#endif
