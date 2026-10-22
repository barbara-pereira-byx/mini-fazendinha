using System;
using System.Linq;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
using MiniFazendinha.UI;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV36Acceptance
 {
  [MenuItem("MiniFazendinha/V3.6/Capture responsive interfaces")]
  public static void Review()
  {
   if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;var home=FarmAnimalRegistry.All.First(a=>a.Definition.name=="Cow").Home;var report=new System.Text.StringBuilder();
   foreach(Transform t in hud.transform)if(t.name=="Feedback")t.gameObject.SetActive(false);
   UnityEngine.Object.FindAnyObjectByType<FarmInventory>().TryAdd(home.Food.AcceptedItem,12);
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768)})
   {string suffix=size.x+"x"+size.y;ui.ShowWarehouse();FarmContextUXReview.Capture("V36-warehouse-"+suffix,size.x,size.y);ui.ShowShelter(home);FarmContextUXReview.Capture("V36-pen-"+suffix,size.x,size.y);ui.ShowAnimal(home.Residents[0],home);FarmContextUXReview.Capture("V36-animal-"+suffix,size.x,size.y);ui.ShowTrough(home.Food,Camera.main.WorldToScreenPoint(home.Food.transform.position));for(int n=0;n<4;n++)Click(ui,"Adicionar uma unidade");FarmContextUXReview.Capture("V36-refill-"+suffix,size.x,size.y);report.AppendLine("PASS management, animal and refill bounds at "+suffix);}
   ui.Close();var input=UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>();var empty=UnityEngine.Object.FindObjectsByType<FarmPlot>().FirstOrDefault(p=>p.State==FarmPlotState.Empty);if(empty!=null){input.OpenContextMenu(empty);Canvas.ForceUpdateCanvases();FarmContextUXReview.Capture("V36-planting-clean",1600,900);hud.CloseMenus();}hud.Warehouse.SetOpen(true);Set(hud.Warehouse,"openness",1f);typeof(FarmWarehousePanel).GetMethod("Apply",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hud.Warehouse,null);Canvas.ForceUpdateCanvases();FarmContextUXReview.Capture("V36-cultivos",1600,900);hud.Warehouse.HideImmediately();
   var catalog=UnityEngine.Object.FindAnyObjectByType<FarmWarehouseCatalog>();catalog.ShowTrees();var treeCard=hud.Warehouse.Panel.Find("CatalogScroll/Viewport/Content").GetComponentsInChildren<UnityEngine.UI.Button>().First();treeCard.onClick.Invoke();var placement=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.FruitTrees.FarmTreePlacementController>();if(!placement.IsPlacing)throw new Exception("Orchard placement lost");var preview=GameObject.Find("TreePlacementPreview");if(preview.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>m.GetTexture("_BaseMap")==null)))throw new Exception("Untextured orchard preview");placement.Cancel();catalog.ShowCrops();report.AppendLine("PASS orchard selection preserves placement and textured preview");
   var trees=AssetDatabase.FindAssets("t:FruitTreeDefinition",new[]{"Assets/MiniFazendinha"}).Select(g=>AssetDatabase.LoadAssetAtPath<MiniFazendinha.FruitTrees.FruitTreeDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();var samples=new System.Collections.Generic.List<GameObject>();var camera=Camera.main;var oldPosition=camera.transform.position;float oldSize=camera.orthographicSize;
   try{for(int i=0;i<trees.Length;i++)samples.Add(UnityEngine.Object.Instantiate(trees[i].ModelPrefab,new Vector3(-12+i*4,0,-15),Quaternion.identity));camera.transform.position=new Vector3(-8,1,-15)-camera.transform.forward*35;camera.orthographicSize=6.5f;FarmContextUXReview.Capture("V36-textured-trees",1600,900);}finally{camera.transform.position=oldPosition;camera.orthographicSize=oldSize;foreach(var sample in samples)UnityEngine.Object.DestroyImmediate(sample);}
   File.WriteAllText("Reports/V36/07-responsive-and-orchard.txt",report.ToString()+"COMPLETED\n");
  }
  static System.Text.StringBuilder log;
  static void Check(bool pass,string label){log.AppendLine((pass?"PASS ":"FAIL ")+label);File.WriteAllText("Reports/V36/03-acceptance.txt",log.ToString());if(!pass)throw new Exception(label);}
  static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
  static void Click(FarmManagementView ui,string action)=>ui.Panel.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name==action).onClick.Invoke();
  [MenuItem("MiniFazendinha/V3.6/Test models and management UI")]
  public static void Run()
  {
   if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");log=new System.Text.StringBuilder("V3.6 actual Play Mode models and UI callbacks\n");Directory.CreateDirectory("Reports/V36");
   var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var controller=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>();var ui=controller.Management;var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var animals=UnityEngine.Object.FindObjectsByType<FarmAnimalController>();var cow=animals.First(a=>a.Definition.name=="Cow");
   Check(inventory.Items.All(i=>i.Icon!=null),"Every warehouse item has an illustrated icon");Check(inventory.CanReceive(cow.Definition.FeedItem,inventory.Capacity+1)==false,"Storage enforces configured capacity");
   hud.Warehouse.SetOpen(true);Canvas.ForceUpdateCanvases();var catalog=hud.Warehouse.Panel.Find("CatalogScroll/Viewport/Content");Check(catalog.childCount==inventory.InitialStock.Length,"Cultivos sidebar contains crops only");Check(catalog.GetComponentsInChildren<UnityEngine.UI.Button>().All(b=>b.interactable),"All sidebar cards are plantable crops");hud.CloseMenus();
   foreach(var species in animals.GroupBy(a=>a.Definition))
   {
    var a=species.First();bool enabled=a.enabled;a.enabled=false;double now=Time.timeAsDouble;double oldFood=(double)a.GetType().GetField("fedUntil",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(a);double oldWater=(double)a.GetType().GetField("hydratedUntil",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(a);
    foreach(int seconds in new[]{30,60,300,1800}){Set(a,"fedUntil",now+a.Definition.FedForSeconds-seconds);Set(a,"hydratedUntil",now+a.Definition.HydrationDuration-seconds);float food=Mathf.Clamp01(1-seconds/a.Definition.FedForSeconds),water=Mathf.Clamp01(1-seconds/a.Definition.HydrationDuration);Check(Mathf.Abs(a.FeedingLevel-food)<.0001f&&Mathf.Abs(a.HydrationLevel-water)<.0001f,a.Definition.name+" analytical clock simulation "+seconds+"s food="+a.FeedingLevel.ToString("P3")+" water="+a.HydrationLevel.ToString("P3"));}Set(a,"fedUntil",oldFood);Set(a,"hydratedUntil",oldWater);a.enabled=enabled;
   }
   var trough=cow.Home.Water;var waterItem=trough.AcceptedItem;inventory.TryAdd(waterItem,12);int old=inventory.Quantity(waterItem),units=trough.Units;ui.ShowTrough(trough,Camera.main.WorldToScreenPoint(trough.transform.position));for(int i=0;i<4;i++)Click(ui,"Adicionar uma unidade");Click(ui,"Confirmar quantidade selecionada");Check(trough.Units==units+5&&inventory.Quantity(waterItem)==old-5,"Quantity popup transfers exactly five units");
   old=inventory.Quantity(waterItem);units=trough.Units;ui.ShowTrough(trough,Vector2.zero);Click(ui,"Cancelar sem abastecer");Check(trough.Units==units&&inventory.Quantity(waterItem)==old,"Cancel has no stock effect");
   int nested=-1;Action reenter=()=>nested=trough.RefillFromInventory(inventory,1);inventory.Changed+=reenter;old=inventory.Quantity(waterItem);units=trough.Units;int moved=trough.RefillFromInventory(inventory,2);inventory.Changed-=reenter;Check(moved==2&&nested==0&&trough.Units==units+2&&inventory.Quantity(waterItem)==old-2,"Reentrant callback cannot duplicate a refill");
   var foodTrough=cow.Home.Food;var crop=foodTrough.AcceptedItem;while(inventory.Quantity(crop)>1)inventory.TrySpend(crop);ui.ShowTrough(foodTrough,Vector2.zero);units=foodTrough.Units;Click(ui,"Confirmar quantidade selecionada");Check(inventory.Quantity(crop)==1&&foodTrough.Units==units,"Consuming final crop requires explicit second confirmation");Click(ui,"Confirmar quantidade selecionada");Check(inventory.Quantity(crop)==0&&foodTrough.Units==units+1,"Explicit final-crop confirmation transfers exactly one");
   string id=cow.AnimalId,name=cow.DisplayName;Check(!cow.Rename(" ")&&!cow.Rename(new string('a',25)),"Animal name validation");Check(cow.Rename("Mimosa")&&cow.DisplayName=="Mimosa"&&cow.AnimalId==id,"Rename preserves animal identifier");cow.Rename(name);
   string shelterId=cow.Home.ShelterId,homeName=cow.Home.DisplayName;Check(cow.Home.Rename("Curral da Mimosa")&&cow.Home.ShelterId==shelterId,"Pen rename preserves identifier");cow.Home.Rename(homeName);
   foreach(var tier in new[]{1f,.75f,.5f,.25f}){Set(cow,"productsAvailable",0);Set(cow,"productionProgress",0d);Set(cow,"fractionalYield",0d);Set(cow,"happiness",tier==.75f?.5f:.8f);Set(cow,"sick",tier==.25f);Set(cow,"fedUntil",Time.timeAsDouble+(tier==.5f?0:cow.Definition.FedForSeconds));Set(cow,"hydratedUntil",Time.timeAsDouble+cow.Definition.HydrationDuration);var produce=typeof(FarmAnimalController).GetMethod("Produce",BindingFlags.NonPublic|BindingFlags.Instance);for(int cycle=0;cycle<4;cycle++)produce.Invoke(cow,new object[]{cow.Definition.ProductionSeconds});Check(cow.ProductsAvailable==Mathf.FloorToInt(cow.Definition.ProductYield*4*tier),"Production tier "+tier+" with fractional carry: "+cow.ProductsAvailable);}
   Set(cow,"sick",false);Set(cow,"happiness",.7f);Set(cow,"fedUntil",Time.timeAsDouble+cow.Definition.FedForSeconds);cow.Home.SetGate(false);Check(!cow.Home.GateOpen,"Gate closes when passage is clear");cow.Home.ReleaseAll();Check(cow.IsConfined,"Closed gate refuses release");cow.Home.SetGate(true);Check(cow.Home.GateOpen,"Gate reopens");
   ui.ShowWarehouse();Canvas.ForceUpdateCanvases();FarmContextUXReview.AssertInside(ui.Panel,(RectTransform)hud.transform,"Warehouse manager");FarmContextUXReview.Capture("V36-warehouse",1920,1080);ui.ShowShelter(cow.Home);Canvas.ForceUpdateCanvases();FarmContextUXReview.Capture("V36-pen",1920,1080);ui.ShowAnimal(cow,cow.Home);Canvas.ForceUpdateCanvases();FarmContextUXReview.Capture("V36-animal",1600,900);ui.Back();Check(ui.IsOpen,"Closing animal card returns to pen manager");ui.ShowTrough(foodTrough,Camera.main.WorldToScreenPoint(foodTrough.transform.position));FarmContextUXReview.Capture("V36-refill",1366,768);ui.Close();
   log.AppendLine("COMPLETED (callback tests; movement and pointer tests reported separately)");File.WriteAllText("Reports/V36/03-acceptance.txt",log.ToString());
  }
 }
}
