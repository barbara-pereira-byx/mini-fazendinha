using System;
using System.Linq;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
namespace MiniFazendinha.EditorTools
{
    public static class FarmPolishSetup
    {
        static void Set(UnityEngine.Object target,string field,object value)
        {
            var s=new SerializedObject(target);var p=s.FindProperty(field);
            if(value==null||value is UnityEngine.Object)p.objectReferenceValue=value as UnityEngine.Object;
            else if(value is string text)p.stringValue=text;
            else if(value is int n)p.intValue=n;
            else if(value is Vector3 v)p.vector3Value=v;
            else if(value is Bounds b)p.boundsValue=b;
            s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);
        }
        [MenuItem("MiniFazendinha/Polish/Organize pens")] public static void Pens()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
            var root=GameObject.Find("06_Animais").transform;
            var fenceRoot=GameObject.Find("04_Cercas").transform;
            foreach(Transform child in fenceRoot)if(child.name.StartsWith("Cerca_")||child.name=="V3_Especies")child.gameObject.SetActive(false);
            var old=fenceRoot.Find("CurraisOrganizados");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var fences=new GameObject("CurraisOrganizados").transform;fences.SetParent(fenceRoot,false);
            var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniFazendinha/Materials/PenWood.mat");
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",new Color(.44f,.29f,.14f));AssetDatabase.CreateAsset(mat,"Assets/MiniFazendinha/Materials/PenWood.mat");}
            string[] ids={"cows","chickens","sheep","pigs","ducks"},names={"Vacas","Galinhas e galos","Ovelhas","Porcos","Patos"},defs={"Cow","Chicken","Sheep","Pig","Duck"};
            for(int i=0;i<ids.Length;i++)
            {
                bool left=i%2==0&&i!=4;float x=left?10.8f:16.2f,z=-11.5f+(i/2)*5;
                var home=root.Find("Abrigo_"+ids[i]).GetComponent<AnimalShelter>();
                var def=AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/MiniFazendinha/Settings/V2/Animals/"+defs[i]+".asset");
                var center=new Vector3(x,0,z);home.transform.position=center;
                foreach(Transform child in home.transform)if(child.name.StartsWith("Visual_"))child.gameObject.SetActive(false);
                var area=home.transform.Find("Area_"+ids[i]).GetComponent<BoxCollider>();area.transform.localPosition=new Vector3(0,.1f,0);area.size=new Vector3(3.6f,.1f,4.3f);area.isTrigger=true;
                Set(home,"displayName",names[i]);Set(home,"permittedSpecies",def);Set(home,"capacity",6);
                var gate=center+new Vector3(left?1.8f:-1.8f,.075f,0);
                Set(home,"gatePoint",gate);Set(home,"pathPoint",new Vector3(left?13f:14f,.075f,z));Set(home,"entrancePoint",center+new Vector3(left?.65f:-.65f,.075f,0));
                Set(home,"farmArea",new Bounds(new Vector3(10,0,-3),new Vector3(16,.15f,26)));
                var food=root.Find("Comedouro_"+ids[i]).GetComponent<FeedingPoint>();var water=root.Find("Bebedouro_"+ids[i]).GetComponent<FeedingPoint>();
                Set(home,"food",food);Set(home,"water",water);
                food.transform.position=center+new Vector3(-.85f,.05f,1.4f);water.transform.position=center+new Vector3(.85f,.05f,1.4f);
                foreach(var p in new[]{food,water}){var stand=p.transform.Find("AnimalStandPoint");stand.localPosition=new Vector3(0,.025f,-1.1f);}
                var oldFence=home.transform.Find("Cercas");if(oldFence!=null)UnityEngine.Object.DestroyImmediate(oldFence.gameObject);var penFence=new GameObject("Cercas").transform;penFence.SetParent(home.transform,false);
Vector3 a=center+new Vector3(-1.8f,0,-2.15f),b=center+new Vector3(1.8f,0,-2.15f),c=center+new Vector3(1.8f,0,2.15f),d=center+new Vector3(-1.8f,0,2.15f);
                Fence(penFence,mat,a,b);Fence(penFence,mat,c,d);
                if(left){Fence(penFence,mat,a,d);Fence(penFence,mat,b,gate+Vector3.back*.9f);Fence(penFence,mat,gate+Vector3.forward*.9f,c);}
                else{Fence(penFence,mat,b,c);Fence(penFence,mat,a,gate+Vector3.back*.9f);Fence(penFence,mat,gate+Vector3.forward*.9f,d);}
                var label=home.transform.Find("Identificacao");if(label!=null)UnityEngine.Object.DestroyImmediate(label.gameObject);
                var sign=new GameObject("Identificacao");sign.transform.SetParent(home.transform,false);sign.transform.localPosition=new Vector3(0,1.15f,-2.15f);sign.transform.rotation=Quaternion.Euler(35,0,0);
                var text=sign.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.text=names[i];text.fontSize=3.4f;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.16f,.22f,.12f);text.rectTransform.sizeDelta=new Vector2(3.5f,.65f);
                var click=home.GetComponent<BoxCollider>();if(click==null)click=home.gameObject.AddComponent<BoxCollider>();click.center=new Vector3(0,.6f,-2);click.size=new Vector3(3.1f,1.2f,.22f);
                var animals=root.GetComponentsInChildren<FarmAnimalController>().Where(a=>a.Home==home).ToArray();
                for(int j=0;j<animals.Length;j++){animals[j].transform.position=center+new Vector3((j%2==0?-.65f:.65f),.075f,-.8f+(j/2)*.9f);EditorUtility.SetDirty(animals[j]);}
            }
                        int hayIndex=0;var decor=GameObject.Find("07_Decoracoes");if(decor!=null)foreach(Transform t in decor.transform)if(t.name.StartsWith("Feno",StringComparison.OrdinalIgnoreCase)){t.position=new Vector3(14.4f+(hayIndex%2)*1.5f,t.position.y,8f+(hayIndex/2)*1.4f);hayIndex++;}
            // The former shared barn remains decoration; its old management component is retired.
            var barn=root.Find("Celeiro");if(barn!=null){var s=barn.GetComponent<AnimalShelter>();if(s!=null)UnityEngine.Object.DestroyImmediate(s);}
            Save();
        }
        static void Fence(Transform root,Material mat,Vector3 a,Vector3 b)
        {
            float length=Vector3.Distance(a,b);if(length<.1f)return;
            var segment=new GameObject("Cerca").transform;segment.SetParent(root,false);segment.position=(a+b)*.5f;segment.rotation=Quaternion.LookRotation(b-a);
            for(int i=0;i<2;i++)Cube(segment,mat,new Vector3(0,.29f+i*.32f,0),new Vector3(.1f,.12f,length));
            int posts=Mathf.CeilToInt(length/1.2f);for(int i=0;i<=posts;i++)Cube(segment,mat,new Vector3(0,.43f,-length*.5f+length*i/posts),new Vector3(.16f,.86f,.16f));
            var wall=segment.gameObject.AddComponent<BoxCollider>();wall.center=new Vector3(0,.43f,0);wall.size=new Vector3(.16f,.86f,length);
        }
        static void Cube(Transform parent,Material mat,Vector3 pos,Vector3 scale)
        {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
        [MenuItem("MiniFazendinha/Polish/Fix well")] public static void Resources()
        {
                        var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var data=new SerializedObject(inventory);var stock=data.FindProperty("initialItems");
            foreach(string guid in AssetDatabase.FindAssets("t:InventoryItemDefinition",new[]{"Assets/MiniFazendinha/Settings/Inventory"}))
            {
                var item=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));if(item==null||item is CropDefinition)continue;
                bool exists=false;for(int i=0;i<stock.arraySize;i++)if(stock.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue==item)exists=true;
                if(exists)continue;int index=stock.arraySize;stock.arraySize++;var row=stock.GetArrayElementAtIndex(index);row.FindPropertyRelative("item").objectReferenceValue=item;row.FindPropertyRelative("quantity").intValue=0;
            }
            data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(inventory);
            var well=UnityEngine.Object.FindAnyObjectByType<WaterWellController>();if(well==null)throw new Exception("Well missing");
            var c=well.GetComponent<BoxCollider>();if(c==null)c=well.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,1,0);c.size=new Vector3(2.2f,2.5f,2.2f);
            Save();
        }
                [MenuItem("MiniFazendinha/Polish/Fix animation loops")]
        public static void Animations()
        {
            foreach(string g in AssetDatabase.FindAssets("t:Model",new[]{"Assets/ThirdParty/VoxelFarm/Animations/Animals"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(g);var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)continue;
                var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
                foreach(var clip in clips){clip.loopTime=true;clip.loopPose=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;clip.lockRootPositionXZ=true;}
                importer.clipAnimations=clips;importer.SaveAndReimport();
            }
            Save();
        }
                [MenuItem("MiniFazendinha/Polish/Polish models and animation bindings")]
        public static void Models()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
            var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniFazendinha/Materials/AnimalProductReady.mat");
            if(gold==null){gold=new Material(Shader.Find("Universal Render Pipeline/Lit"));gold.SetColor("_BaseColor",new Color(1,.78f,.15f));AssetDatabase.CreateAsset(gold,"Assets/MiniFazendinha/Materials/AnimalProductReady.mat");}
            var report=new System.Text.StringBuilder();
            foreach(var a in UnityEngine.Object.FindObjectsByType<FarmAnimalController>())
            {
                var controller=a.Definition.AnimatorController;
                var clip=controller.animationClips.FirstOrDefault(c=>c.name.IndexOf("Walk",StringComparison.OrdinalIgnoreCase)>=0);
                if(clip==null)clip=controller.animationClips.FirstOrDefault();
                var bindings=clip==null?Array.Empty<EditorCurveBinding>():AnimationUtility.GetCurveBindings(clip);
                Transform best=a.transform;int score=-1;
                foreach(var t in a.GetComponentsInChildren<Transform>(true))
                {int count=bindings.Count(b=>AnimationUtility.GetAnimatedObject(t.gameObject,b)!=null);if(count>score){best=t;score=count;}}
                var anim=best.GetComponent<Animator>();if(anim==null)anim=best.gameObject.AddComponent<Animator>();
                foreach(var other in a.GetComponentsInChildren<Animator>(true))other.enabled=other==anim;
                anim.enabled=true;anim.runtimeAnimatorController=controller;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;Set(a,"animator",anim);
                var marker=a.transform.Find("ProdutoPronto");if(marker!=null){foreach(var r in marker.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=gold;}
                report.AppendLine(a.name+": Animator on "+best.name+", bindings "+score+" / "+bindings.Length);
                EditorUtility.SetDirty(a);
            }
            foreach(var p in UnityEngine.Object.FindObjectsByType<FeedingPoint>())
            {
                var visual=p.transform.Find("Visual");if(visual==null)continue;
                var rs=visual.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)continue;var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                report.AppendLine(p.name+": original size "+bounds.size);
                float span=Mathf.Max(bounds.size.x,bounds.size.z);if(span>.001f)visual.localScale*=.85f/span;
                bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);visual.position+=Vector3.up*(p.transform.position.y-bounds.min.y);
            }
            foreach(var obsolete in UnityEngine.Object.FindObjectsByType<AnimalShelter>().Where(s=>s.PermittedSpecies==null))UnityEngine.Object.DestroyImmediate(obsolete);
            System.IO.Directory.CreateDirectory("Reports");System.IO.File.WriteAllText("Reports/Animation-bindings.txt",report.ToString());Save();
        }
        static void Save(){AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }
}







