using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
namespace MiniFazendinha.EditorTools
{
    [InitializeOnLoad]
    public static class FarmInputCleanupTests
    {
        const string Phase="MiniFazendinha.InputCleanupPhase";
        static FarmTestInputScope scope;
        static FarmInputCleanupTests(){EditorApplication.playModeStateChanged+=OnPlayState;EditorApplication.delayCall+=AfterReload;}
        static void Check(bool ok,string label){Directory.CreateDirectory("Reports/V36");File.AppendAllText("Reports/V36/input-cleanup-tests.txt",(ok?"PASS ":"FAIL ")+label+"\n");if(!ok)throw new Exception(label);}
        static void AssertRestored(string label)
        {Check(InputSystem.devices.OfType<Mouse>().Any(m=>m.native&&m.enabled),label+": physical mouse enabled");Check(Mouse.current!=null&&Mouse.current.native&&Mouse.current.enabled,label+": physical mouse current");Check(!InputSystem.devices.Any(d=>!d.native&&d.name=="V36Tests"),label+": no orphan test mouse");}
        static void Isolate()
        {scope=new FarmTestInputScope();foreach(var m in InputSystem.devices.OfType<Mouse>().Where(m=>m.native).ToArray())InputSystem.DisableDevice(m);InputSystem.AddDevice<Mouse>("V36Tests").MakeCurrent();}
        [MenuItem("MiniFazendinha/V3.6/Test input cleanup normally")]
        public static void Normal()
        {FarmTestInputScope.RestorePhysicalInput();var settings=InputSystem.settings;File.WriteAllText("Reports/V36/input-cleanup-tests.txt","Input cleanup lifecycle regression\n");Isolate();scope.Dispose();scope.Dispose();scope=null;AssertRestored("Normal/idempotent dispose");Check(InputSystem.settings==settings,"Original input settings restored");}
        [MenuItem("MiniFazendinha/V3.6/Test input cleanup on Play stop")]
        public static void Interrupt()
        {if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");Isolate();SessionState.SetInt(Phase,1);EditorApplication.ExitPlaymode();}
        static void OnPlayState(PlayModeStateChange state)
        {if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetInt(Phase,0)==1)EditorApplication.delayCall+=()=>{AssertRestored("Interrupted Play Mode");SessionState.SetInt(Phase,0);};}
        [MenuItem("MiniFazendinha/V3.6/Test input cleanup on script reload")]
        public static void Reload()
        {Isolate();SessionState.SetInt(Phase,2);EditorUtility.RequestScriptReload();}
        static void AfterReload()
        {if(SessionState.GetInt(Phase,0)!=2)return;EditorApplication.delayCall+=()=>{AssertRestored("Assembly reload");SessionState.SetInt(Phase,0);File.AppendAllText("Reports/V36/input-cleanup-tests.txt","COMPLETED\n");};}
        [MenuItem("MiniFazendinha/V3.6/Report physical input state")]
        public static void Report()
        {var text="current="+Mouse.current+"\nrouting="+InputSystem.settings.editorInputBehaviorInPlayMode+"\n";foreach(var d in InputSystem.devices)text+=d.name+" enabled="+d.enabled+" native="+d.native+"\n";File.WriteAllText("Reports/V36/input-after-repair.txt",text);}
    }
}
