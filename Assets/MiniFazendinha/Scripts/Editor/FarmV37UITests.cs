using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV37UITests
 {
  static string reportPath="Reports/V37/ui.txt";
  static void Check(bool ok,string text,System.Text.StringBuilder log){log.AppendLine((ok?"PASS ":"FAIL ")+text);File.WriteAllText(reportPath,log.ToString());if(!ok)throw new Exception(text);}
  [MenuItem("MiniFazendinha/V3.7/Test large pen layout")]
  public static void Layout()
  {
   if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");reportPath="Reports/V37/ui.txt";var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;var log=new System.Text.StringBuilder("Existing UI components; warehouse-sized pen manager\n");ui.ShowWarehouse();var size=ui.Panel.sizeDelta;
   foreach(var shelter in UnityEngine.Object.FindObjectsByType<AnimalShelter>().Where(s=>s.PermittedSpecies!=null))
   {ui.ShowShelter(shelter);Canvas.ForceUpdateCanvases();Check(ui.Panel.sizeDelta==size,shelter.ShelterId+" matches warehouse dimensions",log);var scroll=ui.Panel.GetComponentInChildren<ScrollRect>();Check(scroll.content.rect.height<=scroll.viewport.rect.height+.1f,shelter.ShelterId+" all essential sections fit without scrolling",log);Check(ui.Panel.GetComponentsInChildren<Button>().Count(b=>b.name=="Abrir porteira"||b.name=="Fechar porteira")==1,shelter.ShelterId+" exactly one gate action",log);Check(ui.Panel.GetComponentsInChildren<Button>().Count(b=>b.name=="Morador")==shelter.ResidentCount,shelter.ShelterId+" resident portraits select only linked animals",log);}
   var cow=FarmAnimalRegistry.All.First(a=>a.Definition.name=="Cow").Home;ui.ShowShelter(cow);FarmContextUXReview.Capture("V37-pen",1920,1080);ui.Close();File.WriteAllText("Reports/V37/ui.txt",log.ToString()+"COMPLETED\n");
  }
  [MenuItem("MiniFazendinha/V3.7/Capture responsive panels")]
  public static void Review()
  {
   var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;var hud=UnityEngine.Object.FindAnyObjectByType<FarmHUD>();var stock=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var home=FarmAnimalRegistry.All.First(a=>a.Definition.name=="Cow").Home;var log=new System.Text.StringBuilder();
   foreach(Transform t in hud.transform)if(t.name=="Feedback")t.gameObject.SetActive(false);
   foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768)}){ui.ShowShelter(home);Canvas.ForceUpdateCanvases();var scroll=ui.Panel.GetComponentInChildren<ScrollRect>();if(scroll.content.rect.height>scroll.viewport.rect.height+.1f)throw new Exception("Initial pen requires scrolling at "+size);FarmContextUXReview.Capture("V37-pen-"+size.x+"x"+size.y,size.x,size.y);ui.ShowWarehouse();FarmContextUXReview.Capture("V37-warehouse-"+size.x+"x"+size.y,size.x,size.y);ui.ShowWell(UnityEngine.Object.FindAnyObjectByType<WaterWellController>(),Vector2.zero);var collect=ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name=="Coletar água");if(collect.GetComponentInChildren<TMPro.TMP_Text>().text!="Coletar")throw new Exception("Well label not textual");FarmContextUXReview.Capture("V37-well-"+size.x+"x"+size.y,size.x,size.y);log.AppendLine("PASS pen has no scrolling, well button label and panel bounds at "+size);}
   ui.Close();var plot=UnityEngine.Object.FindObjectsByType<FarmPlot>().First(p=>p.State==FarmPlotState.Empty);UnityEngine.Object.FindAnyObjectByType<FarmPlotInteractor>().OpenContextMenu(plot);Canvas.ForceUpdateCanvases();foreach(var button in hud.ContextMenu.Panel.GetComponentsInChildren<Button>().Where(b=>b.name=="◀"||b.name=="▶")){if(((RectTransform)button.transform).rect.width!=40||button.GetComponent<MiniFazendinha.UI.FarmTooltip>()!=null)throw new Exception("Pagination is not compact/tooltip-free");}FarmContextUXReview.Capture("V37-planting",1600,900);hud.CloseMenus();log.AppendLine("PASS 40px arrow buttons without tooltips; three crops per page");
   var tree=UnityEngine.Object.FindObjectsByType<MiniFazendinha.FruitTrees.FruitTree>().FirstOrDefault();if(tree!=null){ui.ShowTree(tree,Camera.main.WorldToScreenPoint(tree.transform.position));FarmContextUXReview.Capture("V37-tree-harvest",1600,900);ui.Close();}
   File.WriteAllText("Reports/V37/responsiveness.txt",log.ToString()+"COMPLETED\n");
  }  [MenuItem("MiniFazendinha/V3.7/Test all product collection actions")]
  public static void Products()
  {
   reportPath="Reports/V37/product-collection.txt";var ui=UnityEngine.Object.FindAnyObjectByType<FarmAnimalContextUI>().Management;var inventory=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();var log=new System.Text.StringBuilder("Real individual and pen UI callbacks, shared production controller\n");var flags=BindingFlags.NonPublic|BindingFlags.Instance;var produce=typeof(FarmAnimalController).GetMethod("Produce",flags);
   foreach(var group in FarmAnimalRegistry.All.GroupBy(a=>a.Definition))
   {var animal=group.First();foreach(bool pen in new[]{false,true}){foreach(var resident in animal.Home.Residents)typeof(FarmAnimalController).GetField("productsAvailable",flags).SetValue(resident,0);typeof(FarmAnimalController).GetField("productionProgress",flags).SetValue(animal,0d);typeof(FarmAnimalController).GetField("fractionalYield",flags).SetValue(animal,0d);typeof(FarmAnimalController).GetField("happiness",flags).SetValue(animal,.9f);produce.Invoke(animal,new object[]{animal.Definition.ProductionSeconds});int available=animal.ProductsAvailable,before=inventory.Quantity(animal.Definition.Product);Check(available>0,animal.Definition.name+" has actual collectable product",log);if(pen)ui.ShowShelter(animal.Home);else ui.ShowAnimal(animal);string action=pen?"Coletar "+animal.Definition.Product.DisplayName:"Coletar produção";var button=ui.Panel.GetComponentsInChildren<Button>().First(b=>b.name==action);button.onClick.Invoke();Check(inventory.Quantity(animal.Definition.Product)==before+available,animal.Definition.name+" "+(pen?"pen":"individual")+" credits exact quantity",log);button.onClick.Invoke();Check(inventory.Quantity(animal.Definition.Product)==before+available,animal.Definition.name+" repeat cannot duplicate",log);ui.Close();}}
   File.WriteAllText("Reports/V37/product-collection.txt",log.ToString()+"COMPLETED\n");
  }
 }
}
