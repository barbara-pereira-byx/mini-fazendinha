using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmV2AssetAudit
    {
        const string Root = "Assets/ThirdParty/VoxelFarm";
        const string ReportPath = "Assets/MiniFazendinha/Settings/V2/VoxelFarmAssetAudit.txt";
        static readonly string[] AnimalFiles =
        {
            "TVS_VoxelFarm_Cow.fbx", "TVS_VoxelFarm_Chicken.fbx", "TVS_VoxelFarm_Pig.fbx",
            "TVS_VoxelFarm_Sheep.fbx", "TVS_VoxelFarm_Duck.fbx", "TVS_VoxelFarm_Goat.fbx",
            "TVS_VoxelFarm_Rooster.fbx", "TVS_VoxelFarm_Bull.fbx", "TVS_VoxelFarm_Mallard.fbx"
        };

        [MenuItem("MiniFazendinha/V2/Audit Voxel Farm Assets")]
        public static void Run()
        {
            var report = new StringBuilder();
            report.AppendLine("Voxel Farm imported asset audit");
            report.AppendLine("Asset root: " + Root);
            report.AppendLine("Date: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var modelFiles = Directory.Exists(Root)
                ? Directory.GetFiles(Root, "*.fbx", SearchOption.AllDirectories)
                    .Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray()
                : Array.Empty<string>();
            report.AppendLine("FBX files: " + modelFiles.Length);
            var animalTransforms = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in modelFiles)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(file);
                var importer = AssetImporter.GetAtPath(file) as ModelImporter;
                report.AppendLine();
                report.AppendLine("MODEL " + file);
                report.AppendLine("  exists=" + (root != null) + "; rig=" + (importer == null ? "n/a" : importer.animationType.ToString()) +
                                  "; importAnimation=" + (importer != null && importer.importAnimation) +
                                  "; materialMode=" + (importer == null ? "n/a" : importer.materialImportMode.ToString()));
                if (root == null) continue;
                var transformPaths = new HashSet<string>(StringComparer.Ordinal);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    transformPaths.Add(TransformPath(t, root.transform));
                if (file.Contains("/FBX/Animals/"))
                {
                    string key = Path.GetFileNameWithoutExtension(file).Replace("TVS_VoxelFarm_", "");
                    animalTransforms[key] = transformPaths;
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    string bounds = mesh == null ? "n/a" : mesh.bounds.size.ToString("F2");
                    string mats = string.Join(", ", renderer.sharedMaterials.Select(m =>
                        m == null ? "null" : m.name + "[" + (m.shader == null ? "no shader" : m.shader.name) + "]"));
                    report.AppendLine("  renderer=" + renderer.GetType().Name + "/" + renderer.name + "; meshBounds=" + bounds + "; materials=" + mats);
                }
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>())
                    AppendClip(report, file, clip, transformPaths);
            }
            var animationFiles = Directory.Exists(Root + "/Animations/Animals")
                ? Directory.GetFiles(Root + "/Animations/Animals", "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                : Enumerable.Empty<string>();
            report.AppendLine();
            report.AppendLine("SEPARATE ANIMAL ANIMATIONS");
            foreach (var file in animationFiles)
            {
                string species = Path.GetFileNameWithoutExtension(file).Split('_')[0];
                animalTransforms.TryGetValue(species, out var targetPaths);
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>())
                    AppendClip(report, file, clip, targetPaths);
                if (!AssetDatabase.LoadAllAssetsAtPath(file).OfType<AnimationClip>().Any())
                    report.AppendLine("NO CLIP | " + file);
            }
            report.AppendLine();
            report.AppendLine("ANIMAL MODEL FILES (expected list)");
            foreach (string name in AnimalFiles)
                report.AppendLine((modelFiles.Any(p => Path.GetFileName(p).Equals(name, StringComparison.OrdinalIgnoreCase)) ? "FOUND " : "MISSING ") + name);
            report.AppendLine();
            report.AppendLine("SCENE LAYOUT");
            var scene = SceneManager.GetActiveScene();
            report.AppendLine("scene=" + scene.path + "; dirty=" + scene.isDirty);
            foreach (var sceneRoot in scene.GetRootGameObjects())
            foreach (var t in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                bool relevant = t.name == "Gramado_Pastagem" || t.name == "Casa_Principal" || t.name == "Celeiro" ||
                                t.name.StartsWith("Cerca_", StringComparison.OrdinalIgnoreCase) ||
                                t.name.StartsWith("Caminho_", StringComparison.OrdinalIgnoreCase) ||
                                t.name == "Main Camera";
                if (!relevant) continue;
                var collider = t.GetComponent<Collider>();
                report.AppendLine("SCENEOBJ " + AnimationUtility.CalculateTransformPath(t, sceneRoot.transform) +
                    "; position=" + t.position.ToString("F2") + "; scale=" + t.lossyScale.ToString("F2") +
                    "; bounds=" + (collider == null ? "no collider" : collider.bounds.ToString()));
            }
            string absolute = Path.GetFullPath(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(ReportPath);
            Debug.Log("[MiniFazendinha] Voxel Farm audit written to " + ReportPath);
        }

        static string TransformPath(Transform target, Transform root)
        {
            var names = new Stack<string>();
            var cursor = target;
            while (cursor != null && cursor != root) { names.Push(cursor.name); cursor = cursor.parent; }
            return string.Join("/", names);
        }

        static void AppendClip(StringBuilder report, string file, AnimationClip clip, HashSet<string> targetPaths)
        {
            var used = new HashSet<string>(AnimationUtility.GetCurveBindings(clip)
                .Where(binding => !string.IsNullOrEmpty(binding.path)).Select(binding => binding.path), StringComparer.Ordinal);
            var missing = targetPaths == null ? used.ToArray() : used.Where(path => !targetPaths.Contains(path)).ToArray();
            report.AppendLine("CLIP " + file + " :: " + clip.name + "; length=" + clip.length.ToString("F2") +
                "s; fps=" + clip.frameRate.ToString("F1") + "; boundPaths=" + used.Count +
                "; skeletonPathMatch=" + (targetPaths != null && missing.Length == 0) +
                "; unmatchedPaths=" + string.Join(",", missing));
        }
    }
}