using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmPlantingInspection
    {
        [MenuItem("MiniFazendinha/Planting/Validate Installed Scene")]
        public static void ValidateInstalled()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();var log=new StringBuilder();
            if(scene.path!="Assets/MiniFazendinha/Scenes/FarmMain.unity")throw new System.InvalidOperationException("FarmMain required.");
            var plots=Object.FindObjectsByType<MiniFazendinha.Planting.FarmPlot>().OrderBy(p=>p.name).ToArray();
            if(plots.Length!=12)throw new System.Exception("Expected 12 plots.");
            foreach(var plot in plots)
            {
                var collider=plot.GetComponentInChildren<MeshCollider>();
                var view=plot.GetComponent<MiniFazendinha.Planting.FarmPlotView>();
                if(collider==null||!collider.enabled||view==null||plot.State!=MiniFazendinha.Planting.FarmPlotState.Empty)throw new System.Exception("Invalid plot "+plot.name);
                string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(plot.gameObject);
                if(!prefab.EndsWith("/Env_FarmLand_03.prefab"))throw new System.Exception("Original plot prefab link lost.");
                var data=new SerializedObject(view);foreach(string field in new[]{"cropAnchor","outline","progressTrack","progressFill","selectedMaterial","growingMaterial","readyMaterial"})if(data.FindProperty(field).objectReferenceValue==null)throw new System.Exception("Missing view reference: "+field);
                log.AppendLine("PASS: "+plot.name+" | original MeshCollider active | prefab="+prefab+" | Empty | view references valid");
            }
            var inventories=Object.FindObjectsByType<MiniFazendinha.Planting.FarmInventory>();
            var interactions=Object.FindObjectsByType<MiniFazendinha.Planting.FarmPlotInteractor>();
            var huds=Object.FindObjectsByType<MiniFazendinha.Planting.FarmHUD>();
            if(inventories.Length!=1||interactions.Length!=1||huds.Length!=1)throw new System.Exception("Expected one inventory, interaction and HUD.");
            var modules=Object.FindObjectsByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length!=1||modules.Length!=1)throw new System.Exception("Expected one Input System EventSystem.");
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true));
            int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));if(missing>0)throw new System.Exception("Missing scripts: "+missing);
            var inventoryData=new SerializedObject(inventories[0]).FindProperty("initialStock");
            if(inventoryData.arraySize!=3)throw new System.Exception("Expected 3 initial stock entries.");
            for(int i=0;i<inventoryData.arraySize;i++)
            {
                var item=inventoryData.GetArrayElementAtIndex(i);var crop=(MiniFazendinha.Planting.CropDefinition)item.FindPropertyRelative("crop").objectReferenceValue;
                if(crop==null||!crop.IsValid||item.FindPropertyRelative("quantity").intValue!=12||crop.GrowingVisual.prefab==null||crop.MatureVisual.prefab==null)throw new System.Exception("Invalid crop configuration.");
                log.AppendLine("PASS: "+AssetDatabase.GetAssetPath(crop)+" | seconds="+crop.GrowthSeconds+" | yield="+crop.YieldAmount+" | initial seeds=12");
                log.AppendLine("Growing visual: "+AssetDatabase.GetAssetPath(crop.GrowingVisual.prefab));log.AppendLine("Mature visual: "+AssetDatabase.GetAssetPath(crop.MatureVisual.prefab));
            }
            log.AppendLine("PASS: one inventory, interactor, HUD and Input System EventSystem; zero missing scripts.");
            log.AppendLine("RESULT: PASS");File.WriteAllText("Library/MiniFazendinha-PlantingSceneValidation.txt",log.ToString());Debug.Log("Installed planting scene validation PASS.");
        }
        [MenuItem("MiniFazendinha/Planting/Inspect Scene And Crops")]
        public static void Inspect()
        {
            var scene=SceneManager.GetActiveScene();var sb=new StringBuilder();
            sb.AppendLine("Scene: "+scene.path+" | dirty="+scene.isDirty);
            var plots=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("Canteiro_")).OrderBy(t=>t.name).ToArray();
            sb.AppendLine("Plots="+plots.Length);
            foreach(var plot in plots)
            {
                sb.AppendLine(plot.name+" | asset="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(plot.gameObject)+" | position="+plot.position+" | scale="+plot.lossyScale);
                foreach(var collider in plot.GetComponentsInChildren<Collider>(true))sb.AppendLine(" collider="+collider.GetType().Name+" | enabled="+collider.enabled+" | trigger="+collider.isTrigger+" | bounds="+collider.bounds);
                if(plot.GetComponentsInChildren<Collider>(true).Length==0)sb.AppendLine(" NO COLLIDERS");
            }
            sb.AppendLine("Canvas count="+Object.FindObjectsByType<Canvas>().Length);
            foreach(var script in AssetDatabase.FindAssets("t:MonoScript",new[]{"Assets/MiniFazendinha"}))sb.AppendLine("SCRIPT "+AssetDatabase.GUIDToAssetPath(script));
            foreach(var font in AssetDatabase.FindAssets("t:TMP_FontAsset"))sb.AppendLine("FONT "+AssetDatabase.GUIDToAssetPath(font));
            var paths=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/Pandazole_Lowpoly_Asset_Bundle/")&&p.EndsWith(".prefab")&&(Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains("carrot")||Path.GetFileNameWithoutExtension(p).ToLowerInvariant()=="food_corn"||Path.GetFileNameWithoutExtension(p)=="Env_Wheat"||Path.GetFileNameWithoutExtension(p)=="Env_GrassPlant_06")).ToArray();
            var preview=EditorSceneManager.NewPreviewScene();
            try {foreach(var path in paths) {var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview);var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length>0){var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);sb.AppendLine("CROP "+path+" | size="+bounds.size.ToString("F3")+" | center="+bounds.center.ToString("F3"));foreach(var mat in rs.SelectMany(r=>r.sharedMaterials).Distinct())if(mat!=null)sb.AppendLine(" MATERIAL "+AssetDatabase.GetAssetPath(mat)+" | shader="+mat.shader.name);}Object.DestroyImmediate(go);}}
            finally {EditorSceneManager.ClosePreviewScene(preview);}
            File.WriteAllText("Library/MiniFazendinha-PlantingInspection.txt",sb.ToString());Debug.Log("Planting inspection complete.");
        }
    }
}