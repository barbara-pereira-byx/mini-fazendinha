using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.Animals;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV38ReturnTests
 {
  static IEnumerator steps;static FarmTestInputScope scope;static Mouse mouse;static MouseState? pending;static int frame;static double next;static StringBuilder log;
  static void Check(bool ok,string message){log.AppendLine((ok?"PASS ":"FAIL ")+message);Save();if(!ok)throw new Exception(message);}
  static void Save()=>File.WriteAllText("Reports/V38/return.txt",log.ToString());
  [MenuItem("MiniFazendinha/V3.8/Test bell and interrupted exits")]
  public static void Start(){if(!EditorApplication.isPlaying||steps!=null)throw new Exception("Fresh Play Mode required");log=new StringBuilder("Queued Input System clicks on the real bell; actual runtime movement\n");scope=new FarmTestInputScope();foreach(var m in InputSystem.devices.OfType<Mouse>().Where(m=>m.native).ToArray())InputSystem.DisableDevice(m);mouse=InputSystem.AddDevice<Mouse>("V36Tests");mouse.MakeCurrent();steps=Run();frame=Time.frameCount;InputSystem.onBeforeUpdate+=Before;InputSystem.onAfterUpdate+=After;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;}
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode){log.AppendLine("ABORTED");Finish();}}
  static void Before(){if(pending.HasValue&&InputState.currentUpdateType==InputUpdateType.Dynamic){InputSystem.QueueStateEvent(mouse,pending.Value);pending=null;}}
  static void After(){if(mouse!=null&&InputState.currentUpdateType==InputUpdateType.Dynamic)mouse.MakeCurrent();}
  static void Queue(Vector2 point,bool press=false){var state=new MouseState{position=point};if(press)state=state.WithButton(MouseButton.Left);pending=state;}
  static IEnumerator Click(Button button){Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));Queue(p);yield return null;Queue(p,true);yield return null;Queue(p);yield return null;yield return null;}
  static void Tick(){if(Time.frameCount<frame+3||EditorApplication.timeSinceStartup<next)return;frame=Time.frameCount;next=EditorApplication.timeSinceStartup+.08;try{if(!steps.MoveNext()){log.AppendLine("COMPLETED");Finish();}}catch(Exception e){log.AppendLine(e.ToString());Finish();}}
  static void Finish(){EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;InputSystem.onBeforeUpdate-=Before;InputSystem.onAfterUpdate-=After;Save();scope?.Dispose();steps=null;mouse=null;pending=null;}
  static bool Home(AnimalShelter s)=>s.Residents.All(a=>a.IsInsideShelter&&a.IsConfined&&!a.IsReturningHome&&!a.IsExitingHome);
  static IEnumerator Run()
  {
   var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;
   var pens=UnityEngine.Object.FindObjectsByType<AnimalShelter>().Where(s=>s.PermittedSpecies!=null).OrderBy(s=>s.PermittedSpecies.name=="Pig"?0:1).ToArray();
   foreach(var pen in pens)
   {
    ui.Close();pen.SetGate(true);pen.ReleaseAll();double limit=Time.timeAsDouble+10;while(!pen.Residents.Any(a=>a.IsExitingHome)&&Time.timeAsDouble<limit)yield return null;
    Check(pen.Residents.Any(a=>a.IsExitingHome),pen.DisplayName+" exit underway before recall");
    ui.ShowShelter(pen);var bell=ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Chamar moradores");var click=Click(bell);while(click.MoveNext())yield return null;ui.Close();
    limit=Time.timeAsDouble+35;while(!Home(pen)&&Time.timeAsDouble<limit)yield return null;
    foreach(var animal in pen.Residents)log.AppendLine("Interrupted exit: "+animal.DisplayName+" pos="+animal.transform.position+" confined="+animal.IsConfined+" returning="+animal.IsReturningHome);
    Check(Home(pen),pen.DisplayName+" bell cancels exit and returns/confines all residents");
   }
   foreach(var pen in pens)pen.ReleaseAll();
   double deadline=Time.timeAsDouble+40;while(pens.Any(s=>s.Residents.Any(a=>a.IsInsideShelter||a.IsExitingHome))&&Time.timeAsDouble<deadline)yield return null;
   foreach(var pen in pens){log.AppendLine("Exit stress: "+pen.DisplayName+" fully outside="+pen.Residents.Count(a=>!a.IsInsideShelter&&!a.IsExitingHome)+" / "+pen.ResidentCount);foreach(var a in pen.Residents)log.AppendLine(a.DisplayName+" pos="+a.transform.position+" exit="+a.IsExitingHome+" returning="+a.IsReturningHome+" dest="+a.GetComponent<FarmAnimalNavigation>().Destination);Check(pen.Residents.Any(a=>!a.IsInsideShelter||a.IsExitingHome),pen.DisplayName+" has animals to recall from outside/exit");}
   double wander=Time.timeAsDouble+12;while(Time.timeAsDouble<wander)yield return null;
   foreach(var pen in pens){ui.ShowShelter(pen);var click=Click(ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Chamar moradores"));while(click.MoveNext())yield return null;ui.Close();}
   deadline=Time.timeAsDouble+75;while(pens.Any(s=>!Home(s))&&Time.timeAsDouble<deadline)yield return null;
   foreach(var pen in pens){foreach(var animal in pen.Residents)log.AppendLine("From roaming: "+animal.DisplayName+" pos="+animal.transform.position+" confined="+animal.IsConfined+" returning="+animal.IsReturningHome);Check(Home(pen),pen.DisplayName+" bell returns roaming residents through real entrance");}
   foreach(var pen in pens){var starts=pen.Residents.Select(a=>a.transform.position).ToArray();double hold=Time.timeAsDouble+2;while(Time.timeAsDouble<hold)yield return null;Check(Home(pen),pen.DisplayName+" residents remain confined after recall");}
  }
 }
}
