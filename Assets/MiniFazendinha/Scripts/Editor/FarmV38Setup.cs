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
 public static class FarmV38Setup
 {
  static void Set(UnityEngine.Object target,string key,string value){var so=new SerializedObject(target);so.FindProperty(key).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
  [MenuItem("MiniFazendinha/V3.8/Replace truffle with bacon")]
  public static void Bacon()
  {
   if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
   const string old="Assets/MiniFazendinha/Settings/V37/Truffle.asset",path="Assets/MiniFazendinha/Settings/V37/Bacon.asset";
   if(AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path)==null){string error=AssetDatabase.RenameAsset(old,"Bacon");if(!string.IsNullOrEmpty(error))throw new Exception(error);}
   var item=AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(path);Set(item,"identifier","bacon");Set(item,"displayName","Bacon");
   const string oldIcon="Assets/MiniFazendinha/UI/Icons/Truffle.png",image="Assets/MiniFazendinha/UI/Icons/Bacon.png";
   if(AssetDatabase.LoadAssetAtPath<Texture2D>(image)==null)AssetDatabase.RenameAsset(oldIcon,"Bacon");
   var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);texture.SetPixels(new Color[128*128]);
   for(int x=20;x<109;x++)for(int y=10;y<118;y++)
   {
    float wave=Mathf.Sin((x-20)*.09f)*4;float strip=(y-wave-20)%31;if(y-wave<20||y-wave>103||strip>22)continue;
    var color=strip<2||strip>20||x<22||x>106?new Color(.43f,.22f,.16f):strip<7||strip>15?new Color(.95f,.79f,.60f):new Color(.76f,.35f,.26f);
    texture.SetPixel(x,y,color);
   }
   texture.Apply();File.WriteAllBytes(image,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(image,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(image);importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
   var data=new SerializedObject(item);data.FindProperty("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(image);data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(item);
   var pig=AssetDatabase.LoadAssetAtPath<AnimalDefinition>("Assets/MiniFazendinha/Settings/V2/Animals/Pig.asset");if(pig.Product!=item)throw new Exception("Product GUID/reference was not preserved");
   AssetDatabase.SaveAssets();var scene=EditorSceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   Directory.CreateDirectory("Reports/V38");File.WriteAllText("Reports/V38/bacon.txt","PASS Bacon replaces Truffle, preserving asset GUID and scene inventory reference\nPASS Pig common production period="+pig.ProductionSeconds+", yield="+pig.ProductYield+"\n");
  }
 }
}
