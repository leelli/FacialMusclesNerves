using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace maleric
{
    public static class AtlasMaterialCreator
    {
        public static string DefaultOutputPath => MeshMergeSettings.Instance.GeneratedAssetsPath;

        public static bool CreateAtlasFromMaterials(
            List<Material> sourceMaterials,
            out Texture2D atlasTexture,
            out Material atlasMaterial,
            out Vector2[] uvOffsets,
            out Vector2[] uvScales,
            out int cols,
            out int rows,
            string outputPath = null)
        {
            MeshMergeSettings.EnsureExists();
            atlasTexture = null;
            atlasMaterial = null;
            uvOffsets = null;
            uvScales = null;
            cols = 1;
            rows = 1;

            if (sourceMaterials == null || sourceMaterials.Count == 0)
            {
                Debug.LogError("AtlasMaterialCreator: No source materials provided.");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
                outputPath = DefaultOutputPath;
            Directory.CreateDirectory(outputPath);

            var textures = new List<Texture2D>();
            var usedNames = new List<string>();

            foreach (var mat in sourceMaterials)
            {
                if (mat == null)
                {
                    textures.Add(CreatePlaceholder(8, 8, Color.magenta));
                    usedNames.Add("null");
                    continue;
                }

                Texture tex = mat.GetTexture("_MainTex");
                if (tex == null) tex = mat.GetTexture("_BaseMap");
                if (tex == null) tex = mat.GetTexture("_Albedo");

                Texture2D tex2D = tex as Texture2D;
                if (tex2D == null)
                {
                    tex2D = CreatePlaceholder(8, 8, Color.white);
                    Debug.LogWarning("AtlasMaterialCreator: Material '" + mat.name + "' has no texture. Using white placeholder.");
                }
                else
                {
                    string sourceTexPath = AssetDatabase.GetAssetPath(tex2D);
                    if (!string.IsNullOrEmpty(sourceTexPath))
                    {
                        var importer = AssetImporter.GetAtPath(sourceTexPath) as TextureImporter;
                        if (importer != null && !importer.isReadable)
                        {
                            var copy = CreateReadableCopy(tex2D);
                            if (copy != null) tex2D = copy;
                        }
                    }
                }

                textures.Add(tex2D);
                usedNames.Add(mat.name);
            }

            int padWidth = 2;
            int texWidth = textures[0].width;
            int texHeight = textures[0].height;

            var sizeCounts = new Dictionary<(int, int), int>();
            foreach (var t in textures)
            {
                var key = (t.width, t.height);
                if (sizeCounts.ContainsKey(key)) sizeCounts[key]++;
                else sizeCounts[key] = 1;
            }
            int maxCount = 0;
            foreach (var kvp in sizeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    texWidth = kvp.Key.Item1;
                    texHeight = kvp.Key.Item2;
                }
            }

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i].width != texWidth || textures[i].height != texHeight)
                {
                    var resized = ResizeTexture(textures[i], texWidth, texHeight);
                    if (textures[i] != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(textures[i])))
                        Object.DestroyImmediate(textures[i]);
                    textures[i] = resized;
                }
            }

            int count = textures.Count;
            cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            rows = Mathf.CeilToInt((float)count / cols);

            int cellW = texWidth + padWidth;
            int cellH = texHeight + padWidth;
            int atlasW = cols * cellW + padWidth;
            int atlasH = rows * cellH + padWidth;

            atlasTexture = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false, false);
            atlasTexture.name = "Atlas_" + string.Join("_", usedNames);
            atlasTexture.filterMode = FilterMode.Bilinear;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[atlasW * atlasH];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            atlasTexture.SetPixels(clear);
            atlasTexture.Apply();

            uvOffsets = new Vector2[count];
            uvScales = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = (i / cols);

                int x = padWidth + col * cellW;
                int y = padWidth + row * cellH;

                Color[] pixels = textures[i].GetPixels(0, 0, texWidth, texHeight);
                atlasTexture.SetPixels(x, y, texWidth, texHeight, pixels);

                uvOffsets[i] = new Vector2(
                    (float)x / atlasW,
                    (float)y / atlasH
                );
                uvScales[i] = new Vector2(
                    (float)texWidth / atlasW,
                    (float)texHeight / atlasH
                );
            }

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                int cx = padWidth + col * cellW;
                int cy = padWidth + row * cellH;

                Color[] topRow = atlasTexture.GetPixels(cx, cy + texHeight - 1, texWidth, 1);
                for (int p = 1; p <= padWidth; p++)
                    atlasTexture.SetPixels(cx, cy + texHeight - 1 + p, texWidth, 1, topRow);

                Color[] botRow = atlasTexture.GetPixels(cx, cy, texWidth, 1);
                for (int p = 1; p <= padWidth; p++)
                    atlasTexture.SetPixels(cx, cy - p, texWidth, 1, botRow);

                Color[] leftCol = atlasTexture.GetPixels(cx, cy, 1, texHeight);
                for (int p = 1; p <= padWidth; p++)
                    atlasTexture.SetPixels(cx - p, cy, 1, texHeight, leftCol);

                Color[] rightCol = atlasTexture.GetPixels(cx + texWidth - 1, cy, 1, texHeight);
                for (int p = 1; p <= padWidth; p++)
                    atlasTexture.SetPixels(cx + texWidth - 1 + p, cy, 1, texHeight, rightCol);

                Color topRight = atlasTexture.GetPixel(cx + texWidth - 1, cy + texHeight - 1);
                Color topLeft = atlasTexture.GetPixel(cx, cy + texHeight - 1);
                Color botRight = atlasTexture.GetPixel(cx + texWidth - 1, cy);
                Color botLeft = atlasTexture.GetPixel(cx, cy);
                for (int py = -padWidth; py <= 0; py++)
                    for (int px = -padWidth; px <= 0; px++)
                    {
                        atlasTexture.SetPixel(cx + px, cy + py, botLeft);
                        atlasTexture.SetPixel(cx + texWidth - 1 - px, cy + py, botRight);
                        atlasTexture.SetPixel(cx + px, cy + texHeight - 1 - py, topLeft);
                        atlasTexture.SetPixel(cx + texWidth - 1 - px, cy + texHeight - 1 - py, topRight);
                    }
            }

            atlasTexture.Apply(false, false);

            string texAssetPath = outputPath + "/" + atlasTexture.name + ".asset";
            texAssetPath = AssetDatabase.GenerateUniqueAssetPath(texAssetPath);
            AssetDatabase.CreateAsset(atlasTexture, texAssetPath);

            string shaderName = MeshMergeSettings.Instance.GetAtlasShader();
            Shader shader = Shader.Find(shaderName);
            if (shader == null) shader = Shader.Find("Standard");

            atlasMaterial = new Material(shader);
            atlasMaterial.name = atlasTexture.name + "_Mat";

            bool isURP = GraphicsSettings.currentRenderPipeline != null
                && GraphicsSettings.currentRenderPipeline.GetType().Name.Contains("UniversalRenderPipeline");

            if (isURP)
            {
                atlasMaterial.SetTexture("_BaseMap", atlasTexture);
                atlasMaterial.SetColor("_BaseColor", Color.white);
            }
            else
            {
                atlasMaterial.SetTexture("_MainTex", atlasTexture);
                atlasMaterial.SetColor("_Color", Color.white);
            }

            if (sourceMaterials.Count > 0)
            {
                var primary = sourceMaterials[0];
                string colorProp = isURP ? "_BaseColor" : "_Color";
                if (primary.HasProperty("_BaseColor"))
                    atlasMaterial.SetColor(colorProp, primary.GetColor("_BaseColor"));
                else if (primary.HasProperty("_Color"))
                    atlasMaterial.SetColor(colorProp, primary.GetColor("_Color"));

                if (primary.HasProperty("_Metallic"))
                    atlasMaterial.SetFloat("_Metallic", primary.GetFloat("_Metallic"));
                if (primary.HasProperty("_Smoothness"))
                    atlasMaterial.SetFloat("_Smoothness", primary.GetFloat("_Smoothness"));
            }

            string matPath = outputPath + "/" + atlasMaterial.name + ".mat";
            matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);
            AssetDatabase.CreateAsset(atlasMaterial, matPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            for (int i = 0; i < textures.Count; i++)
            {
                if (textures[i] != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(textures[i])))
                    Object.DestroyImmediate(textures[i]);
            }

            Debug.Log("AtlasMaterialCreator: Created '" + atlasTexture.name + "' (" + cols + "x" + rows + " grid, " + count + " textures, " + atlasW + "x" + atlasH + "px)");
            return true;
        }

        private static Texture2D CreateReadableCopy(Texture2D source)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            result.name = source.name + "_Readable";
            return result;
        }

        private static Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private static Texture2D CreatePlaceholder(int w, int h, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = "Placeholder";
            return tex;
        }
    }
}
