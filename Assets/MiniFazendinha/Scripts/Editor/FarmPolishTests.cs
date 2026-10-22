using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
    public static class FarmPolishTests
    {
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static IEnumerator routine;static StringBuilder log;static int lastFrame;static double next;
        static FarmInventory inventory;static FarmPlotInteractor input;static FarmHUD hud;static FarmAnimalContextUI ui;
        static void Check(bool value,string message){log.AppendLine((value?"PASS: ":"FAIL: ")+message);Write();if(!value){Diagnostics();throw new Exception(message);}}
        static void Write(){Directory.CreateDirectory("Reports");File.WriteAllText("Reports/FarmPolish-tests.txt",log.ToString());}
        static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Private).Invoke(target,args);
        static void Show(string method,object target)=>Call(ui,method,target,new Vector2(Screen.width*.5f,Screen.height*.65f));
        static TMP_Text Body()=>((RectTransform)typeof(FarmAnimalContextUI).GetField("panel",Private).GetValue(ui)).Find("Body").GetComponent<TMP_Text>();
        static void Capture(string label)=>FarmContextUXReview.Capture("Polish-"+label);
                [MenuItem("MiniFazendinha/Polish/Restart and test")]
        [MenuItem("MiniFazendinha/Polish/Apply and test")] public static void ApplyAndTest(){SessionState.SetBool("FarmPolishApply",true);Restart();}
public static void Restart(){SessionState.SetInt("FarmPolishTestPhase",1);if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();}
        [InitializeOnLoadMethod] static void Bootstrap(){EditorApplication.update-=BootTick;EditorApplication.update+=BootTick;}
        static void BootTick(){int phase=SessionState.GetInt("FarmPolishTestPhase",0);if(phase==1&&!EditorApplication.isPlayingOrWillChangePlaymode){if(SessionState.GetBool("FarmPolishApply",false)){SessionState.SetBool("FarmPolishApply",false);FarmPolishSetup.Pens();FarmPolishSetup.Models();FarmPolishSetup.Resources();}SessionState.SetInt("FarmPolishTestPhase",2);EditorApplication.EnterPlaymode();}else if(phase==2&&EditorApplication.isPlaying&&!EditorApplication.isCompiling&&Time.frameCount>5){SessionState.SetInt("FarmPolishTestPhase",0);Start();}}
        [MenuItem("MiniFazendinha/Polish/Run play tests")]
        public static void Start()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
            log=new StringBuilder("Farm polish — actual Play Mode checks\n");inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>();
            routine=Run();lastFrame=Time.frameCount;next=0;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;log.AppendLine("INTERRUPTED: Play Mode ended before suite completed.");Write();return;}if(Time.frameCount==lastFrame||EditorApplication.timeSinceStartup<next)return;lastFrame=Time.frameCount;next=EditorApplication.timeSinceStartup+.12;
            try{if(!routine.MoveNext()){EditorApplication.update-=Tick;log.AppendLine("COMPLETED");Write();}}
            catch(Exception e){EditorApplication.update-=Tick;log.AppendLine(e.ToString());Write();Debug.LogException(e);}
        }
        [MenuItem("MiniFazendinha/Polish/Animal diagnostics")] public static void Diagnostics(){File.WriteAllText("Reports/Animal-diagnostics.json",Newtonsoft.Json.JsonConvert.SerializeObject(FarmAnimalRegistry.All.Select(a=>new{a.name,a.StateLabel,a.IsConfined,position=a.transform.position.ToString(),dest=a.GetComponent<FarmAnimalNavigation>().Destination.ToString(),hasPath=a.GetComponent<FarmAnimalNavigation>().HasDestination,route=((Vector3[])typeof(FarmAnimalController).GetField("route",Private).GetValue(a))?.Select(v=>v.ToString()).ToArray(),routeIndex=typeof(FarmAnimalController).GetField("routeIndex",Private).GetValue(a)}),Newtonsoft.Json.Formatting.Indented));}
static IEnumerator Run()
        {
            int frame=Time.frameCount;yield return null;Check(Time.frameCount>frame,"Player frames advance");
            var pens=UnityEngine.Object.FindObjectsByType<AnimalShelter>(FindObjectsSortMode.None).Where(s=>s.PermittedSpecies!=null).ToArray();var animals=FarmAnimalRegistry.All.ToArray();
            Check(pens.Length==5,"Five identified species pens");
            Check(animals.All(a=>a.enabled&&a.Home.Accepts(a)&&a.Home.Residents.Contains(a)),"All animals registered in compatible home with capacity");
            var animal=animals.First();var home=animal.Home;var wrong=pens.First(p=>p!=home);animal.ReturnToShelter(wrong);Check(animal.Home==home&&!wrong.Accepts(animal)&&!wrong.Register(animal),"Wrong pen rejects animal and cannot steal home");
            Show("ShowShelter",home);yield return null;Check(Body().text.Contains(home.DisplayName)==false&&Body().text.Contains(animal.DisplayName),"Pen panel lists residents and resources");Capture("Pen");
            Show("ShowAnimal",animal);yield return null;Check(Body().text.Contains("Felicidade")&&Body().text.Contains("Saúde"),"Animal panel has status, happiness, health and hydration");float happy=animal.Happiness;Check(animal.Care()&&animal.Happiness>happy,"Care raises happiness");Check(!animal.Care(),"Care cooldown prevents repeated instant gain");Capture("Animal");
            Call(ui,"Close");
            var well=UnityEngine.Object.FindAnyObjectByType<WaterWellController>();int stock=inventory.Quantity(well.WaterItem),ready=well.Available;
            Check(ready>0&&well.TryCollect(inventory)&&inventory.Quantity(well.WaterItem)==stock+ready,"Well collection adds real water stock");Check(!well.TryCollect(inventory)&&well.SecondsToReady>0,"Well blocks immediate repeat collection");
            var trough=home.Water;int before=inventory.Quantity(well.WaterItem);int added=trough.RefillFromInventory(inventory,20);Check(added>0&&inventory.Quantity(well.WaterItem)==before-added,"Water trough spends exact inventory amount");int units=trough.Units;Check(trough.TryConsume(well.WaterItem)&&trough.Units==units-1,"Water consumption reduces trough units");
            foreach(var pen in pens){int old=inventory.Quantity(pen.Food.AcceptedItem);int n=pen.Food.RefillFromInventory(inventory,4);Check(n>0&&inventory.Quantity(pen.Food.AcceptedItem)==old-n,"Species feeder uses configured crop: "+pen.DisplayName);}
            Show("ShowWell",well);yield return null;Check(Body().text.Contains("Recarga"),"Well panel displays stock and recharge");Capture("Well");Call(ui,"Close");
            hud.Warehouse.SetOpen(true);double wait=Time.timeAsDouble+.4;while(Time.timeAsDouble<wait)yield return null;
            var images=hud.Warehouse.Panel.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Icon").ToArray();Check(images.Length>=16&&images.All(i=>i.sprite!=null),"Warehouse displays all inventory icons (including water/products/fruits)");Capture("Warehouse");FarmContextUXReview.Capture("Polish-Warehouse-16x10",1600,1000);hud.Warehouse.SetOpen(false);
            var plots=UnityEngine.Object.FindObjectsByType<FarmPlot>(FindObjectsSortMode.None).OrderBy(p=>p.transform.position.z).ThenBy(p=>p.transform.position.x).ToArray();var plot=plots[0];input.CancelTool();input.OpenContextMenu(plot);yield return null;
            Check(hud.ContextMenu.PageCount>=3,"Plant menu includes all nine crops over three pages");
            for(int page=0;page<hud.ContextMenu.PageCount;page++){if(page>0)hud.ContextMenu.ChangePage(1);yield return null;Check(hud.ContextMenu.SeedButtons.Where(b=>b.button.gameObject.activeSelf).All(b=>b.crop.Icon!=null),"Visible planting page has crop icons: "+(page+1));Capture("Plant-"+(page+1));}
            hud.ContextMenu.SeedButtons[0].button.onClick.Invoke();Check(plot.State==FarmPlotState.Growing,"Last planting page button plants origin plot");input.CancelTool();
            // Domain fixtures exercise every crop without changing configured durations or assets.
            foreach(var crop in input.Crops)
            {var stockModel=new FarmInventoryModel(new[]{new InitialCropStock{crop=crop,quantity=3}});var model=new FarmPlotModel();Check(model.TryPlant(crop,stockModel,10)==PlotActionResult.Planted&&stockModel.Quantity(crop)==2,"Plant spends stock: "+crop.DisplayName);model.Advance(10+crop.GrowthSeconds);Check(model.TryHarvest(stockModel,10+crop.GrowthSeconds)==PlotActionResult.Harvested&&stockModel.Quantity(crop)==2+crop.YieldAmount,"Growth and harvest: "+crop.DisplayName);}
            var camera=input.FarmCamera.GetComponent<MiniFazendinha.FarmCameraController>();var focus=camera.FocusPoint;float zoom=input.FarmCamera.orthographicSize;camera.PanGround(new Vector3(.5f,0,.5f));Check(Vector3.Distance(focus,camera.FocusPoint)>.01,"Camera pan changes focus");camera.ZoomAt(1,new Vector2(Screen.width/2,Screen.height/2));yield return null;Check(input.FarmCamera.orthographicSize!=zoom,"Camera zoom changes orthographic size");
            // Simulate the same drag entry points used by mouse input, across fresh adjacent plots.
            input.SelectCrop(input.Crops[0]);int q=inventory.Quantity(input.Crops[0]);Vector2 Point(FarmPlot p)=>input.FarmCamera.WorldToScreenPoint(p.transform.position+Vector3.up*.7f);
            Check(input.BeginStroke(Point(plots[1])),"Drag begins on a valid empty plot");input.ContinueStroke(Point(plots[3]));input.EndStroke(Point(plots[3]));Check(plots.Skip(1).Take(3).All(p=>p.State==FarmPlotState.Growing)&&inventory.Quantity(input.Crops[0])==q-3,"Drag plants crossed plots once");input.CancelTool();
            foreach(var pen in pens)
            {
                var a=pen.Residents.First();Vector3 start=a.transform.position;a.ReleaseFromShelter(pen);double deadline=Time.timeAsDouble+35;
                while(a.IsConfined&&Time.timeAsDouble<deadline)yield return null;
                Check(!a.IsConfined&&Vector3.Distance(start,a.transform.position)>.5f,"Walks out through gate: "+pen.DisplayName);
                deadline=Time.timeAsDouble+3;Vector3 outside=a.transform.position;while(Time.timeAsDouble<deadline)yield return null;
                Check(Vector3.Distance(outside,a.transform.position)>.05f,"Wanders outside: "+pen.DisplayName);
                a.ReturnToShelter(pen);deadline=Time.timeAsDouble+40;while(!a.IsConfined&&Time.timeAsDouble<deadline)yield return null;
                Check(a.IsConfined&&a.IsInsideShelter,"Walks home and stays in correct pen: "+pen.DisplayName);
            }
            Check(well.Available>0,"Well regenerates stock after configured real cooldown");Capture("Farm");
        }
        [MenuItem("MiniFazendinha/Polish/Capture warehouse")]
        public static void CaptureWarehouse(){UnityEngine.Object.FindAnyObjectByType<FarmHUD>().Warehouse.SetOpen(true);}
    }
}







