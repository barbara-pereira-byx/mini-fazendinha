using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using MiniFazendinha.FruitTrees;

namespace MiniFazendinha.Editor
{
    public static class FarmV2Setup
    {
        const string Root = "Assets/MiniFazendinha";
        const string Third = "Assets/ThirdParty/VoxelFarm";
        [MenuItem("MiniFazendinha/V2/Build animal prototype")]
        public static void BuildAnimals()
        {
            try { BuildAnimalsInternal(); }
            catch (Exception e)
            {
                System.IO.File.WriteAllText(System.IO.Path.GetFullPath(Root + "/Settings/V2/AnimalSetupError.txt"), e.ToString());
                Debug.LogException(e);
            }
        }
        [MenuItem("MiniFazendinha/V2/Build crop catalog")]
        public static void BuildCropCatalog()
        {
            string folder=Root+"/Settings/Crops";EnsureFolder(folder);EnsureFolder(Root+"/Prefabs/Crops");
            var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var interactor=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();
            if(inventory==null||interactor==null||hud==null)throw new Exception("Farm planting components missing.");
            var crops=new List<CropDefinition>(interactor.Crops);
            AddCrop(crops,folder,"Potato","batata","Batata","TVS_VoxelFarm_Potato.fbx",22f,new Color(.76f,.57f,.31f),.32f);
            AddCrop(crops,folder,"Pumpkin","abobora","Abóbora","TVS_VoxelFarm_Pumpkin.fbx",32f,new Color(1f,.43f,.12f),.68f);
            AddCrop(crops,folder,"Tomato","tomate","Tomate","TVS_VoxelFarm_Tomato.fbx",24f,new Color(.86f,.20f,.16f),.64f);
            AddCrop(crops,folder,"Strawberry","morango","Morango","TVS_VoxelFarm_StrawberryBush.fbx",30f,new Color(.87f,.27f,.34f),.48f);
            AddCrop(crops,folder,"Blueberry","mirtilo","Mirtilo","TVS_VoxelFarm_BlueberryBush.fbx",34f,new Color(.34f,.36f,.74f),.48f);
            AddCrop(crops,folder,"Blackberry","amora","Amora","TVS_VoxelFarm_BlackberryBush.fbx",32f,new Color(.40f,.24f,.45f),.48f);
            var unique=new List<CropDefinition>();foreach(var c in crops)if(c!=null&&!unique.Contains(c))unique.Add(c);
            Set(inventory,"initialStock",ExtendStock(inventory.InitialStock,unique));EditorUtility.SetDirty(inventory);
            Set(interactor,"crops",unique.ToArray());EditorUtility.SetDirty(interactor);
            var catalog=hud.GetComponent<FarmWarehouseCatalog>()??hud.gameObject.AddComponent<FarmWarehouseCatalog>();
            Set(catalog,"warehouse",hud.Warehouse);Set(catalog,"interactor",interactor);Set(catalog,"inventory",inventory);EditorUtility.SetDirty(catalog);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("Crop catalog created: 3 original + 6 Voxel Farm crops.");
        }
        [MenuItem("MiniFazendinha/V2/Build fruit tree prototype")]
        public static void BuildFruitTrees()
        {
            EnsureFolder(Root+"/Settings/FruitTrees");EnsureFolder(Root+"/Settings/Inventory/Fruit");
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();
            if(hud==null||inventory==null)throw new Exception("Farm HUD or inventory missing.");
            var trees=new List<FruitTreeDefinition>();
            AddTree(trees,"AppleTree","apple-tree","Macieira","TVS_VoxelFarm_AppleTree.fbx","Apple","maçã",45,34,new Color(.86f,.22f,.16f));
            AddTree(trees,"OrangeTree","orange-tree","Laranjeira","TVS_VoxelFarm_OrangeTree.fbx","Orange","laranja",50,38,new Color(1f,.47f,.12f));
            AddTree(trees,"LemonTree","lemon-tree","Limoeiro","TVS_VoxelFarm_LemonTree.fbx","Lemon","limão",48,36,new Color(.94f,.82f,.22f));
            var fruitItems=new List<InventoryItemDefinition>();foreach(var tree in trees)fruitItems.Add(tree.Fruit);AddZeroStock(inventory,fruitItems);EditorUtility.SetDirty(inventory);
            var placement=hud.GetComponent<FarmTreePlacementController>()??hud.gameObject.AddComponent<FarmTreePlacementController>();
            var catalog=hud.GetComponent<FarmWarehouseCatalog>()??hud.gameObject.AddComponent<FarmWarehouseCatalog>();
            Set(catalog,"fruitTrees",trees.ToArray());Set(catalog,"treePlacement",placement);EditorUtility.SetDirty(catalog);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static void AddTree(List<FruitTreeDefinition> list,string file,string id,string label,string model,string productFile,string productLabel,float growth,float production,Color color)
        {
            string path=Root+"/Settings/Inventory/Fruit/"+productFile+".asset";var item=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
            if(item==null){item=ScriptableObject.CreateInstance<InventoryItemDefinition>();AssetDatabase.CreateAsset(item,path);}
            Set(item,"identifier",productFile);Set(item,"displayName",productLabel);EditorUtility.SetDirty(item);
            string modelPath=FindModel("FBX/Environment/"+model);var prefab=string.IsNullOrEmpty(modelPath)?null:AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            string defPath=Root+"/Settings/FruitTrees/"+file+".asset";var def=AssetDatabase.LoadAssetAtPath<FruitTreeDefinition>(defPath);
            if(def==null){def=ScriptableObject.CreateInstance<FruitTreeDefinition>();AssetDatabase.CreateAsset(def,defPath);}
            Set(def,"identifier",id);Set(def,"displayName",label);Set(def,"modelPrefab",prefab);Set(def,"fruit",item);Set(def,"growthSeconds",growth);Set(def,"productionSeconds",production);Set(def,"yieldAmount",3);Set(def,"matureScale",1f);Set(def,"spacingRadius",1.3f);EditorUtility.SetDirty(def);list.Add(def);
        }
        static InitialCropStock[] ExtendStock(InitialCropStock[] old,IEnumerable<CropDefinition> crops)
        {
            var rows=new List<InitialCropStock>(old??Array.Empty<InitialCropStock>());
            foreach(var crop in crops)if(!rows.Exists(x=>x!=null&&x.crop==crop))rows.Add(new InitialCropStock{crop=crop,quantity=12});
            return rows.ToArray();
        }
        static void AddCrop(List<CropDefinition> crops,string folder,string file,string id,string label,string model,float seconds,Color color,float height)
        {
            string assetPath=folder+"/"+file+".asset";var crop=AssetDatabase.LoadAssetAtPath<CropDefinition>(assetPath);
            if(crop==null){crop=ScriptableObject.CreateInstance<CropDefinition>();AssetDatabase.CreateAsset(crop,assetPath);}
            string visualPath=Root+"/Prefabs/Crops/"+file+".prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            if(prefab==null)
            {
                var sourcePath=FindModel("FBX/Environment/"+model);
                if(string.IsNullOrEmpty(sourcePath))sourcePath=FindModel("FBX/Props/"+model);
                if(string.IsNullOrEmpty(sourcePath)){Debug.LogWarning("Crop model unavailable: "+model);return;}
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);var root=new GameObject(file);var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);visual.name="Model";FitVisual(visual.transform,height);
                prefab=PrefabUtility.SaveAsPrefabAsset(root,visualPath);UnityEngine.Object.DestroyImmediate(root);
            }
            Set(crop,"identifier",id);Set(crop,"displayName",label);Set(crop,"growthSeconds",seconds);Set(crop,"yieldAmount",3);Set(crop,"accentColor",color);Set(crop,"visualRows",3);Set(crop,"visualSpacing",.47f);
            Set(crop,"visualNotes","Voxel Farm FBX model composed as repeated low-poly plants; same source silhouette in three size stages as a documented prototype until separate growth meshes are authored.");
            SetVisual(crop,"growingVisual",prefab,new Vector3(.42f,.42f,.42f));SetVisual(crop,"intermediateVisual",prefab,new Vector3(.70f,.70f,.70f));SetVisual(crop,"matureVisual",prefab,Vector3.one);
            EditorUtility.SetDirty(crop);if(!crops.Contains(crop))crops.Add(crop);
        }
        static void SetVisual(CropDefinition crop,string path,GameObject prefab,Vector3 scale)
        {
            var so=new SerializedObject(crop);var p=so.FindProperty(path);p.FindPropertyRelative("prefab").objectReferenceValue=prefab;p.FindPropertyRelative("scale").vector3Value=scale;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void BuildAnimalsInternal()
        {
            EnsureFolder(Root + "/Settings/V2/Animals");
            EnsureFolder(Root + "/Prefabs/Animals");
            EnsureFolder(Root + "/Settings/Inventory/AnimalProducts");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/FarmMain.unity")
                EditorSceneManager.OpenScene(Root + "/Scenes/FarmMain.unity");
            var existing = GameObject.Find("06_Animais");
            if (existing != null)
            {
                for (int i=existing.transform.childCount-1;i>=0;i--) UnityEngine.Object.DestroyImmediate(existing.transform.GetChild(i).gameObject);
            }
            var orphan=GameObject.Find("Vaca");if(orphan!=null)UnityEngine.Object.DestroyImmediate(orphan);

            var inventory = UnityEngine.Object.FindAnyObjectByType<FarmInventory>();
            var pasture = GameObject.Find("Gramado_Pastagem");
            var barn = GameObject.Find("Celeiro");
            if (inventory == null || pasture == null || barn == null) throw new Exception("FarmInventory, Gramado_Pastagem or Celeiro missing.");
            var plotInteractor=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();
            if(plotInteractor!=null && plotInteractor.GetComponent<FarmAnimalContextUI>()==null) plotInteractor.gameObject.AddComponent<FarmAnimalContextUI>();

            var products = new Dictionary<string, InventoryItemDefinition>();
            products["milk"] = MakeItem("Milk", "milk", "Leite");
            products["egg"] = MakeItem("Egg", "egg", "Ovos");
            products["wool"] = MakeItem("Wool", "wool", "Lã");
            AddZeroStock(inventory, products.Values);

            var area = existing != null ? existing : new GameObject("06_Animais");
            var shelter = barn.GetComponent<AnimalShelter>() ?? barn.AddComponent<AnimalShelter>();
            Set(shelter, "roamingArea", pasture.GetComponent<Collider>());
            Set(shelter, "gatePoint", new Vector3(9f, .03f, -3.5f));
            Set(shelter, "pathPoint", new Vector3(8f, .03f, 3f));
            Set(shelter, "entrancePoint", new Vector3(7f, .03f, 5.45f));
            Set(shelter, "boundaryInset", .85f);
            if(GameObject.Find("Indicador_Produtos_Abrigo")==null)
            {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="Indicador_Produtos_Abrigo";marker.transform.position=new Vector3(7f,7.6f,11f);marker.transform.localScale=Vector3.one*.32f;
                UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());marker.transform.SetParent(barn.transform,true);
                var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(1f,.78f,.16f);marker.GetComponent<Renderer>().sharedMaterial=mat;
                Set(shelter,"productReadyMarker",marker);
            }

            var food = CreateTrough(area.transform, "Comedouro", "TVS_VoxelFarm_FoodTrough.fbx", new Vector3(14.9f,.05f,-10.7f), FeedingPointKind.Food);
            var water = CreateTrough(area.transform, "Bebedouro", "TVS_VoxelFarm_WaterTrough.fbx", new Vector3(12.1f,.05f,-10.7f), FeedingPointKind.Water);
            var troughs = new[] { food, water };
            SpawnSpecies(area.transform, shelter, inventory, products, troughs, "Cow", "Vaca", "cow", "TVS_VoxelFarm_Cow.fbx", 2, 1.25f, .75f, new Vector3(12.2f,.03f,-6.5f), "Cow_Idle_Anim.fbx", "Cow_Walk_Anim.fbx", null, 45f);
            SpawnSpecies(area.transform, shelter, inventory, products, troughs, "Chicken", "Galinha", "chicken", "TVS_VoxelFarm_Chicken.fbx", 3, .52f, .40f, new Vector3(15.3f,.03f,-6.7f), "Chicken_Idle_Anim.fbx", "Chicken_Walk_Anim.fbx", "Chicken_Peck_Anim.fbx", 28f);
            SpawnSpecies(area.transform, shelter, inventory, products, troughs, "Sheep", "Ovelha", "sheep", "TVS_VoxelFarm_Sheep.fbx", 2, .92f, .6f, new Vector3(12.4f,.03f,-9.4f), "Sheep_Idle_Anim.fbx", "Sheep_Walk_Anim.fbx", null, 60f);
            SpawnSpecies(area.transform, shelter, inventory, products, troughs, "Pig", "Porco", "pig", "TVS_VoxelFarm_Pig.fbx", 2, .72f, .5f, new Vector3(16.1f,.03f,-9.0f), "Pig_Idle_Anim.fbx", "Pig_Walk_Anim.fbx", null, 40f);
            SpawnSpecies(area.transform, shelter, inventory, products, troughs, "Duck", "Pato", "duck", "TVS_VoxelFarm_Duck.fbx", 1, .48f, .38f, new Vector3(14.1f,.03f,-8.0f), "Duck_Idle_Anim.fbx", "Duck_Walk_Anim.fbx", null, 33f);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Farm V2 animals built. 10 animals, 1 shelter, 2 feeding points.");
        }

        static InventoryItemDefinition MakeItem(string file, string id, string label)
        {
            string path = Root + "/Settings/Inventory/AnimalProducts/" + file + ".asset";
            var item = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);
            if (item == null) { item = ScriptableObject.CreateInstance<InventoryItemDefinition>(); AssetDatabase.CreateAsset(item, path); }
            Set(item,"identifier",id); Set(item,"displayName",label); EditorUtility.SetDirty(item); return item;
        }
        static void AddZeroStock(FarmInventory inventory, IEnumerable<InventoryItemDefinition> products)
        {
            var rows = new List<InitialItemStock>(inventory.InitialItems ?? Array.Empty<InitialItemStock>());
            foreach (var item in products) if (!rows.Exists(x=>x!=null&&x.item==item)) rows.Add(new InitialItemStock{item=item,quantity=0});
            Set(inventory,"initialItems",rows.ToArray()); EditorUtility.SetDirty(inventory);
        }
        static FeedingPoint CreateTrough(Transform parent, string name, string model, Vector3 position, FeedingPointKind kind)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.position=position;
            var path = FindModel("FBX/Environment/" + model);
            if (!string.IsNullOrEmpty(path))
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset,go.transform);
                visual.transform.localPosition=Vector3.zero; FitVisual(visual.transform, .8f);
            }
            var point=go.AddComponent<FeedingPoint>(); Set(point,"kind",kind); Set(point,"initialUnits",12); Set(point,"capacity",20);
            var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,.28f,0);box.size=new Vector3(1.6f,.7f,1f);
            var stand=new GameObject("AnimalStandPoint"); stand.transform.SetParent(go.transform,false);stand.transform.localPosition=new Vector3(0,0,.9f);Set(point,"animalStandPoint",stand.transform);
            return point;
        }
        static void SpawnSpecies(Transform parent, AnimalShelter shelter, FarmInventory inventory, Dictionary<string,InventoryItemDefinition> items, FeedingPoint[] troughs, string species, string label,string id,string model,int count,float height,float radius,Vector3 center,string idle,string walk,string eating,float seconds)
        {
            string modelPath=FindModel("FBX/Animals/"+model);
            if (string.IsNullOrEmpty(modelPath)) { Debug.LogError("Missing animal model "+model); return; }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            string controllerPath=Root+"/Settings/V2/Animals/"+species+"Controller.controller";
            var animatorController=MakeAnimator(controllerPath,idle,walk,eating);
            string prefabPath=Root+"/Prefabs/Animals/"+species+".prefab";
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if(prefab==null)
            {
                var root=new GameObject(label);var visual=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);visual.name="Visual";
                visual.transform.localPosition=Vector3.zero;FitVisual(visual.transform,height);
                var animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=animatorController;animator.applyRootMotion=false;
                var collider=root.AddComponent<CapsuleCollider>();collider.direction=1;collider.height=height;collider.radius=radius;collider.center=new Vector3(0,height*.48f,0);
                root.AddComponent<FarmAnimalNavigation>();
                var controller=root.AddComponent<FarmAnimalController>();
                var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere);marker.name="ProdutoPronto";marker.transform.SetParent(root.transform,false);marker.transform.localPosition=new Vector3(0,height+0.34f,0);marker.transform.localScale=Vector3.one*.22f;UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                var mr=marker.GetComponent<Renderer>(); var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=new Color(1f,.8f,.18f);mr.sharedMaterial=mat;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);UnityEngine.Object.DestroyImmediate(root);
            }
            string defPath=Root+"/Settings/V2/Animals/"+species+".asset";
            var def=AssetDatabase.LoadAssetAtPath<AnimalDefinition>(defPath);
            if(def==null){def=ScriptableObject.CreateInstance<AnimalDefinition>();AssetDatabase.CreateAsset(def,defPath);}
            Set(def,"identifier",id);Set(def,"displayName",label);Set(def,"visualPrefab",prefab);Set(def,"animatorController",animatorController);
            Set(def,"product", species=="Cow"?items["milk"]:species=="Chicken"?items["egg"]:species=="Sheep"?items["wool"]:null);
            Set(def,"productYield", species=="Cow"?2:species=="Chicken"?1:species=="Sheep"?2:0);
            Set(def,"productionSeconds",seconds);Set(def,"walkSpeed",species=="Cow"?.68f:species=="Chicken"?.9f:species=="Pig"?.72f:species=="Duck"?.65f:.7f);
            Set(def,"bodyRadius",radius);Set(def,"separationRadius",radius*2.3f);Set(def,"visualScale",1f);Set(def,"idleSecondsMin",2.5f);Set(def,"idleSecondsMax",6f);Set(def,"restChance",.18f);Set(def,"careChance",.26f);
            Set(def,"idleState","Idle");Set(def,"walkState","Walk");Set(def,"eatingState",eating==null?"Idle":"Eating");EditorUtility.SetDirty(def);
            for(int i=0;i<count;i++)
            {
                var animal=(GameObject)PrefabUtility.InstantiatePrefab(prefab);animal.transform.SetParent(parent,false);animal.name=label+"_"+(i+1).ToString("00");
                animal.transform.position=center+new Vector3((i%3)*radius*2.4f,0,(i/3)*radius*2.6f);
                var ac=animal.GetComponent<FarmAnimalController>();
                Set(ac,"definition",def);Set(ac,"animalId",id+"-"+(i+1).ToString("00"));Set(ac,"displayName",label+" "+(i+1));
                Set(ac,"home",shelter);Set(ac,"inventory",inventory);Set(ac,"animator",animal.GetComponentInChildren<Animator>(true));
                Set(ac,"visualRoot",animal.transform.Find("Visual"));Set(ac,"productReadyMarker",animal.transform.Find("ProdutoPronto").gameObject);Set(ac,"feedingPoints",troughs);
                EditorUtility.SetDirty(animal);
            }
        }
        static AnimatorController MakeAnimator(string path,string idle,string walk,string eating)
        {
            var ac=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(ac!=null)return ac;
            ac=AnimatorController.CreateAnimatorControllerAtPath(path);
            AddClipState(ac,idle,"Idle");if(walk!=null)AddClipState(ac,walk,"Walk");if(eating!=null)AddClipState(ac,eating,"Eating");
            return ac;
        }
        static void AddClipState(AnimatorController c,string file,string stateName)
        {
            var path=FindModel("Animations/Animals/"+file);if(string.IsNullOrEmpty(path))return;
            var clips=AssetDatabase.LoadAllAssetsAtPath(path);AnimationClip clip=null;
            foreach(var item in clips)if(item is AnimationClip a&&!a.name.StartsWith("__preview__",StringComparison.Ordinal)){clip=a;break;}
            if(clip==null)return;var state=c.layers[0].stateMachine.AddState(stateName);state.motion=clip;
            if(stateName=="Idle")c.layers[0].stateMachine.defaultState=state;
        }
        static string FindModel(string suffix)
        {
            foreach(var guid in AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(suffix)+" t:Model",new[]{"Assets/ThirdParty/VoxelFarm"}))
            {var path=AssetDatabase.GUIDToAssetPath(guid);if(path.EndsWith(suffix,StringComparison.OrdinalIgnoreCase))return path;}
            return null;
        }
        static void FitVisual(Transform visual,float height)
        {
            var renderers=visual.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
            var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            float factor=height/Mathf.Max(.01f,b.size.y);visual.localScale*=factor;
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            visual.localPosition+=new Vector3(0,-b.min.y,0);
        }
        static void EnsureFolder(string path){var parts=path.Split('/');string p=parts[0];for(int i=1;i<parts.Length;i++){var next=p+"/"+parts[i];if(!AssetDatabase.IsValidFolder(next))AssetDatabase.CreateFolder(p,parts[i]);p=next;}}
        static void Set(UnityEngine.Object obj,string field,object value)
        {
            var so=new SerializedObject(obj);var property=so.FindProperty(field);if(property==null)throw new Exception(obj.name+" lacks serialized field "+field);
            if(value==null)property.objectReferenceValue=null;
            else if(value is UnityEngine.Object asset)property.objectReferenceValue=asset;
            else if(value is string s)property.stringValue=s;else if(value is int n)property.intValue=n;else if(value is float f)property.floatValue=f;
            else if(value is bool b)property.boolValue=b;else if(value is Vector3 v)property.vector3Value=v;else if(value is FeedingPointKind k)property.enumValueIndex=(int)k;
            else if(value is Color color)property.colorValue=color;
            else if(value is Collider c)property.objectReferenceValue=c;else if(value is Transform t)property.objectReferenceValue=t;else if(value is FeedingPoint[] arr){property.arraySize=arr.Length;for(int i=0;i<arr.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=arr[i];}
            else if(value is UnityEngine.Object[] objects){property.arraySize=objects.Length;for(int i=0;i<objects.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=objects[i];}
            else if(value is InitialCropStock[] stocks){property.arraySize=stocks.Length;for(int i=0;i<stocks.Length;i++){var row=property.GetArrayElementAtIndex(i);row.FindPropertyRelative("crop").objectReferenceValue=stocks[i].crop;row.FindPropertyRelative("quantity").intValue=stocks[i].quantity;}}
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
