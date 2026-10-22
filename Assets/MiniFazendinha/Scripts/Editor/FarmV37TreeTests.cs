using System;
using System.Collections;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.FruitTrees;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV37TreeTests
 {
  static bool harvesting;static string ReportPath=>harvesting?"Reports/V37/tree-harvest.txt":"Reports/V37/tree-placement.txt";static FarmTestInputScope scope;static Mouse mouse;static MouseState? pending;static IEnumerator steps;static int frame;static double next;static System.Text.StringBuilder log;
  static void Check(bool pass,string label){log.AppendLine((pass?"PASS ":"FAIL ")+label);File.WriteAllText(ReportPath,log.ToString());if(!pass)throw new Exception(label);}
  static void Queue(Vector3 point,bool press=false){var state=new MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(point)};if(press)state=state.WithButton(MouseButton.Left);pending=state;}
  static void Before(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&pending.HasValue){InputSystem.QueueStateEvent(mouse,pending.Value);pending=null;}}
  static void After(){if(mouse!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic)mouse.MakeCurrent();}
  [MenuItem("MiniFazendinha/V3.7/Test real tree placement clicks")]
  public static void Start(){harvesting=false;Begin();}
  [MenuItem("MiniFazendinha/V3.7/Test planted tree harvesting")]
  public static void Harvest(){harvesting=true;Begin();}
  static void Begin()
  {if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");log=new System.Text.StringBuilder("Actual Input System clicks for all three tree definitions\n");scope=new FarmTestInputScope();foreach(var d in InputSystem.devices.OfType<Mouse>().Where(m=>m.native).ToArray())InputSystem.DisableDevice(d);mouse=InputSystem.AddDevice<Mouse>("V36Tests");mouse.MakeCurrent();steps=harvesting?RunHarvest():Run();frame=Time.frameCount;InputSystem.onBeforeUpdate+=Before;InputSystem.onAfterUpdate+=After;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=PlayChanged;}
  static void PlayChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Finish();}
  static void Tick(){if(Time.frameCount<frame+3||EditorApplication.timeSinceStartup<next)return;frame=Time.frameCount;next=EditorApplication.timeSinceStartup+.08;try{if(!steps.MoveNext()){log.AppendLine("COMPLETED");Finish();}}catch(Exception e){log.AppendLine(e.ToString());Finish();}}
  static void Finish(){EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=PlayChanged;InputSystem.onBeforeUpdate-=Before;InputSystem.onAfterUpdate-=After;File.WriteAllText(ReportPath,log.ToString());scope?.Dispose();mouse=null;steps=null;}
  static void ScreenPointer(Vector2 point,bool press=false){var state=new MouseState{position=point};if(press)state=state.WithButton(MouseButton.Left);pending=state;}
  static IEnumerator RunHarvest()
  {
   var stock=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.Planting.FarmInventory>();var ui=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.Animals.FarmAnimalContextUI>().Management;var trees=UnityEngine.Object.FindObjectsByType<FruitTree>();Check(trees.Length==3,"Three trees from real placement test remain in scene");
   var advance=typeof(FruitTree).GetMethod("Advance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
   foreach(var tree in trees)
   {
    advance.Invoke(tree,new object[]{tree.Definition.GrowthSeconds+tree.Definition.ProductionSeconds});Check(tree.State==FruitTreeState.ReadyToHarvest&&tree.AvailableFruit==tree.Definition.YieldAmount,tree.Definition.name+" complete growth and first fruit cycle");int before=stock.Quantity(tree.Definition.Fruit),available=tree.AvailableFruit;
    Queue(tree.GetComponent<Collider>().bounds.center);yield return null;Queue(tree.GetComponent<Collider>().bounds.center,true);yield return null;Queue(tree.GetComponent<Collider>().bounds.center);yield return null;Check(ui.IsOpen&&ui.Panel.name==tree.Definition.DisplayName,tree.Definition.name+" actual canopy click opens tree panel");
    var button=ui.Panel.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Colher frutas");var r=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));ScreenPointer(p);yield return null;ScreenPointer(p,true);yield return null;ScreenPointer(p);yield return null;
    Check(stock.Quantity(tree.Definition.Fruit)==before+available&&tree.AvailableFruit==0,tree.Definition.name+" basket button credits exact fruit amount");Check(tree!=null&&tree.gameObject.activeInHierarchy,tree.Definition.name+" remains planted after harvest");Check(!tree.TryHarvest(),tree.Definition.name+" repeated harvest is refused");advance.Invoke(tree,new object[]{tree.Definition.ProductionSeconds-1});Check(tree.AvailableFruit==0,tree.Definition.name+" next cycle not ready early");advance.Invoke(tree,new object[]{1f});Check(tree.AvailableFruit==tree.Definition.YieldAmount,tree.Definition.name+" produces again without regrowing");ui.Close();
   }
  }  static IEnumerator Run()
  {
   var placement=UnityEngine.Object.FindAnyObjectByType<FarmTreePlacementController>();var definitions=AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"}).Select(g=>AssetDatabase.LoadAssetAtPath<FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
   foreach(var definition in definitions)
   {
    placement.Begin(definition);for(int i=0;i<3;i++)yield return null;Vector3 point=Vector3.zero;bool found=false;
    for(int x=-17;x<=17&&!found;x++)for(int z=-17;z<=17&&!found;z++){var candidate=new Vector3(x,.03f,z);if(placement.CanPlace(candidate,out _)){point=candidate;found=true;}}
    Check(found,definition.name+" has a valid ground location without a plot");Check(!placement.CanPlace(new Vector3(7,.03f,11),out _),"Building blocks placement");Check(!placement.CanPlace(new Vector3(10.8f,.03f,-11.5f),out _),"Pen blocks placement");
    int before=UnityEngine.Object.FindObjectsByType<FruitTree>().Length;Queue(point);yield return null;yield return null;Check(Vector3.Distance(placement.Preview.transform.position,point)<.25f,definition.name+" preview follows pointer");Queue(point,true);yield return null;Queue(point);yield return null;
    var all=UnityEngine.Object.FindObjectsByType<FruitTree>();Check(all.Length==before+1,definition.name+" actual click creates one tree");var tree=all.First(t=>t.Definition==definition);Check(tree.transform.lossyScale==Vector3.one,definition.name+" gameplay root has unit scale");Check(Mathf.Abs(tree.GetComponent<SphereCollider>().radius-definition.SpacingRadius)<.001f,definition.name+" collider uses world metres");placement.Begin(definition);yield return null;Check(!placement.CanPlace(tree.transform.position,out _),definition.name+" cannot overlap existing tree");placement.Cancel();yield return null;
   }
  }
 }
}
