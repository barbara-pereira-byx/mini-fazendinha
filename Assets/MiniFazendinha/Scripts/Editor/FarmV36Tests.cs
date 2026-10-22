using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.Planting;
using MiniFazendinha.Animals;
namespace MiniFazendinha.EditorTools
{
    public static class FarmV36Tests
    {
        static IEnumerator steps;
        static FarmTestInputScope scope;
        static Mouse mouse, previous;
        static Mouse[] originals;
        static MouseState? pending;
        static int frame;
        static double next;
        static StringBuilder log;
        static string report;
        static void Check(bool ok,string message)
        {log.AppendLine((ok?"PASS: ":"FAIL: ")+message);File.WriteAllText(report,log.ToString());if(!ok)throw new Exception(message);}
        static void Queue(Vector2 p,float wheel=0,bool press=false)
        {var state=new MouseState{position=p,scroll=new Vector2(0,wheel)};if(press)state=state.WithButton(MouseButton.Left);pending=state;}
        static Vector2 Point(Transform t)=>RectTransformUtility.WorldToScreenPoint(null,t is RectTransform r?r.TransformPoint(r.rect.center):t.position);
        static void Start(IEnumerator routine,string name)
        {
            if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
            if(steps!=null)Finish();
            report="Reports/V36/"+name+".txt";Directory.CreateDirectory("Reports/V36");log=new StringBuilder(name+" — actual queued Input System events\n");
            scope=new FarmTestInputScope();previous=Mouse.current;originals=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();foreach(var m in originals)InputSystem.DisableDevice(m);
            mouse=InputSystem.AddDevice<Mouse>("V36Tests");mouse.MakeCurrent();steps=routine;frame=Time.frameCount;next=0;
            InputSystem.onBeforeUpdate+=Before;InputSystem.onAfterUpdate+=After;EditorApplication.update+=Tick;
        }
        static void Before(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&pending.HasValue){InputSystem.QueueStateEvent(mouse,pending.Value);pending=null;}}
        static void After(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&mouse!=null)mouse.MakeCurrent();}
        static void Tick()
        {
            if(!EditorApplication.isPlaying){log.AppendLine("ABORTED: Play Mode stopped before test completion");Finish();return;}
            if(Time.frameCount<frame+3||EditorApplication.timeSinceStartup<next)return;frame=Time.frameCount;next=EditorApplication.timeSinceStartup+.08;
            try{if(!steps.MoveNext()){log.AppendLine("COMPLETED");Finish();}}catch(Exception e){log.AppendLine(e.ToString());Finish();}
        }
        static void Finish()
        {EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Before;InputSystem.onAfterUpdate-=After;File.WriteAllText(report,log.ToString());if(mouse!=null)InputSystem.RemoveDevice(mouse);foreach(var m in originals)if(m.added)InputSystem.EnableDevice(m);if(previous!=null&&previous.added)previous.MakeCurrent();scope.Dispose();steps=null;mouse=null;pending=null;}
        [MenuItem("MiniFazendinha/V3.6/Test scroll priority")]
        public static void Scroll()=>Start(ScrollRoutine(),"01-scroll");
        [MenuItem("MiniFazendinha/V3.6/Test world clicks and planting")]
        public static void Pointer()=>Start(PointerRoutine(),"04-pointer-planting");
        static IEnumerator Click(Vector2 p){Queue(p);yield return null;Queue(p,0,true);yield return null;Queue(p);yield return null;yield return null;}
        static IEnumerator PointerRoutine()
        {
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();hud.CloseMenus();ui.Close();
            var building=UnityEngine.Object.FindAnyObjectByType<FarmWarehouseBuilding>();var collider=building.GetComponentsInChildren<Collider>().First(c=>!c.isTrigger);var click=Click(Camera.main.WorldToScreenPoint(collider.bounds.center));while(click.MoveNext())yield return null;
            Check(ui.IsOpen&&ui.Panel.name=="Armazém","Real building click opens all-item warehouse manager");
            var scroll=ui.Panel.GetComponentInChildren<ScrollRect>();float zoom=Camera.main.orthographicSize;Queue(Point(scroll.viewport),-100);for(int n=0;n<12;n++)yield return null;Check(Camera.main.orthographicSize==zoom,"Full warehouse scroll never changes camera zoom");Check(scroll.verticalNormalizedPosition<.001f,"Full warehouse reaches final item");
            click=Click(Point(ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Fechar").transform));while(click.MoveNext())yield return null;Check(!ui.IsOpen,"Warehouse X closes real panel");
            var well=UnityEngine.Object.FindAnyObjectByType<WaterWellController>();click=Click(Camera.main.WorldToScreenPoint(well.GetComponent<Collider>().bounds.center));while(click.MoveNext())yield return null;Check(ui.IsOpen&&ui.Panel.name=="Poço de água","Real well click opens collection panel");int old=inventory.Quantity(well.WaterItem),available=well.Available;click=Click(Point(ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Coletar água").transform));while(click.MoveNext())yield return null;Check(inventory.Quantity(well.WaterItem)==old+available&&well.Available==0,"Real collect button adds water and starts recharge");ui.Close();
            var plot=UnityEngine.Object.FindObjectsByType<FarmPlot>().First(p=>p.State==FarmPlotState.Empty);click=Click(Camera.main.WorldToScreenPoint(plot.transform.position+Vector3.up*.7f));while(click.MoveNext())yield return null;Check(hud.ContextMenu.IsOpen,"Real plot click opens planting menu");Check(hud.ContextMenu.SeedButtons.Count(b=>b.button.gameObject.activeSelf)==3,"Exactly three crop cards on first page");FarmContextUXReview.Capture("V36-planting",1600,900);yield return null;yield return null;
            var next=hud.ContextMenu.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="▶");click=Click(Point(next.transform));while(click.MoveNext())yield return null;Check(hud.ContextMenu.Page==1&&plot.State==FarmPlotState.Empty,"Arrow changes page without accidental planting (page="+hud.ContextMenu.Page+", state="+plot.State+")");var binding=hud.ContextMenu.SeedButtons[0];var crop=binding.crop;old=inventory.Quantity(crop);click=Click(Point(binding.button.transform));while(click.MoveNext())yield return null;Check(plot.Crop==crop&&plot.State==FarmPlotState.Growing&&inventory.Quantity(crop)==old-1,"Paginated crop click plants exactly one unit");input.CancelTool();
            var choices=UnityEngine.Object.FindObjectsByType<FarmPlot>().Where(p=>p.State==FarmPlotState.Empty).ToArray();var first=choices[0];var empty=new[]{first,choices.Where(p=>p!=first).OrderBy(p=>Vector3.Distance(first.transform.position,p.transform.position)).First()};input.SelectCrop(crop);old=inventory.Quantity(crop);Check(input.BeginStroke(Camera.main.WorldToScreenPoint(empty[0].transform.position+Vector3.up*.7f)),"Plant drag begins");input.ContinueStroke(Camera.main.WorldToScreenPoint(empty[1].transform.position+Vector3.up*.7f));input.EndStroke(Camera.main.WorldToScreenPoint(empty[1].transform.position+Vector3.up*.7f));Check(empty.All(p=>p.Crop==crop)&&inventory.Quantity(crop)==old-2,"Drag planting affects both fields once");input.CancelTool();            var model=typeof(FarmPlot).GetField("model",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(plot);typeof(FarmPlotModel).GetField("readyAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(model,Time.timeAsDouble-1);yield return null;input.SelectHarvestTool();old=inventory.Quantity(crop);click=Click(Camera.main.WorldToScreenPoint(plot.transform.position+Vector3.up*.7f));while(click.MoveNext())yield return null;Check(plot.State==FarmPlotState.Empty&&inventory.Quantity(crop)==old+crop.YieldAmount,"Real sickle click harvests configured yield exactly once");input.CancelTool();
        }
        [MenuItem("MiniFazendinha/V3.6/Test animal circulation")]
        public static void Navigation()=>Start(NavigationRoutine(),"05-navigation");
        static IEnumerator NavigationRoutine()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;ui.Close();var home=UnityEngine.Object.FindObjectsByType<AnimalShelter>().First(s=>s.PermittedSpecies!=null&&s.PermittedSpecies.name=="Cow");var animals=home.Residents.ToArray();Check(animals.Length==2,"Two cows registered before movement test");
            var before=animals.Select(a=>a.transform.position).ToArray();home.SetGate(true);home.ReleaseAll();double timeout=Time.timeAsDouble+30;while(Time.timeAsDouble<timeout&&animals.Any(a=>a.IsInsideShelter))yield return null;
            foreach(var a in animals)log.AppendLine(a.DisplayName+" at "+a.transform.position+" state="+a.StateLabel+" inside="+a.IsInsideShelter);Check(animals.All(a=>!a.IsInsideShelter),"Both cows walk out through the real gate");Check(animals.Select((a,i)=>Vector3.Distance(a.transform.position,before[i])).All(d=>d>1),"Animals move physically without teleport recovery");
            home.RecollectAll();timeout=Time.timeAsDouble+45;while(Time.timeAsDouble<timeout&&animals.Any(a=>!a.IsInsideShelter||!a.IsConfined))yield return null;foreach(var a in animals)log.AppendLine("Return "+a.DisplayName+" at "+a.transform.position+" "+a.StateLabel);Check(animals.All(a=>a.IsInsideShelter&&a.IsConfined),"Bell returns resident cows and keeps them confined");
            float minimum=float.MaxValue;timeout=Time.timeAsDouble+10;while(Time.timeAsDouble<timeout){minimum=Mathf.Min(minimum,Vector3.Distance(animals[0].transform.position,animals[1].transform.position));yield return null;}Check(minimum>=animals[0].Definition.BodyRadius+animals[1].Definition.BodyRadius-.03f,"Two-cow clearance maintained, minimum="+minimum);
        }
        [MenuItem("MiniFazendinha/V3.6/Test four animals and active needs")]
        public static void FourAnimals()=>Start(FourRoutine(),"06-four-animals");
        static IEnumerator FourRoutine()
        {
            var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var source=FarmAnimalRegistry.All.First(a=>a.Definition.name=="Chicken");var home=source.Home;var clone=UnityEngine.Object.Instantiate(source.gameObject);clone.name="V36_FourthAnimal_TEST";var fourth=clone.GetComponent<FarmAnimalController>();fourth.Assign(source.Definition,"v36-test-fourth","Galinha de teste",home,inventory,clone.GetComponentInChildren<Animator>(),clone.transform.Find("Visual"),null,new[]{home.Food,home.Water});var bounds=home.RoamingBounds;Vector3? spawn=null;float radius=source.Definition.BodyRadius;for(float x=bounds.min.x+radius+.2f;x<bounds.max.x-radius-.2f&&!spawn.HasValue;x+=.2f)for(float z=bounds.min.z+radius+.2f;z<bounds.max.z-radius-.2f;z+=.2f){var p=new Vector3(x,.075f,z);if(home.Residents.Any(a=>a!=fourth&&Vector3.Distance(a.transform.position,p)<radius+a.Definition.BodyRadius+.1f))continue;if(Physics.OverlapSphere(p+Vector3.up*.4f,radius,~0,QueryTriggerInteraction.Ignore).Any(c=>!c.transform.IsChildOf(clone.transform)&&c.bounds.max.y>.255f))continue;spawn=p;break;}if(!spawn.HasValue){UnityEngine.Object.Destroy(clone);throw new Exception("No collision-free fourth-animal fixture position");}clone.transform.position=spawn.Value;log.AppendLine("Fourth fixture starts in a verified free position: "+spawn.Value);yield return null;
            var residents=home.Residents.ToArray();Check(residents.Length==4,"Four compatible residents registered without changing existing identifiers");double started=Time.timeAsDouble;float initial=source.FeedingLevel,water=source.HydrationLevel,minimum=float.PositiveInfinity;
            while(Time.timeAsDouble-started<30){for(int i=0;i<residents.Length;i++)for(int j=i+1;j<residents.Length;j++)minimum=Mathf.Min(minimum,Vector3.Distance(residents[i].transform.position,residents[j].transform.position));yield return null;}
            double elapsed=Time.timeAsDouble-started;Check(minimum>=source.Definition.BodyRadius*2-.025f,"Four walking/resting animals preserve clearance, minimum="+minimum);Check(Math.Abs(initial-source.FeedingLevel-elapsed/source.Definition.FedForSeconds)<.001,"Actual active-time food decline over "+elapsed.ToString("F2")+" seconds: "+initial.ToString("F6")+" -> "+source.FeedingLevel.ToString("F6"));Check(Math.Abs(water-source.HydrationLevel-elapsed/source.Definition.HydrationDuration)<.001,"Actual active-time hydration matches configured duration");
            Check(home.Food.TryReserve(residents[0])&&!home.Food.TryReserve(residents[1]),"A station admits one animal at a time");home.Food.Release(residents[0]);Check(home.Food.TryReserve(residents[1]),"Next waiting animal can reserve station");home.Food.Release(residents[1]);UnityEngine.Object.Destroy(clone);
        }
        static IEnumerator ScrollRoutine()
        {
            var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var camera=Camera.main;hud.CloseMenus();hud.Warehouse.SetOpen(true);
            for(int i=0;i<6;i++)yield return null;
            var scroll=hud.Warehouse.Panel.GetComponentInChildren<ScrollRect>();var pointer=Point(scroll.viewport);Queue(pointer);yield return null;
            float zoom=camera.orthographicSize,position=scroll.verticalNormalizedPosition;
            Check(MiniFazendinha.UI.FarmInputPriority.CapturesScroll(pointer),"Scrollable viewport captures wheel");
            Queue(pointer,-3);for(int i=0;i<8;i++)yield return null;
            Check(camera.orthographicSize==zoom,"UI scroll changes camera size by exactly zero");Check(scroll.verticalNormalizedPosition<position,"Wheel moves list responsively");
            Queue(pointer,-100);for(int i=0;i<6;i++)yield return null;Check(scroll.verticalNormalizedPosition<=.001f,"Last item can be reached without overscroll");
            Queue(pointer,100);for(int i=0;i<6;i++)yield return null;Check(scroll.verticalNormalizedPosition>=.999f,"Top boundary is clamped");
            var outside=new Vector2(Screen.width*.35f,Screen.height*.5f);Check(!MiniFazendinha.UI.FarmInputPriority.CapturesScroll(outside),"Free scenery does not capture wheel");Queue(outside,1);for(int i=0;i<10;i++)yield return null;Check(camera.orthographicSize<zoom,"Outside scroll still zooms camera");hud.CloseMenus();
        }
        [MenuItem("MiniFazendinha/V3.6/Audit existing scene")]
        public static void Audit()
        {
            var log=new StringBuilder();log.AppendLine("Scene: "+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);log.AppendLine("Compile error: "+EditorUtility.scriptCompilationFailed);
            foreach(var a in UnityEngine.Object.FindObjectsByType<FarmAnimalController>()){var rs=a.GetComponentsInChildren<SkinnedMeshRenderer>();var b=rs.First().bounds;foreach(var r in rs)b.Encapsulate(r.bounds);log.AppendLine(a.name+" home="+a.Home.ShelterId+" radius="+a.Definition.BodyRadius+" rendered="+b.size);}
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>())log.AppendLine("Canvas "+canvas.name+" "+canvas.renderMode+" scaler="+canvas.GetComponent<CanvasScaler>()?.referenceResolution);
            foreach(string guid in AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"}))
            {var d=AssetDatabase.LoadAssetAtPath<MiniFazendinha.FruitTrees.FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(guid));log.AppendLine("Tree "+d.name+" prefab="+AssetDatabase.GetAssetPath(d.ModelPrefab));foreach(var r in d.ModelPrefab.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)log.AppendLine("  "+r.name+": "+(m==null?"MISSING":m.name+" / "+m.shader.name+" texture="+m.mainTexture));}
            foreach(var t in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))if(t.name.Contains("Celeiro")||t.name.Contains("Casa"))log.AppendLine("Building "+t.name+" "+t.position);
            Directory.CreateDirectory("Reports/V36");File.WriteAllText("Reports/V36/00-audit.txt",log.ToString());
        }
    }
}
