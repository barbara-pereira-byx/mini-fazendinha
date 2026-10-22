using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
    // Extends the existing illustrated inventory style; the original three sprites are reused.
    public static class FarmInventoryIcons
    {
        static Texture2D image;
        static readonly Color Ink=new Color(.30f,.25f,.14f),Leaf=new Color(.30f,.62f,.23f);
        static void Ellipse(float x,float y,float rx,float ry,Color c)
        {for(int py=Mathf.Max(0,(int)(y-ry));py<Mathf.Min(256,y+ry);py++)for(int px=Mathf.Max(0,(int)(x-rx));px<Mathf.Min(256,x+rx);px++)if(Mathf.Pow((px-x)/rx,2)+Mathf.Pow((py-y)/ry,2)<=1)image.SetPixel(px,py,c);}
        static void Oval(float x,float y,float rx,float ry,Color c){Ellipse(x,y,rx+3,ry+3,Ink);Ellipse(x,y,rx,ry,c);}
        static void Line(float ax,float ay,float bx,float by,float width,Color color)
        {for(int i=0;i<=100;i++){float t=i/100f;Ellipse(Mathf.Lerp(ax,bx,t),Mathf.Lerp(ay,by,t),width,width,color);}}
        static void Poly(Color c,params Vector2[] p)
        {for(int y=0;y<256;y++)for(int x=0;x<256;x++){bool inside=false;for(int i=0,j=p.Length-1;i<p.Length;j=i++)if((p[i].y>y)!=(p[j].y>y)&&x<(p[j].x-p[i].x)*(y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;if(inside)image.SetPixel(x,y,c);}for(int i=0;i<p.Length;i++)Line(p[i].x,p[i].y,p[(i+1)%p.Length].x,p[(i+1)%p.Length].y,1.5f,Ink);}
        static Vector2 V(float x,float y)=>new Vector2(x,y);
        static void Leaves(){Line(128,177,133,215,4,Ink);Oval(150,209,23,10,Leaf);}
        [MenuItem("MiniFazendinha/Polish/Restore inventory icons")] public static void Apply()
        {
            string folder="Assets/MiniFazendinha/UI/Icons/";
            var items=AssetDatabase.FindAssets("t:InventoryItemDefinition",new[]{"Assets/MiniFazendinha/Settings"}).Select(g=>AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(x=>x!=null).ToArray();
            foreach(var item in items)
            {
                string path=folder+item.name+".png";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null){Draw(item.name);File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);}
                var s=new SerializedObject(item);s.FindProperty("icon").objectReferenceValue=sprite;s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);
            }
            var cat=Object.FindAnyObjectByType<FarmWarehouseCatalog>();var so=new SerializedObject(cat);var a=so.FindProperty("items");a.arraySize=items.Length;for(int i=0;i<items.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=items[i];so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        static void Draw(string name)
        {
            image=new Texture2D(256,256,TextureFormat.RGBA32,false);image.SetPixels(new Color[256*256]);Ellipse(128,36,76,9,new Color(.2f,.24f,.12f,.12f));
            switch(name)
            {
                case "Tomato": Oval(128,111,67,60,new Color(.92f,.24f,.17f));Leaves();Poly(Leaf,V(128,169),V(92,187),V(109,160),V(83,150),V(124,150),V(143,135),V(142,159),V(175,166));Ellipse(99,131,10,17,new Color(1,.53f,.36f));break;
                case "Strawberry":Poly(new Color(.94f,.27f,.31f),V(66,164),V(92,180),V(158,180),V(189,157),V(176,103),V(131,49),V(91,93));Poly(Leaf,V(78,181),V(108,201),V(128,186),V(156,202),V(181,183),V(143,165),V(119,173));for(int j=0;j<4;j++)for(int k=0;k<3;k++)Ellipse(105+k*22+(j%2)*5,90+j*20,2.5f,4,new Color(1,.88f,.53f));break;
                case "Blueberry":case "Blackberry":
                    Color berry=name=="Blueberry"?new Color(.28f,.42f,.79f):new Color(.34f,.22f,.46f);
                    if(name=="Blackberry"){for(int j=0;j<4;j++)for(int k=0;k<3;k++)Oval(95+k*30+(j%2)*10,70+j*29,20,21,berry);}else{Oval(94,104,43,44,berry);Oval(153,103,44,44,berry);Oval(130,158,44,43,berry);for(int i=0;i<3;i++){float x=i==0?94:i==1?153:130,y=i==2?158:103;for(int j=0;j<5;j++){float angle=j*Mathf.PI*2/5;Line(x,y,x+Mathf.Cos(angle)*12,y+Mathf.Sin(angle)*12,2,Ink);}}}Leaves();break;
                case "Potato":Oval(128,119,75,53,new Color(.75f,.53f,.29f));Ellipse(110,143,37,13,new Color(.87f,.67f,.40f));for(int i=0;i<6;i++)Ellipse(82+i*18,102+(i%2)*24,3,4,Ink);break;
                case "Pumpkin":Oval(128,116,77,64,new Color(.94f,.43f,.10f));Oval(128,115,52,65,new Color(1,.56f,.12f));Oval(128,115,25,65,new Color(1,.64f,.17f));Line(128,181,136,207,7,Ink);Line(128,181,136,207,4,Leaf);Oval(159,192,23,11,Leaf);break;
                case "Apple":Oval(100,117,46,62,new Color(.90f,.25f,.17f));Oval(150,117,46,62,new Color(.95f,.30f,.19f));Leaves();Ellipse(90,137,8,21,new Color(1,.60f,.40f));break;
                case "Orange":Oval(128,119,68,67,new Color(1,.56f,.12f));Leaves();Ellipse(99,142,11,20,new Color(1,.76f,.30f));break;
                case "Lemon":Oval(128,119,77,49,new Color(1,.83f,.18f));Oval(55,120,10,11,new Color(1,.83f,.18f));Leaves();Ellipse(108,138,27,8,new Color(1,.94f,.51f));break;
                case "Water":Poly(new Color(.31f,.67f,.89f),V(128,219),V(73,136),V(64,98),V(76,62),V(107,47),V(149,47),V(180,70),V(189,105),V(176,139));Line(91,89,94,121,6,new Color(.73f,.91f,1));break;
                case "Milk":Poly(new Color(.94f,.96f,.88f),V(85,49),V(171,49),V(174,139),V(150,177),V(150,205),V(108,205),V(108,177),V(82,139));Poly(new Color(.38f,.68f,.84f),V(84,91),V(173,91),V(173,137),V(84,137));Line(108,205,150,205,6,new Color(.35f,.61f,.76f));break;
                case "Egg":Oval(100,112,43,62,new Color(.97f,.84f,.61f));Oval(157,109,43,60,new Color(1,.93f,.76f));Ellipse(144,133,9,20,new Color(1,1,.94f));break;
                case "Wool":for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7;Oval(128+Mathf.Cos(a)*45,124+Mathf.Sin(a)*39,30,30,new Color(.94f,.93f,.85f));}Ellipse(128,124,49,43,new Color(1,.99f,.93f));Line(111,120,141,134,2,new Color(.76f,.75f,.65f));break;
                default:Oval(128,124,64,64,Leaf);break;
            }
            image.Apply();
        }
    }
}

