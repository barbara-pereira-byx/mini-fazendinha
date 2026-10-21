using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using MiniFazendinha.Planting;

namespace MiniFazendinha.EditorTools
{
    public static class FarmDragToolsUpgrade
    {
        const string Root="Assets/MiniFazendinha/";
        const string Farm="Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/";
        const string Corn="Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Kitchen Food/Prefabs/Food_Corn.prefab";
        static readonly StringBuilder dimensions=new StringBuilder();
        static Material farmMaterial,cornMaterial,leafMaterial,youngWheatMaterial;
        [MenuItem("MiniFazendinha/Drag Tools/Save Context Menu Defaults")]
        public static void SaveContextMenuDefaults()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=Root+"Scenes/FarmMain.unity")throw new InvalidOperationException("FarmMain required.");
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();
            hud.transform.Find("SeedBar").gameObject.SetActive(false);
            hud.transform.Find("PlotStatus").gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("MiniFazendinha/Drag Tools/Upgrade Existing Scene")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=Root+"Scenes/FarmMain.unity"||scene.isDirty)throw new InvalidOperationException("Saved FarmMain required.");
            var input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();
            if(input==null||hud==null||hud.transform.Find("SeedBar")==null)throw new InvalidOperationException("Existing planting system required.");
            if(hud.transform.Find("HoverCropPanel")!=null)throw new InvalidOperationException("Upgrade already applied.");
            var plots=UnityEngine.Object.FindObjectsByType<FarmPlot>();if(plots.Length!=12)throw new InvalidOperationException("Expected twelve existing plots.");
            Folder(Root+"Prefabs/Crops");Folder(Root+"Settings/DragTools");
            farmMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Pandazole_Farm_URP.mat");cornMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Pandazole_Corn_URP.mat");
            leafMaterial=Solid("Crop_Leaves",new Color(0.13f,0.43f,0.18f));youngWheatMaterial=Solid("Crop_YoungWheat",new Color(0.48f,0.63f,0.18f));dimensions.Clear();
            string[] files={"Carrot","Corn","Wheat"};
            for(int type=0;type<files.Length;type++)
            {
                var crop=AssetDatabase.LoadAssetAtPath<CropDefinition>(Root+"Settings/Crops/"+files[type]+".asset");if(crop==null)throw new FileNotFoundException(files[type]);
                var assets=new GameObject[3];for(int stage=0;stage<3;stage++)assets[stage]=BuildVisual(files[type],type,stage);
                Undo.RecordObject(crop,"Configure crop visual stages");var data=new SerializedObject(crop);
                string[] fields={"growingVisual","intermediateVisual","matureVisual"};
                for(int stage=0;stage<3;stage++){var visual=data.FindProperty(fields[stage]);visual.FindPropertyRelative("prefab").objectReferenceValue=assets[stage];visual.FindPropertyRelative("materialOverride").objectReferenceValue=null;visual.FindPropertyRelative("scale").vector3Value=Vector3.one;visual.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;}
                data.FindProperty("visualRows").intValue=type==2?5:4;data.FindProperty("visualSpacing").floatValue=type==2?0.78f:1.02f;
                data.FindProperty("visualNotes").stringValue=type==1?"Three composed provisional corn stages: generic leaves, a simple stem and the real Food_Corn cob. No complete corn-stalk prefab exists in the pack. All compositions live under MiniFazendinha, with bounded footprints.":"Three composed crop stages using original Pandazole models with bounded per-cell dimensions; original assets unchanged.";
                data.ApplyModifiedProperties();
            }
            Undo.RecordObject(input,"Configure plot-only gesture targets");var inputData=new SerializedObject(input);var targets=inputData.FindProperty("plots");targets.arraySize=plots.Length;for(int i=0;i<plots.Length;i++)targets.GetArrayElementAtIndex(i).objectReferenceValue=plots[i];inputData.FindProperty("sampleSpacingPixels").floatValue=4;inputData.ApplyModifiedProperties();
            UpgradeHUD(hud);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Library/MiniFazendinha-DragVisualDimensions.txt",dimensions.ToString());Debug.Log("Drag tools upgrade saved on existing FarmMain.");
        }
        [MenuItem("MiniFazendinha/Drag Tools/Improve Composed Leaf Footprints")]
        public static void ImproveFootprints()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            if(File.Exists("Library/MiniFazendinha-LeafFootprintsApplied.txt"))throw new InvalidOperationException("Footprint refinement already applied.");
            string[] names={"Carrot","Corn","Wheat"},stages={"Initial","Intermediate","Mature"};float[,] factors={{2.1f,1.4f,1.2f},{1.9f,1.2f,1.1f},{6f,2f,1.6f}};var log=new StringBuilder();
            for(int crop=0;crop<3;crop++)for(int stage=0;stage<3;stage++)
            {
                string path=Root+"Prefabs/Crops/"+names[crop]+"_"+stages[stage]+".prefab";var contents=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var bounds=BoundsOf(contents);float maxWidth=crop==2?0.60f:0.78f;float factor=Mathf.Min(factors[crop,stage],maxWidth/Mathf.Max(bounds.size.x,bounds.size.z));
                    var plant=contents.transform.Find("BoundedPlant");plant.localScale=Vector3.Scale(plant.localScale,new Vector3(factor,1,factor));
                    PrefabUtility.SaveAsPrefabAsset(contents,path);bounds=BoundsOf(contents);log.AppendLine(path+" | size="+bounds.size.ToString("F3")+" | minY="+bounds.min.y.ToString("F4"));
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
            }
            AssetDatabase.SaveAssets();File.WriteAllText("Library/MiniFazendinha-DragVisualDimensions.txt",log.ToString());File.WriteAllText("Library/MiniFazendinha-LeafFootprintsApplied.txt","Only composed MiniFazendinha prefabs refined. Heights unchanged; widths bounded per plant cell.");
        }
        static void Folder(string path){string current="Assets";foreach(var part in path.Split('/').Skip(1)){if(!AssetDatabase.IsValidFolder(current+"/"+part))AssetDatabase.CreateFolder(current,part);current+="/"+part;}}
        static Material Solid(string name,Color color)
        {
            string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",0.02f);AssetDatabase.CreateAsset(m,path);return m;
        }
        public static Bounds BoundsOf(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length==0)throw new InvalidOperationException("No renderers: "+go.name);var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);return bounds;
        }
        static GameObject Part(string path,Transform parent,Vector3 rotation,float height,float width,Vector3 position,Material material)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new FileNotFoundException(path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.Euler(rotation);
            var bounds=BoundsOf(go);float factor=Mathf.Min(height/bounds.size.y,width/Mathf.Max(bounds.size.x,bounds.size.z));go.transform.localScale*=factor;bounds=BoundsOf(go);
            var wanted=parent.TransformPoint(position);go.transform.position+=new Vector3(wanted.x-bounds.center.x,wanted.y-bounds.min.y,wanted.z-bounds.center.z);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>()){var mats=renderer.sharedMaterials;for(int i=0;i<mats.Length;i++)mats[i]=material;renderer.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
            foreach(var collider in go.GetComponentsInChildren<Collider>()){collider.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
            return go;
        }
        static GameObject BuildVisual(string name,int type,int stage)
        {
            string[] labels={"Initial","Intermediate","Mature"};string path=Root+"Prefabs/Crops/"+name+"_"+labels[stage]+".prefab";
            if(File.Exists(path))throw new InvalidOperationException("Refusing to overwrite composed visual "+path);
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject(name+"_"+labels[stage]);SceneManager.MoveGameObjectToScene(root,preview);var contents=new GameObject("BoundedPlant").transform;contents.SetParent(root.transform,false);
                if(type==0)
                {
                    Part(Farm+"Env_GrassPlant_06.prefab",contents,Vector3.zero,stage==0?0.35f:stage==1?0.68f:0.88f,stage==0?0.45f:0.76f,new Vector3(0,stage==2?0.28f:0,0),leafMaterial);
                    if(stage>0)Part(Farm+"food_Carrot.prefab",contents,new Vector3(90,0,0),stage==1?0.42f:0.76f,0.35f,Vector3.zero,farmMaterial);
                }
                else if(type==1)
                {
                    if(stage==0)Part(Farm+"Env_GrassPlant_06.prefab",contents,Vector3.zero,0.43f,0.53f,Vector3.zero,leafMaterial);
                    else
                    {
                        float height=stage==1?1.10f:1.85f;var stem=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stem.name="Provisional_Corn_Stem";stem.transform.SetParent(contents,false);stem.transform.localPosition=new Vector3(0,height/2,0);stem.transform.localScale=new Vector3(0.085f,height/2,0.085f);stem.GetComponent<Renderer>().sharedMaterial=leafMaterial;UnityEngine.Object.DestroyImmediate(stem.GetComponent<Collider>());
                        Part(Farm+"Env_GrassPlant_06.prefab",contents,new Vector3(0,25,0),0.66f,0.78f,new Vector3(0,height*0.24f,0),leafMaterial);
                        Part(Farm+"Env_GrassPlant_06.prefab",contents,new Vector3(0,150,0),0.66f,0.78f,new Vector3(0,height*0.53f,0),leafMaterial);
                        if(stage==2){Part(Corn,contents,new Vector3(0,0,75),0.65f,0.43f,new Vector3(0.16f,0.72f,0.10f),cornMaterial);Part(Corn,contents,new Vector3(0,65,-70),0.58f,0.41f,new Vector3(-0.14f,1.11f,0),cornMaterial);}
                    }
                }
                else
                {
                    float height=stage==0?0.33f:stage==1?0.90f:1.55f;
                    for(int i=0;i<2;i++)Part(Farm+"Env_Wheat.prefab",contents,new Vector3(0,i*90,0),height,0.51f,Vector3.zero,stage==0?leafMaterial:stage==1?youngWheatMaterial:farmMaterial);
                }
                float maxWidth=type==2?0.61f:0.84f;var bounds=BoundsOf(root);float fit=Mathf.Min(1,maxWidth/Mathf.Max(bounds.size.x,bounds.size.z));contents.localScale*=fit;bounds=BoundsOf(root);contents.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=2;
                var result=PrefabUtility.SaveAsPrefabAsset(root,path);bounds=BoundsOf(root);dimensions.AppendLine(path+" | size="+bounds.size.ToString("F3")+" | minimumY="+bounds.min.y.ToString("F4"));return result;
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
        }
        static RectTransform Rect(string name,Transform parent){var g=new GameObject(name,typeof(RectTransform));Undo.RegisterCreatedObjectUndo(g,"Upgrade farm HUD");g.transform.SetParent(parent,false);return (RectTransform)g.transform;}
        static TMP_Text Label(string name,Transform parent,TMP_FontAsset font,string text,int size)
        {
            var rect=Rect(name,parent);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,5);rect.offsetMax=new Vector2(-8,-5);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=17;label.fontSizeMax=size;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;return label;
        }
        static UnityEngine.UI.Button ToolButton(string name,Transform parent,TMP_FontAsset font,string text,float width,out TMP_Text label)
        {
            var rect=Rect(name,parent);var size=rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();size.minWidth=width*0.65f;size.preferredWidth=width;size.flexibleWidth=0.6f;
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Color.white;var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;label=Label(name+"Label",rect,font,text,25);return button;
        }
        static void UpgradeHUD(FarmHUD hud)
        {
            var canvas=(RectTransform)hud.transform;var bar=(RectTransform)hud.transform.Find("SeedBar");Undo.RecordObject(bar,"Resize existing seed bar");bar.sizeDelta=new Vector2(0,116);
            var data=new SerializedObject(hud);var font=((TMP_Text)data.FindProperty("productsLabel").objectReferenceValue).font;
            foreach(var text in bar.GetComponentsInChildren<TMP_Text>()){Undo.RecordObject(text,"Improve UI legibility");text.fontSize=25;text.fontSizeMax=25;text.fontSizeMin=17;}
            var harvest=ToolButton("HarvestTool",bar,font,"FOICE",150,out var harvestLabel);harvest.transform.SetSiblingIndex(3);
            var cancel=ToolButton("CancelTool",bar,font,"X\n<size=17>Esc</size>",60,out var cancelLabel);cancel.transform.SetSiblingIndex(4);cancelLabel.color=new Color(0.12f,0.23f,0.17f);
            foreach(var seed in hud.SeedButtons){var size=seed.button.GetComponent<UnityEngine.UI.LayoutElement>();Undo.RecordObject(size,"Fit tool bar");size.minWidth=120;size.preferredWidth=205;}
            var counter=bar.Find("HarvestCounter").GetComponent<UnityEngine.UI.LayoutElement>();Undo.RecordObject(counter,"Fit harvest counter");counter.minWidth=190;counter.preferredWidth=360;
            var status=(RectTransform)hud.transform.Find("PlotStatus");Undo.RecordObject(status,"Position tool instructions");status.anchoredPosition=new Vector2(0,140);status.sizeDelta=new Vector2(0,36);
            var panel=Rect("HoverCropPanel",canvas);panel.sizeDelta=new Vector2(280,100);var image=panel.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(0.07f,0.19f,0.13f,0.97f);image.raycastTarget=false;
            var info=Label("CropInfo",panel,font,"",24);var infoRect=(RectTransform)info.transform;infoRect.offsetMin=new Vector2(10,24);infoRect.offsetMax=new Vector2(-10,-6);
            var track=Rect("HoverProgressTrack",panel);track.anchorMin=new Vector2(0,0);track.anchorMax=new Vector2(1,0);track.pivot=new Vector2(0.5f,0);track.anchoredPosition=new Vector2(0,12);track.sizeDelta=new Vector2(-24,8);var trackImage=track.gameObject.AddComponent<UnityEngine.UI.Image>();trackImage.color=new Color(0.33f,0.43f,0.32f);trackImage.raycastTarget=false;
            var fill=Rect("HoverProgressFill",track);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;var fillImage=fill.gameObject.AddComponent<UnityEngine.UI.Image>();fillImage.color=new Color(0.98f,0.80f,0.28f);fillImage.raycastTarget=false;
            data.FindProperty("harvestButton").objectReferenceValue=harvest;data.FindProperty("harvestLabel").objectReferenceValue=harvestLabel;data.FindProperty("cancelButton").objectReferenceValue=cancel;data.FindProperty("hoverPanel").objectReferenceValue=panel;data.FindProperty("hoverLabel").objectReferenceValue=info;data.FindProperty("hoverProgress").objectReferenceValue=fill;data.ApplyModifiedProperties();panel.gameObject.SetActive(false);
        }
    }
}