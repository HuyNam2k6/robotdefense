#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace RobotDefense.Editor
{
    public static class SetupSpaceStationKit
    {
        [InitializeOnLoadMethod]
        private static void OnProjectLoaded()
        {
            EditorApplication.delayCall += () =>
            {
                SetupMaterials();
            };
        }

        [MenuItem("Tools/🚀 Cài Đặt Vật Liệu Space Station Kit")]
        public static void SetupMaterials()
        {
            string texturePath = "Assets/Map/Models/Textures/variation-a.png";
            string matFolder = "Assets/Map/Models/Materials";
            string matPath = Path.Combine(matFolder, "SpaceStation_Mat.mat");

            if (!File.Exists(texturePath)) return;

            if (!Directory.Exists(matFolder))
            {
                Directory.CreateDirectory(matFolder);
                AssetDatabase.Refresh();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) return;

            // Đảm bảo Texture nén chuẩn cho mobile
            TextureImporter texImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (texImporter != null)
            {
                texImporter.textureType = TextureImporterType.Default;
                texImporter.filterMode = FilterMode.Bilinear;
                texImporter.wrapMode = TextureWrapMode.Clamp;
                texImporter.SaveAndReimport();
            }

            // Tạo Material URP
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");

                mat = new Material(shader);
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
                mat.enableInstancing = true; // Bật GPU Instancing để siêu nhẹ CPU
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
            }
            else
            {
                mat.mainTexture = texture;
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
            }

            Debug.Log("<color=#00FF88><b>[SpaceStation]</b> Đã thiết lập Material URP cho Space Station Kit thành công!</color>");
        }
    }
}
#endif

