#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RobotDefense.Editor
{
    public static class ApplyMapAndBaseHQSetup
    {
        [MenuItem("Tools/🚀 Cập Nhật Bản Đồ 2X & Đưa Nhà Chính Lên Hierarchy")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.isLoaded || !scene.path.Contains("SampleScene"))
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            }

            // 1. Chạy sinh lại bản đồ mở rộng gấp 2 lần kích thước & độ rộng
            GenerateIslandMap.Generate();

            // 2. Tìm hoặc nạp Prefab Nhà Chính (BaseHQ_Building) đưa lên cửa sổ Hierarchy
            BaseHQ existingHQ = Object.FindAnyObjectByType<BaseHQ>();
            if (existingHQ == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/BaseHQ_Building.prefab");
                if (prefab != null)
                {
                    GameObject hqObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (hqObj != null)
                    {
                        hqObj.name = "BaseHQ_Building";

                        Vector3 basePos = new Vector3(0f, 9.6f, -31.5f);
                        if (Terrain.activeTerrain != null)
                        {
                            float tY = Terrain.activeTerrain.SampleHeight(basePos) + Terrain.activeTerrain.transform.position.y;
                            basePos.y = tY;
                        }

                        hqObj.transform.position = basePos;
                        hqObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                        Undo.RegisterCreatedObjectUndo(hqObj, "Add BaseHQ to Hierarchy");
                        Debug.Log($"<color=#00FFAA>🏛️ [Hierarchy Setup] ĐÃ ĐƯA NHÀ CHÍNH LÊN CỬA SỔ HIERARCHY tại vị trí: {basePos}!</color>");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Không tìm thấy prefab Assets/prefabs/BaseHQ_Building.prefab!");
                }
            }
            else
            {
                Debug.Log($"<color=#00FFAA>🏛️ [Hierarchy Setup] Nhà Chính đã có sẵn trong Hierarchy: {existingHQ.gameObject.name} tại {existingHQ.transform.position}!</color>");
            }

            // 3. Đánh dấu và lưu Scene vĩnh viễn
            EditorUtility.SetDirty(Terrain.activeTerrain);
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#00FF88><b>[Setup Hoàn Tất]</b> Đã lưu SampleScene.unity (Saved={saved}). Bản đồ x2 & Nhà Chính đã hiện diện 100% trên Hierarchy!</color>");
        }

        [InitializeOnLoadMethod]
        private static void AutoRunOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("Applied_Map2X_And_BaseHQ_Hierarchy_V1", false) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool("Applied_Map2X_And_BaseHQ_Hierarchy_V1", true);
                    Execute();
                }
            };
        }
    }
}
#endif
