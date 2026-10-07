#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace RobotDefense.Editor
{
    [InitializeOnLoad]
    public static class BeautifyTerrainTextures
    {
        static BeautifyTerrainTextures()
        {
            EditorApplication.delayCall += () =>
            {
                ApplyBeautification();
            };
        }

        // [MenuItem("Tools/🌿 Tối Ưu & Làm Đẹp Texture Địa Hình (Beautify Terrain)")]
        public static void ApplyBeautification()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("[BeautifyTerrain] Không tìm thấy Terrain trong Scene!");
                return;
            }

            TerrainData tData = terrain.terrainData;
            if (tData == null) return;

            Undo.RegisterCompleteObjectUndo(tData, "Beautify Terrain Textures");
            Undo.RegisterCompleteObjectUndo(terrain, "Beautify Terrain Settings");

            // 1. Tối ưu thông số hiển thị Terrain (Sắc nét, không nhòe vỡ hình)
            terrain.heightmapPixelError = 1f; // Độ mịn lưới địa hình cao nhất
            terrain.basemapDistance = 800f;   // Texture PBR hiển thị rõ ràng từ mọi cự ly camera
            terrain.drawInstanced = true;     // Bật GPU Instancing mượt mà 60 FPS

            // 2. Tải các TerrainLayer chuẩn PBR chất lượng cao
            TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/Layers/Island_Grass.terrainlayer");
            if (grassLayer == null)
            {
                grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Grass_Soil_TerrainLayer.terrainlayer");
            }

            TerrainLayer rockLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/Layers/Island_Rock.terrainlayer");
            if (rockLayer == null)
            {
                rockLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Rock_TerrainLayer.terrainlayer");
            }

            TerrainLayer pebblesLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Pebbles_A_TerrainLayer.terrainlayer");
            if (pebblesLayer == null)
            {
                pebblesLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TerrainSampleAssets/TerrainLayers/Sand_TerrainLayer.terrainlayer");
            }

            if (grassLayer == null || rockLayer == null)
            {
                Debug.LogWarning("[BeautifyTerrain] Thiếu asset TerrainLayer!");
                return;
            }

            // Đảm bảo tile size chuẩn để không bị vỡ hạt hay mờ căm
            grassLayer.tileSize = new Vector2(4f, 4f);
            rockLayer.tileSize = new Vector2(4.5f, 4.5f);
            if (pebblesLayer != null) pebblesLayer.tileSize = new Vector2(3.5f, 3.5f);

            EditorUtility.SetDirty(grassLayer);
            EditorUtility.SetDirty(rockLayer);
            if (pebblesLayer != null) EditorUtility.SetDirty(pebblesLayer);

            // Gán danh sách layer cho Terrain
            if (pebblesLayer != null)
            {
                tData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer, pebblesLayer };
            }
            else
            {
                tData.terrainLayers = new TerrainLayer[] { grassLayer, rockLayer };
            }

            // 3. Phủ màu thông minh (Splatmap / Alphamap) với hiệu ứng tự nhiên
            int aRes = tData.alphamapResolution;
            int numLayers = tData.terrainLayers.Length;
            float[,,] alphaMaps = new float[aRes, aRes, numLayers];

            for (int y = 0; y < aRes; y++)
            {
                float v = (float)y / (aRes - 1);
                for (int x = 0; x < aRes; x++)
                {
                    float u = (float)x / (aRes - 1);
                    float steepness = tData.GetSteepness(u, v);
                    float height = tData.GetInterpolatedHeight(u, v);

                    // Nhiễu Perlin để hòa trộn màu tự nhiên không bị cứng
                    float noiseDetail = Mathf.PerlinNoise(u * 22f, v * 22f);
                    float noiseMacro = Mathf.PerlinNoise(u * 7f, v * 7f);

                    float wGrass = 0f;
                    float wRock = 0f;
                    float wPebbles = 0f;

                    if (steepness > 20f || height < 3.2f)
                    {
                        // Vách núi dốc đứng hoặc sát mép biển -> Đá núi PBR gồ ghề
                        wRock = 1f;
                    }
                    else if (steepness > 11f)
                    {
                        // Sườn dốc vừa -> Pha trộn giữa đá và sỏi / cỏ
                        float blend = Mathf.InverseLerp(11f, 20f, steepness);
                        wRock = blend;
                        wGrass = (1f - blend) * 0.7f;
                        wPebbles = (1f - blend) * 0.3f;
                    }
                    else
                    {
                        // Mặt bằng đảo căn cứ -> Cỏ xanh rực rỡ điểm xuyết các mảng sỏi/đất tự nhiên
                        if (noiseMacro > 0.68f)
                        {
                            // Mảng sỏi đá nhẹ
                            float pFactor = Mathf.InverseLerp(0.68f, 0.85f, noiseMacro) * 0.45f;
                            wPebbles = pFactor;
                            wGrass = 1f - pFactor;
                        }
                        else
                        {
                            // 100% Cỏ mượt có độ bóng nhẹ từ MaskMap
                            wGrass = 1f;
                        }

                        // Điểm xuyết viền sỏi gần mép vực
                        if (steepness > 8f && steepness <= 11f)
                        {
                            float edgePebble = Mathf.InverseLerp(8f, 11f, steepness) * 0.4f;
                            wPebbles = Mathf.Max(wPebbles, edgePebble);
                            wGrass = Mathf.Max(0f, 1f - wPebbles);
                        }
                    }

                    // Chuẩn hóa trọng số
                    float totalWeight = wGrass + wRock + wPebbles;
                    if (totalWeight <= 0f) { wGrass = 1f; totalWeight = 1f; }

                    alphaMaps[y, x, 0] = wGrass / totalWeight;
                    alphaMaps[y, x, 1] = wRock / totalWeight;
                    if (numLayers > 2)
                    {
                        alphaMaps[y, x, 2] = wPebbles / totalWeight;
                    }
                }
            }

            tData.SetAlphamaps(0, 0, alphaMaps);

            EditorUtility.SetDirty(tData);
            EditorUtility.SetDirty(terrain);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=#00FFAA><b>[BeautifyTerrain]</b> ĐÃ LÀM ĐẸP XONG TEXTURE ĐỊA HÌNH VỚI ĐỦ BỘ NORMAL MAP, MASK MAP VÀ TỈ LỆ CHUẨN!</color>");
        }
    }
}
#endif

