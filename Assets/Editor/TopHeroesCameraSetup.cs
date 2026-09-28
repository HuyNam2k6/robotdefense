#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace IdleFactoryDefense.Editor
{
    public static class TopHeroesCameraSetup
    {
        [MenuItem("Tools/📷 Cài Đặt Camera Góc Dọc Top Heroes")]
        public static void SetupTopHeroesCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = Object.FindAnyObjectByType<Camera>();
            }

            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            Undo.RecordObject(cam.transform, "Setup Top Heroes Camera");

            // Cấu hình vị trí và góc nhìn chuẩn Top Heroes
            cam.transform.position = new Vector3(0f, 15f, -10f);
            cam.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            cam.orthographic = false; // Perspective
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;

            EditorUtility.SetDirty(cam.gameObject);
            Selection.activeGameObject = cam.gameObject;

            Debug.Log("<color=#00FF88><b>[Camera Setup]</b> ĐÃ CÀI ĐẶT GÓC CAMERA CHUẨN TOP HEROES (Y=15, Z=-10, Góc nghiêng 55°)!</color>");
        }
    }
}
#endif
