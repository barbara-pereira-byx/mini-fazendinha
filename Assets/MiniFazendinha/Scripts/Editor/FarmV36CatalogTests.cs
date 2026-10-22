using UnityEngine; using UnityEditor; namespace MiniFazendinha.EditorTools { public static class FarmV36CatalogTests { [MenuItem("MiniFazendinha/V3.6/Test catalog sizes")] public static void Run(){ if(!EditorApplication.isPlaying)throw new System.Exception("Play Mode required");var input=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.Planting.FarmPlotInteractor>();
var hud=UnityEngine.Object.FindAnyObjectByType<MiniFazendinha.Planting.FarmHUD>();
var field=typeof(MiniFazendinha.Planting.FarmPlotInteractor).GetField("crops",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
var original=input.Crops;
var empty=System.Linq.Enumerable.First(UnityEngine.Object.FindObjectsByType<MiniFazendinha.Planting.FarmPlot>(),p=>p.State==MiniFazendinha.Planting.FarmPlotState.Empty);
var report=new System.Text.StringBuilder();
try{
foreach(int count in new[]{1,2,3,4,9,10}){
var catalog=new MiniFazendinha.Planting.CropDefinition[count];for(int i=0;i<count;i++)catalog[i]=original[i%original.Length];field.SetValue(input,catalog);input.OpenContextMenu(empty);hud.ContextMenu.Refresh();int pages=(count+2)/3;if(hud.ContextMenu.PageCount!=pages)throw new System.Exception("Page count mismatch");
hud.ContextMenu.ChangePage(-999);if(hud.ContextMenu.Page!=0)throw new System.Exception("Negative page");
hud.ContextMenu.ChangePage(999);if(hud.ContextMenu.Page!=pages-1)throw new System.Exception("Page overflow");
int active=System.Linq.Enumerable.Count(hud.ContextMenu.SeedButtons,b=>b.button.gameObject.activeSelf);if(active!=count-(pages-1)*3)throw new System.Exception("Last page has wrong number of crops");
report.AppendLine("PASS "+count+" crops => "+pages+" pages, bounds clamped, "+active+" cards on last page");
}
}finally{field.SetValue(input,original);hud.CloseMenus();hud.ContextMenu.Refresh();}
System.IO.File.WriteAllText("Reports/V36/09-pagination-catalogs.txt",report.ToString()+"COMPLETED\n");

} } }