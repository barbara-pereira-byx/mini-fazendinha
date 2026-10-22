using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV37Tests
 {
  static readonly BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Set(object target,string name,object value)=>target.GetType().GetField(name,Fields).SetValue(target,value);
  static System.Text.StringBuilder log;
  static void Check(bool value,string text){log.AppendLine((value?"PASS ":"FAIL ")+text);File.WriteAllText("Reports/V37/production.txt",log.ToString());if(!value)throw new Exception(text);}
  [MenuItem("MiniFazendinha/V3.7/Test gate representation")]
  public static void Gates()
  {
   var report=new System.Text.StringBuilder();foreach(var home in UnityEngine.Object.FindObjectsByType<AnimalShelter>().Where(s=>s.PermittedSpecies!=null))
   {var visual=(Transform)typeof(AnimalShelter).GetField("gateVisual",Fields).GetValue(home);bool before=home.GateOpen;try{home.SetGate(false);if(home.GateOpen||visual.GetComponentsInChildren<Renderer>(true).Any(r=>!r.enabled)||visual.GetComponentsInChildren<Collider>(true).Any(c=>!c.enabled))throw new Exception("Closed gate not visible/blocking");home.SetGate(true);if(!home.GateOpen||visual.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled)||visual.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new Exception("Open gate leaves visual/invisible obstruction");report.AppendLine("PASS "+home.ShelterId+" closed leaf visible/colliding; open leaf hidden/nonblocking");}finally{home.SetGate(before);}}
   File.WriteAllText("Reports/V37/gates.txt",report.ToString()+"COMPLETED\n");
  }  [MenuItem("MiniFazendinha/V3.7/Test production boundaries")]
  public static void Production()
  {
   Directory.CreateDirectory("Reports/V37");log=new System.Text.StringBuilder("Production boundaries using the real animal controller\n");
   foreach(var guid in AssetDatabase.FindAssets("t:AnimalDefinition",new[]{"Assets/MiniFazendinha/Settings/V2/Animals"}))
   {
    var definition=AssetDatabase.LoadAssetAtPath<AnimalDefinition>(AssetDatabase.GUIDToAssetPath(guid));Check(definition.ProductionSeconds>=60,definition.name+" period="+definition.ProductionSeconds);
    if(definition.Product==null)continue;
    var g=new GameObject("ProductionBoundary_TEST");g.SetActive(false);var animal=g.AddComponent<FarmAnimalController>();animal.Assign(definition,"test-clock","Test",null,null,null,null,null,null);Set(animal,"fedUntil",Time.timeAsDouble+definition.FedForSeconds);Set(animal,"hydratedUntil",Time.timeAsDouble+definition.HydrationDuration);Set(animal,"happiness",.9f);
    try{var produce=typeof(FarmAnimalController).GetMethod("Produce",Fields);produce.Invoke(animal,new object[]{definition.ProductionSeconds-1});Check(animal.ProductsAvailable==0&&Mathf.Abs(animal.SecondsToNextProduct-1)<.001f,definition.name+" no product one second before cycle; UI clock agrees");produce.Invoke(animal,new object[]{1f});Check(animal.ProductsAvailable==definition.ProductYield&&Mathf.Abs(animal.SecondsToNextProduct-definition.ProductionSeconds)<.001f,definition.name+" exact cycle yields "+animal.ProductsAvailable);int available=animal.ProductsAvailable;produce.Invoke(animal,new object[]{0f});produce.Invoke(animal,new object[]{-10f});Check(animal.ProductsAvailable==available,definition.name+" zero/negative time cannot duplicate");
    Set(animal,"productsAvailable",0);Set(animal,"productionProgress",0d);Set(animal,"fractionalYield",0d);Set(animal,"happiness",.5f);for(int n=0;n<4;n++)produce.Invoke(animal,new object[]{definition.ProductionSeconds});Check(animal.ProductsAvailable==Mathf.FloorToInt(definition.ProductYield*4*.75f),definition.name+" wellbeing changes yield, not period");}
    finally{UnityEngine.Object.DestroyImmediate(g);}
   }
   log.AppendLine("COMPLETED");File.WriteAllText("Reports/V37/production.txt",log.ToString());
  }
 }
}
