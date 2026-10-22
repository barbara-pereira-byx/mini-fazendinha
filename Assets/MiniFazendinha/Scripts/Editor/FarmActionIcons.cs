using System.IO;
using UnityEditor;
using UnityEngine;
namespace MiniFazendinha.EditorTools
{
 public static class FarmActionIcons
 {
  static Texture2D tex;static Color ink=new Color(.24f,.34f,.22f);
  static void Dot(float x,float y,float radius){for(int yy=Mathf.Max(0,(int)(y-radius));yy<Mathf.Min(96,y+radius+1);yy++)for(int xx=Mathf.Max(0,(int)(x-radius));xx<Mathf.Min(96,x+radius+1);xx++)if((new Vector2(xx-x,yy-y)).sqrMagnitude<=radius*radius)tex.SetPixel(xx,yy,ink);}
  static void Line(float x,float y,float a,float b,float width=3){for(int n=0;n<=100;n++)Dot(Mathf.Lerp(x,a,n/100f),Mathf.Lerp(y,b,n/100f),width);}
  static void Ring(float x,float y,float radius){for(int i=0;i<120;i++){float a=i*Mathf.PI*2/120;Dot(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,3);}}
  [MenuItem("MiniFazendinha/V3.6/Create shared action icons")]
  public static void Create()
  {
   foreach(var name in new[]{"Close","Heart","Feed","Health","Home","GateOpen","GateClosed","Bell","Basket","Pencil","Left","Right","Plus","Minus","Check","Max"})
   {
    tex=new Texture2D(96,96,TextureFormat.RGBA32,false);tex.SetPixels(new Color[96*96]);
    switch(name){
     case "Close":Line(28,28,68,68);Line(28,68,68,28);break;
     case "Heart":for(int y=0;y<96;y++)for(int x=0;x<96;x++){float a=(x-48)/31f,b=(y-48)/30f;float f=a*a+b*b-1;if(f*f*f-a*a*b*b*b<=0)tex.SetPixel(x,y,ink);}break;
     case "Health":Line(48,24,48,72,8);Line(24,48,72,48,8);break;
     case "Plus":Line(48,26,48,70);Line(26,48,70,48);break;
     case "Minus":Line(26,48,70,48);break;
     case "Check":Line(22,47,40,28);Line(40,28,75,70);break;
     case "Left":Line(62,23,34,48);Line(34,48,62,73);break;
     case "Right":Line(34,23,62,48);Line(62,48,34,73);break;
     case "Home":Line(20,49,48,75);Line(48,75,76,49);Line(28,49,28,23);Line(28,23,68,23);Line(68,23,68,49);Line(44,23,44,43);Line(44,43,54,43);Line(54,43,54,23);break;
     case "Pencil":Line(29,29,65,65,7);Line(26,24,30,37,3);Line(62,70,73,59);break;
     case "GateOpen":case "GateClosed":Line(19,23,19,73,4);Line(76,23,76,73,4);Line(23,60,name=="GateOpen"?50:73,60);Line(23,35,name=="GateOpen"?50:73,35);Line(23,35,name=="GateOpen"?50:73,60);break;
     case "Bell":Line(26,28,70,28);Line(29,31,32,56);Line(32,56,40,66);Line(40,66,57,66);Line(57,66,66,55);Line(66,55,68,30);Dot(48,21,6);Dot(48,72,5);break;
     case "Basket":Line(22,53,30,26);Line(30,26,66,26);Line(66,26,74,53);Line(22,53,74,53);Line(31,57,42,73);Line(42,73,56,73);Line(56,73,68,57);Line(39,30,39,49,2);Line(56,30,56,49,2);break;
     case "Feed":Line(22,49,29,28);Line(29,28,66,28);Line(66,28,74,49);Line(22,49,74,49);Dot(35,59,7);Dot(52,64,8);Dot(66,58,7);break;
     case "Max":Line(23,25,23,71);Line(73,25,73,71);Line(33,48,63,48);Line(52,37,63,48);Line(52,59,63,48);break;
    }
    tex.Apply();string path="Assets/MiniFazendinha/Resources/FarmUI/Action"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
   }
  }
 }
}
