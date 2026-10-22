using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

namespace MiniFazendinha.EditorTools
{
    // Owns the entire input isolation lifetime, including interrupted tests and reloads.
    [InitializeOnLoad]
    public sealed class FarmTestInputScope : IDisposable
    {
        const string SessionKey="MiniFazendinha.InputTestActive";
        static FarmTestInputScope active;
        InputSettings original,temporary;
        HideFlags originalFlags;
        readonly InputDevice[] originalDevices;
        readonly bool[] enabledStates;
        readonly Mouse previousMouse;
        readonly Keyboard previousKeyboard;
        bool disposed;

        static FarmTestInputScope()
        {
            EditorApplication.delayCall+=RecoverInterruptedTest;
            EditorApplication.playModeStateChanged+=StateChanged;
            AssemblyReloadEvents.beforeAssemblyReload+=BeforeReload;
            EditorApplication.quitting+=BeforeReload;
        }
        static void BeforeReload(){active?.Dispose();}
        static void StateChanged(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode&&state!=PlayModeStateChange.EnteredEditMode)return;
            if(active!=null)active.Dispose();else RecoverInterruptedTest();
        }
        static bool OwnedTestDevice(InputDevice d)
        {
            if(d.native)return false;
            return d.name=="V36Tests"||d.name=="PolishPointer"||d.name=="ContextUX_TestMouse"||d.name=="ContextUX_TestKeyboard"||d.name=="MiniFazendinha_TestMouse"||d.name=="FarmPlanting_TestMouse";
        }
        static void RecoverInterruptedTest()
        {
            if(active!=null)return;
            bool stale=SessionState.GetBool(SessionKey,false)||InputSystem.devices.Any(OwnedTestDevice);
            if(stale)RestorePhysicalInput();
        }
        [MenuItem("MiniFazendinha/V3.6/Restore physical input")]
        public static void RestorePhysicalInput()
        {
            active?.Dispose();
            foreach(var device in InputSystem.devices.ToArray())if(OwnedTestDevice(device))InputSystem.RemoveDevice(device);
            foreach(var device in InputSystem.devices)if(device.native&&(device is Mouse||device is Keyboard))InputSystem.EnableDevice(device);
            InputSystem.devices.OfType<Mouse>().FirstOrDefault(d=>d.native)?.MakeCurrent();
            InputSystem.devices.OfType<Keyboard>().FirstOrDefault(d=>d.native)?.MakeCurrent();
            SessionState.SetBool(SessionKey,false);
        }
        public FarmTestInputScope()
        {
            if(active!=null)throw new InvalidOperationException("Another farm input test is active.");
            RecoverInterruptedTest();
            originalDevices=InputSystem.devices.ToArray();enabledStates=originalDevices.Select(d=>d.enabled).ToArray();previousMouse=Mouse.current;previousKeyboard=Keyboard.current;
            original=InputSystem.settings;originalFlags=original.hideFlags;
            if(originalFlags==HideFlags.HideAndDontSave)original.hideFlags=HideFlags.DontSave;
            temporary=UnityEngine.Object.Instantiate(original);temporary.hideFlags=HideFlags.DontSave;
            temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings=temporary;active=this;SessionState.SetBool(SessionKey,true);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            try
            {
                // Test runners may already have removed these devices; cleanup is idempotent.
                foreach(var device in InputSystem.devices.ToArray())
                    if(!originalDevices.Contains(device)&&OwnedTestDevice(device))InputSystem.RemoveDevice(device);
                for(int i=0;i<originalDevices.Length;i++)
                {
                    var device=originalDevices[i];if(!device.added)continue;
                    if(enabledStates[i])InputSystem.EnableDevice(device);else InputSystem.DisableDevice(device);
                }
                if(previousMouse!=null&&previousMouse.added&&previousMouse.enabled)previousMouse.MakeCurrent();
                if(previousKeyboard!=null&&previousKeyboard.added&&previousKeyboard.enabled)previousKeyboard.MakeCurrent();
            }
            finally
            {
                if(original==null)original=ScriptableObject.CreateInstance<InputSettings>();
                InputSystem.settings=original;original.hideFlags=originalFlags;
                if(temporary!=null)UnityEngine.Object.DestroyImmediate(temporary);
                temporary=null;if(active==this)active=null;SessionState.SetBool(SessionKey,false);
            }
        }
    }
}
