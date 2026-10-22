using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using MiniFazendinha.FruitTrees;
namespace MiniFazendinha.EditorTools
{
    public static class FarmV36Setup
    {
        const string ResourcesFolder="Assets/MiniFazendinha/Resources/FarmUI/";
        static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value){var so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj);}
        static void ImportSprite(string path){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;i.alphaIsTransparency=true;i.mipmapEnabled=false;i.textureCompression=TextureImporterCompression.Uncompressed;if(path.Contains("RoundedCard"))i.spriteBorder=new Vector4(24,24,24,24);i.SaveAndReimport();}
        [MenuItem("MiniFazendinha/V3.6/Apply presentation and tree materials")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
            Directory.CreateDirectory(ResourcesFolder);Directory.CreateDirectory("Assets/MiniFazendinha/Prefabs/Trees");Directory.CreateDirectory("Assets/MiniFazendinha/Materials/Trees");
            foreach(var path in Directory.GetFiles("Assets/MiniFazendinha/UI/Icons","*.png")){string dest=ResourcesFolder+Path.GetFileName(path);if(!File.Exists(dest)){File.Copy(path,dest);ImportSprite(dest);}}
            foreach(var guid in AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"}))
            {
                var def=AssetDatabase.LoadAssetAtPath<FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(guid));string name=def.name;string source="Assets/ThirdParty/VoxelFarm/FBX/Environment/TVS_VoxelFarm_"+name+".fbx";string texture="Assets/ThirdParty/VoxelFarm/Textures/Environment/TVS_VoxelFarm_"+name+"_Texture.png";
                var map=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);if(map==null)throw new Exception("Missing original tree palette: "+texture);
                string materialPath="Assets/MiniFazendinha/Materials/Trees/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,materialPath);}mat.SetTexture("_BaseMap",map);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",.1f);EditorUtility.SetDirty(mat);
                var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source));model.name=name;foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();string prefabPath="Assets/MiniFazendinha/Prefabs/Trees/"+name+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(model,prefabPath);UnityEngine.Object.DestroyImmediate(model);Set(def,"modelPrefab",prefab);
                foreach(var tree in UnityEngine.Object.FindObjectsByType<FruitTree>())if(tree.Definition==def)foreach(var r in tree.GetComponentsInChildren<Renderer>(true))if(r.GetComponent<MeshFilter>()!=null&&r.name.Contains("Tree"))r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
            }
            var barn=GameObject.Find("Celeiro");if(barn==null)throw new Exception("Warehouse building not found");if(barn.GetComponent<FarmWarehouseBuilding>()==null)barn.AddComponent<FarmWarehouseBuilding>();if(barn.GetComponentsInChildren<Collider>().Length==0){var b=barn.AddComponent<BoxCollider>();b.center=new Vector3(0,1.5f,0);b.size=new Vector3(4,3,4);}
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();foreach(var t in hud.Warehouse.ToggleButton.GetComponentsInChildren<TMP_Text>(true))t.text="Cultivos";
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniFazendinha/Materials/PenWood.mat");
            foreach(var a in UnityEngine.Object.FindObjectsByType<FarmAnimalController>())
            {int index=UnityEngine.Object.FindObjectsByType<FarmAnimalController>().Where(b=>b.Home==a.Home).OrderBy(b=>b.AnimalId).ToList().IndexOf(a);a.transform.position=a.Home.transform.position+new Vector3(index%2==0?-.82f:.82f,.075f,-.85f+(index/2)*1.6f);}
            foreach(var point in UnityEngine.Object.FindObjectsByType<FeedingPoint>())
            {var so=new SerializedObject(point);var stand=so.FindProperty("animalStandPoint").objectReferenceValue as Transform;if(stand!=null)stand.localPosition=new Vector3(0,0,-1.4f);
                var fill=point.transform.Find("NivelV36");if(fill==null){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="NivelV36";fill=g.transform;fill.SetParent(point.transform,false);fill.localPosition=new Vector3(0,.18f,0);fill.localScale=new Vector3(.58f,.12f,.24f);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());string path="Assets/MiniFazendinha/Materials/"+point.Kind+"Fill.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=point.Kind==FeedingPointKind.Water?new Color(.23f,.60f,.79f):new Color(.78f,.56f,.20f);AssetDatabase.CreateAsset(mat,path);}g.GetComponent<Renderer>().sharedMaterial=mat;}Set(point,"fillVisual",fill);}
            foreach(var shelter in UnityEngine.Object.FindObjectsByType<AnimalShelter>().Where(s=>s.PermittedSpecies!=null))
            {
                var settings=new SerializedObject(shelter);settings.FindProperty("pathPoint").vector3Value=new Vector3(13.5f,.075f,shelter.GatePoint.z);settings.ApplyModifiedPropertiesWithoutUndo();Portrait(shelter.PermittedSpecies);var old=shelter.transform.Find("Identificacao");if(old!=null)old.gameObject.SetActive(false);
                var gate=shelter.transform.Find("PorteiraV36");if(gate==null){var g=new GameObject("PorteiraV36");gate=g.transform;gate.SetParent(shelter.transform,true);gate.position=shelter.GatePoint+new Vector3(0,.45f,0);for(int n=0;n<2;n++){var rail=GameObject.CreatePrimitive(PrimitiveType.Cube);rail.name="Travessa";rail.transform.SetParent(gate,false);rail.transform.localPosition=new Vector3(0,n==0?-.17f:.17f,0);rail.transform.localScale=new Vector3(.1f,.1f,1.65f);rail.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());}var c=g.AddComponent<BoxCollider>();c.size=new Vector3(.12f,.6f,1.65f);}Set(shelter,"gateVisual",gate);shelter.SetGate(shelter.GateOpen);
                var sign=shelter.transform.Find("PlacaV36");if(sign==null){var g=new GameObject("PlacaV36");sign=g.transform;sign.SetParent(shelter.transform,true);sign.position=shelter.GatePoint+new Vector3(0,.75f,-1.25f);var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.transform.SetParent(sign,false);board.transform.localScale=new Vector3(.65f,.5f,.08f);board.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(board.GetComponent<Collider>());var post=GameObject.CreatePrimitive(PrimitiveType.Cube);post.transform.SetParent(sign,false);post.transform.localPosition=new Vector3(0,-.3f,0);post.transform.localScale=new Vector3(.08f,.7f,.08f);post.GetComponent<Renderer>().sharedMaterial=wood;UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());var face=new GameObject("Espécie",typeof(SpriteRenderer));face.transform.SetParent(sign,false);face.transform.localPosition=new Vector3(0,0,-.055f);face.transform.localRotation=Quaternion.Euler(0,180,0);var sr=face.GetComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ResourcesFolder+shelter.PermittedSpecies.Identifier+".png");face.transform.localScale=Vector3.one*.2f;var click=g.AddComponent<BoxCollider>();click.size=new Vector3(.7f,.65f,.18f);click.isTrigger=true;}
            }
            AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AuditMaterials();
        }
        static void Portrait(AnimalDefinition definition)
        {
            string path=ResourcesFolder+definition.Identifier+".png";
            var settings=new SerializedObject(definition);settings.FindProperty("nutritionDuration").floatValue=1800;settings.FindProperty("hydrationDuration").floatValue=1800;settings.FindProperty("careInterval").floatValue=30;settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(definition);var model=UnityEngine.Object.Instantiate(definition.VisualPrefab);model.transform.position=new Vector3(0,-500,0);model.transform.rotation=Quaternion.Euler(0,155,0);foreach(var t in model.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;foreach(var a in model.GetComponentsInChildren<Animator>())UnityEngine.Object.DestroyImmediate(a);
            foreach(var r in model.GetComponentsInChildren<Renderer>())if(!(r is SkinnedMeshRenderer))r.enabled=false;var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);var cg=new GameObject("PortraitCamera",typeof(Camera));var camera=cg.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.55f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.transform.position=bounds.center+new Vector3(0,.5f,-4);camera.transform.LookAt(bounds.center);var light=new GameObject("PortraitLight",typeof(Light));light.GetComponent<Light>().type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.GetComponent<Light>().intensity=1.8f;light.GetComponent<Light>().cullingMask=1<<31;
            var rt=new RenderTexture(256,256,24);var tex=new Texture2D(256,256,TextureFormat.RGBA32,false);var prev=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=prev;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(cg);UnityEngine.Object.DestroyImmediate(light);UnityEngine.Object.DestroyImmediate(model);ImportSprite(path);
        }
        [MenuItem("MiniFazendinha/V3.6/Audit repaired tree materials")]
        public static void AuditMaterials(){var log=new System.Text.StringBuilder();foreach(var guid in AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"})){var d=AssetDatabase.LoadAssetAtPath<FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(guid));foreach(var r in d.ModelPrefab.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials){if(m==null||m.GetTexture("_BaseMap")==null)throw new Exception("Untextured tree "+d.name);log.AppendLine(d.name+": "+AssetDatabase.GetAssetPath(m)+" -> "+AssetDatabase.GetAssetPath(m.GetTexture("_BaseMap")));}}Directory.CreateDirectory("Reports/V36");File.WriteAllText("Reports/V36/02-tree-materials.txt",log.ToString());}
    }
}
