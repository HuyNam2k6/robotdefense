#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace RobotDefense.Editor
{
    public static class SetupBaseHQAndCoreLoop
    {
        [MenuItem("Tools/🏛️ Cập Nhật Model Nhà Chính (Lõi Năng Lượng Cơ Khí)")]
        public static void ExecuteSetup()
        {
            EnsurePrefabsFolder();

            // 1. Tạo Prefab Lõi Năng Lượng Cơ Khí Hoành Tráng (Mechanical Energy Core)
            GameObject hqPrefab = CreateMechanicalCorePrefab();

            // 2. Cập nhật vào Scene: ĐẢM BẢO FIX CỨNG VỊ TRÍ BAN ĐẦU CỦA NGƯỜI CHƠI
            if (hqPrefab != null)
            {
                PlaceOrUpdateBaseHQInScene(hqPrefab);
            }

            // 3. Kích hoạt HUD Ngày Đêm trên Scene nếu có
            DayNightSystem dns = Object.FindAnyObjectByType<DayNightSystem>();
            if (dns != null)
            {
                dns.showDayNightHUD = true;
                EditorUtility.SetDirty(dns);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#00FF88><b>[Core Loop Setup]</b> ĐÃ HOÀN TẤT THIẾT KẾ MODEL LÕI NĂNG LƯỢNG CƠ KHÍ & FIX CỨNG VỊ TRÍ BAN ĐẦU!</color>");
        }

        private static void EnsurePrefabsFolder()
        {
            if (!Directory.Exists("Assets/prefabs")) Directory.CreateDirectory("Assets/prefabs");
        }

        private static GameObject CreateMechanicalCorePrefab()
        {
            string prefabPath = "Assets/prefabs/BaseHQ_Building.prefab";

            GameObject root = new GameObject("BaseHQ_Building");

            // Material kim loại sắc nét của FactoryKit
            Material factoryMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/FactoryKit/Models/Materials/Factory_Mat.mat");
            string fbxFolder = "Assets/Models/FactoryKit/Models/FBX format/";

            // 1. Khối móng phễu tròn cơ khí nặng (Heavy Industrial Base Pedestal)
            GameObject hopperHighFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "hopper-high-round.fbx");
            if (hopperHighFbx != null)
            {
                GameObject basePart = Object.Instantiate(hopperHighFbx, root.transform);
                basePart.name = "HQ_01_HeavyBasePedestal";
                basePart.transform.localPosition = Vector3.zero;
                basePart.transform.localScale = new Vector3(2.4f, 1.4f, 2.4f);
                if (factoryMat != null) ApplyMaterial(basePart, factoryMat);
            }

            // 2. Bốn chân Piston thủy lực bọc 4 góc neo chặt xuống nền đất
            GameObject pistonFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "piston-round.fbx");
            if (pistonFbx != null)
            {
                Vector3[] cornerPositions = new Vector3[]
                {
                    new Vector3(1.65f, 0f, 1.65f),
                    new Vector3(-1.65f, 0f, 1.65f),
                    new Vector3(-1.65f, 0f, -1.65f),
                    new Vector3(1.65f, 0f, -1.65f)
                };

                for (int i = 0; i < cornerPositions.Length; i++)
                {
                    GameObject leg = Object.Instantiate(pistonFbx, root.transform);
                    leg.name = $"HQ_02_HydraulicPistonFoot_{i + 1}";
                    leg.transform.localPosition = cornerPositions[i];
                    leg.transform.localScale = new Vector3(1.2f, 1.1f, 1.2f);
                    if (factoryMat != null) ApplyMaterial(leg, factoryMat);
                }
            }

            // 3. Buồng máy móc cơ khí bảo vệ đế (Fortified Base Housing)
            GameObject machineFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "machine-fortified.fbx");
            if (machineFbx != null)
            {
                GameObject machinePart = Object.Instantiate(machineFbx, root.transform);
                machinePart.name = "HQ_03_FortifiedCoreHousing";
                machinePart.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                machinePart.transform.localScale = new Vector3(1.85f, 0.95f, 1.85f);
                if (factoryMat != null) ApplyMaterial(machinePart, factoryMat);
            }

            // 4. BÁNH RĂNG CƠ KHÍ TẦNG DƯỚI (Spinning Gear Lower)
            GameObject cogAFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "cog-a.fbx");
            GameObject cog1Instance = null;
            if (cogAFbx != null)
            {
                cog1Instance = Object.Instantiate(cogAFbx, root.transform);
                cog1Instance.name = "HQ_05_SpinningGear_Clockwise";
                cog1Instance.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                cog1Instance.transform.localScale = new Vector3(2.6f, 0.6f, 2.6f);
                if (factoryMat != null) ApplyMaterial(cog1Instance, factoryMat);
            }

            // 5. Hệ thống đường ống cơ khí & van điều áp năng lượng (Pipes & Valves)
            GameObject pipeCurveFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "pipe-large-curve.fbx");
            if (pipeCurveFbx != null)
            {
                GameObject pipeLeft = Object.Instantiate(pipeCurveFbx, root.transform);
                pipeLeft.name = "HQ_07_CoolantConduit_Left";
                pipeLeft.transform.localPosition = new Vector3(-1.45f, 1.25f, 0f);
                pipeLeft.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                pipeLeft.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                if (factoryMat != null) ApplyMaterial(pipeLeft, factoryMat);
            }

            GameObject pipeValveFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "pipe-large-valve.fbx");
            if (pipeValveFbx != null)
            {
                GameObject pipeRight = Object.Instantiate(pipeValveFbx, root.transform);
                pipeRight.name = "HQ_08_PressureValveConduit_Right";
                pipeRight.transform.localPosition = new Vector3(1.45f, 1.25f, 0f);
                pipeRight.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                pipeRight.transform.localScale = new Vector3(1.15f, 1.15f, 1.15f);
                if (factoryMat != null) ApplyMaterial(pipeRight, factoryMat);
            }

            // 6. Bảng điều khiển vi tính phía trước (Screen Control Console)
            GameObject screenFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "screen-panel-wide.fbx");
            if (screenFbx != null)
            {
                GameObject screenPart = Object.Instantiate(screenFbx, root.transform);
                screenPart.name = "HQ_09_ControlConsoleScreen";
                screenPart.transform.localPosition = new Vector3(0f, 0.95f, 1.65f);
                screenPart.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                screenPart.transform.localScale = new Vector3(1.35f, 1.35f, 1.35f);
                if (factoryMat != null) ApplyMaterial(screenPart, factoryMat);
            }

            // 7. LÕI NĂNG LƯỢNG PLASMA PHÁT SÁNG Ở TRUNG TÂM (LỘ RÕ 360 ĐỘ KHÔNG BỊ CHE KHUẤT)
            GameObject coreSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreSphere.name = "HQ_11_GlowingPlasmaCore";
            coreSphere.transform.SetParent(root.transform, false);
            coreSphere.transform.localPosition = new Vector3(0f, 2.45f, 0f);
            coreSphere.transform.localScale = new Vector3(1.75f, 1.75f, 1.75f);
            Collider coreCol = coreSphere.GetComponent<Collider>();
            if (coreCol != null) Object.DestroyImmediate(coreCol);

            // NẠP MATERIAL ASSET PHÁT SÁNG TRỰC TIẾP TỪ ĐĨA (ĐẢM BẢO PREFAB GIỮ NGUYÊN MATERIAL PHÁT SÁNG VĨNH VIỄN)
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HQ_PlasmaCore_Glow.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/MechaDragon/Materials/MechaDragon_ReactorCore.mat")
                            ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/LightningGlow.mat");

            Renderer coreRend = coreSphere.GetComponent<Renderer>();
            if (coreRend != null && glowMat != null)
            {
                coreRend.sharedMaterial = glowMat;
            }

            // 8. BÁNH RĂNG CƠ KHÍ TẦNG TRÊN (Spinning Gear Upper)
            GameObject cogBFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "cog-b.fbx");
            GameObject cog2Instance = null;
            if (cogBFbx != null)
            {
                cog2Instance = Object.Instantiate(cogBFbx, root.transform);
                cog2Instance.name = "HQ_06_SpinningGear_CounterClockwise";
                cog2Instance.transform.localPosition = new Vector3(0f, 3.45f, 0f);
                cog2Instance.transform.localScale = new Vector3(2.3f, 0.6f, 2.3f);
                if (factoryMat != null) ApplyMaterial(cog2Instance, factoryMat);
            }

            // 9. Mái vòm nắp chụp bảo vệ nóc (Reactor Pressure Cap)
            GameObject topFbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxFolder + "top-large.fbx");
            if (topFbx != null)
            {
                GameObject topPart = Object.Instantiate(topFbx, root.transform);
                topPart.name = "HQ_10_ReactorTopDome";
                topPart.transform.localPosition = new Vector3(0f, 4.05f, 0f);
                topPart.transform.localScale = new Vector3(2.1f, 0.8f, 2.1f);
                if (factoryMat != null) ApplyMaterial(topPart, factoryMat);
            }

            // 10. Đèn Point Light ánh sáng Cyan rực rỡ tỏa ra xung quanh toàn bộ căn cứ
            GameObject lightObj = new GameObject("HQ_12_BeaconLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.45f, 0f); // Ngay tâm quả cầu
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(0.1f, 0.85f, 1f);
            pLight.range = 16f;
            pLight.intensity = 4.2f;
            pLight.shadows = LightShadows.None;

            // 11. BoxCollider bao bọc toàn bộ công trình
            BoxCollider boxCol = root.AddComponent<BoxCollider>();
            boxCol.center = new Vector3(0f, 2.1f, 0f);
            boxCol.size = new Vector3(3.6f, 4.4f, 3.6f);

            // 12. Component BaseHQ với cấu hình hoàn chỉnh
            BaseHQ hqScript = root.AddComponent<BaseHQ>();
            hqScript.maxHealth = 2000f;
            hqScript.currentHealth = 2000f;
            hqScript.healthBarHeight = 5.6f;
            hqScript.lockToInitialPosition = true;
            hqScript.energyCoreTransform = coreSphere.transform;
            hqScript.hqLight = pLight;

            List<Transform> cogs1 = new List<Transform>();
            if (cog1Instance != null) cogs1.Add(cog1Instance.transform);
            hqScript.rotatingGears = cogs1.ToArray();

            List<Transform> cogs2 = new List<Transform>();
            if (cog2Instance != null) cogs2.Add(cog2Instance.transform);
            hqScript.counterRotatingGears = cogs2.ToArray();

            // Lưu Prefab
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"<color=#00FF88>✅ Đã tạo Prefab Nhà Chính Lõi Năng Lượng Cơ Khí: {prefabPath}</color>");
            return savedPrefab;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in rends)
            {
                r.sharedMaterial = mat;
            }
        }

        private static void PlaceOrUpdateBaseHQInScene(GameObject hqPrefab)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.isLoaded) return;

            // Kiểm tra xem trên Scene đã có BaseHQ do người dùng đặt chưa
            BaseHQ existing = Object.FindAnyObjectByType<BaseHQ>();
            if (existing != null)
            {
                // BẢO LƯU 100% VỊ TRÍ VÀ GÓC XOAY BAN ĐẦU CỦA NGƯỜI CHƠI TRÊN SCENE!
                Vector3 userPos = existing.transform.position;
                Quaternion userRot = existing.transform.rotation;
                Transform userParent = existing.transform.parent;
                string objName = existing.gameObject.name;

                Undo.DestroyObjectImmediate(existing.gameObject);

                GameObject newHQ = PrefabUtility.InstantiatePrefab(hqPrefab) as GameObject;
                if (newHQ != null)
                {
                    newHQ.name = objName;
                    newHQ.transform.SetParent(userParent, false);
                    newHQ.transform.position = userPos;
                    newHQ.transform.rotation = userRot;
                    Undo.RegisterCreatedObjectUndo(newHQ, "Update BaseHQ Mechanical Model");
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    Debug.Log($"<color=#00FF88>📍 ĐÃ CẬP NHẬT MODEL MỚI VÀ FIX CỨNG TẠI ĐÚNG VỊ TRÍ BAN ĐẦU BẠN ĐẶT: {userPos}</color>");
                }
                return;
            }

            // Nếu trên Scene chưa có: Đặt tại vị trí trung tâm căn cứ
            Vector3 defaultPos = new Vector3(0f, 9.6f, -31.5f);
            if (Terrain.activeTerrain != null)
            {
                float tY = Terrain.activeTerrain.SampleHeight(defaultPos) + Terrain.activeTerrain.transform.position.y;
                defaultPos.y = tY;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(hqPrefab) as GameObject;
            if (instance != null)
            {
                instance.name = "BaseHQ_Building";
                instance.transform.position = defaultPos;
                instance.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"<color=#00FF88>📍 Đã đặt Nhà Chính Lõi Năng Lượng Cơ Khí tại vị trí: {defaultPos}</color>");
            }
        }
    }
}
#endif
