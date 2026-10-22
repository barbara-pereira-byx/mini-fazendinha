using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV37Setup
 {
  static void Set(UnityEngine.Object target,string name,object value){var so=new SerializedObject(target);var p=so.FindProperty(name);if(value is int n)p.intValue=n;else if(value is float f)p.floatValue=f;else if(value is string s)p.stringValue=s;else p.objectReferenceValue=value as UnityEngine.Object;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
  [MenuItem("MiniFazendinha/V3.7/Apply production times")]
  public static void Times(){if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");string[] names={"Chicken","Cow","Sheep","Duck","Pig"};float[] times={60,120,180,90,150};for(int i=0;i<names.Length;i++){var def=AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/MiniFazendinha/Settings/V2/Animals/"+names[i]+".asset");Set(def,"productionSeconds",times[i]);}AssetDatabase.SaveAssets();}
  [MenuItem("MiniFazendinha/V3.7/Add duck eggs and bacon")]
  public static void Products()
  {
   if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");Directory.CreateDirectory("Assets/MiniFazendinha/Settings/V37");
   FarmV38Setup.Bacon();var egg=Item("DuckEgg","duck-egg","Ovo de pato");var truffle=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>("Assets/MiniFazendinha/Settings/V37/Bacon.asset");
   var duck=AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/MiniFazendinha/Settings/V2/Animals/Duck.asset");var pig=AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/MiniFazendinha/Settings/V2/Animals/Pig.asset");Set(duck,"product",egg);Set(duck,"productYield",1);Set(pig,"product",truffle);Set(pig,"productYield",1);
   var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var so=new SerializedObject(inventory);var rows=so.FindProperty("initialItems");foreach(var item in new[]{egg,truffle}){bool exists=false;for(int i=0;i<rows.arraySize;i++)if(rows.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue==item)exists=true;if(exists)continue;int index=rows.arraySize;rows.InsertArrayElementAtIndex(index);rows.GetArrayElementAtIndex(index).FindPropertyRelative("item").objectReferenceValue=item;rows.GetArrayElementAtIndex(index).FindPropertyRelative("quantity").intValue=0;}so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);EditorSceneManager.SaveScene(inventory.gameObject.scene);
  }
  [MenuItem("MiniFazendinha/V3.7/Configure tree placement")]
  public static void Trees()
  {
   if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var placement=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.FruitTrees.FarmTreePlacementController>();Set(placement,"readyMaterial",AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniFazendinha/Materials/AnimalProductReady.mat"));
   var scene=placement.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   File.WriteAllText("Reports/V37/tree-scene-audit.txt","Existing fruit trees in saved scene: "+UnityEngine.Object.FindObjectsByType<MiniFazendinha.FruitTrees.FruitTree>().Length+"\nTree source prefabs are project-owned; vendor FBXs retain import scale in visual child only.\n");
  }  [MenuItem("MiniFazendinha/V3.7/Configure fruit cycles")]
  public static void FruitCycles(){foreach(var guid in AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"})){var def=AssetDatabase.LoadAssetAtPath<MiniFazendinha.FruitTrees.FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(guid));Set(def,"growthSeconds",60f);Set(def,"productionSeconds",90f);}AssetDatabase.SaveAssets();}  static InventoryItemDefinition Item(string name,string id,string label)
  {
   string path="Assets/MiniFazendinha/Settings/V37/"+name+".asset";var item=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);if(item==null){item=ScriptableObject.CreateInstance<InventoryItemDefinition>();AssetDatabase.CreateAsset(item,path);}Set(item,"identifier",id);Set(item,"displayName",label);
   string image="Assets/MiniFazendinha/UI/Icons/"+name+".png";var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);tex.SetPixels(new Color[128*128]);for(int y=0;y<128;y++)for(int x=0;x<128;x++){float dx=(x-64)/39f,dy=(y-62)/(name=="DuckEgg"?48f:32f);if(dx*dx+dy*dy<1){Color color=name=="DuckEgg"?new Color(.68f,.84f,.76f):new Color(.42f,.29f,.20f);if(dx*dx+dy*dy>.88f)color=new Color(.26f,.24f,.16f);else if(name=="DuckEgg"&&x<53&&y>65)color=new Color(.88f,.95f,.83f);else if(name=="Truffle"&&((x/10+y/9)%3==0))color=new Color(.59f,.42f,.28f);tex.SetPixel(x,y,color);}}tex.Apply();File.WriteAllBytes(image,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(image);imp.textureType=TextureImporterType.Sprite;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.SaveAndReimport();Set(item,"icon",AssetDatabase.LoadAssetAtPath<Sprite>(image));return item;
  }
 }
}
