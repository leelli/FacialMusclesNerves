using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace maleric
{
    public static class TextureArrayMaterialCreator
    {
        public static string DefaultOutputPath => MeshMergeSettings.Instance.GeneratedAssetsPath;

        /// <summary>
        /// Creates a Texture2DArray from a list of textures, and a material that samples it.
        /// </summary>
        public static bool CreateTextureArrayFromMaterials(
            List<Material> sourceMaterials,
            out Texture2DArray textureArray,
            out Material arrayMaterial,
            out int[] sliceIndices,
            string outputPath = null)
        {
            MeshMergeSettings.EnsureExists();
            textureArray = null;
            arrayMaterial = null;
            sliceIndices = null;

            if (sourceMaterials == null || sourceMaterials.Count == 0)
            {
                Debug.LogError("TextureArrayMaterialCreator: No source materials provided.");
                return false;
            }

            if (string.IsNullOrEmpty(outputPath))
                outputPath = DefaultOutputPath;
            Directory.CreateDirectory(outputPath);

            long inMemoryTextureKey = 0;

            var textures = new List<Texture2D>();
            sliceIndices = new int[sourceMaterials.Count];
            var textureKeyToSlice = new Dictionary<string, int>();
            var usedMaterialNames = new List<string>();

            for (int i = 0; i < sourceMaterials.Count; i++)
            {
                var mat = sourceMaterials[i];
                if (mat == null)
                {
                    textures.Add(CreatePlaceholderTexture(2, 2, Color.magenta));
                    sliceIndices[i] = textures.Count - 1;
                    usedMaterialNames.Add("null_" + i);
                    continue;
                }

                Texture tex = mat.GetTexture("_MainTex");
                if (tex == null) tex = mat.GetTexture("_BaseMap");
                if (tex == null) tex = mat.GetTexture("_Albedo");

                Texture2D tex2D = tex as Texture2D;

                if (tex2D == null)
                {
                    tex2D = CreatePlaceholderTexture(2, 2, Color.white);
                    Debug.LogWarning("TextureArrayMaterialCreator: Material '" + mat.name + "' has no readable texture. Using white placeholder.");
                }

                string texPath = AssetDatabase.GetAssetPath(tex2D);
                string key = !string.IsNullOrEmpty(texPath)
                    ? texPath
                    : "mem_" + (inMemoryTextureKey++);
                if (textureKeyToSlice.TryGetValue(key, out int existingSlice))
                {
                    sliceIndices[i] = existingSlice;
                    usedMaterialNames.Add(mat.name);
                    continue;
                }

                if (!string.IsNullOrEmpty(texPath))
                {
                    var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        var readableCopy = CreateReadableCopy(tex2D);
                        if (readableCopy != null)
                            tex2D = readableCopy;
                    }
                }

                textures.Add(tex2D);
                sliceIndices[i] = textures.Count - 1;
                textureKeyToSlice[key] = sliceIndices[i];
                usedMaterialNames.Add(mat.name);
            }

            if (!ValidateAndResizeTextures(textures, out int width, out int height))
            {
                Debug.LogError("TextureArrayMaterialCreator: Textures could not be resized to a common size.");
                return false;
            }

            int count = textures.Count;
            TextureFormat format = GetCommonFormat(textures);
            bool hasMips = textures[0].mipmapCount > 1;

            textureArray = new Texture2DArray(width, height, count, format, hasMips, false);
            textureArray.name = "TextureArray_" + string.Join("_", usedMaterialNames);
            textureArray.filterMode = FilterMode.Trilinear;
            textureArray.wrapMode = TextureWrapMode.Repeat;
            textureArray.anisoLevel = Mathf.Max(4, textures[0].anisoLevel);

            for (int i = 0; i < count; i++)
            {
                Texture2D src = textures[i];
                if (src.width != width || src.height != height)
                {
                    var resized = ResizeTexture(src, width, height);
                    if (src != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(src)))
                        Object.DestroyImmediate(src);
                    src = resized;
                    textures[i] = src;
                }

                textureArray.SetPixels(src.GetPixels(0, 0, width, height), i, 0);

                if (hasMips)
                {
                    int mipCount = textureArray.mipmapCount;
                    for (int mip = 1; mip < mipCount; mip++)
                    {
                        int mipW = Mathf.Max(1, width >> mip);
                        int mipH = Mathf.Max(1, height >> mip);
                        Texture2D downscaled = ResizeTexture(src, mipW, mipH);
                        textureArray.SetPixels(downscaled.GetPixels(), i, mip);
                        Object.DestroyImmediate(downscaled);
                    }
                }
            }

            textureArray.Apply(false, true);

            string arrayPath = outputPath + "/" + textureArray.name + ".asset";
            arrayPath = AssetDatabase.GenerateUniqueAssetPath(arrayPath);
            AssetDatabase.CreateAsset(textureArray, arrayPath);

            string shaderName = MeshMergeSettings.Instance.GetTextureArrayShader();
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("TextureArrayMaterialCreator: Could not find shader '" + shaderName + "'!");
                Object.DestroyImmediate(textureArray);
                return false;
            }

            arrayMaterial = new Material(shader);
            arrayMaterial.name = textureArray.name + "_Mat";
            arrayMaterial.SetTexture("_MainTexArr", textureArray);

            Color avgColor = Color.white;
            float avgMetallic = 0f;
            float avgSmoothness = 0.5f;
            if (sourceMaterials.Count > 0)
            {
                var primary = sourceMaterials[0];
                if (primary.HasProperty("_Color"))
                    avgColor = primary.GetColor("_Color");
                else if (primary.HasProperty("_BaseColor"))
                    avgColor = primary.GetColor("_BaseColor");

                if (primary.HasProperty("_Metallic"))
                    avgMetallic = primary.GetFloat("_Metallic");
                if (primary.HasProperty("_Smoothness"))
                    avgSmoothness = primary.GetFloat("_Smoothness");
                else if (primary.HasProperty("_Glossiness"))
                    avgSmoothness = primary.GetFloat("_Glossiness");
            }
            arrayMaterial.SetColor("_BaseColor", avgColor);
            arrayMaterial.SetFloat("_Metallic", avgMetallic);
            arrayMaterial.SetFloat("_Smoothness", avgSmoothness);
            arrayMaterial.SetFloat("_Occlusion", 1.0f);

            string matPath = outputPath + "/" + arrayMaterial.name + ".mat";
            matPath = AssetDatabase.GenerateUniqueAssetPath(matPath);
            AssetDatabase.CreateAsset(arrayMaterial, matPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            for (int i = 0; i < textures.Count; i++)
            {
                string texPath2 = AssetDatabase.GetAssetPath(textures[i]);
                if (string.IsNullOrEmpty(texPath2) && textures[i] != null)
                    Object.DestroyImmediate(textures[i]);
            }

            Debug.Log("TextureArrayMaterialCreator: Created '" + textureArray.name + "' with " + count + " slices, saved to " + arrayPath);
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

        private static bool ValidateAndResizeTextures(List<Texture2D> textures, out int width, out int height)
        {
            width = textures[0].width;
            height = textures[0].height;

            bool allSame = true;
            for (int i = 1; i < textures.Count; i++)
            {
                if (textures[i].width != width || textures[i].height != height)
                {
                    allSame = false;
                    break;
                }
            }

            if (allSame) return true;

            var sizeCounts = new Dictionary<(int, int), int>();
            for (int i = 0; i < textures.Count; i++)
            {
                var key = (textures[i].width, textures[i].height);
                if (sizeCounts.ContainsKey(key)) sizeCounts[key]++;
                else sizeCounts[key] = 1;
            }

            int maxCount = 0;
            foreach (var kvp in sizeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    width = kvp.Key.Item1;
                    height = kvp.Key.Item2;
                }
            }

            Debug.LogWarning("TextureArrayMaterialCreator: Textures have different sizes. Resizing all to " + width + "x" + height + ".");
            return true;
        }

        private static TextureFormat GetCommonFormat(List<Texture2D> textures)
        {
            TextureFormat format = textures[0].format;
            for (int i = 1; i < textures.Count; i++)
            {
                if (textures[i].format != format)
                {
                    Debug.LogWarning("TextureArrayMaterialCreator: Textures have different formats. Using RGBA32.");
                    return TextureFormat.RGBA32;
                }
            }
            return format;
        }

        private static Texture2D CreatePlaceholderTexture(int w, int h, Color color)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = "Placeholder_" + color.r + "_" + color.g + "_" + color.b;
            return tex;
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

        public static string GetArrayMaterialGUID(Material arrayMaterial)
        {
            if (arrayMaterial == null) return null;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(arrayMaterial, out string guid, out long _))
                return guid;
            return null;
        }
    }
}

