using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MiniFazendinha.Planting;

namespace MiniFazendinha.EditorTools
{
    public static class FarmPlantingTests
    {
        const string Data="Assets/MiniFazendinha/Settings/Crops/";
        static CropDefinition[] Definitions() => new[]{"Carrot","Corn","Wheat"}.Select(n=>AssetDatabase.LoadAssetAtPath<CropDefinition>(Data+n+".asset")).ToArray();
        static void Check(bool condition,string message,StringBuilder log)
        {if(!condition)throw new Exception(message);log.AppendLine("PASS: "+message);}
        [MenuItem("MiniFazendinha/Planting/Test Business Rules")]
        public static void DomainTests()
        {
            var log=new StringBuilder("UNIFIED CROP STOCK BUSINESS TESTS\n");CropDefinition future=null;
            try
            {
                var crops=Definitions();Check(crops.All(c=>c!=null&&c.IsValid),"Three valid crop assets",log);
                Check(crops.Select(c=>c.Identifier).Distinct().Count()==3,"Unique identifiers",log);
                Check(crops[0].GrowthSeconds==20&&crops[1].GrowthSeconds==35&&crops[2].GrowthSeconds==50,"20/35/50 second assets unchanged",log);
                foreach(var crop in crops)
                {
                    var stock=new FarmInventoryModel(new[]{new InitialCropStock{crop=crop,quantity=12}});var plot=new FarmPlotModel();
                    Check(stock.Quantity(crop)==12,"Starts with 12 resources: "+crop.Identifier,log);
                    Check(plot.TryPlant(crop,stock,100)==PlotActionResult.Planted&&stock.Quantity(crop)==11,"Planting spends one from unified stock",log);
                    Check(plot.TryPlant(crop,stock,100)==PlotActionResult.Occupied&&stock.Quantity(crop)==11,"Double planting cannot spend twice",log);
                    Check(plot.TryHarvest(stock,100+crop.GrowthSeconds-.01)==PlotActionResult.NotReady&&stock.Quantity(crop)==11,"Early harvest leaves stock unchanged",log);
                    Check(Math.Abs(plot.Progress(100+crop.GrowthSeconds*.5)-.5)<.001,"Halfway progress uses configured duration",log);
                    Check(plot.Advance(100+crop.GrowthSeconds)&&plot.State==FarmPlotState.ReadyToHarvest,"Ready at configured deadline",log);
                    Check(plot.TryHarvest(stock,100+crop.GrowthSeconds)==PlotActionResult.Harvested&&stock.Quantity(crop)==14&&plot.State==FarmPlotState.Empty,"Harvest adds 3 to same balance: 12 -> 11 -> 14",log);
                    Check(plot.TryHarvest(stock,1000)==PlotActionResult.NotReady&&stock.Quantity(crop)==14,"Duplicate harvest cannot increase stock",log);
                    Check(plot.TryPlant(crop,stock,1001)==PlotActionResult.Planted&&stock.Quantity(crop)==13,"Harvested resources can immediately be replanted",log);
                    var empty=new FarmInventoryModel(new[]{new InitialCropStock{crop=crop,quantity=0}});var emptyPlot=new FarmPlotModel();
                    Check(emptyPlot.TryPlant(crop,empty,0)==PlotActionResult.NoStock&&empty.Quantity(crop)==0&&emptyPlot.State==FarmPlotState.Empty,"Zero stock rejects planting without negatives",log);
                    var multiply=new FarmInventoryModel(new[]{new InitialCropStock{crop=crop,quantity=1}});var first=new FarmPlotModel();first.TryPlant(crop,multiply,0);first.TryHarvest(multiply,crop.GrowthSeconds);
                    var next=new[]{new FarmPlotModel(),new FarmPlotModel(),new FarmPlotModel()};foreach(var p in next)p.TryPlant(crop,multiply,1000);
                    Check(next.All(p=>p.State==FarmPlotState.Growing)&&multiply.Quantity(crop)==0,"One resource grows into enough stock to plant three plots",log);
                    var overflow=new FarmInventoryModel(new[]{new InitialCropStock{crop=crop,quantity=int.MaxValue}});var protectedPlot=new FarmPlotModel();protectedPlot.TryPlant(crop,overflow,0);
                    Check(protectedPlot.TryHarvest(overflow,1000)==PlotActionResult.InventoryFull&&protectedPlot.State==FarmPlotState.ReadyToHarvest&&overflow.Quantity(crop)==int.MaxValue-1,"Overflow cannot destroy crop or corrupt stock",log);
                }
                var guardedStock=new FarmInventoryModel(new[]{new InitialCropStock{crop=crops[0],quantity=12}});var guarded=new FarmPlotModel();PlotActionResult nested=PlotActionResult.Planted;
                Action replant=()=>nested=guarded.TryPlant(crops[0],guardedStock,0);guardedStock.Changed+=replant;guarded.TryPlant(crops[0],guardedStock,0);guardedStock.Changed-=replant;
                Check(nested==PlotActionResult.Occupied&&guardedStock.Quantity(crops[0])==11,"Reentrant planting cannot spend twice",log);
                Action reharvest=()=>nested=guarded.TryHarvest(guardedStock,1000);guardedStock.Changed+=reharvest;guarded.TryHarvest(guardedStock,1000);guardedStock.Changed-=reharvest;
                Check(nested==PlotActionResult.NotReady&&guardedStock.Quantity(crops[0])==14,"Reentrant harvesting cannot credit twice",log);
                future=ScriptableObject.CreateInstance<CropDefinition>();var data=new SerializedObject(future);data.FindProperty("identifier").stringValue="future";data.FindProperty("displayName").stringValue="Future";data.FindProperty("growthSeconds").floatValue=2;data.FindProperty("yieldAmount").intValue=4;data.ApplyModifiedPropertiesWithoutUndo();
                var futureStock=new FarmInventoryModel(new[]{new InitialCropStock{crop=future,quantity=1}});var futurePlot=new FarmPlotModel();futurePlot.TryPlant(future,futureStock,0);futurePlot.TryHarvest(futureStock,2);
                Check(futureStock.Quantity(future)==4,"Fourth data-only crop uses configured yield without new logic",log);log.AppendLine("RESULT: PASS");Debug.Log("Unified stock business tests PASS.");
            }
            catch(Exception e){log.AppendLine("RESULT: FAIL "+e);Debug.LogError(log.ToString());}
            finally{if(future!=null)UnityEngine.Object.DestroyImmediate(future);File.WriteAllText("Library/MiniFazendinha-PlantingRulesTest.txt",log.ToString());}
        }
        static Mouse mouse,previousMouse;static Mouse[] isolatedMice;static FarmTestInputScope inputScope;
        static MouseState? pending;
        static FarmPlotInteractor input;
        static FarmInventory inventoryLive;
        static FarmHUD hud;
        static FarmPlot[] plotsLive;
        static CropDefinition[] definitions;
        static readonly List<Action> steps=new List<Action>();
        static readonly double[] planted=new double[3],observedReady=new double[3];
        static StringBuilder report;
        static int index,lastFrame,firstFrame;
        static bool active,waitingGrowth,awaitingReview;
        static double deadline;
        [MenuItem("MiniFazendinha/Planting/Test Full Cycle In Play Mode")]
        public static void StartPlayTest()
        {
            if(!EditorApplication.isPlaying||active)throw new InvalidOperationException("Start from a fresh Play Mode, with no active planting test.");
            input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();inventoryLive=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();
            plotsLive=UnityEngine.Object.FindObjectsByType<FarmPlot>().OrderBy(p=>p.name).ToArray();definitions=Definitions();
            if(input==null||hud==null||inventoryLive==null||plotsLive.Length!=12||plotsLive.Any(p=>p.State!=FarmPlotState.Empty))throw new InvalidOperationException("Expected a fresh scene with twelve empty plots.");
            inputScope=new FarmTestInputScope();previousMouse=Mouse.current;isolatedMice=InputSystem.devices.OfType<Mouse>().Where(m=>m.enabled).ToArray();foreach(var device in isolatedMice)InputSystem.DisableDevice(device);mouse=InputSystem.AddDevice<Mouse>("FarmPlanting_TestMouse");mouse.MakeCurrent();
            active=true;waitingGrowth=awaitingReview=false;index=0;firstFrame=lastFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+300;
            Array.Clear(planted,0,3);Array.Clear(observedReady,0,3);report=new StringBuilder("FULL PLAY MODE CYCLE: real Input System clicks; timeScale=1; original duration assets\n");
            steps.Clear();
            steps.Add(()=>{Assert(Time.frameCount>firstFrame,"Player loop advances");Assert(Mathf.Approximately(Time.timeScale,1f),"Normal time scale, no accelerated growth");Assert(plotsLive.All(p=>p.GetComponentInChildren<Collider>()!=null&&p.GetComponent<FarmPlotView>()!=null),"All twelve plots retain colliders and have views");Press(ButtonPoint(1));});
            steps.Add(()=>Release(ButtonPoint(1)));
            steps.Add(()=>{Assert(input.SelectedCrop==definitions[1],"UI button selects corn through Input System");Assert(plotsLive.All(p=>p.State==FarmPlotState.Empty)&&definitions.All(c=>inventoryLive.Quantity(c)==12),"Clicking UI does not plant or consume seeds");Assert(input.IsOverUI(ButtonPoint(1))&&!input.HandleScreenClick(ButtonPoint(1)),"UI raycast blocks plot interaction at the click position");Press(ButtonPoint(0));});
            steps.Add(()=>Release(ButtonPoint(0)));
            steps.Add(()=>{Assert(input.SelectedCrop==definitions[0],"UI button selects carrot");planted[0]=Time.timeAsDouble;Press(PlotPoint(8));});
            steps.Add(()=>Release(PlotPoint(8)));
            steps.Add(()=>{Assert(plotsLive[8].State==FarmPlotState.Growing&&plotsLive[8].Crop==definitions[0]&&inventoryLive.Quantity(definitions[0])==11,"Real click plants carrot and consumes one seed");Assert(plotsLive[8].TryPlant(definitions[1],inventoryLive)==PlotActionResult.Occupied,"A second culture cannot occupy a growing plot");Assert(plotsLive[8].TryHarvest(inventoryLive)==PlotActionResult.NotReady,"Growing plot cannot be harvested");Press(PlotPoint(8));});
            steps.Add(()=>Release(PlotPoint(8)));
            steps.Add(()=>{Assert(inventoryLive.Quantity(definitions[0])==11&&inventoryLive.Quantity(definitions[0])==11,"Repeated click during growth does not change inventory");Press(ButtonPoint(1));});
            steps.Add(()=>Release(ButtonPoint(1)));
            steps.Add(()=>{Assert(input.SelectedCrop==definitions[1],"Corn seed selected");planted[1]=Time.timeAsDouble;Press(PlotPoint(9));});
            steps.Add(()=>Release(PlotPoint(9)));
            steps.Add(()=>{Assert(plotsLive[9].State==FarmPlotState.Growing&&plotsLive[9].Crop==definitions[1]&&inventoryLive.Quantity(definitions[1])==11,"Real click plants corn");Press(ButtonPoint(2));});
            steps.Add(()=>Release(ButtonPoint(2)));
            steps.Add(()=>{Assert(input.SelectedCrop==definitions[2],"Wheat seed selected");planted[2]=Time.timeAsDouble;Press(PlotPoint(10));});
            steps.Add(()=>Release(PlotPoint(10)));
            steps.Add(()=>{Assert(plotsLive[10].State==FarmPlotState.Growing&&plotsLive[10].Crop==definitions[2]&&inventoryLive.Quantity(definitions[2])==11,"Real click plants wheat");Assert(plotsLive[8].Progress>0&&plotsLive[8].Progress<1,"Growing plot exposes progress");waitingGrowth=true;Write("STATE: WAITING_FOR_20_35_50_SECOND_GROWTH");});
            InputSystem.onBeforeUpdate+=BeforeInput;InputSystem.onAfterUpdate+=AfterInput;EditorApplication.update+=Tick;Write("STATE: PLANTING");
        }
        static Vector2 ButtonPoint(int seed)
        {
            if(!hud.Warehouse.IsOpen)
            {
                hud.ToggleWarehouse();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                typeof(FarmWarehousePanel).GetField("openness",flags).SetValue(hud.Warehouse,1f);
                typeof(FarmWarehousePanel).GetMethod("Apply",flags).Invoke(hud.Warehouse,null);
            }
            Canvas.ForceUpdateCanvases();return RectTransformUtility.WorldToScreenPoint(null,hud.SeedButtons[seed].button.transform.position);
        }
        static Vector2 PlotPoint(int plot) => input.FarmCamera.WorldToScreenPoint(plotsLive[plot].transform.TransformPoint(new Vector3(0,0.7f,0)));
        static void Press(Vector2 position){pending=new MouseState{position=position}.WithButton(MouseButton.Left);}
        static void Release(Vector2 position){pending=new MouseState{position=position};}
        static void BeforeInput()
        {
            if(InputState.currentUpdateType==InputUpdateType.Dynamic&&mouse!=null&&pending.HasValue){InputSystem.QueueStateEvent(mouse,pending.Value);pending=null;}
        }
        static void AfterInput(){if(InputState.currentUpdateType==InputUpdateType.Dynamic&&mouse!=null)mouse.MakeCurrent();}
        static void Assert(bool condition,string message){Check(condition,message,report);Write("STEP "+index);}
        static void Write(string state){File.WriteAllText("Library/MiniFazendinha-PlantingPlayTest.txt",report+"\n"+state+"\n");}
        static void Tick()
        {
            try
            {
                if(!active)return;
                if(!EditorApplication.isPlaying)throw new Exception("Play Mode stopped.");
                if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Play Mode test timed out.");
                for(int i=0;i<3;i++)if(planted[i]>0&&observedReady[i]==0&&plotsLive[i+8].State==FarmPlotState.ReadyToHarvest)
                {
                    observedReady[i]=Time.timeAsDouble;
                    Assert(observedReady[i]-planted[i]>=definitions[i].GrowthSeconds-0.1,"Natural growth completed for "+definitions[i].Identifier+" after "+(observedReady[i]-planted[i]).ToString("F2")+" game seconds");
                }
                if(waitingGrowth)
                {
                    if(observedReady.All(t=>t>0)){waitingGrowth=false;awaitingReview=true;Write("STATE: READY_FOR_VISUAL_REVIEW; invoke Continue Test Harvest");}return;
                }
                if(awaitingReview||Time.frameCount<lastFrame+3)return;
                lastFrame=Time.frameCount;if(index<steps.Count)steps[index++]();
            }
            catch(Exception ex){Finish(ex);}
        }
        [MenuItem("MiniFazendinha/Planting/Continue Test Harvest")]
        public static void ContinueHarvest()
        {
            if(!active||!awaitingReview)throw new InvalidOperationException("Wait for all three crops to become ready first.");
            input.SelectHarvestTool();awaitingReview=false;index=0;lastFrame=Time.frameCount;deadline=EditorApplication.timeSinceStartup+120;steps.Clear();
            steps.Add(()=>Press(PlotPoint(8)));steps.Add(()=>Release(PlotPoint(8)));
            steps.Add(()=>{Assert(plotsLive[8].State==FarmPlotState.Empty&&inventoryLive.Quantity(definitions[0])==14,"Click harvests carrot even while wheat seed is selected");Assert(plotsLive[8].TryHarvest(inventoryLive)==PlotActionResult.NotReady&&inventoryLive.Quantity(definitions[0])==14,"Second harvest cannot duplicate carrot production");Press(PlotPoint(9));});
            steps.Add(()=>Release(PlotPoint(9)));
            steps.Add(()=>{Assert(plotsLive[9].State==FarmPlotState.Empty&&inventoryLive.Quantity(definitions[1])==14,"Click harvests corn");Assert(plotsLive[9].TryHarvest(inventoryLive)==PlotActionResult.NotReady,"Second corn harvest rejected");Press(PlotPoint(10));});
            steps.Add(()=>Release(PlotPoint(10)));
            steps.Add(()=>{Assert(plotsLive[10].State==FarmPlotState.Empty&&inventoryLive.Quantity(definitions[2])==14,"Click harvests wheat");Assert(plotsLive[10].TryHarvest(inventoryLive)==PlotActionResult.NotReady,"Second wheat harvest rejected");Assert(definitions.All(c=>inventoryLive.Quantity(c)==14),"One seed per initial planting, no extra seed consumption");Assert(hud.SeedButtons.All(b=>b.label.text.Contains("14")),"Seed quantity labels update in UI");input.SelectCrop(definitions[2]);Press(PlotPoint(8));});
            steps.Add(()=>Release(PlotPoint(8)));
            steps.Add(()=>{Assert(plotsLive[8].State==FarmPlotState.Growing&&plotsLive[8].Crop==definitions[2]&&inventoryLive.Quantity(definitions[2])==13,"Harvested plot can be replanted with another culture");Assert(inventoryLive.Quantity(definitions[0])==14,"Replanting does not duplicate the previous harvest");Finish(null);});Write("STATE: HARVESTING");
        }
        static void Finish(Exception error)
        {
            if(!active)return;active=false;
            EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=BeforeInput;InputSystem.onAfterUpdate-=AfterInput;pending=null;
            if(mouse!=null){InputSystem.RemoveDevice(mouse);mouse=null;}foreach(var device in isolatedMice)if(device.added)InputSystem.EnableDevice(device);if(previousMouse!=null&&previousMouse.added)previousMouse.MakeCurrent();inputScope?.Dispose();
            report.AppendLine(error==null?"RESULT: PASS":"RESULT: FAIL "+error);Write("STATE: FINISHED");if(error==null)Debug.Log("Full planting Play Mode cycle PASS.");else Debug.LogError(report.ToString());
        }
    }
}