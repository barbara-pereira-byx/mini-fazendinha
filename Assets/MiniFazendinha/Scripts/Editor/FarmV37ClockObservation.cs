using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using MiniFazendinha.Animals;
namespace MiniFazendinha.EditorTools
{
 public static class FarmV37ClockObservation
 {
  sealed class Entry {public AnimalDefinition definition;public FarmAnimalController animal;public double first=-1;}
  static readonly List<Entry> entries=new List<Entry>();static readonly List<GameObject> roots=new List<GameObject>();static double started,next;static System.Text.StringBuilder report;
  static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
  [MenuItem("MiniFazendinha/V3.7/Observe 185 seconds of actual production")]
  public static void Start()
  {
   if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");if(entries.Count>0)throw new Exception("Observation already running");report=new System.Text.StringBuilder("Real unaccelerated runtime Update; no direct Produce calls for these isolated fixtures\n");started=Time.realtimeSinceStartupAsDouble;next=0;
   var definitions=AssetDatabase.FindAssets("t:AnimalDefinition",new[]{"Assets/MiniFazendinha/Settings/V2/Animals"}).Select(g=>AssetDatabase.LoadAssetAtPath<AnimalDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
   for(int i=0;i<definitions.Length;i++)
   {
    var definition=UnityEngine.Object.Instantiate(definitions[i]);definition.name="V37Clock_"+definitions[i].name;var root=new GameObject(definition.name);root.transform.position=new Vector3(-1000+i*8,0,-1000);roots.Add(root);var area=root.AddComponent<BoxCollider>();area.isTrigger=true;area.size=new Vector3(4,.1f,4);var home=root.AddComponent<AnimalShelter>();Set(home,"shelterId","clock-"+i);Set(home,"permittedSpecies",definition);Set(home,"roamingArea",area);Set(home,"farmArea",new Bounds(root.transform.position,new Vector3(4,.1f,4)));
    var g=new GameObject("Clock animal");g.SetActive(false);g.transform.SetParent(root.transform,false);g.transform.localPosition=Vector3.up*.075f;var animal=g.AddComponent<FarmAnimalController>();animal.Assign(definition,"clock-fixture-"+i,"Clock",home,null,null,null,null,null);g.SetActive(true);entries.Add(new Entry{definition=definition,animal=animal});
   }
   EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=State;AssemblyReloadEvents.beforeAssemblyReload+=Abort;
  }
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.ExitingPlayMode)Abort();}
  static void Abort(){if(entries.Count==0)return;report.AppendLine("ABORTED before observation completed");Finish();}
  static void Tick()
  {
   double now=Time.realtimeSinceStartupAsDouble;if(now<next)return;next=now+.1;double elapsed=now-started;
   if(!Mathf.Approximately(Time.timeScale,1)){report.AppendLine("FAIL Time.timeScale differs from 1");Finish();return;}
   foreach(var e in entries)if(e.first<0&&e.animal.ProductsAvailable>0){e.first=elapsed;report.AppendLine((elapsed+.02>=e.definition.ProductionSeconds?"PASS ":"FAIL ")+e.definition.name+" first product at "+elapsed.ToString("F3")+" s, configured "+e.definition.ProductionSeconds+" s");}
   if(elapsed>=185){foreach(var e in entries)report.AppendLine((e.first>=60?"PASS ":"FAIL ")+e.definition.name+" available="+e.animal.ProductsAvailable+" after "+elapsed.ToString("F2")+" s");report.AppendLine("COMPLETED");Finish();}
   else File.WriteAllText("Reports/V37/production-real-time.txt",report.ToString()+"Observing: "+elapsed.ToString("F1")+" / 185 seconds\n");
  }
  static void Finish(){EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=State;AssemblyReloadEvents.beforeAssemblyReload-=Abort;File.WriteAllText("Reports/V37/production-real-time.txt",report.ToString());foreach(var root in roots)UnityEngine.Object.DestroyImmediate(root);foreach(var e in entries)UnityEngine.Object.DestroyImmediate(e.definition);roots.Clear();entries.Clear();}
 }
}
