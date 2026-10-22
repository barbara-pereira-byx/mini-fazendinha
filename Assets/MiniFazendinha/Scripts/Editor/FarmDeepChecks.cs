using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using MiniFazendinha.Animals;
using MiniFazendinha.Planting;
public static class FarmDeepChecks
{
    static StringBuilder log=new StringBuilder("Additional Play Mode checks; explicit hunger and production fixtures\n");
    static void Check(bool ok,string name){log.AppendLine((ok?"PASS: ":"FAIL: ")+name);File.WriteAllText("Reports/FarmDeep-tests.txt",log.ToString());if(!ok)throw new Exception(name);}
    [MenuItem("MiniFazendinha/Polish/Test consumption animation and herd")] public static async void Start(){try{await Main();}catch(Exception e){Debug.LogException(e);}}
public static async Task<string> Main()
    {
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var animals=FarmAnimalRegistry.All.ToArray();var cow=animals.First(a=>a.Definition.Identifier=="cow");var stock=UnityEngine.Object.FindAnyObjectByType<FarmInventory>();
        var anim=(Animator)typeof(FarmAnimalController).GetField("animator",flags).GetValue(cow);
        Check(anim.transform.name=="Visual"&&!anim.applyRootMotion,"Animator targets visual skeleton; code controls root position");
        var bones=anim.GetComponentsInChildren<Transform>();var rotations=bones.Select(b=>b.localRotation).ToArray();float t=anim.GetCurrentAnimatorStateInfo(0).normalizedTime;await Task.Delay(400);
        Check(anim.GetCurrentAnimatorStateInfo(0).normalizedTime!=t,"Animation state time advances");Check(bones.Where((b,i)=>Quaternion.Angle(rotations[i],b.localRotation)>.01f).Any(),"Actual bone pose changes between frames");
        foreach(var pen in animals.Select(a=>a.Home).Distinct()){stock.TryAdd(pen.Food.AcceptedItem,6);stock.TryAdd(pen.Water.AcceptedItem,6);pen.Food.RefillFromInventory(stock,6);pen.Water.RefillFromInventory(stock,6);}
        typeof(FarmAnimalController).GetField("fedUntil",flags).SetValue(cow,0d);typeof(FarmAnimalController).GetField("hydratedUntil",flags).SetValue(cow,0d);int food=cow.Home.Food.Units,water=cow.Home.Water.Units;
        cow.ReturnToShelter(cow.Home);double deadline=Time.timeAsDouble+20;while(!cow.IsConfined&&Time.timeAsDouble<deadline)await Task.Delay(100);
        deadline=Time.timeAsDouble+60;while(cow.FeedingLevel<=0&&Time.timeAsDouble<deadline)await Task.Delay(100);
        Check(cow.Home.Food.Units<food&&cow.FeedingLevel>0,"Animal walks to feeder, consumes stock and gains feeding level");
        deadline=Time.timeAsDouble+60;while(cow.HydrationLevel<=0&&Time.timeAsDouble<deadline)await Task.Delay(100);
        Check(cow.Home.Water.Units<water&&cow.HydrationLevel>0,"Animal walks to trough, drinks and gains hydration");
        typeof(FarmAnimalController).GetField("productionProgress",flags).SetValue(cow,(double)cow.Definition.ProductionSeconds-.1d);typeof(FarmAnimalController).GetMethod("Produce",flags).Invoke(cow,new object[]{.2f});int available=cow.ProductsAvailable;int before=stock.Quantity(cow.Definition.Product);
        Check(available>0&&cow.CollectProducts()==available&&stock.Quantity(cow.Definition.Product)==before+available,"Fed and hydrated production credits real inventory (near-deadline fixture)");Check(cow.CollectProducts()==0,"Repeated collection cannot duplicate product");
        typeof(FarmAnimalController).GetField("sick",flags).SetValue(cow,true);Check(cow.Health==AnimalHealthState.Sick,"Basic illness state is exposed");typeof(FarmAnimalController).GetField("sick",flags).SetValue(cow,false);
        foreach(var pen in animals.Select(a=>a.Home).Distinct())pen.ReleaseAll();deadline=Time.timeAsDouble+90;while(animals.Any(a=>a.IsConfined)&&Time.timeAsDouble<deadline)await Task.Delay(100);Check(animals.All(a=>!a.IsConfined),"All ten animals can leave their own gates together");
        foreach(var pen in animals.Select(a=>a.Home).Distinct())pen.RecollectAll();deadline=Time.timeAsDouble+120;while(animals.Any(a=>!a.IsConfined)&&Time.timeAsDouble<deadline)await Task.Delay(100);Check(animals.All(a=>a.IsConfined&&a.IsInsideShelter),"All ten animals return to their own species pens");
        log.AppendLine("COMPLETED");File.WriteAllText("Reports/FarmDeep-tests.txt",log.ToString());return log.ToString();
    }
}



