#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class CheckFbxAnimations
{
    [MenuItem("Tools/🔍 Kiểm tra Animations FBX")]
    [InitializeOnLoadMethod]
    public static void CheckAll()
    {
        EditorApplication.delayCall += () =>
        {
            AssetDatabase.Refresh();
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models", "Assets/Animations" });
            string report = "=== DANH SACH ANIMATION TRONG FBX ===\n";
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            report += $"\n[File] {path}:\n";
            int clipCount = 0;
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    report += $"   - Clip: {clip.name} (Length: {clip.length:F2}s)\n";
                    clipCount++;
                }
            }
            if (clipCount == 0) report += "   -> Không có AnimationClip nào!\n";
        }
        File.WriteAllText("Assets/Editor/fbx_anim_report.txt", report);
        Debug.Log(report);
        };
    }
}
#endif
