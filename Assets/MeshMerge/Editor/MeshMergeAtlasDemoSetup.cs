using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;

namespace maleric
{
    public static class MeshMergeAtlasDemoSetup
    {
        [MenuItem("GameObject/MeshMerge/Create Atlas Demo", false, 51)]
        public static void CreateAtlasDemo()
        {
            string texDir = "Assets/MeshMerge/Examples/AtlasDemo_Textures";
            Directory.CreateDirectory(texDir);

            var root = new GameObject("Atlas Demo");

            var colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };
            var names = new string[] { "Red", "Green", "Blue", "Yellow" };

            for (int i = 0; i < 4; i++)
            {
                int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
                var pixels = new Color[size * size];
                for (int p = 0; p < pixels.Length; p++)
                {
                    int x = p % size;
                    int y = p / size;
                    float stripe = (y / 8) % 2;
                    pixels[p] = Color.Lerp(colors[i], Color.Lerp(colors[i], Color.white, 0.5f), stripe);
                }
                tex.SetPixels(pixels);
                tex.Apply(true);
                tex.name = "AtlasDemo_" + names[i];

                string texAssetPath = texDir + "/AtlasDemo_" + names[i] + ".asset";
                AssetDatabase.CreateAsset(tex, texAssetPath);

                bool isURP = PipelineUtils.IsURP();
                string shaderName = PipelineUtils.GetLitShaderName();
                var mat = new Material(Shader.Find(shaderName));
                mat.name = "Mat_" + names[i];
                if (isURP)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetColor("_BaseColor", Color.white);
                }
                else
                {
                    mat.SetTexture("_MainTex", tex);
                    mat.SetColor("_Color", Color.white);
                }
                string matPath = texDir + "/Mat_" + names[i] + ".mat";
                AssetDatabase.CreateAsset(mat, matPath);

                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = names[i] + " Cube";
                cube.transform.SetParent(root.transform);
                cube.transform.position = new Vector3(i * 1.5f, 0, 0);
                cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var combiner = root.AddComponent<MergeableMeshOwner>();
            combiner.UseNewInstance = true;
            combiner.UpdateMeshObjectInputData();

            Undo.RegisterCreatedObjectUndo(root, "Create Atlas Demo");
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog("Atlas Demo Created",
                "4 colored cubes, each with a unique striped texture.\n\n" +
                "Current: 4 draw calls\n\n" +
                "NEXT STEPS:\n" +
                "1. Select '" + root.name + "' in the Hierarchy\n" +
                "2. Click 'Bake to 1 Material (Atlas)'\n" +
                "→ 4 textures packed into 1 atlas → 1 draw call!\n" +
                "Child renderers auto-disabled after merge.",
                "OK");
        }
    }
}

