using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmV3MaterialRepair
    {
        const string Root = "Assets/MiniFazendinha/Materials/Animals";
        static readonly string[] Species = { "Cow", "Chicken", "Sheep", "Pig", "Duck" };

        [MenuItem("MiniFazendinha/V3.1/Repair Voxel Farm animal palettes")]
        public static void Repair()
        {
            EnsureFolder(Root);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader not found.");
            var materials = new Dictionary<string, Material>();
            foreach (var species in Species)
            {
                string texturePath = $"Assets/ThirdParty/VoxelFarm/Textures/Animals/TVS_VoxelFarm_{species}_Texture.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null) throw new FileNotFoundException("Animal palette not found: " + texturePath);
                var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter != null && (textureImporter.mipmapEnabled || textureImporter.filterMode != FilterMode.Point || textureImporter.wrapMode != TextureWrapMode.Clamp))
                {
                    textureImporter.mipmapEnabled = false;
                    textureImporter.filterMode = FilterMode.Point;
                    textureImporter.wrapMode = TextureWrapMode.Clamp;
                    textureImporter.SaveAndReimport();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                }

                string materialPath = $"{Root}/VoxelFarm_{species}_URP.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.shader = shader;
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0f);
                EditorUtility.SetDirty(material);
                materials.Add(species, material);
            }
            AssetDatabase.SaveAssets();

            int rendererCount = 0;
            foreach (var species in Species)
            {
                string prefabPath = $"Assets/MiniFazendinha/Prefabs/Animals/{species}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) is GameObject)
                {
                    var root = PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            renderer.sharedMaterial = materials[species];
                            rendererCount++;
                        }
                        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                }
            }

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != "06_Animais") continue;
                foreach (var animal in root.GetComponentsInChildren<Transform>(true))
                {
                    string species = SpeciesFor(animal.name);
                    if (species == null) continue;
                    foreach (var renderer in animal.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        renderer.sharedMaterial = materials[species];
                        EditorUtility.SetDirty(renderer);
                        rendererCount++;
                    }
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MiniFazendinha V3.1] Applied URP/Lit and original point-sampled palette textures to {rendererCount} animal renderers. Source model and PNG pixels unchanged.");
        }

        static string SpeciesFor(string name)
        {
            if (name.StartsWith("Vaca", StringComparison.OrdinalIgnoreCase)) return "Cow";
            if (name.StartsWith("Galinha", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Galo", StringComparison.OrdinalIgnoreCase)) return "Chicken";
            if (name.StartsWith("Ovelha", StringComparison.OrdinalIgnoreCase)) return "Sheep";
            if (name.StartsWith("Porco", StringComparison.OrdinalIgnoreCase)) return "Pig";
            if (name.StartsWith("Pato", StringComparison.OrdinalIgnoreCase)) return "Duck";
            return null;
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}