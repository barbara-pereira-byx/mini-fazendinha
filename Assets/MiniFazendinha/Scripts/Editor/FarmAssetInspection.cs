using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmAssetInspection
    {
        [MenuItem("MiniFazendinha/Inspect Assets")]
        public static void Inspect()
        {
            var sb = new StringBuilder();
            string root = "Assets/Pandazole_Lowpoly_Asset_Bundle/";
            var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(root) && p.EndsWith(".prefab") &&
                (p.Contains("Farm Ranch Pack") || p.EndsWith("Tree_02_Spring.prefab") || p.EndsWith("Bush_04.prefab") || p.EndsWith("Flower_23.prefab"))).OrderBy(p => p).ToArray();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (string path in paths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, preview);
                    var renderers = go.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var b = renderers[0].bounds;
                        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
                        sb.AppendLine(path + " | bounds=" + b.size.ToString("F2") + " | center=" + b.center.ToString("F2") + " | scale=" + go.transform.localScale.ToString("F2"));
                        foreach (var mat in renderers.SelectMany(r => r.sharedMaterials).Where(m=>m != null).Distinct())
                            sb.AppendLine("  MATERIAL " + AssetDatabase.GetAssetPath(mat) + " | " + mat.shader.name + " | supported=" + mat.shader.isSupported);
                    }
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            File.WriteAllText("Library/MiniFazendinha-Assets.txt", sb.ToString());
            Debug.Log("MiniFazendinha asset inspection complete: " + paths.Length + " prefabs.");
        }
    }
}