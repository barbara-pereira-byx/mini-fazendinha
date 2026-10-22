using System;
using System.Collections.Generic;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniFazendinha.EditorTools
{
    public static class FarmV3PenSetup
    {
        const string FencePath = "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Env_WoodFence_05.prefab";
        const string BarnPath = "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Bld_Barn_02.prefab";
        const string CoopPath = "Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/Bld_ChickenCoop.prefab";
        const string FoodModel = "Assets/ThirdParty/VoxelFarm/FBX/Environment/TVS_VoxelFarm_FoodTrough.fbx";
        const string WaterModel = "Assets/ThirdParty/VoxelFarm/FBX/Environment/TVS_VoxelFarm_WaterTrough.fbx";

        sealed class Pen
        {
            public string id, label, prefix, definition, feedCrop, shelterPrefab;
            public Vector3 center;
            public AnimalShelter shelter;
            public FeedingPoint food, water;
        }

        [MenuItem("MiniFazendinha/V3.1/Build species pens")]
        [MenuItem("MiniFazendinha/V3.1/Build species pens")]
        public static void Build()
        {
            try { BuildImpl(); }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText("Assets/MiniFazendinha/Settings/V3_BuildError.txt", ex.ToString());
                throw;
            }
        }

        static void BuildImpl()
        {
            var scene = SceneManager.GetActiveScene();
            var animalsRoot = GameObject.Find("06_Animais");
            var fencesRoot = GameObject.Find("04_Cercas");
            var fencePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FencePath);
            if (animalsRoot == null || fencesRoot == null || fencePrefab == null)
                throw new InvalidOperationException("Farm animal/fence roots or Pandazole fence prefab are missing.");

            var pens = new[]
            {
                NewPen("cows", "Curral bovino", "Vaca", "Cow", "Wheat", "11.25,-11.5", BarnPath),
                NewPen("chickens", "Galinheiro", "Galinha", "Chicken", "Corn", "15.75,-11.5", CoopPath),
                NewPen("sheep", "Aprisco", "Ovelha", "Sheep", "Wheat", "11.25,-6.5", BarnPath),
                NewPen("pigs", "Chiqueiro", "Porco", "Pig", "Pumpkin", "15.75,-6.5", BarnPath),
                NewPen("ducks", "Abrigo dos patos", "Pato", "Duck", "Corn", "15.75,-1.5", CoopPath)
            };
            BuildDividers(fencesRoot.transform, fencePrefab);
            foreach (var pen in pens) BuildPen(animalsRoot.transform, pen);
            AssignAnimals(animalsRoot.transform, pens);
            SetOldCareObjectsInactive(animalsRoot.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[MiniFazendinha V3.1] Created five species pens with Pandazole wood-fence dividers, individual shelters, crop-specific feeders and water troughs.");
        }

        static Pen NewPen(string id, string label, string prefix, string def, string feed, string center, string shelter)
        {
            var parts = center.Split(',');
            return new Pen
            {
                id=id, label=label, prefix=prefix, definition=def, feedCrop=feed,
                center=new Vector3(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),0,float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture)),
                shelterPrefab=shelter
            };
        }

        static void BuildDividers(Transform parent, GameObject prefab)
        {
            Transform existing = parent.Find("V3_Especies");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var root = new GameObject("V3_Especies").transform;
            root.SetParent(parent, false);
            // Existing outer paddock spans about 9 x 15 units. Two columns and three rows create five small species areas.
            var sceneFence = parent.Find("Cerca_Sul_0");
            GameObject template = sceneFence == null ? prefab : sceneFence.gameObject;
            for (int z=-12; z<=0; z+=3) PlaceFence(root,template,new Vector3(13.5f,.02f,z-.5f),Quaternion.Euler(0,90,0),"Divisoria_vertical");
            foreach (float z in new[]{-9f,-4f})
                foreach (float x in new[]{10.5f,13.5f,16.5f})
                    PlaceFence(root,template,new Vector3(x,.02f,z),Quaternion.identity,"Divisoria_horizontal");
        }

        static void PlaceFence(Transform parent, GameObject prefab, Vector3 position, Quaternion rotation, string name)
        {
            var go=UnityEngine.Object.Instantiate(prefab);
            go.name=name;
            go.transform.SetParent(parent,false);
            go.transform.SetPositionAndRotation(position,rotation);
            ApplyUrpMaterials(go);
        }

        static void BuildPen(Transform root, Pen pen)
        {
            var existing=root.Find("Abrigo_"+pen.id);
            if(existing!=null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var shelterRoot=new GameObject("Abrigo_"+pen.id);
            shelterRoot.transform.SetParent(root,false);
            shelterRoot.transform.position=pen.center+new Vector3(0,0,-1.75f);
            var shelter=Undo.AddComponent<AnimalShelter>(shelterRoot);
            var bounds=new GameObject("Area_"+pen.id);
            bounds.transform.SetParent(shelterRoot.transform,false);
            bounds.transform.localPosition=new Vector3(0,.1f,1.75f);
            var volume=bounds.AddComponent<BoxCollider>();
            volume.isTrigger=true;
            volume.size=new Vector3(4.15f,.12f,4.65f);
            Set(shelter,"shelterId",pen.id);
            Set(shelter,"roamingArea",volume);
            Set(shelter,"gatePoint",pen.center+new Vector3(0,.03f,-.8f));
            Set(shelter,"pathPoint",pen.center+new Vector3(.1f,.03f,.4f));
            Set(shelter,"entrancePoint",pen.center+new Vector3(.1f,.03f,-1.35f));
            Set(shelter,"boundaryInset",.45f);

            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(pen.shelterPrefab);
            if(prefab!=null)
            {
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                visual.name="Visual_"+pen.id;
                visual.transform.SetParent(shelterRoot.transform,false);
                visual.transform.localPosition=Vector3.zero;
                visual.transform.localScale=Vector3.one*(pen.shelterPrefab==CoopPath?.42f:.28f);
                ApplyUrpMaterials(visual);
            }
            pen.shelter=shelter;
            var feed=AssetDatabase.LoadAssetAtPath<CropDefinition>("Assets/MiniFazendinha/Settings/Crops/"+pen.feedCrop+".asset");
            var water=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>("Assets/MiniFazendinha/Settings/Inventory/Resources/Water.asset");
            if(feed==null||water==null)throw new InvalidOperationException("Pen feed or water definition missing.");
            pen.food=CreateTrough(root,pen,"Comedouro_"+pen.id,FeedingPointKind.Food,feed,FoodModel,pen.center+new Vector3(-1.15f,.05f,.65f));
            pen.water=CreateTrough(root,pen,"Bebedouro_"+pen.id,FeedingPointKind.Water,water,WaterModel,pen.center+new Vector3(1.15f,.05f,.65f));
        }

        static FeedingPoint CreateTrough(Transform root,Pen pen,string name,FeedingPointKind kind,InventoryItemDefinition item,string modelPath,Vector3 position)
        {
            var old=root.Find(name); if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=new GameObject(name);
            go.transform.SetParent(root,false);go.transform.position=position;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if(model==null)throw new InvalidOperationException("Voxel Farm trough model missing: "+modelPath);
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.name="Visual";
            visual.transform.SetParent(go.transform,false);visual.transform.localScale=Vector3.one*.4f;
            var collider=go.AddComponent<BoxCollider>();collider.center=new Vector3(0,.25f,0);collider.size=new Vector3(.9f,.55f,.85f);
            var point=go.AddComponent<FeedingPoint>();
            Set(point,"kind",kind);Set(point,"acceptedItem",item);Set(point,"initialUnits",0);Set(point,"capacity",20);
            var stand=new GameObject("AnimalStandPoint").transform;stand.SetParent(go.transform,false);stand.localPosition=new Vector3(0,0,.75f);
            Set(point,"animalStandPoint",stand);
            string materialPath="Assets/MiniFazendinha/Materials/Animals/VoxelFarm_"+(kind==FeedingPointKind.Food?"FoodTrough":"WaterTrough")+"_URP.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;
            EditorUtility.SetDirty(point);
            return point;
        }

        static void AssignAnimals(Transform animalsRoot,Pen[] pens)
        {
            foreach(var animal in animalsRoot.GetComponentsInChildren<FarmAnimalController>(true))
            {
                Pen chosen=null;
                foreach(var pen in pens)if(animal.name.StartsWith(pen.prefix,StringComparison.OrdinalIgnoreCase)){chosen=pen;break;}
                if(chosen==null)continue;
                Set(animal,"home",chosen.shelter);
                Set(animal,"feedingPoints",new[]{chosen.food,chosen.water});
                var list=new List<FarmAnimalController>();
                foreach(var other in animalsRoot.GetComponentsInChildren<FarmAnimalController>(true))
                    if(other.name.StartsWith(chosen.prefix,StringComparison.OrdinalIgnoreCase))list.Add(other);
                int index=list.IndexOf(animal);
                float ox=(index%2==0?-.55f:.55f);
                float oz=(index/2)*.55f;
                animal.transform.position=chosen.center+new Vector3(ox,.03f,oz);
                EditorUtility.SetDirty(animal);
            }
        }

        static void SetOldCareObjectsInactive(Transform root)
        {
            foreach(string name in new[]{"Comedouro","Comedouro_Milho","Comedouro_Abobora","Bebedouro"})
            {
                var old=root.Find(name);
                if(old!=null&&!old.name.StartsWith("Comedouro_")&&!old.name.StartsWith("Bebedouro_"))old.gameObject.SetActive(false);
                else if(old!=null)old.gameObject.SetActive(false);
            }
        }

        static void ApplyUrpMaterials(GameObject root)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++) mats[i]=MakeUrpMaterial(mats[i]);
                renderer.sharedMaterials=mats;
            }
        }

        static Material MakeUrpMaterial(Material source)
        {
            if(source==null)return null;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null||source.shader==shader)return source;
            const string folder="Assets/MiniFazendinha/Materials/Animals/Compatibility";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/MiniFazendinha/Materials/Animals","Compatibility");
            string path=folder+"/"+source.name.Replace('/','_')+"_URP.mat";
            var result=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(result==null)
            {
                result=new Material(shader);
                if(source.HasProperty("_Color"))result.SetColor("_BaseColor",source.GetColor("_Color"));
                if(source.HasProperty("_MainTex"))result.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
                if(source.HasProperty("_Glossiness"))result.SetFloat("_Smoothness",source.GetFloat("_Glossiness"));
                AssetDatabase.CreateAsset(result,path);
            }
            return result;
        }

        static void Set(UnityEngine.Object target,string field,object value)
        {
            var so=new SerializedObject(target);var p=so.FindProperty(field);
            if(p==null)throw new InvalidOperationException("Field not found: "+field);
            if(value is UnityEngine.Object obj)p.objectReferenceValue=obj;
            else if(value is string s)p.stringValue=s;
            else if(value is int integer)p.intValue=integer;
            else if(value is float f)p.floatValue=f;
            else if(value is Vector3 v)p.vector3Value=v;
            else if(value is FeedingPointKind k)p.enumValueIndex=(int)k;
            else if(value is FeedingPoint[] points){p.arraySize=points.Length;for(int i=0;i<points.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=points[i];}
            else throw new InvalidOperationException("Unsupported value: "+field);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}