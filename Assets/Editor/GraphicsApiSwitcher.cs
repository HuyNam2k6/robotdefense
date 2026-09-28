#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace IdleFactoryDefense.Editor
{
    public static class GraphicsApiSwitcher
    {
        [MenuItem("Tools/⚙️ Ép Chuyển Sang DirectX 11 (DX11)")]
        public static void SwitchToDirectX11()
        {
            // Tắt chế độ tự động chọn Graphics API cho Windows Standalone 64-bit
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, false);

            // Đặt danh sách Graphics API chỉ dùng Direct3D11
            GraphicsDeviceType[] apis = new GraphicsDeviceType[] { GraphicsDeviceType.Direct3D11 };
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, apis);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, apis);

            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00FF88><b>[Graphics Settings]</b> ĐÃ ÉP CẤU HÌNH SANG DIRECTX 11 THÀNH CÔNG!</color>");
            EditorUtility.DisplayDialog("Thành Công", "Đã chuyển Graphics API của Windows sang DirectX 11 (Direct3D11)!\n\nNếu muốn Unity Editor áp dụng ngay, anh chỉ cần khởi động lại Unity (Restart).", "OK");
        }
    }
}
#endif

