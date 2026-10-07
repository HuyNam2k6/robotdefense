using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class FixParticles {
    static FixParticles() {
        EditorApplication.delayCall += FixAllParticles;
    }
    
    // [MenuItem("Tools/Fix Particle Systems")]
    public static void FixAllParticles() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;
        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            
            bool changed = false;
            ParticleSystem[] pss = prefab.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in pss) {
                var vel = ps.velocityOverLifetime;
                if (vel.enabled) {
                    if (vel.x.mode != vel.y.mode || vel.x.mode != vel.z.mode) {
                        vel.x = new ParticleSystem.MinMaxCurve(0f);
                        vel.y = new ParticleSystem.MinMaxCurve(0f);
                        vel.z = new ParticleSystem.MinMaxCurve(0f);
                        changed = true;
                    }
                }
            }
            if (changed) {
                EditorUtility.SetDirty(prefab);
                count++;
            }
        }
        if (count > 0) {
            AssetDatabase.SaveAssets();
            Debug.Log("<color=orange>Fixed Particle Velocity mismatch on " + count + " prefabs.</color>");
        }
    }
}
