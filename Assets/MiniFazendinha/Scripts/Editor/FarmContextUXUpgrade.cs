using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using MiniFazendinha.Planting;

namespace MiniFazendinha.EditorTools
{
    public static class FarmContextUXUpgrade
    {
        const string Root="Assets/MiniFazendinha/";static TMP_FontAsset font;static Sprite rounded;
        static readonly Color Cream=new Color(1,.97f,.87f),Green=new Color(.16f,.32f,.23f),Wood=new Color(.49f,.31f,.16f);
        [MenuItem("MiniFazendinha/Context UX/Upgrade Existing HUD")]
        public static void Upgrade()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=Root+"Scenes/FarmMain.unity")throw new InvalidOperationException("FarmMain required.");
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();if(hud.GetComponent<FarmWarehousePanel>()!=null)throw new InvalidOperationException("UX already installed.");
            var data=new SerializedObject(hud);font=((TMP_Text)data.FindProperty("productsLabel").objectReferenceValue).font;
            Directory.CreateDirectory(Root+"UI/Icons");Directory.CreateDirectory(Root+"Settings/ContextUX");
            var icons=new Sprite[5];string[] names={"Carrot","Corn","Wheat","Sickle","Warehouse"};
            for(int i=0;i<5;i++)icons[i]=FarmUXIconArt.Create(Root+"UI/Icons/"+names[i]+".png",i);
            rounded=FarmUXIconArt.Create(Root+"UI/Icons/RoundedCard.png",5);
            var canvas=(RectTransform)hud.transform;var scaler=hud.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,900);scaler.matchWidthOrHeight=.5f;
            var side=Card("WarehousePanel",canvas,new Vector2(316,428));side.anchorMin=side.anchorMax=new Vector2(1,0);side.pivot=new Vector2(1,0);side.anchoredPosition=new Vector2(-24,100);
            var group=side.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.interactable=group.blocksRaycasts=false;
            TopText("WarehouseTitle",side,"Armaz\u00e9m",27,-30);var close=SmallClose(side);
            var subtitle=TopText("WarehouseSubtitle",side,"SEMENTES E FERRAMENTAS",12,-62);subtitle.color=Wood;
            var bindings=hud.SeedButtons;
            for(int i=0;i<bindings.Length;i++)
            {
                var b=bindings[i];var r=(RectTransform)b.button.transform;r.SetParent(side,false);PlaceCard(r,i);StyleButton(b.button,b.label,icons[i]);
            }
            var harvest=hud.HarvestButton;var harvestLabel=(TMP_Text)data.FindProperty("harvestLabel").objectReferenceValue;var hr=(RectTransform)harvest.transform;hr.SetParent(side,false);PlaceCard(hr,3);StyleButton(harvest,harvestLabel,icons[3]);
            var counter=(TMP_Text)data.FindProperty("productsLabel").objectReferenceValue;counter.transform.SetParent(side,false);var cr=(RectTransform)counter.transform;cr.anchorMin=cr.anchorMax=new Vector2(.5f,0);cr.pivot=new Vector2(.5f,0);cr.anchoredPosition=new Vector2(0,18);cr.sizeDelta=new Vector2(284,48);counter.fontSize=14;counter.enableAutoSizing=true;counter.fontSizeMin=12;counter.fontSizeMax=14;counter.color=Green;counter.alignment=TextAlignmentOptions.Center;
            var toggle=Button("WarehouseToggle",canvas,new Vector2(174,62),"Armaz\u00e9m",20);var tr=(RectTransform)toggle.transform;tr.anchorMin=tr.anchorMax=new Vector2(1,0);tr.pivot=new Vector2(1,0);tr.anchoredPosition=new Vector2(-24,24);Icon(tr,icons[4],new Vector2(-54,0),46);var tl=toggle.GetComponentInChildren<TMP_Text>();((RectTransform)tl.transform).offsetMin=new Vector2(60,4);
            var warehouse=hud.gameObject.AddComponent<FarmWarehousePanel>();Set(warehouse,"panel",side,"group",group,"toggleButton",toggle,"closeButton",close);
            var context=Card("PlotContextMenu",canvas,new Vector2(340,210));var title=TopText("ContextTitle",context,"O que vamos plantar?",23,-27);var sub=TopText("ContextSubtitle",context,"Escolha uma semente",15,-53);sub.color=Wood;var contextClose=SmallClose(context);
            var choices=Rect("ContextSeedChoices",context,new Vector2(330,118));choices.anchoredPosition=new Vector2(0,-25);
            var contextBindings=new SeedButtonBinding[bindings.Length];
            for(int i=0;i<bindings.Length;i++){var button=Button("Choose"+bindings[i].crop.Identifier,choices,new Vector2(96,112),"",17);((RectTransform)button.transform).anchoredPosition=new Vector2((i-1)*108,0);var label=button.GetComponentInChildren<TMP_Text>();StyleButton(button,label,icons[i],true);contextBindings[i]=new SeedButtonBinding{crop=bindings[i].crop,button=button,label=label};}
            var harvestChoice=Rect("ContextHarvestChoice",context,new Vector2(240,110));harvestChoice.anchoredPosition=new Vector2(0,-24);var contextHarvest=Button("ContextSickle",harvestChoice,new Vector2(132,110),"Colher",18);StyleButton(contextHarvest,contextHarvest.GetComponentInChildren<TMP_Text>(),icons[3],true);
            var track=Card("ContextProgressTrack",context,new Vector2(222,10));track.anchoredPosition=new Vector2(0,-35);track.GetComponent<UnityEngine.UI.Image>().color=new Color(.81f,.80f,.68f);track.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var fill=Rect("ContextProgressFill",track,Vector2.zero);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;var fi=fill.gameObject.AddComponent<UnityEngine.UI.Image>();fi.sprite=rounded;fi.type=UnityEngine.UI.Image.Type.Sliced;fi.color=new Color(.47f,.66f,.28f);fi.raycastTarget=false;
            var menu=hud.gameObject.AddComponent<FarmContextMenu>();Set(menu,"panel",context,"seedChoices",choices,"harvestChoice",harvestChoice,"progressTrack",track,"progressFill",fill,"title",title,"subtitle",sub,"harvestButton",contextHarvest,"closeButton",contextClose);
            var menuData=new SerializedObject(menu);Bindings(menuData.FindProperty("seedButtons"),contextBindings);menuData.ApplyModifiedPropertiesWithoutUndo();context.gameObject.SetActive(false);
            var badge=Card("ActiveToolBadge",canvas,new Vector2(206,58));badge.anchorMin=badge.anchorMax=new Vector2(.5f,0);badge.pivot=new Vector2(.5f,0);badge.anchoredPosition=new Vector2(0,24);var activeIcon=Icon(badge,icons[0],new Vector2(-73,0),40);var activeLabel=Text("ActiveToolName",badge,"Cenoura",19);((RectTransform)activeLabel.transform).sizeDelta=new Vector2(108,40);((RectTransform)activeLabel.transform).anchoredPosition=new Vector2(-4,0);var cancel=Button("CancelActiveTool",badge,new Vector2(30,30),"X",17);((RectTransform)cancel.transform).anchoredPosition=new Vector2(80,0);badge.gameObject.SetActive(false);
            data.Update();data.FindProperty("warehouse").objectReferenceValue=warehouse;data.FindProperty("contextMenu").objectReferenceValue=menu;data.FindProperty("activeToolBadge").objectReferenceValue=badge;data.FindProperty("activeToolLabel").objectReferenceValue=activeLabel;data.FindProperty("activeToolIcon").objectReferenceValue=activeIcon;data.FindProperty("activeToolCancel").objectReferenceValue=cancel;data.FindProperty("sickleIcon").objectReferenceValue=icons[3];var ia=data.FindProperty("cropIcons");ia.arraySize=3;for(int i=0;i<3;i++)ia.GetArrayElementAtIndex(i).objectReferenceValue=icons[i];data.ApplyModifiedPropertiesWithoutUndo();
            hud.transform.Find("SeedBar").gameObject.SetActive(false);hud.transform.Find("PlotStatus").gameObject.SetActive(false);
            var hover=hud.HoverPanel;hover.sizeDelta=new Vector2(250,90);var hi=hover.GetComponent<UnityEngine.UI.Image>();hi.sprite=rounded;hi.type=UnityEngine.UI.Image.Type.Sliced;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("Context UX installed on existing HUD and saved FarmMain.");
        }
        static double stopDeadline;
        [MenuItem("MiniFazendinha/Context UX/Stop And Finalize Safely")]
        public static void StopAndFinalize()
        {
            File.WriteAllText("Library/MiniFazendinha-EditorTransition.txt","Requested stop: playing="+EditorApplication.isPlaying+" changing="+EditorApplication.isPlayingOrWillChangePlaymode+" paused="+EditorApplication.isPaused);
            EditorApplication.isPaused=false;EditorApplication.isPlaying=false;stopDeadline=EditorApplication.timeSinceStartup+25;EditorApplication.update-=WaitForEdit;EditorApplication.update+=WaitForEdit;
        }
        static void WaitForEdit()
        {
            if(!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update-=WaitForEdit;Refine();FarmPlantingInspection.ValidateInstalled();File.WriteAllText("Library/MiniFazendinha-EditorTransition.txt","EDIT MODE CONFIRMED; UI refinements saved and scene validated.");
            }
            else if(EditorApplication.timeSinceStartup>stopDeadline)
            {EditorApplication.update-=WaitForEdit;File.WriteAllText("Library/MiniFazendinha-EditorTransition.txt","TIMEOUT playing="+EditorApplication.isPlaying+" changing="+EditorApplication.isPlayingOrWillChangePlaymode+" paused="+EditorApplication.isPaused);}
        }
        [MenuItem("MiniFazendinha/Context UX/Refine UI Details")]
        public static void Refine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            FarmUXIconArt.Create(Root+"UI/Icons/Sickle.png",3);
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var title=hud.transform.Find("PlotContextMenu/ContextTitle").GetComponent<TMP_Text>();title.fontSize=21;var r=(RectTransform)title.transform;r.sizeDelta=new Vector2(278,32);r.anchoredPosition=new Vector2(-13,-27);
            var hover=hud.HoverPanel.GetComponentInChildren<TMP_Text>();hover.fontSize=20;hover.fontSizeMax=20;hover.fontSizeMin=17;
            AssetDatabase.SaveAssets();var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("MiniFazendinha/Context UX/Apply Unified Stock UI")]
        public static void ApplyUnifiedStockUI()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();
            hud.HarvestButton.transform.SetParent(hud.transform.Find("SeedBar"),false);hud.HarvestButton.gameObject.SetActive(false);
            var panel=hud.Warehouse.Panel;panel.sizeDelta=new Vector2(278,400);
            for(int i=0;i<hud.SeedButtons.Length;i++)
            {
                var binding=hud.SeedButtons[i];var r=(RectTransform)binding.button.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.sizeDelta=new Vector2(234,82);r.anchoredPosition=new Vector2(0,-120-i*90);
                var icon=(RectTransform)r.Find("ToolIcon");icon.anchoredPosition=new Vector2(-77,0);icon.sizeDelta=new Vector2(62,62);
                var label=(RectTransform)binding.label.transform;label.anchorMin=label.anchorMax=label.pivot=new Vector2(.5f,.5f);label.sizeDelta=new Vector2(142,60);label.anchoredPosition=new Vector2(27,0);binding.label.fontSize=20;
            }
            var title=panel.Find("WarehouseTitle").GetComponent<TMP_Text>();((RectTransform)title.transform).sizeDelta=new Vector2(224,32);((RectTransform)title.transform).anchoredPosition=new Vector2(-7,-30);
            panel.Find("WarehouseSubtitle").GetComponent<TMP_Text>().text="SEU ESTOQUE";
            var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();EditorUtility.SetDirty(inventory);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Debug.Log("Unified stock UI saved; warehouse sickle retired; initial quantities migrated to 12.");
        }
        static void Bindings(SerializedProperty prop,SeedButtonBinding[] values){prop.arraySize=values.Length;for(int i=0;i<values.Length;i++){var p=prop.GetArrayElementAtIndex(i);p.FindPropertyRelative("crop").objectReferenceValue=values[i].crop;p.FindPropertyRelative("button").objectReferenceValue=values[i].button;p.FindPropertyRelative("label").objectReferenceValue=values[i].label;}}
        static void Set(UnityEngine.Object target,params object[] values){var s=new SerializedObject(target);for(int i=0;i<values.Length;i+=2)s.FindProperty((string)values[i]).objectReferenceValue=(UnityEngine.Object)values[i+1];s.ApplyModifiedPropertiesWithoutUndo();}
        static RectTransform Rect(string name,Transform parent,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;return r;}
        static RectTransform Card(string name,Transform parent,Vector2 size){var r=Rect(name,parent,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Cream;var shadow=r.gameObject.AddComponent<UnityEngine.UI.Shadow>();shadow.effectColor=new Color(.16f,.23f,.13f,.22f);shadow.effectDistance=new Vector2(0,-5);return r;}
        static TMP_Text Text(string name,Transform parent,string value,float size){var r=Rect(name,parent,new Vector2(260,40));var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.fontStyle=FontStyles.Bold;text.color=Green;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;}
        static TMP_Text TopText(string name,RectTransform parent,string value,float size,float y){var text=Text(name,parent,value,size);var r=(RectTransform)text.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,y);r.sizeDelta=new Vector2(parent.sizeDelta.x-44,32);return text;}
        static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 size,string value,float fontSize){var r=Card(name,parent,size);var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=r.GetComponent<UnityEngine.UI.Image>();button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};var text=Text("Label",r,value,fontSize);var tr=(RectTransform)text.transform;tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=tr.offsetMax=Vector2.zero;return button;}
        static UnityEngine.UI.Button SmallClose(RectTransform parent){var b=Button("Close",parent,new Vector2(28,28),"X",16);var r=(RectTransform)b.transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.anchoredPosition=new Vector2(-21,-23);return b;}
        static UnityEngine.UI.Image Icon(Transform parent,Sprite sprite,Vector2 pos,float size){var r=Rect("ToolIcon",parent,new Vector2(size,size));r.anchoredPosition=pos;var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;}
        static void PlaceCard(RectTransform r,int index){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.sizeDelta=new Vector2(132,130);r.anchoredPosition=new Vector2(index%2==0?-73:73,index<2?-145:-290);var layout=r.GetComponent<UnityEngine.UI.LayoutElement>();if(layout!=null)UnityEngine.Object.DestroyImmediate(layout);}
        static void StyleButton(UnityEngine.UI.Button button,TMP_Text label,Sprite sprite,bool compact=false)
        {
            var r=(RectTransform)button.transform;button.targetGraphic=button.GetComponent<UnityEngine.UI.Image>();var image=(UnityEngine.UI.Image)button.targetGraphic;image.sprite=rounded;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=Color.white;button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            foreach(var child in r.GetComponentsInChildren<UnityEngine.UI.Image>())if(child!=image){child.gameObject.SetActive(false);}
            var lr=(RectTransform)label.transform;lr.anchorMin=lr.anchorMax=new Vector2(.5f,0);lr.pivot=new Vector2(.5f,0);lr.anchoredPosition=new Vector2(0,7);lr.sizeDelta=new Vector2(r.sizeDelta.x-8,compact?40:44);label.fontSize=compact?16:18;label.enableAutoSizing=false;label.color=Green;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;Icon(r,sprite,new Vector2(0,compact?16:20),compact?58:70);
            var colors=button.colors;colors.normalColor=new Color(.96f,.91f,.77f);colors.highlightedColor=Color.white;colors.pressedColor=new Color(.70f,.82f,.53f);colors.selectedColor=colors.normalColor;colors.fadeDuration=.09f;button.colors=colors;
        }
    }
    // Original code-drawn vector-style placeholders. No external art or package required.
    static class FarmUXIconArt
    {
        static Texture2D texture;const int S=384;static Color Ink=new Color(.30f,.25f,.14f);static Color Orange=new Color(1,.52f,.15f),Leaf=new Color(.30f,.62f,.23f),Gold=new Color(1,.76f,.26f);
        static Vector2 P(float x,float y)=>new Vector2(x*2,y*2);
        static void Ellipse(float x,float y,float rx,float ry,Color c){for(int py=Mathf.Max(0,(int)((y-ry)*2));py<Mathf.Min(S,(y+ry)*2);py++)for(int px=Mathf.Max(0,(int)((x-rx)*2));px<Mathf.Min(S,(x+rx)*2);px++)if(Mathf.Pow((px/2f-x)/rx,2)+Mathf.Pow((py/2f-y)/ry,2)<1)texture.SetPixel(px,py,c);}
        static void Line(Vector2 a,Vector2 b,float width,Color c){int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*2);for(int i=0;i<=steps;i++){var p=Vector2.Lerp(a,b,(float)i/Mathf.Max(1,steps));Ellipse(p.x,p.y,width/2,width/2,c);}}
        static void Poly(Color c,params Vector2[] points){for(int y=0;y<S;y++)for(int x=0;x<S;x++){bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].y>y/2f)!=(points[j].y>y/2f)&&x/2f<(points[j].x-points[i].x)*(y/2f-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;if(inside)texture.SetPixel(x,y,c);}}
        static void Shape(Color fill,params Vector2[] points){Poly(fill,points);for(int i=0;i<points.Length;i++)Line(points[i],points[(i+1)%points.Length],3,Ink);}
        static Vector2 V(float x,float y)=>new Vector2(x,y);
        public static Sprite Create(string path,int kind)
        {
            texture=new Texture2D(S,S,TextureFormat.RGBA32,false);texture.SetPixels(new Color[S*S]);
            if(kind==5){for(int y=0;y<S;y++)for(int x=0;x<S;x++){float dx=Mathf.Max(22-Mathf.Min(x,S-1-x),0),dy=Mathf.Max(22-Mathf.Min(y,S-1-y),0);if(dx*dx+dy*dy<=22*22)texture.SetPixel(x,y,Color.white);}}
            else
            {
                Ellipse(98,25,61,7,new Color(.20f,.24f,.12f,.12f));
                if(kind==0){Shape(Orange,V(61,121),V(88,137),V(124,125),V(127,105),V(70,33),V(66,29),V(63,39));Line(V(78,110),V(97,103),4,new Color(.88f,.35f,.10f));Line(V(76,85),V(87,81),3,new Color(.88f,.35f,.10f));Line(V(76,119),V(88,125),4,new Color(1,.76f,.37f));Shape(Leaf,V(93,132),V(63,164),V(68,177),V(83,168),V(104,137));Shape(Leaf,V(102,136),V(103,183),V(117,184),V(121,168),V(111,134));Shape(Leaf,V(109,134),V(139,170),V(152,165),V(146,151),V(116,130));}
                if(kind==1){Ellipse(99,103,36,72,Ink);Ellipse(99,104,33,69,Gold);for(int row=0;row<9;row++)for(int col=0;col<4;col++){float x=77+col*14,y=53+row*13;Ellipse(x,y,5.4f,5.1f,new Color(1,.87f,.40f));}Shape(Leaf,V(98,32),V(60,57),V(46,112),V(73,96),V(91,69),V(98,47));Shape(new Color(.38f,.69f,.25f),V(96,32),V(127,51),V(150,123),V(119,98),V(106,67));Line(V(95,31),V(98,19),7,Ink);}
                if(kind==2){Line(V(71,28),V(114,174),6,Ink);Line(V(71,28),V(114,174),3,Gold);for(int i=0;i<5;i++){float y=66+i*21,x=80+i*6;Shape(Gold,V(x,y),V(x-24,y+13),V(x-27,y+29),V(x-13,y+28),V(x+4,y+8));Shape(new Color(1,.83f,.36f),V(x+4,y+8),V(x+25,y+15),V(x+35,y+34),V(x+19,y+30));}Line(V(89,31),V(124,159),2,new Color(.78f,.53f,.20f));}
                if(kind==3){Line(V(63,34),V(55,118),16,Ink);Line(V(63,34),V(55,118),11,new Color(.62f,.36f,.16f));for(int pass=0;pass<2;pass++)for(int i=0;i<40;i++){float a=Mathf.Lerp(175,5,i/40f)*Mathf.Deg2Rad,b=Mathf.Lerp(175,5,(i+1)/40f)*Mathf.Deg2Rad;var from=V(110+60*Mathf.Cos(a),107+62*Mathf.Sin(a));var to=V(110+60*Mathf.Cos(b),107+62*Mathf.Sin(b));Line(from,to,pass==0?17:11,pass==0?Ink:new Color(.76f,.83f,.80f));}Shape(new Color(.85f,.90f,.85f),V(169,115),V(179,101),V(177,126));Ellipse(55,116,5,5,Gold);}
                if(kind==4){Shape(new Color(.64f,.39f,.19f),V(39,40),V(153,40),V(153,126),V(39,126));for(int i=0;i<3;i++)Line(V(47,59+i*24),V(145,59+i*24),3,new Color(.43f,.27f,.14f));Line(V(50,44),V(50,123),9,new Color(.82f,.58f,.30f));Line(V(142,44),V(142,123),9,new Color(.82f,.58f,.30f));Shape(Gold,V(50,128),V(61,160),V(78,165),V(86,127));Shape(Orange,V(92,127),V(103,167),V(123,163),V(127,128));Line(V(113,166),V(122,180),8,Leaf);}
            }
            texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.spritePixelsPerUnit=100;if(kind==5)importer.spriteBorder=new Vector4(26,26,26,26);importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}