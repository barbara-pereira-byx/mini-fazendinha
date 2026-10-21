using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using MiniFazendinha.Planting;

namespace MiniFazendinha.EditorTools
{
    public static class FarmPlantingSetup
    {
        const string Root="Assets/MiniFazendinha/";
        const string Farm="Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Farm Ranch Pack/Prefabs/";
        const string Kitchen="Assets/Pandazole_Lowpoly_Asset_Bundle/Pandazole Kitchen Food/";
        const string ScenePath=Root+"Scenes/FarmMain.unity";
        [MenuItem("MiniFazendinha/Planting/Prepare Local TMP Resources")]
        public static void PrepareFonts()
        {
            if(AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets"}).Length==0)
            {
                TMP_PackageResourceImporter.ImportResources(true,false,false);
                AssetDatabase.Refresh();
            }
            Debug.Log("Local TMP resources ready; no external packages installed.");
        }
        [MenuItem("MiniFazendinha/Planting/Install On Existing FarmMain")]
        public static void Install()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.path!=ScenePath||scene.isDirty)throw new InvalidOperationException("Saved FarmMain Edit Mode required.");
            if(UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>()!=null)throw new InvalidOperationException("Planting is already installed; refusing to duplicate it.");
            var fontPaths=AssetDatabase.FindAssets("t:TMP_FontAsset",new[]{"Assets"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if(fontPaths.Length==0)throw new InvalidOperationException("Run Prepare Local TMP Resources first.");
            var font=TMP_Settings.defaultFontAsset??AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPaths[0]);
            var root=scene.GetRootGameObjects().Single(g=>g.name=="MiniFazendinha").transform;
            var plots=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Canteiro_")).OrderBy(t=>t.name).ToArray();
            if(plots.Length!=12 || plots.Any(p=>p.GetComponentInChildren<Collider>()==null))throw new InvalidOperationException("Expected exactly twelve original plots with colliders.");
            foreach(var path in new[]{Farm+"food_Carrot.prefab",Kitchen+"Prefabs/Food_Corn.prefab",Farm+"Env_Wheat.prefab",Farm+"Env_GrassPlant_06.prefab"})if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)throw new FileNotFoundException(path);
            Folder(Root+"Settings/Crops");
            var farmMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Pandazole_Farm_URP.mat");
            if(farmMaterial==null)throw new InvalidOperationException("Existing farm URP material missing.");
            var cornMaterial=ConvertCornMaterial();
            var carrot=Crop("Carrot","carrot","Cenoura",20,3,Farm+"food_Carrot.prefab",farmMaterial,new Vector3(90,0,0),Vector3.one*2.3f,3,1.3f,new Color(0.91f,0.43f,0.17f),"Mature carrots use the original harvested carrot model, upright and enlarged for readability. Growing stage uses a generic seedling.");
            var corn=Crop("Corn","corn","Milho",35,3,Kitchen+"Prefabs/Food_Corn.prefab",cornMaterial,new Vector3(0,0,90),Vector3.one*3.6f,3,1.3f,new Color(0.91f,0.72f,0.18f),"No complete corn stalk was found. The real Food_Corn cob is used as a stylized mature visual. Growing stage uses a generic seedling.");
            var wheat=Crop("Wheat","wheat","Trigo",50,3,Farm+"Env_Wheat.prefab",farmMaterial,Vector3.zero,Vector3.one,5,0.78f,new Color(0.72f,0.58f,0.22f),"Mature stage uses the original Env_Wheat plant; growing stage uses a generic seedling.");
            CropDefinition[] crops={carrot,corn,wheat};
            var selected=LineMaterial("Plot_Selected",new Color(0.32f,0.85f,0.91f));
            var growing=LineMaterial("Plot_Growing",new Color(0.21f,0.60f,0.27f));
            var ready=LineMaterial("Plot_Ready",new Color(1f,0.75f,0.15f));
            var track=LineMaterial("Plot_ProgressTrack",new Color(0.26f,0.31f,0.23f));
            Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install farm planting");
            try
            {
                var systems=new GameObject("11_Sistema_de_Plantio");Undo.RegisterCreatedObjectUndo(systems,"Create planting systems");systems.transform.SetParent(root,false);
                var inventory=systems.AddComponent<FarmInventory>();
                var inv=new SerializedObject(inventory);var initial=inv.FindProperty("initialStock");initial.arraySize=crops.Length;
                for(int i=0;i<crops.Length;i++){var item=initial.GetArrayElementAtIndex(i);item.FindPropertyRelative("crop").objectReferenceValue=crops[i];item.FindPropertyRelative("quantity").intValue=12;}inv.ApplyModifiedPropertiesWithoutUndo();
                var interactor=systems.AddComponent<FarmPlotInteractor>();var input=new SerializedObject(interactor);
                input.FindProperty("farmCamera").objectReferenceValue=Camera.main;input.FindProperty("inventory").objectReferenceValue=inventory;
                var catalog=input.FindProperty("crops");catalog.arraySize=crops.Length;for(int i=0;i<crops.Length;i++)catalog.GetArrayElementAtIndex(i).objectReferenceValue=crops[i];input.ApplyModifiedPropertiesWithoutUndo();
                foreach(var plot in plots)
                {
                    Undo.AddComponent<FarmPlot>(plot.gameObject);var view=Undo.AddComponent<FarmPlotView>(plot.gameObject);
                    var anchor=new GameObject("CropAnchor").transform;Undo.RegisterCreatedObjectUndo(anchor.gameObject,"Create crop anchor");anchor.SetParent(plot,false);anchor.localPosition=new Vector3(0,0.61f,0);
                    var outline=Line("SelectionOutline",plot,selected,0.06f,new[]{new Vector3(-2.46f,0.65f,-2.46f),new Vector3(2.46f,0.65f,-2.46f),new Vector3(2.46f,0.65f,2.46f),new Vector3(-2.46f,0.65f,2.46f)},true);
                    var progressTrack=Line("GrowthTrack",plot,track,0.12f,new[]{new Vector3(-2,0.655f,-2.25f),new Vector3(2,0.655f,-2.25f)},false);
                    var fill=Line("GrowthFill",plot,growing,0.075f,new[]{new Vector3(-2,0.66f,-2.25f),new Vector3(-1.98f,0.66f,-2.25f)},false);
                    outline.enabled=progressTrack.enabled=fill.enabled=false;
                    var so=new SerializedObject(view);so.FindProperty("cropAnchor").objectReferenceValue=anchor;so.FindProperty("outline").objectReferenceValue=outline;so.FindProperty("progressTrack").objectReferenceValue=progressTrack;so.FindProperty("progressFill").objectReferenceValue=fill;so.FindProperty("selectedMaterial").objectReferenceValue=selected;so.FindProperty("growingMaterial").objectReferenceValue=growing;so.FindProperty("readyMaterial").objectReferenceValue=ready;so.ApplyModifiedPropertiesWithoutUndo();
                }
                var oldDecoration=root.Find("08_Vegetacao_Estatica");if(oldDecoration!=null){Undo.RecordObject(oldDecoration.gameObject,"Disable static decorative crops");oldDecoration.gameObject.SetActive(false);}
                BuildHUD(root,inventory,interactor,crops,font);
                var eventSystems=UnityEngine.Object.FindObjectsByType<EventSystem>();
                if(eventSystems.Length>1)throw new InvalidOperationException("Multiple EventSystems present.");
                if(eventSystems.Length==0)
                {
                    var eventGO=new GameObject("FarmEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));Undo.RegisterCreatedObjectUndo(eventGO,"Create farm EventSystem");eventGO.transform.SetParent(systems.transform,false);eventGO.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                }
                else if(eventSystems[0].GetComponent<InputSystemUIInputModule>()==null)throw new InvalidOperationException("Existing EventSystem is not using Input System.");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Failed to save FarmMain.");Undo.CollapseUndoOperations(undo);
                File.WriteAllText("Library/MiniFazendinha-PlantingSetup.txt","Installed in-place on FarmMain.\n12 FarmPlot + FarmPlotView; original MeshColliders preserved.\n12 seeds per culture, 3 products per harvest.\nCarrot=20s; Corn=35s; Wheat=50s (ScriptableObject assets).\nStatic decorative seedlings disabled, not deleted.\nCamera controller unchanged.\nOne Canvas, GraphicRaycaster, EventSystem and InputSystemUIInputModule.\nOriginal Pandazole assets not modified.\n");
                Debug.Log("Planting installed on twelve existing plots and FarmMain saved.");
            }
            catch {Undo.RevertAllDownToGroup(undo);throw;}
        }
        static void Folder(string path)
        {
            string[] parts=path.Split('/');string current=parts[0];foreach(var part in parts.Skip(1)){if(!AssetDatabase.IsValidFolder(current+"/"+part))AssetDatabase.CreateFolder(current,part);current+="/"+part;}
        }
        static CropDefinition Crop(string file,string id,string display,float seconds,int yield,string maturePath,Material matureMaterial,Vector3 rotation,Vector3 scale,int rows,float spacing,Color color,string notes)
        {
            string path=Root+"Settings/Crops/"+file+".asset";var existing=AssetDatabase.LoadAssetAtPath<CropDefinition>(path);if(existing!=null)return existing;
            var crop=ScriptableObject.CreateInstance<CropDefinition>();var so=new SerializedObject(crop);
            so.FindProperty("identifier").stringValue=id;so.FindProperty("displayName").stringValue=display;so.FindProperty("growthSeconds").floatValue=seconds;so.FindProperty("yieldAmount").intValue=yield;so.FindProperty("accentColor").colorValue=color;so.FindProperty("visualRows").intValue=rows;so.FindProperty("visualSpacing").floatValue=spacing;so.FindProperty("visualNotes").stringValue=notes;
            var growing=so.FindProperty("growingVisual");growing.FindPropertyRelative("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Farm+"Env_GrassPlant_06.prefab");growing.FindPropertyRelative("materialOverride").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Pandazole_Farm_URP.mat");growing.FindPropertyRelative("scale").vector3Value=Vector3.one;growing.FindPropertyRelative("eulerAngles").vector3Value=Vector3.zero;
            var mature=so.FindProperty("matureVisual");mature.FindPropertyRelative("prefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(maturePath);mature.FindPropertyRelative("materialOverride").objectReferenceValue=matureMaterial;mature.FindPropertyRelative("scale").vector3Value=scale;mature.FindPropertyRelative("eulerAngles").vector3Value=rotation;
            so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(crop,path);return crop;
        }
        static Material ConvertCornMaterial()
        {
            string path=Root+"Materials/Pandazole_Corn_URP.mat";var found=AssetDatabase.LoadAssetAtPath<Material>(path);if(found!=null)return found;
            var original=AssetDatabase.LoadAssetAtPath<Material>(Kitchen+"Materials/Panda Mat.mat");if(original==null)throw new FileNotFoundException("Kitchen material missing.");
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.name="Pandazole_Corn_URP";material.SetTexture("_BaseMap",original.GetTexture("_MainTex"));material.SetColor("_BaseColor",original.GetColor("_Color"));material.SetFloat("_Smoothness",0.06f);material.SetFloat("_Metallic",0);AssetDatabase.CreateAsset(material,path);return material;
        }
        static Material LineMaterial(string name,Color color)
        {
            string path=Root+"Materials/"+name+".mat";var found=AssetDatabase.LoadAssetAtPath<Material>(path);if(found!=null)return found;
            var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.name=name;material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);return material;
        }
        static LineRenderer Line(string name,Transform parent,Material material,float width,Vector3[] points,bool loop)
        {
            var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Create plot feedback");go.transform.SetParent(parent,false);go.layer=2;
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=loop;line.positionCount=points.Length;line.SetPositions(points);line.sharedMaterial=material;line.widthMultiplier=width;line.numCornerVertices=3;line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;return line;
        }
        static RectTransform Rect(string name,Transform parent)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;
        }
        static TMP_Text Text(string name,Transform parent,TMP_FontAsset font,string text,int size)
        {
            var rect=Rect(name,parent);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(8,4);rect.offsetMax=new Vector2(-8,-4);
            var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=text;label.fontSize=size;label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=size;label.alignment=TextAlignmentOptions.Center;label.color=new Color(0.16f,0.26f,0.20f);label.raycastTarget=false;return label;
        }
        static void BuildHUD(Transform root,FarmInventory inventory,FarmPlotInteractor interactor,CropDefinition[] crops,TMP_FontAsset font)
        {
            var canvasRect=Rect("12_Interface_Plantio",root);Undo.RegisterCreatedObjectUndo(canvasRect.gameObject,"Create planting HUD");
            var canvas=canvasRect.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=canvasRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=0.5f;
            canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var bar=Rect("SeedBar",canvasRect);bar.anchorMin=new Vector2(0.035f,0);bar.anchorMax=new Vector2(0.965f,0);bar.pivot=new Vector2(0.5f,0);bar.anchoredPosition=new Vector2(0,16);bar.sizeDelta=new Vector2(0,106);
            var panel=bar.gameObject.AddComponent<UnityEngine.UI.Image>();panel.color=new Color(0.94f,0.96f,0.89f,0.97f);panel.raycastTarget=true;
            var layout=bar.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();layout.padding=new RectOffset(12,12,12,12);layout.spacing=10;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=true;layout.childForceExpandWidth=false;
            var buttons=new SeedButtonBinding[crops.Length];
            for(int i=0;i<crops.Length;i++)
            {
                var rect=Rect("Seed_"+crops[i].Identifier,bar);var sizing=rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();sizing.minWidth=140;sizing.preferredWidth=230;sizing.flexibleWidth=1;
                var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=Color.white;image.raycastTarget=true;var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
                var accent=Rect("CropAccent",rect);accent.anchorMin=new Vector2(0,0);accent.anchorMax=new Vector2(0,1);accent.pivot=new Vector2(0,0.5f);accent.sizeDelta=new Vector2(5,0);accent.anchoredPosition=Vector2.zero;var accentImage=accent.gameObject.AddComponent<UnityEngine.UI.Image>();accentImage.color=crops[i].AccentColor;accentImage.raycastTarget=false;
                buttons[i]=new SeedButtonBinding {crop=crops[i],button=button,label=Text("SeedLabel",rect,font,crops[i].DisplayName,23)};
            }
            var counter=Rect("HarvestCounter",bar);var counterLayout=counter.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();counterLayout.minWidth=250;counterLayout.preferredWidth=430;counterLayout.flexibleWidth=1.6f;
            var products=Text("Products",counter,font,"COLHEITA",22);
            var statusRect=Rect("PlotStatus",canvasRect);statusRect.anchorMin=new Vector2(0.035f,0);statusRect.anchorMax=new Vector2(0.965f,0);statusRect.pivot=new Vector2(0.5f,0);statusRect.anchoredPosition=new Vector2(0,127);statusRect.sizeDelta=new Vector2(0,36);
            var statusBackground=statusRect.gameObject.AddComponent<UnityEngine.UI.Image>();statusBackground.color=new Color(0.94f,0.96f,0.89f,0.94f);statusBackground.raycastTarget=false;
            var status=Text("StatusText",statusRect,font,"Selecione uma semente e clique para plantar",21);
            var hud=canvasRect.gameObject.AddComponent<FarmHUD>();var so=new SerializedObject(hud);so.FindProperty("inventory").objectReferenceValue=inventory;so.FindProperty("interactor").objectReferenceValue=interactor;so.FindProperty("productsLabel").objectReferenceValue=products;so.FindProperty("statusLabel").objectReferenceValue=status;
            var bindings=so.FindProperty("seedButtons");bindings.arraySize=buttons.Length;for(int i=0;i<buttons.Length;i++){var item=bindings.GetArrayElementAtIndex(i);item.FindPropertyRelative("crop").objectReferenceValue=buttons[i].crop;item.FindPropertyRelative("button").objectReferenceValue=buttons[i].button;item.FindPropertyRelative("label").objectReferenceValue=buttons[i].label;}so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}