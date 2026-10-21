using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
    public static class FarmContextUXTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static FarmPlotInteractor input;static FarmInventory inventory;static FarmHUD hud;static FarmPlot[] plots;static CropDefinition[] crops;static MiniFazendinha.FarmCameraController camera;
        static Mouse mouse,previous;static Keyboard keyboard;static Mouse[] isolated;static FarmTestInputScope scope;static MouseState? pendingMouse;static KeyboardState? pendingKeyboard;
        static List<Action> steps=new List<Action>();static int index,frame;static double deadline,waitUntil;static bool active;static StringBuilder report;static int[] originalSeeds;static Vector3 focus;static float zoom;static Vector2 pointer;
        static FarmPlotModel Model(FarmPlot p)=>(FarmPlotModel)typeof(FarmPlot).GetField("model",Private).GetValue(p);
        static void Refresh(FarmPlot p)=>typeof(FarmPlotView).GetMethod("Refresh",Private).Invoke(p.GetComponent<FarmPlotView>(),null);
        static Vector2 Point(int i)=>input.FarmCamera.WorldToScreenPoint(plots[i].transform.TransformPoint(new Vector3(0,.70f,0)));
        static Vector2 Blank()=>input.FarmCamera.WorldToScreenPoint(new Vector3(13,0,-7));
        static Vector2 Button(UnityEngine.UI.Button b){Canvas.ForceUpdateCanvases();return RectTransformUtility.WorldToScreenPoint(null,b.transform.position);}
        static void Check(bool pass,string text){if(!pass)throw new Exception(text);report.AppendLine("PASS: "+text);Write("RUNNING "+index);}
        static void Write(string state)=>File.WriteAllText("Library/MiniFazendinha-ContextUXTest.txt",report+"\n"+state);
        static void Queue(Vector2 p,bool left=false,bool right=false,float scroll=0){var s=new MouseState{position=p,scroll=new Vector2(0,scroll)};if(left)s=s.WithButton(MouseButton.Left);if(right)s=s.WithButton(MouseButton.Right);pendingMouse=s;}
        static void Wait(float seconds=.3f)=>steps.Add(()=>waitUntil=EditorApplication.timeSinceStartup+seconds);
        static void Click(Func<Vector2> point){steps.Add(()=>Queue(point(),true));steps.Add(()=>Queue(point()));}
        static int Growing()=>plots.Count(p=>p.State==FarmPlotState.Growing);
        static void Reset(int seeds=12)
        {
            hud.CloseMenus();hud.StopAllCoroutines();input.UpdateHover(new Vector2(-100,-100));camera.PanGround(-camera.FocusPoint);camera.ZoomAt((input.FarmCamera.orthographicSize-23)/1.6f,new Vector2(input.FarmCamera.pixelWidth*.5f,input.FarmCamera.pixelHeight*.5f));camera.PanGround(-camera.FocusPoint);
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>())if(t.name=="HarvestAmount_Runtime"||t.name=="HarvestParticles_Runtime")UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach(var p in plots){var view=p.GetComponent<FarmPlotView>();view.SetHover(false,false);typeof(FarmPlotView).GetField("pulseUntil",Private).SetValue(view,0f);typeof(FarmPlotModel).GetMethod("Clear",Private).Invoke(Model(p),null);Refresh(p);}
            foreach(var stock in inventory.InitialStock)stock.quantity=seeds;typeof(FarmInventory).GetField("model",Private).SetValue(inventory,null);var initialized=inventory.Model;
        }
        static void Aged(int i,int crop,float progress){Model(plots[i]).TryPlant(crops[crop],inventory.Model,Time.timeAsDouble-crops[crop].GrowthSeconds*progress);Model(plots[i]).Advance(Time.timeAsDouble);Refresh(plots[i]);}
        static void Stroke(params int[] path){Check(input.BeginStroke(Point(path[0])),"Stroke begins on a valid plot");for(int i=1;i<path.Length;i++)input.ContinueStroke(Point(path[i]));input.EndStroke(Point(path[path.Length-1]));}
        [MenuItem("MiniFazendinha/Context UX/Test In Play Mode")]
        public static void Start()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused||active)throw new InvalidOperationException("Fresh unpaused Play Mode required.");
            input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();camera=input.FarmCamera.GetComponent<MiniFazendinha.FarmCameraController>();plots=UnityEngine.Object.FindObjectsByType<FarmPlot>().OrderBy(p=>p.transform.position.z).ThenBy(p=>p.transform.position.x).ToArray();crops=input.Crops;originalSeeds=inventory.InitialStock.Select(s=>s.quantity).ToArray();
            scope=new FarmTestInputScope();previous=Mouse.current;isolated=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();foreach(var m in isolated)InputSystem.DisableDevice(m);mouse=InputSystem.AddDevice<Mouse>("ContextUX_TestMouse");keyboard=InputSystem.AddDevice<Keyboard>("ContextUX_TestKeyboard");mouse.MakeCurrent();
            active=true;index=0;frame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+180;waitUntil=0;report=new StringBuilder("CONTEXT UX PLAY MODE TESTS\nReal queued Input System mouse/keyboard events plus deterministic model fixtures. Durations/assets unchanged.\n");steps.Clear();
            steps.Add(()=>{Check(plots.Length==12&&!hud.Warehouse.IsOpen&&!hud.ContextMenu.IsOpen,"Fresh scene: 12 plots; warehouse and contextual menu closed");Check(!hud.HarvestButton.gameObject.activeInHierarchy,"Sickle hidden when no harvest is ready");FarmContextUXReview.Capture("01-Closed-16x9");focus=camera.FocusPoint;});
            Click(()=>Button(hud.Warehouse.ToggleButton));Wait();
            steps.Add(()=>{Check(hud.Warehouse.IsOpen&&Vector3.Distance(focus,camera.FocusPoint)<.001f,"Warehouse button opens animated panel without moving camera");FarmContextUXReview.Capture("02-Warehouse-16x9");FarmContextUXReview.Capture("03-Warehouse-16x10",1600,1000);});
            Click(()=>Button(hud.Warehouse.CloseButton));steps.Add(()=>Check(!hud.Warehouse.IsOpen,"Warehouse X closes panel"));Wait();
            Click(()=>Button(hud.Warehouse.ToggleButton));Wait();
            Click(Blank);steps.Add(()=>Check(!hud.Warehouse.IsOpen&&!hud.ContextMenu.IsOpen,"World click closes warehouse"));
            Click(()=>Point(0));steps.Add(()=>{Check(hud.ContextMenu.IsOpen&&hud.ContextMenu.BoundPlot==plots[0]&&Growing()==0,"Empty plot click opens anchored seed choices without accidental planting");Check(!hud.ContextMenu.HarvestButton.gameObject.activeInHierarchy,"Empty contextual menu never offers sickle");FarmContextUXReview.Capture("04-Context-Empty-16x9");FarmContextUXReview.Capture("05-Context-Empty-16x10",1600,1000);});
            Click(()=>Button(hud.ContextMenu.CloseButton));steps.Add(()=>Check(!hud.ContextMenu.IsOpen,"Context X closes panel"));
            Click(()=>Point(0));
            Click(()=>Button(hud.ContextMenu.SeedButtons[1].button));
            steps.Add(()=>{Check(plots[0].Crop==crops[1]&&Growing()==1&&inventory.Quantity(crops[1])==11&&input.Mode==FarmInteractionMode.Plant&&!hud.ContextMenu.IsOpen,"Context seed choice plants origin exactly once and keeps tool active");focus=camera.FocusPoint;Queue(Point(1),true);});steps.Add(()=>Queue(Point(3),true));steps.Add(()=>Queue(Point(3)));
            steps.Add(()=>{Check(Growing()==4&&inventory.Quantity(crops[1])==8&&Vector3.Distance(camera.FocusPoint,focus)<.001f,"Real fast left drag paints only 3 crossed plots and does not pan camera");FarmContextUXReview.Capture("06-Planting-16x9");input.CancelTool();});
            Click(()=>Point(0));steps.Add(()=>{Check(hud.ContextMenu.IsOpen&&!hud.ContextMenu.HarvestButton.gameObject.activeInHierarchy,"Growing context shows crop/time/progress without sickle");FarmContextUXReview.Capture("07-Context-Growing-16x9");pendingKeyboard=new KeyboardState(Key.Escape);});
            steps.Add(()=>pendingKeyboard=new KeyboardState());steps.Add(()=>Check(!hud.ContextMenu.IsOpen&&input.Mode==FarmInteractionMode.None,"Real Escape closes context and cancels tool"));
            Click(()=>Point(0));Click(()=>Button(hud.Warehouse.ToggleButton));Wait();steps.Add(()=>Check(hud.Warehouse.IsOpen&&!hud.ContextMenu.IsOpen,"Opening warehouse closes previous plot context"));steps.Add(()=>pendingKeyboard=new KeyboardState(Key.Escape));steps.Add(()=>pendingKeyboard=new KeyboardState());steps.Add(()=>Check(!hud.Warehouse.IsOpen,"Real Escape closes warehouse"));
            steps.Add(()=>Reset());Wait();
            steps.Add(()=>{foreach(var crop in crops){Reset();input.SelectCrop(crop);for(int i=0;i<12;i++)input.HandleScreenClick(Point(i));Check(plots.All(p=>p.Crop==crop)&&inventory.Quantity(crop)==0,"Each data-defined crop works in any of 12 plots: "+crop.Identifier);}});
            steps.Add(()=>{Reset();input.SelectCrop(crops[0]);Stroke(0);Check(Growing()==1,"Single plot path plants one");Reset();input.SelectCrop(crops[1]);Stroke(0,2);Check(Growing()==3,"3 plot fast path plants three");Reset();input.SelectCrop(crops[2]);Stroke(0,3,7);Check(Growing()==5&&new[]{0,1,2,3,7}.All(i=>plots[i].Crop==crops[2]),"5 plot path excludes untraversed neighbors");Reset();input.SelectCrop(crops[0]);Stroke(0,3,7,4,8,11);Check(Growing()==12,"12 plot snake path plants all crossed plots");});
            steps.Add(()=>{Reset();input.SelectCrop(crops[0]);Stroke(0,3,0,3);Check(Growing()==4&&inventory.Quantity(crops[0])==8,"Backtracking never consumes twice");Reset(2);input.SelectCrop(crops[0]);Stroke(0,3);Check(Growing()==2&&inventory.Quantity(crops[0])==0,"Insufficient seeds stop new plantings at zero");Reset();plots[1].TryPlant(crops[2],inventory);input.SelectCrop(crops[0]);Stroke(0,3);Check(plots[1].Crop==crops[2]&&inventory.Quantity(crops[0])==9,"Occupied crop preserved without spending a selected seed");});
            steps.Add(()=>{Reset();input.SelectCrop(crops[0]);input.BeginStroke(Point(0));input.SelectHarvestTool();input.ContinueStroke(Point(3));Check(!input.IsDragging&&Growing()==1,"Tool switch cancels old gesture");input.SelectCrop(crops[0]);input.BeginStroke(Point(1));input.gameObject.SendMessage("OnApplicationFocus",false);input.ContinueStroke(Point(3));Check(!input.IsDragging&&Growing()==2,"Focus loss cancels gesture");});
            steps.Add(()=>{Reset();for(int i=0;i<12;i++)Aged(i,i%3,i%2==0?1.05f:.1f);input.CancelTool();});
            Click(()=>Point(0));steps.Add(()=>{Check(hud.ContextMenu.HarvestButton.gameObject.activeInHierarchy,"Mature context offers sickle");FarmContextUXReview.Capture("08-Context-Ready-16x9");});
            Click(()=>Button(hud.ContextMenu.HarvestButton));steps.Add(()=>{Check(plots[0].State==FarmPlotState.Empty&&inventory.Quantity(crops[0])==11&&input.Mode==FarmInteractionMode.Harvest,"Context sickle harvests origin with configured quantity");Queue(Point(1),true);});steps.Add(()=>Queue(Point(3),true));steps.Add(()=>Queue(Point(3)));
            steps.Add(()=>{Check(plots[2].State==FarmPlotState.Empty&&plots[1].State==FarmPlotState.Growing&&plots[3].State==FarmPlotState.Growing,"Real harvest drag skips growing plots");FarmContextUXReview.Capture("09-Harvesting-16x9");Stroke(0,3,7,4,8,11);Check(crops.All(c=>inventory.Quantity(c)==8+2*c.YieldAmount)&&plots.Where((p,i)=>i%2!=0).All(p=>p.State==FarmPlotState.Growing),"Mass harvest credits mixed crops and leaves growing plants");Stroke(0,3,7,4,8,11);Check(crops.All(c=>inventory.Quantity(c)==8+2*c.YieldAmount),"Repeated mass harvest cannot duplicate products");});
            Wait();steps.Add(()=>{Check(!hud.HarvestButton.gameObject.activeSelf&&input.Mode==FarmInteractionMode.None,"Sickle disappears and harvest mode ends when no crop is ready");Reset();});Wait();
            Click(()=>Button(hud.Warehouse.ToggleButton));Wait();
            steps.Add(()=>Queue(Button(hud.SeedButtons[0].button),true));steps.Add(()=>Queue(Point(3),true));steps.Add(()=>Queue(Point(3)));
            steps.Add(()=>{Check(Growing()==0,"Drag beginning over UI cannot paint after entering field");hud.SelectSeed(crops[0],null);focus=camera.FocusPoint;pointer=Blank();Queue(pointer,false,true);});steps.Add(()=>Queue(pointer+new Vector2(120,65),false,true));steps.Add(()=>Queue(pointer+new Vector2(120,65)));
            steps.Add(()=>{Check(Vector3.Distance(focus,camera.FocusPoint)>.1f&&Growing()==0&&input.Mode==FarmInteractionMode.Plant,"Right drag pans independently of selected planting tool");focus=camera.FocusPoint;pointer=Blank();Queue(pointer,true);});steps.Add(()=>Queue(pointer+new Vector2(100,45),true));steps.Add(()=>Queue(pointer+new Vector2(100,45)));
            steps.Add(()=>{Check(Vector3.Distance(focus,camera.FocusPoint)>.1f&&Growing()==0,"Left drag on empty terrain pans without planting");focus=camera.FocusPoint;Queue(pointer+new Vector2(190,90));});steps.Add(()=>{Check(Vector3.Distance(focus,camera.FocusPoint)<.001f,"Mouse release stops navigation");camera.PanGround(-camera.FocusPoint);pointer=new Vector2(input.FarmCamera.pixelWidth*.45f,input.FarmCamera.pixelHeight*.5f);zoom=input.FarmCamera.orthographicSize;focus=Ground(pointer);Queue(pointer,false,false,1);});Wait(.65f);
            steps.Add(()=>{Check(zoom-input.FarmCamera.orthographicSize>1f&&Vector3.Distance(focus,Ground(pointer))<.12f,"Normalized scroll=1 changes zoom visibly and preserves cursor ground region");Check(input.FarmCamera.orthographic,"Camera remains orthographic");camera.ZoomAt(1000,pointer);Check(Mathf.Approximately(input.FarmCamera.orthographicSize,camera.MinimumZoom),"Minimum zoom clamps");camera.ZoomAt(-1000,pointer);Check(Mathf.Approximately(input.FarmCamera.orthographicSize,camera.MaximumZoom),"Maximum zoom clamps");camera.PanGround(new Vector3(999,0,-999));Check(Mathf.Abs(camera.FocusPoint.x)<=20&&Mathf.Abs(camera.FocusPoint.z)<=20,"Navigation limits keep center near playable area");Reset();});Wait();
            steps.Add(()=>{hud.ToggleWarehouse();});Wait();
            steps.Add(()=>{zoom=input.FarmCamera.orthographicSize;pointer=Button(hud.Warehouse.ToggleButton);Queue(pointer,false,false,1);});Wait(.65f);
            steps.Add(()=>{Check(zoom-input.FarmCamera.orthographicSize>1&&hud.Warehouse.IsOpen&&hud.Warehouse.ToggleButton.gameObject.activeInHierarchy,"Scroll over UI zooms Game camera while warehouse and button remain visible");FarmContextUXReview.Capture("13-Zoomed-Warehouse-16x9");pointer=new Vector2(input.FarmCamera.pixelWidth*.5f,input.FarmCamera.pixelHeight*.5f);zoom=input.FarmCamera.orthographicSize;Queue(pointer,false,false,-1);});Wait(.65f);
            steps.Add(()=>{Check(input.FarmCamera.orthographicSize-zoom>1,"Normalized negative scroll visibly zooms out");Reset();});Wait();
            steps.Add(()=>{foreach(var i in new[]{0,3,8,11}){camera.PanGround(new Vector3(plots[i].transform.position.x<0?18:-18,0,plots[i].transform.position.z< -8?18:-18)-camera.FocusPoint);input.OpenContextMenu(plots[i]);hud.ContextMenu.Refresh();hud.ContextMenu.Reposition();FarmContextUXReview.AssertInside(hud.ContextMenu.Panel,(RectTransform)hud.transform,"Corner menu");}Check(true,"Context menus clamp to screen edges for all four field corners");Reset();input.OpenContextMenu(plots[0]);hud.ContextMenu.Refresh();hud.ContextMenu.Reposition();var size=hud.ContextMenu.Panel.sizeDelta;var position=hud.ContextMenu.Panel.anchoredPosition;camera.ZoomAt(2,new Vector2(input.FarmCamera.pixelWidth*.5f,input.FarmCamera.pixelHeight*.5f));hud.ContextMenu.Reposition();Check(hud.ContextMenu.Panel.sizeDelta==size&&Vector2.Distance(position,hud.ContextMenu.Panel.anchoredPosition)>1,"Context follows zoom while retaining fixed canvas size");Reset();for(int i=0;i<12;i++)Aged(i,i%3,i<4?.06f:i<8?.60f:1.05f);});Wait();
            steps.Add(()=>{hud.ToggleWarehouse();});Wait();steps.Add(()=>{Check(!hud.HarvestButton.gameObject.activeInHierarchy,"Warehouse never displays a sickle, even when crops are ready");FarmContextUXReview.Capture("10-Overview-Warehouse-16x9");FarmContextUXReview.Capture("11-Overview-Warehouse-16x10",1600,1000);hud.CloseMenus();});Wait();
            steps.Add(()=>{FarmContextUXReview.Capture("12-Overview-Closed-16x9");Check(plots.All(p=>p.GetComponentInChildren<MeshCollider>()!=null)&&UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length==1,"12 original colliders and single EventSystem retained");Finish(null);});
            InputSystem.onBeforeUpdate+=BeforeInput;InputSystem.onAfterUpdate+=AfterInput;EditorApplication.update+=Tick;Write("STARTED");
        }
        static Vector3 Ground(Vector2 p){var ray=input.FarmCamera.ScreenPointToRay(p);new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float d);return ray.GetPoint(d);}
        static void BeforeInput(){if(InputState.currentUpdateType!=InputUpdateType.Dynamic)return;if(pendingMouse.HasValue){InputSystem.QueueStateEvent(mouse,pendingMouse.Value);pendingMouse=null;}if(pendingKeyboard.HasValue){InputSystem.QueueStateEvent(keyboard,pendingKeyboard.Value);pendingKeyboard=null;}}
        static void AfterInput(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&mouse!=null)mouse.MakeCurrent();}
        static void Tick(){try{if(!active)return;if(!EditorApplication.isPlaying||EditorApplication.timeSinceStartup>deadline)throw new Exception("Test interrupted/timeout");if(Time.frameCount<frame+3||EditorApplication.timeSinceStartup<waitUntil)return;frame=Time.frameCount;if(index<steps.Count)steps[index++]();}catch(Exception e){Finish(e);}}
        static void Finish(Exception e){if(!active)return;active=false;EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=BeforeInput;InputSystem.onAfterUpdate-=AfterInput;input.CancelDrag();if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);foreach(var m in isolated)if(m.added)InputSystem.EnableDevice(m);if(previous!=null&&previous.added)previous.MakeCurrent();scope.Dispose();for(int i=0;i<originalSeeds.Length;i++)inventory.InitialStock[i].quantity=originalSeeds[i];report.AppendLine(e==null?"RESULT: PASS":"RESULT: FAIL "+e);Write("FINISHED");if(e==null){EditorApplication.isPaused=true;Debug.Log("Context UX tests PASS; paused for review.");}else Debug.LogError(report.ToString());}
    }
}