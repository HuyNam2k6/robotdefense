using System;
using UnityEngine;
using UnityEditor;

public static class CheckAvatars
{
    [MenuItem("Tools/Check Avatars")]
    public static void Run()
    {
        string[] fbxPaths = new string[]
        {
            "Assets/Models/NewMech/Stan.fbx",
            "Assets/Models/NewMech/Mike.fbx",
            "Assets/Models/NewMech/George.fbx",
            "Assets/Models/NewMech/Leela.fbx"
        };
        foreach(var p in fbxPaths)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(p);
            foreach(var a in assets)
            {
                if (a is Avatar av)
                {
                    Debug.Log($"[Avatar] In {p}: name='{av.name}', isValid={av.isValid}, isHuman={av.isHuman}");
                }
            }
        }
    }
}
