using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class CheckRobotAnimationsEditor
{
    [MenuItem("Tools/Check Robot Animations")]
    public static void Check()
    {
        string[] fbxPaths = new string[]
        {
            "Assets/Models/NewMech/Stan.fbx",
            "Assets/Models/NewMech/Mike.fbx",
            "Assets/Models/NewMech/George.fbx",
            "Assets/Models/NewMech/Leela.fbx"
        };

        foreach (var path in fbxPaths)
        {
            Debug.Log($"=== FBX: {path} ===");
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int clipCount = 0;
            foreach (var a in assets)
            {
                if (a is AnimationClip clip)
                {
                    clipCount++;
                    Debug.Log($"   Clip: '{clip.name}', Length: {clip.length:F2}s, IsLoop: {clip.isLooping}");
                }
            }
            Debug.Log($"Total Clips in {path}: {clipCount}");
        }
    }
}
