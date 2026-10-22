using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using MiniFazendinha.EditorTools;
public static class FarmPointerChecks
{
    static MouseState? pending; static double nextTick; static IEnumerator steps;static Mouse mouse;static Mouse[] originals;static FarmTestInputScope scope;static int frame;static StringBuilder log=new StringBuilder("Queued Input System pointer checks\n");
    static FarmHUD hud;static FarmPlotInteractor input;static FarmInventory inventory;
    static void Check(bool ok,string message){log.AppendLine((ok?"PASS: ":"FAIL: ")+message);File.WriteAllText("Reports/FarmPointer-tests.txt",log.ToString());if(!ok)throw new Exception(message);}
    static Vector2 Screen(Transform t)=>RectTransformUtility.WorldToScreenPoint(null,t is RectTransform r?r.TransformPoint(r.rect.center):t.position);
    static Vector2 World(Vector3 p)=>Camera.main.WorldToScreenPoint(p);
    static void Queue(Vector2 p,bool pressed=false,float scroll=0){var s=new MouseState{position=p,scroll=new Vector2(0,scroll)};if(pressed)s=s.WithButton(MouseButton.Left);pending=s;}
    static IEnumerator Click(Vector2 p){Queue(p);yield return null;Queue(p,true);yield return null;Queue(p);yield return null;yield return null;}
    [MenuItem("MiniFazendinha/Polish/Test pointer clicks")] public static void Main()
    {
        hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();scope=new FarmTestInputScope();originals=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();foreach(var m in originals)InputSystem.DisableDevice(m);mouse=InputSystem.AddDevice<Mouse>("PolishPointer");mouse.MakeCurrent();steps=Run();frame=Time.frameCount;InputSystem.onBeforeUpdate+=BeforeInput;InputSystem.onAfterUpdate+=AfterInput;EditorApplication.update+=Tick;
    }
    static void BeforeInput(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&pending.HasValue){InputSystem.QueueStateEvent(mouse,pending.Value);pending=null;}} static void AfterInput(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&mouse!=null)mouse.MakeCurrent();}
static void Tick(){if(Time.frameCount<frame+3||EditorApplication.timeSinceStartup<nextTick)return;nextTick=EditorApplication.timeSinceStartup+.08;frame=Time.frameCount;try{if(!steps.MoveNext())Finish();}catch(Exception e){log.AppendLine(e.ToString());File.WriteAllText("Reports/FarmPointer-tests.txt",log.ToString());Finish();}}
    static void Finish(){EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=BeforeInput;InputSystem.onAfterUpdate-=AfterInput;InputSystem.RemoveDevice(mouse);foreach(var m in originals)if(m.added)InputSystem.EnableDevice(m);scope.Dispose();}
    static IEnumerator Run()
    {
        hud.CloseMenus();var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>();var uiPanel=hud.transform.Find("AnimalContextPanel");
        var well=UnityEngine.Object.FindAnyObjectByType<WaterWellController>();
        var click=Click(World(well.GetComponent<BoxCollider>().bounds.center));while(click.MoveNext())yield return null;
        Check(uiPanel.gameObject.activeSelf&&uiPanel.Find("Title").GetComponent<TMP_Text>().text=="Poço","Actual well click opens correct panel");
        Check(well.Available>0,"Water ready before collection click");int q=inventory.Quantity(well.WaterItem);click=Click(Screen(uiPanel.Find("Action_0")));while(click.MoveNext())yield return null;Check(inventory.Quantity(well.WaterItem)>q,"Actual collect button transfers water");
        click=Click(Screen(uiPanel.Find("Close")));while(click.MoveNext())yield return null;
        var a=FarmAnimalRegistry.All.First();var collider=a.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger);click=Click(World(collider.bounds.center));while(click.MoveNext())yield return null;
        Check(uiPanel.gameObject.activeSelf&&uiPanel.Find("Title").GetComponent<TMP_Text>().text==a.DisplayName,"Actual animal click opens its panel");click=Click(Screen(uiPanel.Find("Close")));while(click.MoveNext())yield return null;
        var pen=a.Home;click=Click(World(pen.GetComponent<BoxCollider>().bounds.center));while(click.MoveNext())yield return null;Check(uiPanel.gameObject.activeSelf&&uiPanel.Find("Title").GetComponent<TMP_Text>().text==pen.DisplayName,"Actual pen sign click opens correct management panel");click=Click(Screen(uiPanel.Find("Close")));while(click.MoveNext())yield return null;
        click=Click(Screen(hud.Warehouse.ToggleButton.transform));while(click.MoveNext())yield return null;for(int i=0;i<25;i++)yield return null;Check(hud.Warehouse.IsOpen,"Actual warehouse toggle opens icon grid");
        var scroll=hud.Warehouse.Panel.GetComponentInChildren<ScrollRect>();float before=scroll.verticalNormalizedPosition;Queue(Screen(scroll.transform),false,-240);for(int i=0;i<15;i++)yield return null;Check(scroll.verticalNormalizedPosition<before,"Actual mouse wheel scrolls inventory grid");FarmContextUXReview.Capture("Polish-Warehouse-scrolled");
        click=Click(Screen(hud.Warehouse.CloseButton.transform));while(click.MoveNext())yield return null;Check(!hud.Warehouse.IsOpen,"Actual warehouse X closes catalog");
        var plot=UnityEngine.Object.FindObjectsByType<FarmPlot>().First(p=>p.State==FarmPlotState.Empty);click=Click(World(plot.transform.position+Vector3.up*.7f));while(click.MoveNext())yield return null;Check(hud.ContextMenu.IsOpen,"Actual empty plot click opens seed menu");
        var next=hud.ContextMenu.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Próxima");click=Click(Screen(next.transform));while(click.MoveNext())yield return null;Check(hud.ContextMenu.Page==1,"Actual next page button changes crops");
        var selected=hud.ContextMenu.SeedButtons[0];click=Click(Screen(selected.button.transform));while(click.MoveNext())yield return null;Check(plot.Crop==selected.crop&&plot.State==FarmPlotState.Growing,"Actual paginated crop button plants origin");input.CancelTool();
        log.AppendLine("COMPLETED");File.WriteAllText("Reports/FarmPointer-tests.txt",log.ToString());
    }
}



